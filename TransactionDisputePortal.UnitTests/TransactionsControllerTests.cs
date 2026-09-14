using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TransactionDisputePortal.API.Controllers;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;
using Xunit;

namespace TransactionDisputePortal.UnitTests;

public class TransactionsControllerTests
{
    private static TransactionsController CreateController(
        Mock<ITransactionRepository> transactionRepository,
        Mock<IAuditRepository> auditRepository)
    {
        return new TransactionsController(
            transactionRepository.Object,
            auditRepository.Object,
            NullLogger<TransactionsController>.Instance);
    }

    [Fact]
    public async Task GetTransactions_WhenAccountNumberIsMissing_ReturnsBadRequest()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        var controller = new TransactionsController(
            transactionRepository.Object,
            auditRepository.Object,
            NullLogger<TransactionsController>.Instance);

        var result = await controller.GetTransactions(new GetTransactionsRequestDto
        {
            AccountNumber = "",
            StartDate = DateTimeOffset.UtcNow.AddDays(-1),
            EndDate = DateTimeOffset.UtcNow
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Account number is required.", badRequest.Value);
        transactionRepository.Verify(
            repository => repository.GetTransactionsByAccountAndDateRangeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetTransactions_WhenStartDateIsAfterEndDate_ReturnsBadRequest()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        var controller = new TransactionsController(
            transactionRepository.Object,
            auditRepository.Object,
            NullLogger<TransactionsController>.Instance);

        var result = await controller.GetTransactions(new GetTransactionsRequestDto
        {
            AccountNumber = "ACC-123",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(-1)
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Start date must be earlier than end date.", badRequest.Value);
    }

    [Fact]
    public async Task GetTransactions_WhenRequestIsValid_ReturnsTransactions()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        var expectedTransactions = new[]
        {
            new TransactionResponseDto(
                10,
                20,
                "ACC-123",
                DateTimeOffset.UtcNow.AddDays(-1),
                "REF-10",
                "Example Store",
                "Card Purchase",
                42.50m,
                "Completed",
                false)
        };

        transactionRepository
            .Setup(repository => repository.GetTransactionsByAccountAndDateRangeAsync(
                "ACC-123",
                It.IsAny<DateTimeOffset>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTransactions);

        var result = await CreateController(transactionRepository, auditRepository)
            .GetTransactions(new GetTransactionsRequestDto
            {
                AccountNumber = "ACC-123",
                StartDate = DateTimeOffset.UtcNow.AddDays(-7),
                EndDate = DateTimeOffset.UtcNow
            }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expectedTransactions, ok.Value);
    }

    [Fact]
    public async Task GetTransactionById_WhenTransactionDoesNotExist_ReturnsNotFound()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        transactionRepository
            .Setup(repository => repository.GetTransactionByID(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TransactionResponseDto?)null);

        var result = await CreateController(transactionRepository, auditRepository)
            .GetTransactionById(999, CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Transaction with ID 999 was not found.", notFound.Value);
    }

    [Fact]
    public async Task CreateDispute_WhenRequestIsValid_ReturnsNewDisputeIdAndWritesAuditLog()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        transactionRepository
            .Setup(repository => repository.CreateDispute(It.IsAny<CreateDisputeRequestDTO>()))
            .ReturnsAsync(55);

        var request = new CreateDisputeRequestDTO
        {
            TransactionID = 10,
            CustomerID = 20,
            DisputeStatusID = 1,
            ReasonCategory = "Unauthorized transaction",
            CustomerNotes = "I do not recognize this transaction.",
            DisputedAmount = 42.50m
        };

        var result = await CreateController(transactionRepository, auditRepository)
            .CreateDispute(request);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(55, ok.Value);
        auditRepository.Verify(repository => repository.InsertAuditLog(
            It.Is<InsertAuditLogRequestDto>(audit =>
                audit.DisputeID == 55 &&
                audit.ChangedByCustomerID == 20 &&
                audit.Notes == request.CustomerNotes)),
            Times.Once);
    }

    [Fact]
    public async Task UpdateDisputeStatus_WhenDisputeDoesNotExist_ReturnsNotFound()
    {
        var transactionRepository = new Mock<ITransactionRepository>();
        var auditRepository = new Mock<IAuditRepository>();
        transactionRepository
            .Setup(repository => repository.UpdateDisputeStatus(It.IsAny<UpdateDisputeStatusRequestDto>()))
            .ReturnsAsync(false);

        var result = await CreateController(transactionRepository, auditRepository)
            .UpdateDisputeStatus(new UpdateDisputeStatusRequestDto(55, 2, 1, "Rejected"));

        var notFound = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal("Dispute with ID 55 not found.", notFound.Value);
    }
}

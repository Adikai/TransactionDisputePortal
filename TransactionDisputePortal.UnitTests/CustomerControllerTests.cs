using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using TransactionDisputePortal.API.Controllers;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;
using Xunit;

namespace TransactionDisputePortal.UnitTests;

public class CustomerControllerTests
{
    private readonly Mock<IAccountRepository> _accountRepository = new();
    private readonly Mock<IAuditRepository> _auditRepository = new();

    private CustomerController CreateController()
    {
        return new CustomerController(
            _auditRepository.Object,
            NullLogger<TransactionsController>.Instance,
            _accountRepository.Object);
    }

    [Fact]
    public async Task Login_WhenRequestIsMissing_ReturnsBadRequest()
    {
        var result = await CreateController().Login(null!);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email and Password are required.", badRequest.Value);
        _accountRepository.Verify(repository => repository.loginCustomer(It.IsAny<LoginRequestDto>()), Times.Never);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreInvalid_ReturnsUnauthorized()
    {
        _accountRepository
            .Setup(repository => repository.loginCustomer(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync((LoginResponseDto)null!);

        var result = await CreateController().Login(new LoginRequestDto
        {
            Email = "admin",
            PasswordHash = "wrong-password"
        });

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Invalid email or password.", unauthorized.Value);
    }

    [Fact]
    public async Task Login_WhenCredentialsAreValid_ReturnsUserAndToken()
    {
        _accountRepository
            .Setup(repository => repository.loginCustomer(It.IsAny<LoginRequestDto>()))
            .ReturnsAsync(new LoginResponseDto
            {
                UserID = 1,
                FirstName = "Admin",
                Email = "admin",
                UserRole = "Admin"
            });

        var result = await CreateController().Login(new LoginRequestDto
        {
            Email = "admin",
            PasswordHash = "admin"
        });

        var ok = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<LoginResponseDto>(ok.Value);
        Assert.Equal(1, response.UserID);
        Assert.Equal("Admin", response.UserRole);
        Assert.Equal("dummy-Token-xyz", response.Token);
    }
}

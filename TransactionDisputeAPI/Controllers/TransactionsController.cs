namespace TransactionDisputePortal.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;

    public TransactionsController(ITransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
    }

    /// <summary>
    /// Retrieves historical transactions for an account within a given date range.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(IEnumerable<TransactionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TransactionResponseDto>>> GetTransactions(
        [FromBody] GetTransactionsRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            return BadRequest("Account number is required.");
        }

        if (request.StartDate >= request.EndDate)
        {
            return BadRequest("Start date must be earlier than end date.");
        }

        var transactions = await _transactionRepository.GetTransactionsByAccountAndDateRangeAsync(
            request.AccountNumber,
            request.StartDate,
            request.EndDate,
            cancellationToken);

        return Ok(transactions);
    }
}
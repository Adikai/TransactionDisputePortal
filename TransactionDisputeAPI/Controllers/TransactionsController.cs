namespace TransactionDisputePortal.API.Controllers;

using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;
using TransactionDisputePortal.Shared.Models.Enums;

[ApiController]
[Route("api/[controller]")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly IAuditRepository _auditRepository;
    private ILogger<TransactionsController> _logger;

    public TransactionsController(ITransactionRepository transactionRepository, IAuditRepository auditRepository, ILogger<TransactionsController> logger)
    {
        _transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
        try
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occured when retrieving transactions");
            return BadRequest(ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Updates a Dispute Status and logs the change in the audit log.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> UpdateDisputeStatus([FromBody] UpdateDisputeStatusRequestDto request)
    {
        try
        {
            if (request == null || request.DisputeID <= 0 || request.NewStatusID <= 0)
            {
                return BadRequest("Invalid dispute ID or status ID.");
            }
            bool isUpdated = await _transactionRepository.UpdateDisputeStatus(request.DisputeID, request.NewStatusID);
            if (!isUpdated)
            {
                return NotFound($"Dispute with ID {request.DisputeID} not found.");
            }

            InsertAuditLogRequestDto auditLogRequest = new InsertAuditLogRequestDto
            {
                DisputeID = request.DisputeID,
                PreviousStatusID = request.PreviousStatusID,
                NewStatusID = request.NewStatusID,
                ChangedByStaffID = request.ChangedByStaffID,
                ChangedByCustomerID = request.ChangedByCustomerID,
                Notes = request.Notes
            };
            await _auditRepository.InsertAuditLog(auditLogRequest);

            return Ok(request.DisputeID);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating dispute status for DisputeID: {DisputeID}", request?.DisputeID);
            return BadRequest(ex.Message);
            throw;
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<int>> CreateDispute([FromBody] CreateDisputeRequestDTO request)
    {
        try
        {
            if (request == null || request.TransactionID <= 0 || request.CustomerID <= 0 || request.DisputeStatusID <= 0)
            {
                return BadRequest("Invalid dispute creation request.");
            }
            int newDisputeID = await _transactionRepository.CreateDispute(request);

            InsertAuditLogRequestDto auditLogRequest = new InsertAuditLogRequestDto
            {
                DisputeID = newDisputeID,
                PreviousStatusID = null,
                NewStatusID = (int)DisputeStatusEnum.Submitted,
                ChangedByStaffID = null,
                ChangedByCustomerID = request.CustomerID,
                Notes = request.CustomerNotes
            };
            await _auditRepository.InsertAuditLog(auditLogRequest);
            return Ok(newDisputeID);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a dispute for TransactionID: {TransactionID}", request?.TransactionID);
            return BadRequest(ex.Message);
            throw;
        }
    }
}
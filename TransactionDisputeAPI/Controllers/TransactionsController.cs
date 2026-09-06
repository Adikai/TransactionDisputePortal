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
    private readonly ILogger<TransactionsController> _logger;

    public TransactionsController(
        ITransactionRepository transactionRepository,
        IAuditRepository auditRepository,
        ILogger<TransactionsController> logger)
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
            _logger.LogError(ex, "An error occurred when retrieving transactions");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a single transaction by ID.
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(TransactionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TransactionResponseDto>> GetTransactionById(int id, CancellationToken cancellationToken)
    {
        try
        {
            var transaction = await _transactionRepository.GetTransactionByID(id, cancellationToken);
            if (transaction == null)
            {
                return NotFound($"Transaction with ID {id} was not found.");
            }

            return Ok(transaction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving transaction {TransactionID}", id);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Creates a new dispute for a transaction.
    /// </summary>
    [HttpPost("CreateDispute")]
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

            var auditLogRequest = new InsertAuditLogRequestDto
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
        }
    }

    /// <summary>
    /// Updates a Dispute Status and logs the change in the audit log.
    /// </summary>
    [HttpPost("UpdateDisputeStatus")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

            var auditLogRequest = new InsertAuditLogRequestDto
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
        }
    }

    /// <summary>
    /// Retrieves all disputes lodged by a specific customer.
    /// </summary>
    [HttpGet("disputes/customer/{customerId:int}")]
    [ProducesResponseType(typeof(IEnumerable<DisputeResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<DisputeResponseDto>>> GetDisputesByCustomer(int customerId, CancellationToken cancellationToken)
    {
        try
        {
            var disputes = await _transactionRepository.GetAllDisputesByCustomerID(customerId, cancellationToken);
            return Ok(disputes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving disputes for CustomerID {CustomerID}", customerId);
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves a single dispute details by Dispute ID.
    /// </summary>
    [HttpGet("disputes/{disputeId:int}")]
    [ProducesResponseType(typeof(GetDisputeByCustomerIDResponseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<GetDisputeByCustomerIDResponseDTO>> GetDisputeById(int disputeId, CancellationToken cancellationToken)
    {
        try
        {
            var dispute = await _transactionRepository.GetDisputeByID(disputeId, cancellationToken);
            if (dispute == null)
            {
                return NotFound($"Dispute with ID {disputeId} was not found.");
            }

            return Ok(dispute);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving dispute {DisputeID}", disputeId);
            return BadRequest(ex.Message);
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private IAccountRepository _accountRepository;
        private readonly IAuditRepository _auditRepository;
        private ILogger<TransactionsController> _logger;
        public AdminController(IAuditRepository auditRepository, ILogger<TransactionsController> logger, IAccountRepository accountRepository)
        {
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        }
        /// <summary>
        /// Gets All Disputes for Admin
        /// </summary>
        [HttpPost("GetDisputes")]
        [ProducesResponseType(typeof(AdminDisputesResponseDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetDisputesForAdmin([FromBody] AdminDisputesRequestDTO request)
        {
            try
            {
                var disputes = await _accountRepository.GetDisputesForAdminAsync(request.pageNumber, request.pageSize, request.disputeStatusID, request.searchTerm);
                return Ok(disputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching disputes for admin.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }
    }
}

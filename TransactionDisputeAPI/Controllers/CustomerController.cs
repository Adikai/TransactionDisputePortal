using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerController : ControllerBase
    {
        private IAccountRepository _accountRepository;
        private readonly IAuditRepository _auditRepository;
        private ILogger<TransactionsController> _logger;

        public CustomerController(IAuditRepository auditRepository, ILogger<TransactionsController> logger, IAccountRepository accountRepository)
        {
            _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            var customer = await _accountRepository.loginCustomer(loginRequest);

            if (customer == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            var response = new LoginResponseDto(
                customer.CustomerID,
                "dummy-token-xyz123", //Using dummy token for demonstration purposes. In a real application,I would generate a JWT token.
                customer.FirstName,
                customer.LastName,
                customer.Email
            );

            return Ok(response);
        }

        [HttpPost("UpdateBalance")]
        [ProducesResponseType(typeof(decimal), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult UpdateBalance([FromBody] UpdateBalanceRequestDTO updateBalanceRequest)
        {
            try
            {
                if (updateBalanceRequest.AccountID < 0  || updateBalanceRequest.Amount == 0)
                {
                    return BadRequest("Invalid account ID or amount.");
                }

                var newBalance = _accountRepository.UpdateAccountBalance(updateBalanceRequest).Result;
                return Ok(newBalance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating account balance.");
                return BadRequest(ex.Message);
                throw;
            }
        }

        [HttpGet("GetCustomerDashboard/{customerID}")]
        [ProducesResponseType(typeof(CustomerDashboardResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetCustomerDashboard(int customerID)
        {
            try
            {
                var dashboardData = await _accountRepository.GetCustomerDashboardAsync(customerID);
                return Ok(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching customer dashboard data.");
                return BadRequest(ex.Message);
                throw;
            }
        }
    }
}

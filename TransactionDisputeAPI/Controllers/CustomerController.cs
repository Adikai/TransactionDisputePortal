using Microsoft.AspNetCore.Mvc;
using TransactionDisputePortal.Core.Helpers;
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
        /// <summary>
        /// Logs in a Customer
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto loginRequest)
        {
            if (loginRequest == null || string.IsNullOrWhiteSpace(loginRequest.Email) || string.IsNullOrWhiteSpace(loginRequest.PasswordHash))
            {
                return BadRequest("Email and Password are required.");
            }
            string computedHash;

            //This is done for demo purposes.
            // Admin passwords should also be hashed and stored in the DB.
            if (!loginRequest.Email.Equals("admin"))
            {
                computedHash = PasswordHelper.HashPassword(loginRequest.PasswordHash);
            }
            else
            {
                computedHash = loginRequest.PasswordHash;
            }

            var requestWithHash = new LoginRequestDto
            {
                Email = loginRequest.Email,
                PasswordHash = computedHash
            };

            var customer = await _accountRepository.loginCustomer(requestWithHash);

            if (customer == null)
            {
                return Unauthorized("Invalid email or password.");
            }

            var response = new LoginResponseDto
                 {
                UserID = customer.UserID,
                FirstName = customer.DisplayName,
                LastName = customer.LastName,
                Email = customer.Email,
                UserRole = customer.UserRole,
                Token = "dummy-Token-xyz" // added dummy token for demonstration purposes, I would normally generate a JWT token here for authentication.
            };

            return Ok(response);
        }
        /// <summary>
        /// Updates a customer's balance
        /// </summary>
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
        /// <summary>
        /// Gets the dashboard data per customer
        /// </summary>
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

        /// <summary>
        /// Gets accounts per customerID
        /// </summary>
        [HttpGet("Accounts/{customerID}")]
        [ProducesResponseType(typeof(List<AccountSummaryDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Accounts(int customerID)
        {
            try
            {
                var accounts = await _accountRepository.GetAccountNumbersByCustomerID(customerID);
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching customer accounts");
                return BadRequest(ex.Message);
                throw;
            }
        }
    }
}

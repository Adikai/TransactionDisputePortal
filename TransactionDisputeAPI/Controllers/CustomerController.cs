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
        public IActionResult Login([FromBody] LoginRequestDto loginRequest)
        {
            //Keeping it simple here to save time. 
            //Normally I'd check the credentials against a database and generate a JWT token if valid.
            //Store that Token into both Redis or a secure cookie for future requests.
            if (loginRequest.Email == "john.doe@example.com" && loginRequest.Password == "AQAAAAIAAYagAAAAE...")
            {
                return Ok("dummy-token");
            }
            else
            {
                return Unauthorized();
            }
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

        [HttpGet("GetAccountDetails/{customerID}")]
        [ProducesResponseType(typeof(AccountDTOResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult GetAccountDetails(int customerID)
        {
            try
            {
                var accountDetails = _accountRepository.GetAccountDetails(customerID).Result;
                return Ok(accountDetails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching account details.");
                return BadRequest(ex.Message);
                throw;
            }
        }
    }
}

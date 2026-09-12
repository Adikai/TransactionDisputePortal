using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TransactionDisputePortal.Core.Helpers;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger<TransactionsController> _logger;

        public AdminController(
            IAuditRepository auditRepository,
            ILogger<TransactionsController> logger,
            IAccountRepository accountRepository)
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
                var disputes = await _accountRepository.GetDisputesForAdminAsync(
                    request.pageNumber,
                    request.pageSize,
                    request.disputeStatusID,
                    request.searchTerm);

                return Ok(disputes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching disputes for admin.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Creates a new customer
        /// </summary>
        [HttpPost("CreateCustomer")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto request)
        {
            if (request == null)
            {
                return BadRequest(new { Errors = new[] { "Request body cannot be null." } });
            }

            var errors = CustomerValidationHelper.ValidateCreateCustomer(request);
            if (errors.Count > 0)
            {
                return BadRequest(new { Errors = errors });
            }

            try
            {
                int newCustomerId = await _accountRepository.CreateCustomerAsync(request);
                return StatusCode(StatusCodes.Status201Created, new { CustomerID = newCustomerId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating customer.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Updates an existing customer
        /// </summary>
        [HttpPut("UpdateCustomer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCustomer([FromBody] UpdateCustomerDto request)
        {
            if (request == null)
            {
                return BadRequest(new { Errors = new[] { "Request body cannot be null." } });
            }

            var errors = CustomerValidationHelper.ValidateUpdateCustomer(request);
            if (errors.Count > 0)
            {
                return BadRequest(new { Errors = errors });
            }

            try
            {
                await _accountRepository.UpdateCustomerAsync(request);
                return Ok(new { Message = "Customer updated successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating customer ID {CustomerID}.", request.CustomerID);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Deletes a customer and cascading records
        /// </summary>
        [HttpDelete("DeleteCustomer/{customerId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteCustomer(int customerId)
        {
            if (customerId <= 0)
            {
                return BadRequest(new { Errors = new[] { "A valid Customer ID must be provided." } });
            }

            try
            {
                await _accountRepository.DeleteCustomerAsync(customerId);
                return Ok(new { Message = $"Customer ID {customerId} deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting customer ID {CustomerID}.", customerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets all customers
        /// </summary>
        [HttpGet("GetCustomers")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCustomers()
        {
            try
            {
                var customers = await _accountRepository.GetCustomersAsync();
                return Ok(customers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching customers.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing your request.");
            }
        }

        /// <summary>
        /// Gets all accounts for a specific customer
        /// </summary>
        [HttpGet("GetAccountsByCustomer/{customerId:int}")]
        [ProducesResponseType(typeof(IEnumerable<AccountDTOResponse>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAccountsByCustomer(int customerId)
        {
            try
            {
                var accounts = await _accountRepository.GetAccountNumbersByCustomerID(customerId);
                return Ok(accounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching accounts for Customer ID {CustomerID}", customerId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving customer accounts.");
            }
        }

        /// <summary>
        /// Creates a new bank account for a customer
        /// </summary>
        [HttpPost("CreateAccount")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.AccountNumber) || request.CustomerID <= 0)
            {
                return BadRequest(new { Errors = new[] { "Invalid account details supplied." } });
            }

            try
            {
                await _accountRepository.CreateAccountAsync(request);
                return StatusCode(StatusCodes.Status201Created, new { Message = "Account created successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating account for Customer ID {CustomerID}", request.CustomerID);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating account.");
            }
        }

        /// <summary>
        /// Deletes an account
        /// </summary>
        [HttpDelete("DeleteAccount/{accountId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAccount(int accountId)
        {
            try
            {
                await _accountRepository.DeleteAccountAsync(accountId);
                return Ok(new { Message = "Account deleted successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting account ID {AccountID}", accountId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error deleting account.");
            }
        }
    }
}
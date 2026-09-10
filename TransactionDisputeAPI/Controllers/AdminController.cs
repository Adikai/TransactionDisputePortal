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
    }
}
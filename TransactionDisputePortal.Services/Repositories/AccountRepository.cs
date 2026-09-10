using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {

        private readonly ISqlExecuter _sqlExecuter;

        public AccountRepository(ISqlExecuter sqlExecuter)
        {
            _sqlExecuter = sqlExecuter ?? throw new ArgumentNullException(nameof(sqlExecuter));
        }

        public async Task<decimal> UpdateAccountBalance(UpdateBalanceRequestDTO request)
        {
            decimal UpdatedBalance = await _sqlExecuter.QueryFirstOrDefaultAsync<decimal>(
                "[dbo].[UpdateAccountBalance]",
                new { AccountID = request.AccountID, NewBalance = request.Amount },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return UpdatedBalance;
        }

        public async Task<CustomerDashboardResponseDto> GetCustomerDashboardAsync(int customerId)
        {
            var param = new { CustomerID = customerId };

            // Execute all 3 procedures asynchronously in parallel
            var accountsTask = _sqlExecuter.QueryAsync<AccountSummaryDto>(
                "dbo.GetAccountsByCustomerID", param, commandType: CommandType.StoredProcedure);

            var metricsTask = _sqlExecuter.QueryFirstOrDefaultAsync<CustomerMetricsDto>(
                "dbo.GetCustomerDashboardMetrics", param, commandType: CommandType.StoredProcedure);

            var disputesTask = _sqlExecuter.QueryAsync<RecentDisputeDto>(
                "dbo.GetRecentDisputesByCustomerID", param, commandType: CommandType.StoredProcedure);

            await Task.WhenAll(accountsTask, metricsTask, disputesTask);

            var accounts = (await accountsTask).ToList();
            var metrics = await metricsTask ?? new CustomerMetricsDto(0, 0, 0, 0);
            var recentDisputes = (await disputesTask).ToList();

            return new CustomerDashboardResponseDto(accounts, metrics, recentDisputes);
        }
        public async Task<LoginResponseDto?> loginCustomer(LoginRequestDto loginRequest)
        {
            var row = await _sqlExecuter.QueryFirstOrDefaultAsync<dynamic>(
                "[dbo].[GetCustomerDetailsByEmailAndPassword]",
                new { Email = loginRequest.Email, PasswordHash = loginRequest.PasswordHash },
                commandType: System.Data.CommandType.StoredProcedure
            );

            if (row == null) return null;

            return new LoginResponseDto
            {
                UserID = (int)row.UserID,
                FirstName = (string)row.FirstName,
                LastName = (string)row.LastName,
                Email = (string)row.Email,
                UserRole = (string?)row.UserRole,
                Token = null
            };
        }
        public async Task<List<AccountSummaryDto>> GetAccountNumbersByCustomerID(int customerID)
        {
            var accountNumbers = await _sqlExecuter.QueryAsync<AccountSummaryDto>(
                "dbo.GetAccountsByCustomerID",
                new { CustomerID = customerID },
                commandType: CommandType.StoredProcedure
            );

            return accountNumbers.ToList();
        }

        public async Task<IEnumerable<AdminDisputesResponseDTO>> GetDisputesForAdminAsync(
            int pageNumber,
            int pageSize,
            int? disputeStatusID,
            string? searchTerm)
        {
            var param = new
            {
                PageNumber = pageNumber < 1 ? 1 : pageNumber,
                PageSize = pageSize < 1 ? 20 : pageSize,
                DisputeStatusID = (disputeStatusID.HasValue && disputeStatusID.Value > 0) ? disputeStatusID.Value : (int?)null,
                SearchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim()
            };

            var disputes = await _sqlExecuter.QueryAsync<AdminDisputesResponseDTO>(
                "[dbo].[GetAllDisputes]",
                param,
                commandType: CommandType.StoredProcedure
            );

            return disputes ?? Enumerable.Empty<AdminDisputesResponseDTO>();
        }

        public async Task<int> CreateCustomerAsync(CreateCustomerDto request)
        {
            var parameters = new DynamicParameters();
            parameters.Add("FirstName", request.FirstName);
            parameters.Add("LastName", request.LastName);
            parameters.Add("Email", request.Email);
            parameters.Add("PhoneNumber", request.PhoneNumber);
            parameters.Add("PasswordHash", request.PasswordHash);
            parameters.Add("NewCustomerID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _sqlExecuter.ExecuteAsync(
                "[dbo].[sp_CreateCustomer]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return parameters.Get<int>("NewCustomerID");
        }

        public async Task UpdateCustomerAsync(UpdateCustomerDto request)
        {
            var param = new
            {
                CustomerID = request.CustomerID,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                PasswordHash = string.IsNullOrWhiteSpace(request.PasswordHash) ? null : request.PasswordHash
            };

            await _sqlExecuter.ExecuteAsync(
                "[dbo].[sp_UpdateCustomer]",
                param,
                commandType: CommandType.StoredProcedure
            );
        }

        public async Task DeleteCustomerAsync(int customerId)
        {
            var param = new { CustomerID = customerId };

            await _sqlExecuter.ExecuteAsync(
                "[dbo].[sp_DeleteCustomer]",
                param,
                commandType: CommandType.StoredProcedure
            );
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Infrastructure.Repositories
{
    public class AccountRepository: IAccountRepository
    {

        private readonly ISqlExecuter _sqlExecuter;

        public AccountRepository(ISqlExecuter sqlExecuter)
        {
            _sqlExecuter = sqlExecuter ?? throw new ArgumentNullException(nameof(sqlExecuter));
        }

        public async Task<decimal> UpdateAccountBalance(int accountID, decimal amount)
        {
            decimal UpdatedBalance = await _sqlExecuter.QueryFirstOrDefaultAsync<decimal>(
                "[dbo].[UpdateAccountBalance]",
                new { AccountID = accountID, NewBalance = amount },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return UpdatedBalance;
        }

        public async Task<AccountDTOResponse> GetAccountDetails(int customerID)
        {
            var account = await _sqlExecuter.QueryFirstOrDefaultAsync<AccountDTOResponse>(
                "[dbo].[GetAccountDetailsByCustomerID]",
                new { CustomerID = customerID },
                commandType: System.Data.CommandType.StoredProcedure
            );

            return account;
        }
    }
}

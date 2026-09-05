using System;
using System.Collections.Generic;
using System.Text;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Core.Interfaces
{
    public interface IAccountRepository
    {
        Task<decimal> UpdateAccountBalance(UpdateBalanceRequestDTO request);
        Task<AccountDTOResponse> GetAccountDetails(int customerID);
    }
}

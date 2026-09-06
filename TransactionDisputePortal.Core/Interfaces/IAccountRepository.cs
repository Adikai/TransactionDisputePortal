using System;
using System.Collections.Generic;
using System.Text;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Core.Interfaces
{
    public interface IAccountRepository
    {
        Task<decimal> UpdateAccountBalance(UpdateBalanceRequestDTO request);
        Task<LoginResponseDto> loginCustomer(LoginRequestDto loginRequest);
        Task<CustomerDashboardResponseDto> GetCustomerDashboardAsync(int customerId);
        Task<List<AccountSummaryDto>> GetAccountNumbersByCustomerID(int customerID);
    }
}

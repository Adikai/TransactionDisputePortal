using System;
using System.Collections.Generic;
using System.Text;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Core.Interfaces
{
    public interface ITransactionRepository
    {
        Task<IEnumerable<TransactionResponseDto>> GetTransactionsByAccountAndDateRangeAsync(
            string accountNumber,
            DateTimeOffset startDate,
            DateTimeOffset endDate,
            CancellationToken cancellationToken = default);
    }
}

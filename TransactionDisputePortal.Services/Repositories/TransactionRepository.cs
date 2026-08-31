using System.Data;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

public class TransactionRepository : ITransactionRepository
{
    private readonly ISqlExecuter _sqlExecuter;

    public TransactionRepository(ISqlExecuter sqlExecuter)
    {
        _sqlExecuter = sqlExecuter ?? throw new ArgumentNullException(nameof(sqlExecuter));
    }

    public async Task<IEnumerable<TransactionResponseDto>> GetTransactionsByAccountAndDateRangeAsync(
        string accountNumber,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        CancellationToken cancellationToken = default)
    {
        var parameters = new
        {
            AccountNumber = accountNumber,
            StartDate = startDate,
            EndDate = endDate
        };

        return await _sqlExecuter.QueryAsync<TransactionResponseDto>(
            "[dbo].[GetTransactionsByAccountAndDateRange]",
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken
        );
    }
}

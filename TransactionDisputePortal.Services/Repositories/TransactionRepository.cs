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

    public async Task<bool> UpdateDisputeStatus(int disputeID, int newStatusID)
    {
        int rowsAffected = await _sqlExecuter.ExecuteAsync(
            "[dbo].[UpdateDisputeStatus]",
            new { DisputeID = disputeID, NewStatusID = newStatusID },
            commandType: CommandType.StoredProcedure
        );

        return rowsAffected > 0;
    }

    public async Task<int> CreateDispute(CreateDisputeRequestDTO request)
    {
        var parameters = new
        {
            TransactionID = request.TransactionID,
            CustomerID = request.CustomerID,
            DisputeStatusID = request.DisputeStatusID,
            ReasonCategory = request.ReasonCategory,
            CustomerNotes = request.CustomerNotes,
            DisputedAmount = request.DisputedAmount
        };

        int newDisputeID = await _sqlExecuter.QueryFirstOrDefaultAsync<int>(
            "[dbo].[CreateDispute]",
            parameters,
            commandType: CommandType.StoredProcedure
        );
        return newDisputeID;
    }
}

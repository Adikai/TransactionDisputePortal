using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace TransactionDisputePortal.Core.Interfaces
{
    public interface ISqlExecuter
    {
        Task<IEnumerable<T>> QueryAsync<T>(
            string sql,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default);

        Task<T?> QueryFirstOrDefaultAsync<T>(
            string sql,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default);

        Task<int> ExecuteAsync(
            string sql,
            object? param = null,
            CommandType commandType = CommandType.StoredProcedure,
            CancellationToken cancellationToken = default);
    }
}

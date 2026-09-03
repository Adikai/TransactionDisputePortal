using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TransactionDisputePortal.Core.Interfaces;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Infrastructure.Repositories
{
    public class AuditRepository : IAuditRepository
    {
        private readonly ISqlExecuter _sqlExecuter;

        public AuditRepository(ISqlExecuter sqlExecuter)
        {
            _sqlExecuter = sqlExecuter ?? throw new ArgumentNullException(nameof(sqlExecuter));
        }

        public async Task GetTransactionsByAccountAndDateRangeAsync(
            InsertAuditLogRequestDto auditlog,
            CancellationToken cancellationToken = default)
        {
            var parameters = new
            {
                DisputeID = auditlog.DisputeID,
                PreviousStatusID = auditlog.PreviousStatusID,
                NewStatusID = auditlog.NewStatusID,
                ChangedByStaffID = auditlog.ChangedByStaffID,
                ChangedByCustomerID = auditlog.ChangedByCustomerID,
                Notes = auditlog.Notes
            };

            await _sqlExecuter.ExecuteAsync(
                "[dbo].[InsertAuditLog]",
                parameters,
                commandType: CommandType.StoredProcedure,
                cancellationToken: cancellationToken
            );
            
        }
    }

  

        
    }

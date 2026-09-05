using System;
using System.Collections.Generic;
using System.Text;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Core.Interfaces
{
    public interface IAuditRepository
    {
        Task InsertAuditLog(InsertAuditLogRequestDto auditlog);
    }
}

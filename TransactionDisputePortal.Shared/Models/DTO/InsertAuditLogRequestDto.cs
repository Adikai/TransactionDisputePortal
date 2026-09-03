using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class InsertAuditLogRequestDto
    {
        public int DisputeID { get; set; }
        public int PreviousStatusID { get; set; }
        public int NewStatusID { get; set; }
        public int ChangedByStaffID { get; set; }
        public int ChangedByCustomerID { get; set; }
        public string? Notes { get; set; }
    }
}


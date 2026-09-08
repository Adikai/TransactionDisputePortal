using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public  class AdminDisputesResponseDTO
    {
        public int DisputeID { get; set; }
        public int TransactionID { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public decimal DisputedAmount { get; set; }
        public string ReasonCategory { get; set; } = string.Empty;
        public int DisputeStatusID { get; set; }
        public string DisputeStatus { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int TotalRecords { get; set; }
    }
}

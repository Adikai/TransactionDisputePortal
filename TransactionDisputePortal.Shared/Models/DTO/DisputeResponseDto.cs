using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class DisputeResponseDto
    {
        public int DisputeID { get; set; }
        public long TransactionID { get; set; } 
        public int CustomerID { get; set; }
        public int DisputeStatusID { get; set; }
        public string ReferenceNumber { get; set; } = string.Empty;
        public string MerchantName { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string DisputeStatus { get; set; } = string.Empty;
        public string ReasonCategory { get; set; } = string.Empty;
        public string CustomerNotes { get; set; } = string.Empty;
        public decimal DisputedAmount { get; set; }
        public DateTimeOffset CreatedDate { get; set; } 
        public DateTimeOffset? UpdatedDate { get; set; } 
    }
}

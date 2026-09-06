using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class GetDisputeByCustomerIDResponseDTO
    {
        public int DisputeID { get; set; }
        public long TransactionID { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? MerchantName { get; set; }
        public decimal DisputedAmount { get; set; }
        public string? ReasonCategory { get; set; }
        public string? DisputeStatus { get; set; }
        public DateTimeOffset CreatedDate { get; set; }
        public DateTimeOffset LastUpdatedDate { get; set; }
    }
}

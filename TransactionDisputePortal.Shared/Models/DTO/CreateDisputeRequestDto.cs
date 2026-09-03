using System;
using System.Collections.Generic;
using System.Text;
using System.Transactions;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class CreateDisputeRequestDTO
    {

        public long TransactionID { get; set; }
        public int CustomerID { get; set; }
        public int DisputeStatusID { get; set; }
        public string ReasonCategory { get; set; }
        public string? CustomerNotes { get; set; }
        public decimal DisputedAmount { get; set; }
    }
}

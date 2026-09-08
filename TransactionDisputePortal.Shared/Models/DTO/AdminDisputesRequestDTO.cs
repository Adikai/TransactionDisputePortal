using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class AdminDisputesRequestDTO
    {
        public int pageNumber { get; set; }
        public int pageSize { get; set; }
        public int disputeStatusID { get; set; }
        public string? searchTerm { get; set; } = null;
    }
}

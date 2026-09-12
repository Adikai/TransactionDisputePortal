using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class UpdateAccountDto
    {
        public int AccountID { get; set; }
        public string AccountType { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Note { get; set; } = string.Empty;
    }
}

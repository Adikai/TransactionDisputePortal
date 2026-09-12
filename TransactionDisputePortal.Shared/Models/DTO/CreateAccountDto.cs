using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class CreateAccountDto
    {
        public int CustomerID { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = "Checking";
        public decimal InitialBalance { get; set; } = 0.00m;
    }
}

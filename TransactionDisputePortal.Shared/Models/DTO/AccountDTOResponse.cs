using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class AccountDTOResponse
    {
      public int AccountID { get; set; }
      public string AccountNumber { get; set; }
      public int CustomerID { get; set; }
      public string AccountType { get; set; }
      public decimal Balance { get; set; }
      public DateTimeOffset CreatedAt { get; set; }
    }
}

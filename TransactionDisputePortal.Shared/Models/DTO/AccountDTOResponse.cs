using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record AccountDTOResponse(
            int AccountID,
            string AccountNumber,
            int CustomerID,
            string AccountType,
            decimal Balance,
            DateTimeOffset CreatedAt
        );
}

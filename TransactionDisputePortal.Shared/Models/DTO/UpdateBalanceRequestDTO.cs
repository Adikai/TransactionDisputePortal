using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record UpdateBalanceRequestDTO
    (
        int AccountID,
        decimal Amount
    );
}

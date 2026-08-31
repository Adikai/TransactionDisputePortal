using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record GetTransactionsRequestDto(
        string AccountNumber,
        DateTimeOffset StartDate,
        DateTimeOffset EndDate
    );
}

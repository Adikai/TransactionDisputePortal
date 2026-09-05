using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record TransactionResponseDto(
        long TransactionID,
        int AccountID,
        string MerchantName,
        decimal Amount,
        string TransactionType,
        string ReferenceNumber,
        string StatusName,
        DateTimeOffset TransactionDate
    );
}

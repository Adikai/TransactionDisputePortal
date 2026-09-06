using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record TransactionResponseDto(
        long TransactionID,
        int AccountID,
        string AccountNumber,
        DateTimeOffset TransactionDate,
        string ReferenceNumber,
        string MerchantName,
        string TransactionType,
        decimal Amount,
        string StatusName,
        bool HasActiveDispute
    );
}

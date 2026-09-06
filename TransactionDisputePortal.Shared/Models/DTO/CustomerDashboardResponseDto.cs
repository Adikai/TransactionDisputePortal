using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record CustomerDashboardResponseDto(
        List<AccountSummaryDto> Accounts,
        CustomerMetricsDto Metrics,
        List<RecentDisputeDto> RecentDisputes
    );

    public record AccountSummaryDto(
        int AccountId,
        string AccountNumber,
        int CustomerId,
        string AccountType,
        decimal Balance,
        DateTimeOffset CreatedAt,
        int ActiveDisputeCount,
        decimal TotalDisputedAmount
    );

    public record CustomerMetricsDto(
        decimal OverallBalance,
        int TotalAccountsCount,
        int OverallActiveDisputeCount,
        decimal OverallDisputedAmount
    );

    public record RecentDisputeDto(
        int DisputeId,
        string ReferenceNumber,
        string ReasonCategory,
        decimal DisputedAmount,
        string StatusName,
        DateTimeOffset CreatedAt
    );
}

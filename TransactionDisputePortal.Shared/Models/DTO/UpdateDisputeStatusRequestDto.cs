using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record UpdateDisputeStatusRequestDto(
            int DisputeID,
            int NewStatusID,
            int? StaffID,
            string? AdminNotes
        );
}

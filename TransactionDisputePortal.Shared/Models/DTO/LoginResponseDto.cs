using System;
using System.Collections.Generic;
using System.Text;

namespace TransactionDisputePortal.Shared.Models.DTO
{
    public record LoginResponseDto(
            int CustomerID,
            string FirstName,
            string LastName,
            string Email,
            string? Token
        );
}

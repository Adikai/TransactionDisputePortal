namespace TransactionDisputePortal.Shared.Models.DTO
{
    public class LoginResponseDto
    {
        public int UserID { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty; // "Customer", "DisputeAnalyst", or "Admin"
        public string Token { get; set; } = string.Empty;

        // Helper property to get full display name
        public string DisplayName => string.IsNullOrWhiteSpace(LastName)
            ? FirstName
            : $"{FirstName} {LastName}".Trim();
    }
}
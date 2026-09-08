using Microsoft.JSInterop;

namespace TransactionDisputePortal.Client
{
    public class AuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string TokenKey = "dummy_auth_token";
        private const string UserIDKey = "user_id";
        private const string CustomerNameKey = "customer_name";
        private const string UserRoleKey = "user_role";

        public AuthService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetTokenAsync(string token, int UserID, string customerName, string role)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserIDKey, UserID.ToString());
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CustomerNameKey, customerName);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", UserRoleKey, role);
        }

        public async Task<string?> GetUserRoleAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserRoleKey);
        }

        public async Task<bool> IsStaffAsync()
        {
            var role = await GetUserRoleAsync();

            if (string.IsNullOrWhiteSpace(role))
                return false;

            // Treat any non-Customer role (Admin, DisputeAnalyst, Staff) as staff
            return !role.Equals("Customer", StringComparison.OrdinalIgnoreCase);
        }

        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserIDKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CustomerNameKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", UserRoleKey);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }

        public async Task<int?> GetUserIDAsync()
        {
            var idStr = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", UserIDKey);
            return int.TryParse(idStr, out var id) ? id : null;
        }

        public async Task<string?> GetCustomerNameAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CustomerNameKey);
        }
    }
}
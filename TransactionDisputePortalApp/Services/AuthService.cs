using Microsoft.JSInterop;

namespace TransactionDisputePortal.Client
{
    public class AuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string TokenKey = "dummy_auth_token";
        private const string CustomerIdKey = "customer_id";
        private const string CustomerNameKey = "customer_name";

        public AuthService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetTokenAsync(string token, int customerId, string customerName)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CustomerIdKey, customerId.ToString());
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", CustomerNameKey, customerName);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }

        public async Task<int?> GetCustomerIdAsync()
        {
            var idStr = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CustomerIdKey);
            return int.TryParse(idStr, out var id) ? id : null;
        }

        public async Task<string?> GetCustomerNameAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", CustomerNameKey);
        }

        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CustomerIdKey);
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", CustomerNameKey);
        }
    }
}
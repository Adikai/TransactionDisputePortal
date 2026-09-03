using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.JSInterop;

namespace TransactionDisputePortal.Client
{
    public class AuthService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string TokenKey = "dummy_auth_token";

        public AuthService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task SetTokenAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }

        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        }
    }
}

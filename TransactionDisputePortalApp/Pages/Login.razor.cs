using System.Net.Http.Json;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Client.Pages
{
    public partial class Login
    {
        private LoginRequestDto loginModel = new LoginRequestDto
        {
            Email = "john.doe@example.com",
            PasswordHash = "AQAAAAIAAYagAAAAE..."
        };

        private bool isLoading;
        private string? errorMessage;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {

                var isAuthenticated = await AuthService.IsAuthenticatedAsync();
                if (isAuthenticated)
                {
                    var isStaff = await AuthService.IsStaffAsync();
                    if (isStaff)
                    {
                        Navigation.NavigateTo("/admin/disputes", replace: true);
                    }
                    else
                    {
                        Navigation.NavigateTo("/", replace: true);
                    }
                }

            }
        }

        private async Task HandleLogin()
        {
            isLoading = true;
            errorMessage = null;

            try
            {
                var response = await Http.PostAsJsonAsync("api/Customer/login", loginModel);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<LoginResponseDto>();

                    if (result != null)
                    {
                        await AuthService.RemoveTokenAsync();
                        await AuthService.SetTokenAsync(result.Token, result.UserID, result.DisplayName, result.UserRole);

                        if (result.UserRole.Equals("Customer", StringComparison.OrdinalIgnoreCase))
                        {
                            Navigation.NavigateTo("/", replace: true);
                        }
                        else
                        {
                            Navigation.NavigateTo("/admin/disputes", replace: true);
                        }
                        return;
                    }

                    errorMessage = "Received invalid authentication token.";
                }
                else
                {
                    var errorResponse = await response.Content.ReadAsStringAsync();
                    errorMessage = !string.IsNullOrWhiteSpace(errorResponse)
                        ? errorResponse
                        : "Login failed. Please check your credentials.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"An unexpected error occurred: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }
    }
}
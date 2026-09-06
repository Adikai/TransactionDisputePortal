using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using TransactionDisputePortal.Client;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Client.Pages
{
    public partial class Home : ComponentBase
    {
        [Inject] public NavigationManager Navigation { get; set; } = default!;
        [Inject] public AuthService AuthService { get; set; } = default!;
        [Inject] public HttpClient Http { get; set; } = default!;

        private CustomerDashboardResponseDto? dashboardData;
        private bool isLoading = true;
        private string? errorMessage;
        private int selectedAccountId;

        private AccountSummaryDto? SelectedAccount =>
            dashboardData?.Accounts.FirstOrDefault(a => a.AccountId == selectedAccountId);

        protected override async Task OnInitializedAsync()
        {
            await CheckAuthAndLoadDashboardAsync();
        }

        private async Task CheckAuthAndLoadDashboardAsync()
        {
            isLoading = true;
            errorMessage = null;

            var token = await AuthService.GetTokenAsync();
            var customerId = await AuthService.GetCustomerIdAsync();

            if (string.IsNullOrEmpty(token) || !customerId.HasValue)
            {
                Navigation.NavigateTo("/login", replace: true);
                return;
            }

            await LoadDashboardDataAsync(customerId.Value);
        }

        private async Task LoadDashboardDataAsync(int customerId)
        {
            try
            {
                dashboardData = await Http.GetFromJsonAsync<CustomerDashboardResponseDto>(
                    $"api/Customer/GetCustomerDashboard/{customerId}");

                if (dashboardData?.Accounts.Any() == true)
                {
                    selectedAccountId = dashboardData.Accounts.First().AccountId;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load dashboard details: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            var customerId = await AuthService.GetCustomerIdAsync();
            if (customerId.HasValue)
            {
                isLoading = true;
                await LoadDashboardDataAsync(customerId.Value);
            }
            else
            {
                Navigation.NavigateTo("/login", replace: true);
            }
        }

        private void SelectAccount(int accountId)
        {
            selectedAccountId = accountId;
        }

        private void NavigateToTransactions()
        {
            if (SelectedAccount != null && !string.IsNullOrWhiteSpace(SelectedAccount.AccountNumber))
            {
                Navigation.NavigateTo($"/transactions?accountNumber={Uri.EscapeDataString(SelectedAccount.AccountNumber)}");
            }
            else
            {
                Navigation.NavigateTo("/transactions");
            }
        }

        private async Task HandleLogoutAsync()
        {
            await AuthService.RemoveTokenAsync();
            Navigation.NavigateTo("/login", replace: true);
        }

        private static string GetStatusBadgeClass(string statusName) => statusName switch
        {
            "Submitted" => "bg-warning text-dark",
            "Under Review" => "bg-info text-dark",
            "Approved" => "bg-success",
            "Rejected" => "bg-danger",
            _ => "bg-secondary"
        };
    }
}
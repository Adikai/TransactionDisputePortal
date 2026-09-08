using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Client.Pages
{
    public partial class Disputes : ComponentBase
    {
        private List<DisputeResponseDto>? disputes;
        private DisputeResponseDto? selectedDispute;

        private bool isLoading = true;
        private string? errorMessage;
        private string searchQuery = string.Empty;
        private string selectedStatusFilter = string.Empty;
        private int UserID;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                UserID = (int)await AuthService.GetUserIDAsync();
                if (UserID > 0)
                {
                    await LoadDisputesAsync();
                }
                else
                {
                    errorMessage = "Customer session not found. Please log in again.";
                    isLoading = false;
                }

                StateHasChanged();
            }
        }

        private async Task LoadDisputesAsync()
        {
            isLoading = true;
            errorMessage = null;

            try
            {
                disputes = await Http.GetFromJsonAsync<List<DisputeResponseDto>>($"api/Transactions/disputes/customer/{UserID}");
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load disputes: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private IEnumerable<DisputeResponseDto>? FilteredDisputes => disputes?
            .Where(d => string.IsNullOrWhiteSpace(selectedStatusFilter) || d.DisputeStatus.Equals(selectedStatusFilter, StringComparison.OrdinalIgnoreCase))
            .Where(d => string.IsNullOrWhiteSpace(searchQuery) ||
                        d.ReferenceNumber.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                        d.MerchantName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

        private int ActiveCount => disputes?.Count(d => d.DisputeStatus == "Submitted" || d.DisputeStatus == "Under Review") ?? 0;
        private int ResolvedCount => disputes?.Count(d => d.DisputeStatus == "Approved" || d.DisputeStatus == "Resolved") ?? 0;
        private decimal TotalDisputedValue => disputes?.Sum(d => d.DisputedAmount) ?? 0m;

        private void ViewDisputeDetails(DisputeResponseDto dispute)
        {
            selectedDispute = dispute;
        }

        private void CloseDetailsModal()
        {
            selectedDispute = null;
        }

        private string GetStatusBadgeClass(int statusId) => statusId switch
        {
            1 => "bg-warning text-dark", // Submitted
            2 => "bg-info text-dark",    // Under Review
            3 => "bg-success",          // Approved
            4 => "bg-danger",           // Rejected
            _ => "bg-secondary"
        };
    }
}
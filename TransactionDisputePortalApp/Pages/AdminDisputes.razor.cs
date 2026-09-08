using Microsoft.AspNetCore.Components;
using System.Net.Http.Json;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Client.Pages
{
    public partial class AdminDisputes
    {
        private List<AdminDisputesResponseDTO> disputes = new();
        private bool isLoading = true;

        // Pagination & Filter State
        private int currentPageNumber = 1;
        private int pageSize = 10;
        private int? selectedStatusId = 0;
        private string? searchTerm = null;
        private int totalRecords = 0;
        private int totalPages => (int)Math.Ceiling((double)totalRecords / pageSize);

        // Modal State
        private bool isModalOpen = false;
        private bool isSaving = false;
        private AdminDisputesResponseDTO? selectedDispute;
        private int selectedNewStatusId;
        private string adminNotes = string.Empty;
        private string? modalErrorMessage;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                var isStaff = await Auth.IsStaffAsync();
                if (!isStaff)
                {
                    Navigation.NavigateTo("/", replace: true);
                    return;
                }

                await LoadAllDisputesAsync();
            }
        }

        private async Task LoadAllDisputesAsync()
        {
            isLoading = true;

            var request = new AdminDisputesRequestDTO
            {
                pageNumber = currentPageNumber,
                pageSize = pageSize,
                disputeStatusID = selectedStatusId,
                searchTerm = searchTerm
            };

            var response = await Http.PostAsJsonAsync("api/Admin/GetDisputes", request);

            if (response.IsSuccessStatusCode)
            {
                disputes = await response.Content.ReadFromJsonAsync<List<AdminDisputesResponseDTO>>() ?? new();
                totalRecords = disputes.FirstOrDefault()?.TotalRecords ?? 0;
            }

            isLoading = false;
            StateHasChanged();
        }

        private async Task OnStatusFilterChanged(ChangeEventArgs e)
        {
            if (int.TryParse(e.Value?.ToString(), out var statusId))
            {
                selectedStatusId = statusId;
                currentPageNumber = 1;
                await LoadAllDisputesAsync();
            }
        }

        private async Task ChangePageAsync(int newPage)
        {
            if (newPage >= 1 && newPage <= totalPages && newPage != currentPageNumber)
            {
                currentPageNumber = newPage;
                await LoadAllDisputesAsync();
            }
        }

        private void OpenReviewModal(AdminDisputesResponseDTO dispute)
        {
            selectedDispute = dispute;
            selectedNewStatusId = dispute.DisputeStatusID;
            adminNotes = string.Empty;
            modalErrorMessage = null;
            isModalOpen = true;
            StateHasChanged();
        }

        private void CloseModal()
        {
            isModalOpen = false;
            selectedDispute = null;
            modalErrorMessage = null;
        }

        private async Task SaveStatusUpdateAsync()
        {
            if (selectedDispute == null) return;

            isSaving = true;
            modalErrorMessage = null;

            var staffId = await Auth.GetUserIDAsync();

            var request = new UpdateDisputeStatusRequestDto(
                selectedDispute.DisputeID,
                selectedNewStatusId,
                staffId,
                adminNotes
            );

            var response = await Http.PostAsJsonAsync("api/Transactions/UpdateDisputeStatus", request);

            if (response.IsSuccessStatusCode)
            {
                isModalOpen = false;
                await LoadAllDisputesAsync();
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                modalErrorMessage = !string.IsNullOrWhiteSpace(error)
                    ? error
                    : "Failed to update dispute status.";
            }

            isSaving = false;
            StateHasChanged();
        }
    }
    }

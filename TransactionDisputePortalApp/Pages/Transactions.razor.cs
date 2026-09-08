using Microsoft.AspNetCore.Components;

using System.Net.Http.Json;
using TransactionDisputePortal.Shared.Models.DTO;
using TransactionDisputePortal.Shared.Models.Enums;


namespace TransactionDisputePortal.Client.Pages
{
    public partial class Transactions : ComponentBase
    {
        [Inject] public HttpClient Http { get; set; } = default!;

        private GetTransactionsRequestDto filterModel = new GetTransactionsRequestDto
        {
            AccountNumber = string.Empty,
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today
        };

        private List<AccountDTOResponse>? customerAccounts;
        private List<TransactionResponseDto>? transactions;
        private TransactionResponseDto? selectedTransaction;
        private CreateDisputeRequestDTO disputeModel = new();

        private bool isLoading = false;
        private bool isSubmittingDispute = false;
        private bool showDisputeModal = false;

        private string? errorMessage;
        private string? successMessage;

        private int UserID;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                UserID = (int)await AuthService.GetUserIDAsync();
                await LoadCustomerAccountsAsync();
                StateHasChanged();
            }
        }

        private async Task LoadCustomerAccountsAsync()
        {
            try
            {
                var response = await Http.GetFromJsonAsync<List<AccountDTOResponse>>($"api/Customer/Accounts/{UserID}");

                if (response != null && response.Any())
                {
                    customerAccounts = response;
                    filterModel.AccountNumber = customerAccounts.First().AccountNumber;
                    await FetchTransactionsAsync();
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to load customer accounts: {ex.Message}";
            }
        }

        private async Task FetchTransactionsAsync()
        {
            if (string.IsNullOrWhiteSpace(filterModel.AccountNumber))
            {
                errorMessage = "Please select an account number.";
                return;
            }

            isLoading = true;
            errorMessage = null;
            successMessage = null;

            try
            {
                var response = await Http.PostAsJsonAsync("api/Transactions/GetTransactions", filterModel);

                if (response.IsSuccessStatusCode)
                {
                    transactions = await response.Content.ReadFromJsonAsync<List<TransactionResponseDto>>();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    errorMessage = !string.IsNullOrWhiteSpace(err) ? err : "Failed to load transactions.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Connection error: {ex.Message}";
            }
            finally
            {
                isLoading = false;
            }
        }

        private void OpenDisputeModal(TransactionResponseDto transaction)
        {
            selectedTransaction = transaction;
            disputeModel = new CreateDisputeRequestDTO
            {
                TransactionID = transaction.TransactionID,
                CustomerID = UserID,
                DisputeStatusID = (int)DisputeStatusEnum.Submitted,
                DisputedAmount = transaction.Amount,
                ReasonCategory = "",
                CustomerNotes = ""
            };
            showDisputeModal = true;
        }

        private void CloseDisputeModal()
        {
            showDisputeModal = false;
            selectedTransaction = null;
        }

        private async Task SubmitDisputeAsync()
        {
            if (string.IsNullOrWhiteSpace(disputeModel.ReasonCategory))
            {
                errorMessage = "Please select a reason category for the dispute.";
                return;
            }

            isSubmittingDispute = true;
            errorMessage = null;

            try
            {
                var response = await Http.PostAsJsonAsync("api/Transactions/CreateDispute", disputeModel);

                if (response.IsSuccessStatusCode)
                {
                    successMessage = "Dispute submitted successfully. Our team will review your request.";
                    CloseDisputeModal();
                    await FetchTransactionsAsync();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    errorMessage = !string.IsNullOrWhiteSpace(err) ? err : "Failed to lodge dispute.";
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error submitting dispute: {ex.Message}";
            }
            finally
            {
                isSubmittingDispute = false;
            }
        }

        private string GetTransactionStatusBadge(string? status) => status switch
        {
            "Posted" => "bg-success-subtle text-success border border-success-subtle",
            "Pending" => "bg-warning-subtle text-warning border border-warning-subtle",
            "Reversed" => "bg-danger-subtle text-danger border border-danger-subtle",
            _ => "bg-light text-dark"
        };
    }
}
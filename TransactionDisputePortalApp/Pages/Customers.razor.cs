using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using TransactionDisputePortal.Core.Helpers;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Client.Pages
{
    public partial class Customers
    {
        [Inject]
        private HttpClient Http { get; set; } = default!;

        private string searchTerm = string.Empty;
        private bool showModal = false;
        private bool showDeleteModal = false;
        private bool isEditMode = false;
        private string plainPassword = string.Empty;
        private string? errorMessage;
        private bool showAccountsModal = false;
        private CustomerViewModel? selectedCustomerForAccounts;
        private List<AccountDTOResponse> customerAccounts = new();
        private CreateAccountDto? newAccountModel;

        private CustomerViewModel currentModel = new();
        private CustomerViewModel? customerToDelete;

        private List<CustomerViewModel> customers = new();

        protected override async Task OnInitializedAsync()
        {
            await LoadCustomersAsync();
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                errorMessage = null;
                var result = await Http.GetFromJsonAsync<List<CustomerViewModel>>("api/Admin/GetCustomers");
                if (result != null)
                {
                    customers = result;
                }
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading customers: {ex.Message}";
            }
        }

        private IEnumerable<CustomerViewModel> FilteredCustomers => customers
            .Where(c => string.IsNullOrWhiteSpace(searchTerm) ||
                        c.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        c.AccountNumber.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));

        private void OpenCreateModal()
        {
            isEditMode = false;
            currentModel = new CustomerViewModel();
            plainPassword = string.Empty;
            showModal = true;
        }

        private void OpenEditModal(CustomerViewModel customer)
        {
            isEditMode = true;
            currentModel = new CustomerViewModel
            {
                CustomerID = customer.CustomerID,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                PhoneNumber = customer.PhoneNumber,
                AccountNumber = customer.AccountNumber,
                PasswordHash = customer.PasswordHash,
                CreatedAt = customer.CreatedAt
            };
            plainPassword = string.Empty;
            showModal = true;
        }

        private void CloseModal()
        {
            showModal = false;
        }

        private async Task SaveCustomerAsync()
        {
            var firstName = currentModel.FirstName;
            var lastName = currentModel.LastName;

            if (isEditMode)
            {
                var updateDto = new UpdateCustomerDto
                {
                    CustomerID = currentModel.CustomerID,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = currentModel.Email,
                    PhoneNumber = currentModel.PhoneNumber,
                    PasswordHash = string.IsNullOrWhiteSpace(plainPassword) ? null : PasswordHelper.HashPassword(plainPassword)
                };

                var response = await Http.PutAsJsonAsync("api/Admin/UpdateCustomer", updateDto);
                if (response.IsSuccessStatusCode)
                {
                    await LoadCustomersAsync();
                    showModal = false;
                }
                else
                {
                    errorMessage = "Failed to update customer.";
                }
            }
            else
            {
                string hashedPassword = string.IsNullOrWhiteSpace(plainPassword)
                    ? PasswordHelper.HashPassword("DefaultPassword123!")
                    : PasswordHelper.HashPassword(plainPassword);

                var createDto = new CreateCustomerDto
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = currentModel.Email,
                    PhoneNumber = currentModel.PhoneNumber,
                    PasswordHash = hashedPassword
                };

                var response = await Http.PostAsJsonAsync("api/Admin/CreateCustomer", createDto);
                if (response.IsSuccessStatusCode)
                {
                    await LoadCustomersAsync();
                    showModal = false;
                }
                else
                {
                    errorMessage = "Failed to create customer.";
                }
            }
        }

        private void ConfirmDelete(CustomerViewModel customer)
        {
            customerToDelete = customer;
            showDeleteModal = true;
        }

        private async Task DeleteCustomerAsync()
        {
            if (customerToDelete != null)
            {
                var response = await Http.DeleteAsync($"api/Admin/DeleteCustomer/{customerToDelete.CustomerID}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadCustomersAsync();
                }
                else
                {
                    errorMessage = "Failed to delete customer.";
                }
                customerToDelete = null;
            }
            showDeleteModal = false;
        }

        private static (string FirstName, string LastName) SplitFullName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (string.Empty, string.Empty);

            var parts = fullName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length switch
            {
                1 => (parts[0], string.Empty),
                2 => (parts[0], parts[1]),
                _ => (string.Empty, string.Empty)
            };
        }

        private async Task OpenAccountsModal(CustomerViewModel customer)
        {
            selectedCustomerForAccounts = customer;
            newAccountModel = new CreateAccountDto { CustomerID = customer.CustomerID };
            await FetchCustomerAccountsAsync(customer.CustomerID);
            showAccountsModal = true;
        }

        private void CloseAccountsModal()
        {
            showAccountsModal = false;
            selectedCustomerForAccounts = null;
            customerAccounts.Clear();
        }

        private async Task FetchCustomerAccountsAsync(int customerId)
        {
            try
            {
                var response = await Http.GetFromJsonAsync<List<AccountDTOResponse>>($"api/Admin/GetAccountsByCustomer/{customerId}");
                customerAccounts = response ?? new List<AccountDTOResponse>();
            }
            catch (Exception ex)
            {
                errorMessage = $"Error loading customer accounts: {ex.Message}";
            }
        }

        private async Task CreateAccountAsync()
        {
            if (selectedCustomerForAccounts == null) return;

            var response = await Http.PostAsJsonAsync("api/Admin/CreateAccount", newAccountModel);
            if (response.IsSuccessStatusCode)
            {
                await FetchCustomerAccountsAsync(selectedCustomerForAccounts.CustomerID);
                newAccountModel = new CreateAccountDto { CustomerID = selectedCustomerForAccounts.CustomerID };
            }
            else
            {
                errorMessage = "Failed to create account.";
            }
        }

        private async Task DeleteAccountAsync(int accountId)
        {
            if (selectedCustomerForAccounts == null) return;

            var response = await Http.DeleteAsync($"api/Admin/DeleteAccount/{accountId}");
            if (response.IsSuccessStatusCode)
            {
                await FetchCustomerAccountsAsync(selectedCustomerForAccounts.CustomerID);
            }
            else
            {
                errorMessage = "Failed to delete account.";
            }
        }
    }
}
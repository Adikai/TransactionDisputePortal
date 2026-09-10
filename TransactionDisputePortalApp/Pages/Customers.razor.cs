using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
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
                        c.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
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
                CustomerId = customer.CustomerId,
                FullName = customer.FullName,
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
            var (firstName, lastName) = SplitFullName(currentModel.FullName);

            if (isEditMode)
            {
                var updateDto = new UpdateCustomerDto
                {
                    CustomerID = currentModel.CustomerId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = currentModel.Email,
                    PhoneNumber = currentModel.PhoneNumber,
                    PasswordHash = string.IsNullOrWhiteSpace(plainPassword) ? null : HashPassword(plainPassword)
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
                    ? HashPassword("DefaultPassword123!")
                    : HashPassword(plainPassword);

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
                var response = await Http.DeleteAsync($"api/Admin/DeleteCustomer/{customerToDelete.CustomerId}");
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

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }
}
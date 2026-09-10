using System;
using System.Collections.Generic;
using System.Net.Mail;
using System.Text;
using TransactionDisputePortal.Shared.Models.DTO;

namespace TransactionDisputePortal.Core.Helpers
{
    public static class CustomerValidationHelper
    {
        public static List<string> ValidateCreateCustomer(CreateCustomerDto request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.FirstName))
                errors.Add("First name is required.");
            else if (request.FirstName.Length > 100)
                errors.Add("First name cannot exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(request.LastName))
                errors.Add("Last name is required.");
            else if (request.LastName.Length > 100)
                errors.Add("Last name cannot exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                errors.Add("Email address is required.");
            }
            else if (request.Email.Length > 100)
            {
                errors.Add("Email address cannot exceed 100 characters.");
            }
            else if (!IsValidEmail(request.Email))
            {
                errors.Add("Email address format is invalid.");
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber.Length > 20)
            {
                errors.Add("Phone number cannot exceed 20 characters.");
            }

            if (string.IsNullOrWhiteSpace(request.PasswordHash))
                errors.Add("Password hash is required.");
            else if (request.PasswordHash.Length > 255)
                errors.Add("Password hash cannot exceed 255 characters.");

            return errors;
        }

        public static List<string> ValidateUpdateCustomer(UpdateCustomerDto request)
        {
            var errors = new List<string>();

            if (request.CustomerID <= 0)
            {
                errors.Add("A valid Customer ID is required.");
            }

            if (string.IsNullOrWhiteSpace(request.FirstName))
                errors.Add("First name is required.");
            else if (request.FirstName.Length > 100)
                errors.Add("First name cannot exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(request.LastName))
                errors.Add("Last name is required.");
            else if (request.LastName.Length > 100)
                errors.Add("Last name cannot exceed 100 characters.");

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                errors.Add("Email address is required.");
            }
            else if (request.Email.Length > 100)
            {
                errors.Add("Email address cannot exceed 100 characters.");
            }
            else if (!IsValidEmail(request.Email))
            {
                errors.Add("Email address format is invalid.");
            }

            if (!string.IsNullOrEmpty(request.PhoneNumber) && request.PhoneNumber.Length > 20)
            {
                errors.Add("Phone number cannot exceed 20 characters.");
            }

            if (!string.IsNullOrEmpty(request.PasswordHash) && request.PasswordHash.Length > 255)
            {
                errors.Add("Password hash cannot exceed 255 characters.");
            }

            return errors;
        }

        public static bool IsValidEmail(string email)
        {
            try
            {
                var mailAddress = new MailAddress(email);
                return mailAddress.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}

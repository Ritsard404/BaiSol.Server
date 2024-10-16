﻿using BaseLibrary.DTO.Payment;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Globalization;
using System.Text.Json;

namespace BaseLibrary.Services.Repositories
{
    public class PaymentRepository(IConfiguration _config, UserManager<AppUsers> _userManager, DataContext _dataContext) : IPayment
    {
        public async Task<(bool, string)> AcknowledgePayment(AcknowledgePaymentDTO acknowledgePayment)
        {
            var payment = await _dataContext.Payment
                .Include(p => p.Project)
                .FirstOrDefaultAsync(r => r.Id == acknowledgePayment.referenceNumber);
            if (payment == null)
                return (false, "Payment not exist!");

            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(acknowledgePayment.userEmail);
            if (user == null) return (false, "Invalid user!");

            payment.IsAcknowledged = true;
            payment.AcknowledgedBy = user;
            payment.AcknowledgedAt = DateTimeOffset.UtcNow;

            _dataContext.Payment.Update(payment);
            await _dataContext.SaveChangesAsync();

            await LogUserActionAsync(
                    acknowledgePayment.userEmail,
                    "Acknowledge",
                    "Payment",
                    payment.Id,
                    $"Payment for '{acknowledgePayment.description}' associated with project '{payment.Project.ProjName}' has been successfully acknowledged by user {acknowledgePayment.userEmail}.",
                    acknowledgePayment.UserIpAddress
                );

            return (true, "Payment acknowledged successfully.");
        }

        public async Task<(bool, string)> CreatePayment(CreatePaymentDTO createPayment)
        {
            var options = new RestClientOptions(_config["Payment:API"]);
            var client = new RestClient(options);

            var project = await _dataContext.Project
                .FirstOrDefaultAsync(id => id.ProjId == createPayment.projId);
            if (project == null)
                return (false, "Project not found!");

            var payment = await _dataContext.Payment
                .FirstOrDefaultAsync(i => i.Project == project);
            if (payment != null)
                return (false, "Payments have already been created for this project.");

            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(createPayment.userEmail);
            if (user == null) return (false, "Invalid user!");



            var amount = await GetTotalProjectExpense(projId: createPayment.projId);

            // Define the payloads with different amounts and descriptions
            var payloads = new[]
            {
                new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = (amount * 0.6m) * 100,
                            description = $"60% downpayment."
                        }
                    }
                },
                new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = (amount * 0.3m) * 100,
                            description = $"30% progress payment."
                        }
                    }
                },
                new
                {
                    data = new
                    {
                        attributes = new
                        {
                            amount = (amount * 0.1m) * 100,
                            description = $"10% final payment."
                        }
                    }
                }
            };

            bool allPaymentsSuccessful = true; // Track the success status of all payments

            // Loop through each payload to attempt the payment creation
            foreach (var payload in payloads)
            {
                // Create a new request instance for each API call
                var request = new RestRequest("");

                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", $"Basic {_config["Payment:Key"]}");
                request.AddJsonBody(payload);

                var response = await client.PostAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content into a dynamic object
                    var responseData = JsonDocument.Parse(response.Content);

                    // Extract specific fields from the JSON response
                    var data = responseData.RootElement.GetProperty("data");
                    var attributes = data.GetProperty("attributes");

                    // Create a new payment entry in the database
                    var newPayment = new Payment
                    {
                        Id = attributes.GetProperty("reference_number").GetString(),
                        checkoutUrl = attributes.GetProperty("checkout_url").GetString(),
                        Project = project,
                    };

                    // Save the new payment to the database
                    _dataContext.Payment.Add(newPayment);
                    await _dataContext.SaveChangesAsync();

                    await LogUserActionAsync(
                          createPayment.userEmail,
                          "Create",
                          "Payment",
                          newPayment.Id,
                           $"Successfully created a payment of  {(attributes.GetProperty("amount").GetDecimal() / 100).ToString("#,##0.00")} for '{payload.data.attributes.description}' associated with project '{project.ProjName}'.",
                          createPayment.UserIpAddress
                      );
                }
                else
                {
                    allPaymentsSuccessful = false; // Mark as unsuccessful if any payment fails
                }
            }

            // Return a success message if all payments were successfully created
            if (allPaymentsSuccessful)
            {
                project.Status = "OnWork";
                project.UpdatedAt = DateTimeOffset.UtcNow;
                _dataContext.Project.Update(project);
                await _dataContext.SaveChangesAsync();

                return (true, "All payments successfully created.");
            }

            return (false, "One or more payments were not successfully created.");
        }

        public async Task<ICollection<GetClientPaymentDTO>> GetAllPayments()
        {
            var allPayments = await _dataContext.Payment
                .OrderByDescending(o => o.Project)
                .ToListAsync();

            var clientPayments = new List<GetClientPaymentDTO>();

            foreach (var reference in allPayments)
            {
                // Initialize variables to store the extracted data
                int amount = 0;
                string description = string.Empty;
                string status = string.Empty;
                int createdAt = 0;
                int updatedAt = 0;
                int paidAt = 0;
                int paymentFee = 0;
                string sourceType = string.Empty;

                // Create a new request instance for each API call

                var options = new RestClientOptions($"{_config["Payment:API"]}/{reference.Id}");
                var client = new RestClient(options);
                var request = new RestRequest("");

                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", $"Basic {_config["Payment:Key"]}");

                var response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content into a dynamic object
                    var responseData = JsonDocument.Parse(response.Content);

                    // Extract specific fields from the JSON response
                    var data = responseData.RootElement.GetProperty("data");
                    var attributes = data.GetProperty("attributes");

                    // Extract the required properties from attributes
                    amount = attributes.GetProperty("amount").GetInt32();
                    description = attributes.GetProperty("description").GetString();
                    status = attributes.GetProperty("status").GetString();
                    createdAt = attributes.GetProperty("created_at").GetInt32();
                    updatedAt = attributes.GetProperty("updated_at").GetInt32();

                    // Access the 'payments' property
                    var payments = attributes.GetProperty("payments");
                    // Check if the payments array has any elements

                    if (payments.ValueKind == JsonValueKind.Array && payments.GetArrayLength() > 0)
                    {
                        foreach (var payment in payments.EnumerateArray())
                        {
                            // Extract properties from each payment object
                            var paymentData = payment.GetProperty("data");
                            var paymentAttributes = paymentData.GetProperty("attributes");

                            paymentFee = paymentAttributes.GetProperty("fee").GetInt32(); // Payment status
                            sourceType = paymentAttributes.GetProperty("source").GetProperty("type").GetString(); // Payment status
                            paidAt = paymentAttributes.GetProperty("paid_at").GetInt32(); // Use appropriate property
                                                                                          //var billingEmail = paymentAttributes.GetProperty("billing").GetProperty("email").GetString(); // Example for nested billing
                                                                                          // Additional properties as needed...
                        }
                    }
                }

                clientPayments.Add(new GetClientPaymentDTO
                {
                    referenceNumber = reference.Id,
                    checkoutUrl = reference.checkoutUrl, // Ensure this is defined in the Payment model
                    IsAcknowledged = reference.IsAcknowledged,
                    AcknowledgedBy = reference.AcknowledgedBy?.Email ?? string.Empty,
                    amount = (amount / 100).ToString("#,##0.00"),
                    description = description,
                    status = status,
                    sourceType = sourceType,
                    createdAt = DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    updatedAt = DateTimeOffset.FromUnixTimeSeconds(updatedAt).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    paidAt = paidAt > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(paidAt).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        : string.Empty,
                    paymentFee = (paymentFee / 100).ToString("#,##0.00"),
                    acknowledgedAt = reference.AcknowledgedAt.HasValue
                            ? reference.AcknowledgedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                            : null,
                });

            }

            // Order the client payments by the amount in descending order
            var orderedClientPayments = clientPayments
                .OrderByDescending(cp => decimal.Parse(cp.amount, NumberStyles.Currency, CultureInfo.InvariantCulture))
                .ToList();

            return orderedClientPayments;

        }

        public async Task<ICollection<GetClientPaymentDTO>> GetClientPayments(string projId)
        {
            var project = await _dataContext.Project
                .FirstOrDefaultAsync(id => id.ProjId == projId);
            if (project == null)
                return null;

            var paymentReference = await _dataContext.Payment
                .Where(p => p.Project == project)
                .ToListAsync();


            var clientPayments = new List<GetClientPaymentDTO>();

            foreach (var reference in paymentReference)
            {
                // Initialize variables to store the extracted data
                int amount = 0;
                string description = string.Empty;
                string status = string.Empty;
                int createdAt = 0;
                int updatedAt = 0;
                int paidAt = 0;
                int paymentFee = 0;
                string sourceType = string.Empty;

                // Create a new request instance for each API call

                var options = new RestClientOptions($"{_config["Payment:API"]}/{reference.Id}");
                var client = new RestClient(options);
                var request = new RestRequest("");

                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", $"Basic {_config["Payment:Key"]}");

                var response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content into a dynamic object
                    var responseData = JsonDocument.Parse(response.Content);

                    // Extract specific fields from the JSON response
                    var data = responseData.RootElement.GetProperty("data");
                    var attributes = data.GetProperty("attributes");

                    // Extract the required properties from attributes
                    amount = attributes.GetProperty("amount").GetInt32();
                    description = attributes.GetProperty("description").GetString();
                    status = attributes.GetProperty("status").GetString();
                    createdAt = attributes.GetProperty("created_at").GetInt32();
                    updatedAt = attributes.GetProperty("updated_at").GetInt32();

                    // Access the 'payments' property
                    var payments = attributes.GetProperty("payments");
                    // Check if the payments array has any elements

                    if (payments.ValueKind == JsonValueKind.Array && payments.GetArrayLength() > 0)
                    {
                        foreach (var payment in payments.EnumerateArray())
                        {
                            // Extract properties from each payment object
                            var paymentData = payment.GetProperty("data");
                            var paymentAttributes = paymentData.GetProperty("attributes");

                            paymentFee = paymentAttributes.GetProperty("fee").GetInt32(); // Payment status
                            sourceType = paymentAttributes.GetProperty("source").GetProperty("type").GetString(); // Payment status
                            paidAt = paymentAttributes.GetProperty("paid_at").GetInt32(); // Use appropriate property
                                                                                          //var billingEmail = paymentAttributes.GetProperty("billing").GetProperty("email").GetString(); // Example for nested billing
                                                                                          // Additional properties as needed...
                        }
                    }
                }

                clientPayments.Add(new GetClientPaymentDTO
                {
                    referenceNumber = reference.Id,
                    checkoutUrl = reference.checkoutUrl, // Ensure this is defined in the Payment model
                    IsAcknowledged = reference.IsAcknowledged,
                    AcknowledgedBy = reference.AcknowledgedBy?.Email ?? string.Empty,
                    amount = (amount / 100).ToString("#,##0.00"),
                    description = description,
                    status = status,
                    sourceType = sourceType,
                    createdAt = DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    updatedAt = DateTimeOffset.FromUnixTimeSeconds(updatedAt).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    paidAt = paidAt > 0
                        ? DateTimeOffset.FromUnixTimeSeconds(paidAt).UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss")
                        : string.Empty,
                    paymentFee = (paymentFee / 100).ToString("#,##0.00"),
                    acknowledgedAt = reference.AcknowledgedAt.HasValue
                            ? reference.AcknowledgedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                            : null,

                });

            }

            // Order the client payments by the amount in descending order
            var orderedClientPayments = clientPayments
                .OrderByDescending(cp => decimal.Parse(cp.amount, NumberStyles.Currency, CultureInfo.InvariantCulture))
                .ToList();

            return orderedClientPayments;

        }

        public async Task<bool> IsProjectPayedDownpayment(string projId)
        {
            var project = await _dataContext.Project
                .FirstOrDefaultAsync(id => id.ProjId == projId);
            if (project == null)
                return false;

            var paymentReferences = await _dataContext.Payment
             .Where(p => p.Project == project)
             .ToListAsync();

            var totalAmount = await GetTotalProjectExpense(projId: projId);

            string status = string.Empty;

            foreach (var reference in paymentReferences)
            {
                var options = new RestClientOptions($"{_config["Payment:API"]}/{reference.Id}");
                var client = new RestClient(options);
                var request = new RestRequest("");

                request.AddHeader("accept", "application/json");
                request.AddHeader("authorization", $"Basic {_config["Payment:Key"]}");

                var response = await client.GetAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseData = JsonDocument.Parse(response.Content);
                    var data = responseData.RootElement.GetProperty("data");
                    var attributes = data.GetProperty("attributes");

                    decimal amount = attributes.GetProperty("amount").GetDecimal() / 100m;
                    string currentStatus = attributes.GetProperty("status").GetString();

                    // Update largest amount and status if a larger amount is found
                    if (amount == (totalAmount * 0.6m))
                    {
                        status = currentStatus;
                    }
                }
            }

            if (status != "paid")
                return false;

            return true;
        }

        private async Task<decimal> GetTotalProjectExpense(string projId)
        {
            if (string.IsNullOrEmpty(projId))
                return 0;

            // Fetch material supply for the project
            var materialSupply = await _dataContext.Supply
                .Include(i => i.Material)
                .Where(p => p.Project.ProjId == projId)
                .ToListAsync();

            // Calculate total unit cost
            var totalUnitCost = materialSupply
                .Where(m => m.Material != null)
                .Sum(m => (m.MTLQuantity ?? 0) * m.Material.MTLPrice);

            // Calculate profit and material total
            var profitRate = materialSupply.Select(p => p.Project.ProfitRate).FirstOrDefault();
            var overallMaterialTotal = totalUnitCost * (1 + profitRate);

            // Calculate overall labor cost
            var overallLaborTotal = await _dataContext.Labor
                .Where(p => p.Project.ProjId == projId)
                .SumAsync(o => o.LaborCost) * (1 + profitRate);

            // Fetch discount and VAT rates
            var project = await _dataContext.Project
                .Where(p => p.ProjId == projId)
                .Select(p => new { Discount = p.Discount ?? 0, VatRate = p.VatRate ?? 0 })
                .FirstOrDefaultAsync();

            // Calculate final total with discount and VAT
            var subtotal = overallMaterialTotal + overallLaborTotal - project.Discount;
            var finalTotal = subtotal * (1 + project.VatRate);

            return (finalTotal);
        }

        private async Task<bool> LogUserActionAsync(string userEmail, string action, string entityName, string entityId, string details, string userIpAddress)
        {
            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null) return false;

            // Fetch the user's roles
            var userRole = await _userManager.GetRolesAsync(user);

            // Log the action
            var logs = new UserLogs
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserIPAddress = userIpAddress,
                Details = details,
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };

            // Add the logs to the database
            _dataContext.UserLogs.Add(logs);

            // Save changes to the database (assuming you have a Save method)

            await _dataContext.SaveChangesAsync();

            return true;
        }
    }
}

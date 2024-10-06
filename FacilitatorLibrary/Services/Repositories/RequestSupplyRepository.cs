using DataLibrary.Data;
using DataLibrary.Models;
using FacilitatorLibrary.DTO.Request;
using FacilitatorLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.Services.Repositories
{
    public class RequestSupplyRepository(DataContext _dataContext, UserManager<AppUsers> _userManager) : IRequestSupply
    {
        public async Task<(bool, string)> AcknowledgeRequest(AcknowledgeRequestDTO acknowledgeRequest)
        {
            if (acknowledgeRequest.reqId == null)
                return (false, "Empty requested supply!");

            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(acknowledgeRequest.userEmail);
            if (user == null) return (false, "Invalid user");

            //var userRole = await _userManager.GetRolesAsync(user);
            //if (!userRole.Contains("Facilitator")) 
            //    return (false, "Invalid user");

            // Retrieve all the requisitions that match the provided IDs
            var requests = await _dataContext.Requisition
                .Where(r => acknowledgeRequest.reqId.Contains(r.ReqId))
                .Include(m => m.RequestSupply)
                .ThenInclude(rs => rs.Material)
                .Include(r => r.RequestSupply)
                .ThenInclude(rs => rs.Equipment)
                .ToListAsync();

            // Check if the found requests are empty
            if (!requests.Any())
                return (false, "No valid requests found!");

            var missingIds = acknowledgeRequest.reqId.Except(requests.Select(r => r.ReqId)).ToList();
            if (missingIds.Any())
                return (false, $"Missing request IDs: {string.Join(", ", missingIds)}");

            // Validate if any requests have QuantityRequested <= 0
            var invalidRequests = requests.Where(r => r.QuantityRequested <= 0).ToList();
            if (invalidRequests.Any())
                return (false, "Quantity Requested cannot be 0 or below.");

            // **Validation: Check if any requests have status "OnReview"**
            var requestsOnReview = requests.Where(r => r.Status != "Approved").ToList();
            if (requestsOnReview.Any())
                return (false, "Some requests are still on review and cannot be acknowledged.");

            // Approve requests and adjust supplies
            foreach (var request in requests)
            {
                request.Status = "Acknowledged";

                var supply = await _dataContext.Supply
                    .FirstOrDefaultAsync(s => s.SuppId == request.RequestSupply.SuppId);

                // Adjust material quantities
                if (request.RequestSupply.Material != null)
                {
                    //request.RequestSupply.Material.MTLQOH -= request.QuantityRequested;
                    //request.RequestSupply.Material.UpdatedAt = DateTimeOffset.UtcNow;
                    supply!.MTLQuantity += request.QuantityRequested;


                    await LogUserActionAsync(
                        acknowledgeRequest.userEmail,
                        "Update",
                        "Supply",
                        request.RequestSupply.SuppId.ToString(),
                        $"Updated the supply quantity because of the acknowledge requests supply, added by {request.QuantityRequested}",
                        acknowledgeRequest.UserIpAddress
                    );
                }

                // Adjust supply quantities
                if (request.RequestSupply.Equipment != null)
                {
                    //request.RequestSupply.Equipment.EQPTQOH -= request.QuantityRequested;
                    //request.RequestSupply.Equipment.UpdatedAt = DateTimeOffset.UtcNow;
                    supply!.EQPTQuantity += request.QuantityRequested;


                    await LogUserActionAsync(
                        acknowledgeRequest.userEmail,
                        "Update",
                        "Supply",
                        request.RequestSupply.SuppId.ToString(),
                        $"Updated the supply quantity because of the acknowledge requests supply, added by {request.QuantityRequested}",
                        acknowledgeRequest.UserIpAddress
                    );
                }



                await LogUserActionAsync(
                    acknowledgeRequest.userEmail,
                    "Update",
                    "Requisition",
                    request.ReqId.ToString(),
                    "Request acknowledge by the facilitator.",
                    acknowledgeRequest.UserIpAddress
                );
            }

            await _dataContext.SaveChangesAsync();
            return (true, "Request(s) successfully acknowledge!");
        }

        public async Task<List<AvailableRequestSupplies>> RequestSupplies(string userEmail, string supplyCtgry)
        {
            // Retrieve the assigned facilitator's project information based on the user email
            var assignedFacilitator = await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status == "OnGoing")
                .Select(e => e.Project.ProjId) // Only select the project ID
                .FirstOrDefaultAsync();

            // If no assigned facilitator is found, return an empty list
            if (assignedFacilitator == null)
                return new List<AvailableRequestSupplies>();

            var supplies = await _dataContext.Supply
                .Where(p => p.Project.ProjId == assignedFacilitator
                             && !_dataContext.Requisition.Any(r => r.RequestSupply.SuppId == p.SuppId && (r.Status == "OnReview" || r.Status == "Approved"))
                             && (supplyCtgry.Equals("Material", StringComparison.OrdinalIgnoreCase) ? p.Material != null : true)
                             && (supplyCtgry.Equals("Equipment", StringComparison.OrdinalIgnoreCase) ? p.Equipment != null : true))
                .Select(s => new AvailableRequestSupplies
                {
                    suppId = s.SuppId,
                    supplyName = supplyCtgry.Equals("Material", StringComparison.OrdinalIgnoreCase)
                        ? s.Material.MTLDescript
                        : s.Equipment.EQPTDescript
                })
                .ToListAsync();

            return supplies;
        }

        public async Task<List<RequestsDTO>> SentRequestByProj(string userEmail)
        {
            // Retrieve the assigned facilitator's project information based on the user email
            var assignedFacilitator = await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status == "OnGoing")
                .Select(e => e.Project.ProjId) // Only select the project ID
                .FirstOrDefaultAsync();

            // If no assigned facilitator is found, return an empty list
            if (assignedFacilitator == null)
                return new List<RequestsDTO>();

            return await _dataContext.Requisition
                .Where(id => id.RequestSupply.Project.ProjId == assignedFacilitator)
                .OrderByDescending(d => d.Status)
                .ThenBy(d => d.SubmittedAt)
                .ThenBy(m => m.RequestSupply.Material)
                .ThenBy(m => m.RequestSupply.Material.MTLCategory)
                .ThenBy(m => m.RequestSupply.Equipment)
                .ThenBy(m => m.RequestSupply.Equipment.EQPTCategory)
                .Select(r => new RequestsDTO
                {
                    ReqId = r.ReqId,
                    SubmittedAt = r.SubmittedAt.ToString("MMM dd, yyyy HH:mm:ss"), // Formatting SubmittedAt
                    ReviewedAt = r.ReviewedAt.HasValue
                        ? r.ReviewedAt.Value.ToString("MMM dd, yyyy HH:mm:ss")    // Formatting ReviewedAt if it has a value
                        : "",                                                      // Empty string if null
                    Status = r.Status,
                    QuantityRequested = r.QuantityRequested,
                    RequestSupply = r.RequestSupply.Material.MTLDescript ?? r.RequestSupply.Equipment.EQPTDescript,
                    ProjectName = r.RequestSupply.Project.ProjName,
                    SupplyCategory = r.RequestSupply.Material != null ? "Material" : "Equipment",
                    SubmittedBy = r.SubmittedBy.Email,
                    ReviewedBy = r.ReviewedBy != null
                        ? r.ReviewedBy.Email
                        : ""
                })
                .ToListAsync();
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

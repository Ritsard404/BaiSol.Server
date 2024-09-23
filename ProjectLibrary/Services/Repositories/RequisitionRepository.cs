using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectLibrary.DTO.Requisition;
using ProjectLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.Services.Repositories
{
    public class RequisitionRepository(DataContext _dataContext, UserManager<AppUsers> _userManager) : IRequisition
    {
        public async Task<List<Requisition>> AllRequest()
        {
            return await _dataContext.Requisition
                .OrderBy(p => p.RequestSupply.Project)
                .ThenBy(d => d.SubmittedAt)
                .ThenBy(m => m.RequestSupply.Material)
                .ThenBy(m => m.RequestSupply.Material.MTLCategory)
                .ThenBy(m => m.RequestSupply.Equipment)
                .ThenBy(m => m.RequestSupply.Equipment.EQPTCategory)
                .ToListAsync();
        }

        public async Task<(bool, string)> ApproveRequest(StatusRequestDTO approveRequest)
        {
            if (approveRequest.reqId == null)
                return (false, "Empty requested supply!");

            // Retrieve all the requisitions that match the provided IDs
            var requests = await _dataContext.Requisition
                .Where(r => approveRequest.reqId.Contains(r.ReqId))
                .Include(m => m.RequestSupply)
                .ToListAsync();

            // Check if the found requests are empty
            if (!requests.Any())
                return (false, "No valid requests found!");

            var missingIds = approveRequest.reqId.Except(requests.Select(r => r.ReqId)).ToList();
            if (missingIds.Any())
                return (false, $"Missing request IDs: {string.Join(", ", missingIds)}");

            // Approve requests and adjust supplies
            foreach (var request in requests)
            {
                request.Status = "Approved";
                var supply = await _dataContext.Supply
                    .FirstOrDefaultAsync(s => s.SuppId == request.RequestSupply.SuppId);

                // Adjust material quantities
                if (request.RequestSupply.Material != null)
                {
                    request.RequestSupply.Material.MTLQOH -= request.QuantityRequested;
                    request.RequestSupply.Material.UpdatedAt = DateTimeOffset.UtcNow;
                    supply!.MTLQuantity += request.QuantityRequested;

                    await LogUserActionAsync(
                        approveRequest.userEmail,
                        "Update",
                        "Material",
                        request.RequestSupply.Material.MTLId.ToString(),
                        $"Updated the quantity because of the approved requests supply, subtracted by {request.QuantityRequested}",
                        approveRequest.UserIpAddress
                    );

                    await LogUserActionAsync(
                        approveRequest.userEmail,
                        "Update",
                        "Supply",
                        request.RequestSupply.SuppId.ToString(),
                        $"Updated the material quantity because of the approved requests supply, added by {request.QuantityRequested}",
                        approveRequest.UserIpAddress
                    );
                }

                // Adjust equipment quantities
                if (request.RequestSupply.Equipment != null)
                {
                    request.RequestSupply.Equipment.EQPTQOH -= request.QuantityRequested;
                    request.RequestSupply.Equipment.UpdatedAt = DateTimeOffset.UtcNow;
                    supply!.EQPTQuantity += request.QuantityRequested;

                    await LogUserActionAsync(
                        approveRequest.userEmail,
                        "Update",
                        "Equipment",
                        request.RequestSupply.Equipment.EQPTId.ToString(),
                        $"Updated the quantity because of the approved requests supply, subtracted by {request.QuantityRequested}",
                        approveRequest.UserIpAddress
                    );

                    await LogUserActionAsync(
                        approveRequest.userEmail,
                        "Update",
                        "Supply",
                        request.RequestSupply.SuppId.ToString(),
                        $"Updated the equipment quantity because of the approved requests supply, added by {request.QuantityRequested}",
                        approveRequest.UserIpAddress
                    );
                }



                await LogUserActionAsync(
                    approveRequest.userEmail,
                    "Update",
                    "Requisition",
                    request.ReqId.ToString(),
                    "Request approved by the admin.",
                    approveRequest.UserIpAddress
                );
            }

            await _dataContext.SaveChangesAsync();
            return (true, "Request(s) successfully approved!");
        }

        public async Task<(bool, string)> DeclineRequest(StatusRequestDTO declineRequest)
        {
            if (declineRequest.reqId == null)
                return (false, "Empty requested supply!");

            // Retrieve all the requisitions that match the provided IDs
            var requests = await _dataContext.Requisition
                .Where(r => declineRequest.reqId.Contains(r.ReqId))
                .Include(m => m.RequestSupply)
                .ToListAsync();

            // Check if the found requests are empty
            if (!requests.Any())
                return (false, "No valid requests found!");

            var missingIds = declineRequest.reqId.Except(requests.Select(r => r.ReqId)).ToList();
            if (missingIds.Any())
                return (false, $"Missing request IDs: {string.Join(", ", missingIds)}");

            requests.ForEach(s => s.Status = "Declined");

            foreach (var request in requests)
            {
                request.Status = "Declined";

                await LogUserActionAsync(
                    declineRequest.userEmail,
                    "Update",
                    "Requisition",
                    request.ReqId.ToString(),
                    "Request declined by the admin.",
                    declineRequest.UserIpAddress
                );
            }

            await _dataContext.SaveChangesAsync();
            return (true, "Request(s) successfully declined!");
        }

        public async Task<(bool, string)> DeleteRequest(DeleteRequest deleteRequest)
        {

            // Retrieve all the requisitions that match the provided IDs
            var request = await _dataContext.Requisition
                .FirstOrDefaultAsync(i => i.ReqId == deleteRequest.reqId);

            // Check if the found request are empty
            if (request == null)
                return (false, "No valid request found!");

            _dataContext.Requisition.Remove(request);
            await _dataContext.SaveChangesAsync();

            await LogUserActionAsync(
                deleteRequest.userEmail,
                "Delete",
                "Requisition",
                request.ReqId.ToString(),
                "Request deleted by the facilitator.",
                deleteRequest.UserIpAddress
            );

            return (true, "Requested supply deleted!");
        }

        public async Task<List<AvailableRequestSupplies>> RequestSupplies(string projId, string supplyCtgry)
        {
            var supplies = await _dataContext.Supply
                .Where(p => p.Project.ProjId == projId
                             && !_dataContext.Requisition.Any(r => r.RequestSupply.SuppId == p.SuppId)
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

        public async Task<(bool, string)> RequestSupply(AddRequestDTO addRequest)
        {
            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(addRequest.SubmittedBy);
            if (user == null) return (false, "User does not exist.");

            foreach (var detail in addRequest.RequestDetails)
            {
                var supply = await _dataContext.Supply
                    .FirstOrDefaultAsync(i => i.SuppId == detail.SuppId);

                // Check if supply is found
                if (supply == null)
                    return (false, $"Supply with ID {detail.SuppId} not found.");

                var request = new Requisition
                {
                    QuantityRequested = detail.QuantityRequested,
                    RequestSupply = supply,
                    SubmittedBy = user
                };

                await _dataContext.Requisition.AddAsync(request);

                await LogUserActionAsync(
                    addRequest.SubmittedBy,
                    "Request",
                    "Requisition",
                    request.ReqId.ToString(),
                    $"New supply request for the project {supply.Project.ProjId} sent to the admin.",
                    addRequest.UserIpAddress
                );
            }

            await _dataContext.SaveChangesAsync();
            return (true, "Request successfully sent!");
        }

        public async Task<List<Requisition>> SentRequestByProj(string projId)
        {
            return await _dataContext.Requisition
                .Where(id => id.RequestSupply.Project.ProjId == projId)
                .OrderBy(d => d.SubmittedAt)
                .ThenBy(m => m.RequestSupply.Material)
                .ThenBy(m => m.RequestSupply.Material.MTLCategory)
                .ThenBy(m => m.RequestSupply.Equipment)
                .ThenBy(m => m.RequestSupply.Equipment.EQPTCategory)
                .ToListAsync();
        }

        public async Task<(bool, string)> UpdateRequest(UpdateQuantity updateQuantity)
        {

            // Retrieve all the requisitions that match the provided IDs
            var request = await _dataContext.Requisition
                .FirstOrDefaultAsync(i => i.ReqId == updateQuantity.reqId);

            // Check if the found request are empty
            if (request == null)
                return (false, "No valid request found!");

            if (updateQuantity.newQuantity < 0)
                return (false, "Invalid Quantity!");

            var oldQty = request.QuantityRequested;
            request.QuantityRequested = updateQuantity.newQuantity;

            await _dataContext.SaveChangesAsync();

            await LogUserActionAsync(
                updateQuantity.userEmail,
                "Update",
                "Requisition",
                request.ReqId.ToString(),
                $"Request updated quantity by the facilitator. From {oldQty} to {updateQuantity.newQuantity}",
                updateQuantity.UserIpAddress
            );


            return (true, "Request quantity updated successfully!");
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

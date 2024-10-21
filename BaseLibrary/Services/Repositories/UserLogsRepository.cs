using BaseLibrary.DTO.UserLogs;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Repositories
{
    public class UserLogsRepository(DataContext _dataContext, UserManager<AppUsers> _userManager) : IUserLogs
    {
        public async Task<ICollection<InventoryLogs>> GetInventoryLogs(string supplyCategory, string id)
        {
            var logs = await _dataContext.UserLogs
                .Include(u => u.User)
                .Where(s => s.EntityName.Equals(supplyCategory) && s.EntityId.Equals(id))
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();

            // Step 2: Format the Timestamp and create the InventoryLogs collection
            var inventoryLogs = logs.Select(log => new InventoryLogs
            {
                Action = log.Action,
                Details = log.Details,
                EntityName = log.EntityName,
                UserIPAddress = log.UserIPAddress,
                UserEmail = log.UserName,
                UserName = log.User.UserName.Replace('_', ' '),
                // Format Timestamp after retrieval
                Timestamp = log.Timestamp.ToString("MMM dd, yyyy HH:mm:ss"),
            }).ToList();

            return inventoryLogs;
        }

        public async Task<ICollection<AllLogsDTO>> GetActivityLogs()
        {
            var logs = await _dataContext.UserLogs
                .Include(u => u.User)
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();


            return logs.Select(l => new AllLogsDTO
            {
                LogId = l.LogId,
                Action = l.Action,
                Details = l.Details,
                EntityName = l.EntityName,
                UserRole = l.UserRole,
                UserName = l.User.UserName.Replace('_', ' '),
                UserEmail = l.UserName,
                UserIPAddress = l.UserIPAddress,
                Timestamp = l.Timestamp.ToString("MMM dd, yyyy HH:mm:ss"),
            }).ToList();
        }

        public async Task<bool> LogUserActionAsync(string userEmail, string action, string entityName, string entityId, string details, string userIpAddress)
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

﻿using BaseLibrary.DTO.UserLogs;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Repositories
{
    public class UserLogsRepository(DataContext _dataContext) : IUserLogs
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
    }
}

﻿using BaseLibrary.DTO.UserLogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Interfaces
{
    public interface IUserLogs
    {
        Task<ICollection<InventoryLogs>> GetInventoryLogs(string supplyCategory, string id);
        Task<ICollection<AllLogsDTO>> GetActivityLogs();    
    }
}

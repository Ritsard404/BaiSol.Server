using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.UserLogs
{
    public class AllLogsDTO
    {
        public int LogId { get; set; }
        public required string Action { get; set; } // e.g., Create, Read, Update, Delete

        public required string EntityName { get; set; } // Name of the affected entity/table

        public required string UserIPAddress { get; set; } // IP address of the user

        public required string Details { get; set; }
        public required string UserName { get; set; } // Name of the user performing the action
        public required string UserEmail { get; set; } // Name of the user performing the action
        public required string UserRole { get; set; } // Name of the user performing the action
        public required string Timestamp { get; set; }
    }
}

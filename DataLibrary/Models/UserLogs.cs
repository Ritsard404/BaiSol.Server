using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class UserLogs
    {
        [Key]
        public int LogId { get; set; }

        public required string Action { get; set; } // e.g., Create, Read, Update, Delete

        public required string EntityName { get; set; } // Name of the affected entity/table
        public required string EntityId { get; set; } // ID of the affected entity

        public required string UserIPAddress { get; set; } // IP address of the user

        // Additional details or JSON representation of the changes made
        public required string Details { get; set; }

        // Reference to the user who performed the action
        public required string UserId { get; set; } // ID of the user performing the action
        public required string UserName { get; set; } // Name of the user performing the action
        public required string UserRole { get; set; } // Name of the user performing the action
        public required AppUsers User { get; set; } // Navigation property to the AppUsers model

        // Timestamp of when the action occurred
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    }
}

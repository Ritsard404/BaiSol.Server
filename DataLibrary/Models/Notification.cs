using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class Notification
    {
        [Key]
        public int NotifId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required string Type { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;
        public bool isRead { get; set; } = false;
        public Project? Project { get; set; }
    }
}

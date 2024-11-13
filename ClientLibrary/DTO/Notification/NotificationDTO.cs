using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.DTO.Notification
{
    public class NotificationDTO
    {
        public int NotifId { get; set; }
        public required string Title { get; set; }
        public required string Message { get; set; }
        public required string Type { get; set; }
        public required string CreatedAt { get; set; }
        public bool isRead { get; set; }

        public required string FacilitatorName { get; set; }
        public required string FacilitatorEmail { get; set; }

    }
}

using ClientLibrary.DTO.CLientProjectDTOS;
using ClientLibrary.DTO.Notification;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Services.Interfaces
{
    public interface IClientProject
    {
        Task<ProjectId> GetClientProject(string userEmail);

        Task<ICollection<ClientProjectHistoryDTO>> GetClientProjectHistory(string userEmail);

        Task<ICollection<NotificationDTO>> NotificationMessages(string userEmail);
        Task<NotificationDTO> NotificationMessage(string userEmail);
        Task ReadNotif(int notifId);
    }
}

using ClientLibrary.DTO.CLientProjectDTOS;
using ClientLibrary.DTO.Notification;
using ProjectLibrary.DTO.Project;

namespace ClientLibrary.Services.Interfaces
{
    public interface IClientProject
    {
        Task<ProjectId> GetClientProject(string userEmail);

        Task<ICollection<ClientProjectHistoryDTO>> GetClientProjectHistory(string userEmail);

        Task<ICollection<NotificationDTO>> NotificationMessages(string userEmail);
        Task<NotificationDTO> NotificationMessage(int notifId);
        Task ReadNotif(int notifId);


        Task<(bool, string)> ApproveProjectQuotation(UpdateProjectStatusDTO approveProjectQuotation);

        Task<bool> IsProjectApprovedQuotation(string projId);
    }
}

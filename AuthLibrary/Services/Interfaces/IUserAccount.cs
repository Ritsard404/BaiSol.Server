using AuthLibrary.DTO;
using DataLibrary.Models;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Interfaces
{
    public interface IUserAccount
    {
        Task<RegisterResponse> CreateAdminAccount(AdminDto adminDto);
        Task<RegisterResponse> CreateFacilitatorAccount(FacilitatorDto facilitatorDto);
        Task<RegisterResponse> CreateClientAccount(ClientDto clientDto);
        Task<ApprovalResponse> ApproveClient(string clientId);
        void CreateClient(Client client, Project project);
        Task EnsureRoleExists(string roleName);
        Task<ICollection<UsersDto>> GetUsersAsync();
        Task<ICollection<AdminUsersDto>> GetAdminUsers();
        Task<ICollection<UsersDto>> GetUsersByRole(string role);
        Task<ICollection<AvailableClients>> GetAvailableClients();
        Task<bool> SuspendUser(string id);
        Task<bool> DeactivateUser(string id);
        Task<bool> ActivateUser(string id);
        Task<bool> Save();
    }
}

﻿using AuthLibrary.DTO;
using DataLibrary.Models;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Interfaces
{
    public interface IUserAccount
    {
        Task<RegisterResponse> CreateAdminAccount(AdminDto adminDto);
        Task<RegisterResponse> CreateFacilitatorAccount(FacilitatorDto facilitatorDto);
        Task<RegisterResponse> CreateClientAccount(ClientDto clientDto);
        void CreateClient(Client client);
        Task EnsureRoleExists(string roleName);
        Task<ICollection<UsersDto>> GetUsersAsync();
        Task<ICollection<AdminUsersDto>> GetAdminUsers();
        Task<ICollection<UsersDto>> GetUsersByRole(string role);
        Task<bool> SuspendUser(string id);
        Task<bool> DeactivateUser(string id);
        Task<bool> ActivateUser(string id);
        Task<bool> Save();
    }
}

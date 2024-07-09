using AuthLibrary.DTO;
using AuthLibrary.Models;
using System.Security.Claims;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Interfaces
{
    public interface IUserAccount
    {
        Task<RegisterResponse> CreateAdminAccount(AdminDto adminDto);
        Task<GeneralResponse> LoginAccount(LoginDto loginDto);
        Task<LoginResponse> Login2FA(string code, string email);
        Task<LoginResponse> RefreshToken(Token token);
        Task<bool> IsUserExist(string email);
        Task<bool> SuspendUser(string id);
        Task<bool> UnSuspendUser(string id);
        Task<bool> DeactivateUser(string id);
        Task<bool> ActivateUser(string id);
        Task<bool> Save();
        Task EnsureRoleExists(string roleName);
        string GenerateAccessToken(UserSession user); 
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}

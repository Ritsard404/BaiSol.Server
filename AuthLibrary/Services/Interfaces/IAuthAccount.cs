using AuthLibrary.DTO;
using AuthLibrary.Models;
using System.Security.Claims;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Interfaces
{
    public interface IAuthAccount
    {
        Task<GeneralResponse> LoginAccount(LoginDto loginDto,bool? isMobile);
        Task<LoginResponse> Login2FA(string code, string email, string ipAddress);
        Task<(bool,string)> LogOut(string email, string ipAddress);
        Task<LoginResponse> RefreshToken(Token token);
        Task<bool> ResendOTP(string email);
        Task<bool> IsUserExist(string email);
        Task<bool> IsUserSuspend(string id);
        Task<bool> IsUserActive(string id);
        string GenerateAccessToken(UserSession user); 
        string GenerateRefreshToken();
        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
    }
}

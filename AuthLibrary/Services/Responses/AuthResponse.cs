using DataLibrary.Models;

namespace AuthLibrary.Services.Responses
{
    public class AuthResponse
    {
        public record class GeneralResponse(string Message, bool Flag, bool isDefaultAdmin, string AccessToken, string RefreshToken);
        public record class RegisterResponse(string Message, bool Flag, AppUsers AppUsers);
        public record class ApprovalResponse(bool Flag, AppUsers ClientUser);
        public record class LoginResponse(string AccessToken, string RefreshToken, string Message);

    }
}

using DataLibrary.Models;

namespace AuthLibrary.Services.Responses
{
    public class AuthResponse
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }

        public record class GeneralResponse(string Message, bool Flag);
        public record class RegisterResponse(string Message, bool Flag, AppUsers AppUsers);
        public record class LoginResponse(string AccessToken, string RefreshToken, string Message);

    }
}

using Microsoft.AspNetCore.Identity;

namespace DataLibrary.Models
{
    public class AppUsers: IdentityUser
    {
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsSuspend { get; set; } = false;
    }
}

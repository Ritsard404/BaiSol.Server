﻿using Microsoft.AspNetCore.Identity;

namespace DataLibrary.Models
{
    public class AppUsers: IdentityUser
    {
        public string? RefreshToken { get; set; }
        public string Status { get; set; } = "Active";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public string? AdminEmail { get; set; }
        public Client? Client { get; set; }
        public virtual ICollection<Installer>? Installer { get; set; }


    }
}

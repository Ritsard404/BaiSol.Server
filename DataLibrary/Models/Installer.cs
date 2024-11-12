

using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Installer
    {
        [Key]
        public int InstallerId { get; set; }
        public required string Name { get; set; }
        public required string Position { get; set; }
        public required string Status { get; set; } = "Active";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public  AppUsers? Admin { get; set; }
    }
}

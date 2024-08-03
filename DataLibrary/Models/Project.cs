
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Project
    {
        [Key]
        public int ProjId { get; set; }
        public required string ProjName { get; set; }
        public string Status { get; set; } = "OnGoing";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public Labor? Labor { get; set; }
        public virtual ICollection<AppUsers>? Client { get; set; }
    }
}

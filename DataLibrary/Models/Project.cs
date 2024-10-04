
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Project
    {
        [Key]
        public string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public string Status { get; set; } = "OnGoing";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public decimal? DiscountRate { get; set; }
        public decimal? VatRate { get; set; }
        public AppUsers? Client { get; set; }

        public Project()
        {
            ProjId = Guid.NewGuid().ToString();
        }
    }

}

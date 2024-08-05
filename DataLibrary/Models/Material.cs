using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Material
    {
        [Key]
        public int MTLId { get; set; }
        public string MTLCode { get; set; }
        public required string MTLDescript { get; set; }
        public decimal MTLPrice { get; set; }
        public int MTLQOH { get; set; }
        public required string MTLUnit { get; set; }
        public required string MTLCategory { get; set; }
        public string MTLStatus { get; set; } = "Good";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public Material()
        {
            MTLCode = Guid.NewGuid().ToString();
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Material
    {
        [Key]
        public int MTLId { get; set; }
        public required string MTLCode { get; set; }
        public required string MTLDescript { get; set; }
        public decimal MTLPrice { get; set; }
        public int MTLQOH { get; set; }
        public required string MTLUnit { get; set; }
        public required string MTLStatus { get; set; } = "Good";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int MyProperty { get; set; }
        public Supply? Supply { get; set; }
    }
}

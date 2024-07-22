
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Equipment
    {
        [Key]
        public int EQPTId { get; set; }
        public required string EQPTCode { get; set; }
        public required string EQPTDescript { get; set; }
        public decimal EQPTPrice { get; set; }
        public int EQPTQOH { get; set; }
        public required string EQPTUnit { get; set; }
        public required string EQPTStatus { get; set; } = "Good";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public int MyProperty { get; set; }
        public Supply? Supply { get; set; }
    }
}

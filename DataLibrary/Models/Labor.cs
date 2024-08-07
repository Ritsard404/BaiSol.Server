using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Labor
    {
        [Key]
        public int LaborId { get; set; }
        public required string LaborDescript { get; set; }
        public int LaborQuantity { get; set; }
        public required string LaborUnit { get; set; }
        public decimal LaborUnitCost { get; set; }
        public int LaborNumUnit { get; set; }
        public decimal LaborCost { get; set; }
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public Project? Project { get; set; }
    }
}

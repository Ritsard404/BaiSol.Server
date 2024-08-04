
namespace ProjectLibrary.DTO.Quote
{
    public class MaterialCostDto
    {
        public required string Description { get; set; }
        public int? Quantity { get; set; }
        public required string  Unit { get; set; }
        public required string Category { get; set; }
        public required decimal UnitCost { get; set; }
        public required decimal TotalUnitCost { get; set; }
        public required decimal BuildUpCost { get; set; }

    }
}

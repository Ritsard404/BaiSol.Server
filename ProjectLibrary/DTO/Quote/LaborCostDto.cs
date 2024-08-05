
using DataLibrary.Models;

namespace ProjectLibrary.DTO.Quote
{
    public class LaborCostDto
    {
        public int LaborId { get; set; }
        public required string Description { get; set; }
        public required int Quantity { get; set; }
        public required string Unit { get; set; }
        public required decimal UnitCost { get; set; }
        public required int UnitNum { get; set; }
        public required decimal TotalCost { get; set; }
    }
}

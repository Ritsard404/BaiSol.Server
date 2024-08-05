

namespace ProjectLibrary.DTO.Quote
{
    public class TotalLaborCostDto
    {
        public required decimal TotalCost { get; set; }
        public decimal ProfitPercentage { get; set; } = 1.3m;
        public required decimal Profit { get; set; }
        public required decimal OverallLaborProjectTotal { get; set; }
    }
}

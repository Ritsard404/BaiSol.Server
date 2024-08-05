

namespace ProjectLibrary.DTO.Quote
{
    public class ProjectCostDto
    {
        public decimal? TotalCost { get; set; }
        public decimal? ProfitPercentage { get; set; } = 1.3m;
        public decimal? Profit { get; set; }
        public decimal? OverallMaterialTotal { get; set; }
        public decimal? OverallProjMgtCost { get; set; }
        public decimal? NetMeteringCost { get; set; }
        public decimal? TotalProjectCost { get; set; }
    }
}

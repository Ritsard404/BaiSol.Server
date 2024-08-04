

namespace ProjectLibrary.DTO.Quote
{
    public class ProjectCostDto
    {
        public required decimal TotalCost { get; set; }
        public required decimal ProfitPercentage { get; set; } = 1.3m;
        public required decimal Profit { get; set; }
        public required decimal OverallMaterialTotal { get; set; }
        public required decimal OverallProjMgtCost { get; set; }
        public required decimal TotalProjectCost { get; set; }
    }
}

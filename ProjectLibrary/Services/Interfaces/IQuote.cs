
using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IQuote
    {
        Task<string> AddNewMaterialSupply(MaterialQuoteDto materialQuoteDto);
        Task<string> AddNewClientProject(ProjectDto projectDto);
        Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(string? projectID);
        Task<ICollection<ProjectCostDto>> GetProjectTotalCostQuote(string? projectID);
        Task<string> AddNewLaborCost(LaborQuoteDto laborQuoteDto);
        Task<ICollection<LaborCostDto>> GetLaborCostQuote(string? projectID);
        Task<ICollection<TotalLaborCostDto>> GetTotalLaborCostQuote(string? projectID);
        Task<bool> IsProjIdExist(string projId);
        Task<bool> Save();
    }
}

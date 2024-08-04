
using ProjectLibrary.DTO.Quote;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IQuote
    {
        Task<string> AddNewMaterialSupply(MaterialQuoteDto materialQuoteDto);
        Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(int? projectID);
        Task<string> AddNewLaborCost(LaborQuoteDto laborQuoteDto);
        Task<ICollection<LaborCostDto>> GetLaborCostQuote(LaborCostDto laborCostDto);
        Task<bool> Save();
    }
}


using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IQuote
    {
        Task<string> AddNewMaterialSupply(MaterialQuoteDto materialQuoteDto);
        Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(string? projectID);
        Task<ProjectCostDto> GetProjectTotalCostQuote(string? projectID);
        Task<ICollection<AllMaterialCategoriesCostDto>> GetProjectAndMaterialsTotalCostQuote(string? projectID);
        Task<ICollection<AllMaterialCategoriesExpense>> GetMaterialCategoryCostQuote(string? projectID);


        Task<string> AddNewLaborCost(LaborQuoteDto laborQuoteDto);
        Task<ICollection<LaborCostDto>> GetLaborCostQuote(string? projectID);
        Task<TotalLaborCostDto> GetTotalLaborCostQuote(string? projectID);


        Task<ICollection<AssignedEquipmentDto>> GetAssignedEquipment(string projectID);
        Task<(bool, string)> AssignNewEquipment(AssignEquipmentDto assignEquipmentDto);


        Task<bool> UpdateMaterialQuantity(UpdateMaterialSupplyQuantity materialSupplyQuantity);
        Task<bool> UpdateLaborQuoote(UpdateLaborQuote updateLaborQuote);
        Task<bool> UpdateEquipmentQuantity(UpdateEquipmentSupply updateEquipmentSupply);


        Task<bool> DeleteMaterialSupply(int suppId, int MTLId);
        Task<bool> DeleteLaborQuote(int laborId);
        Task<bool> DeleteEquipmentSupply(DeleteEquipmentSupplyDto deleteEquipmentSupply);

        Task<bool> LogUserActionAsync(
            string userEmail,
            string action,
            string entityName,
            string entityId,
            string details,
            string userIpAddress);
        Task<bool> Save();
    }
}

using ProjectLibrary.DTO.Material;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IMaterial
    {
        Task<string> AddNewMaterial(MaterialDTO material);
        Task<(bool, string)> UpdateQAndPMaterial(UpdateQAndPMaterialDTO updateMaterial);
        Task<(bool, string)> UpdateUAndDMaterial(UpdateMaterialUAndC updateMaterial);
        Task<bool> IsMTLCodeExist(string mtlCode);
        Task<ICollection<GetMaterialDTO>> GetMaterials();
        Task<ICollection<GetAllMaterialCategory>> GetMaterialCategories();
        Task<ICollection<AvailableByCategoryMaterialDTO>> GetMaterialsByCategory(string projId, string category);
        Task<int> GetQOHMaterial(int mtlId);
        Task<bool> IsMaterialExist(string name);
        Task<(bool, string)> DeleteMaterial(int mtlId, string adminEmail, string ipAdd);
        Task<bool> Save();
    }
}

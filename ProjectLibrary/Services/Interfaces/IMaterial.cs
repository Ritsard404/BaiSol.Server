using ProjectLibrary.DTO.Material;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IMaterial
    {
        Task<string> AddNewMaterial(MaterialDTO material);
        Task<bool> IsMTLCodeExist(string mtlCode);
        Task<ICollection<GetMaterialDTO>> GetMaterials();
        Task<bool> IsMaterialExist(string name);
        Task<bool> Save();
    }
}

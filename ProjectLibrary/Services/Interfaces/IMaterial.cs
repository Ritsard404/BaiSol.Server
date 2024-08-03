using DataLibrary.Models;
using ProjectLibrary.DTO.Material;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IMaterial
    {
        Task<string> AddNewMaterial(MaterialDTO material);
        Task<ICollection<GetMaterialDTO>> GetMaterials();
        Task<bool> IsMaterialExist(string name);
        Task<bool> Save();
    }
}



using AutoMapper;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.Services.Interfaces;

namespace ProjectLibrary.Services.Repositories
{
    public class MaterialRepository(DataContext _dataContext, IMapper _mapper) : IMaterial
    {
        public async Task<string> AddNewMaterial(MaterialDTO materialDto)
        {

            if (materialDto == null)
            {
                throw new ArgumentNullException(nameof(materialDto));
            }

            // Check if the material already exists
            var isMaterialExist = await IsMaterialExist(materialDto.MTLDescript);
            if (isMaterialExist)
            {
                return "Material Already Exists";
            }

            // Map DTO to model
            var materialMap = _mapper.Map<Material>(materialDto);

            // Ensure the MTLCode is unique
            string uniqueMTLCode;
            do
            {
                //uniqueMTLCode = Guid.NewGuid().ToString("N").ToUpper().Substring(0, 8);
                uniqueMTLCode = Guid.NewGuid().ToString();
            } while (await IsMTLCodeExist(uniqueMTLCode));

            materialMap.MTLCode = uniqueMTLCode;

            // Add the material to the database
            _dataContext.Material.Add(materialMap);
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<ICollection<GetMaterialDTO>> GetMaterials()
        {
            var materials = await _dataContext.Material.ToListAsync();

            var materialsList = new List<GetMaterialDTO>();

            foreach (var material in materials)
            {
                materialsList.Add(new GetMaterialDTO
                {
                    MTLId = material.MTLId,
                    MTLCode = material.MTLCode,
                    MTLDescript = material.MTLDescript,
                    MTLPrice = material.MTLPrice,
                    MTLQOH = material.MTLQOH,
                    MTLUnit = material.MTLUnit,
                    MTLStatus = material.MTLStatus,
                    UpdatedAt = material.UpdatedAt.ToString("MMM dd, yyyy"),
                    CreatedAt = material.CreatedAt.ToString("MMM dd, yyyy"),

                });
            }

            return materialsList.OrderBy(o=>o.MTLUnit).ToList();
        }

        public async Task<bool> IsMaterialExist(string name)
        {
            return await _dataContext.Material.AnyAsync(i => i.MTLDescript == name);
        }

        public async Task<bool> IsMTLCodeExist(string mtlCode)
        {
            return await _dataContext.Material.AnyAsync(m => m.MTLCode == mtlCode);
        }

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }
    }
}

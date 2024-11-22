

using AutoMapper;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.Services.Interfaces;
using System.Globalization;

namespace ProjectLibrary.Services.Repositories
{
    public class MaterialRepository(DataContext _dataContext, UserManager<AppUsers> _userManager, IMapper _mapper) : IMaterial
    {
        TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;

        private string textFormat(string text)
        {
            var formmatedText = textInfo.ToTitleCase(text).Trim();
            return formmatedText;
        }

        public async Task<string> AddNewMaterial(MaterialDTO materialDto)
        {

            if (materialDto == null)
            {
                throw new ArgumentNullException(nameof(materialDto));
            }

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(materialDto.UserEmail);
            if (user == null) return "Invalid User!";
            var userRole = await _userManager.GetRolesAsync(user);

            materialDto.MTLDescript = textFormat(materialDto.MTLDescript);
            materialDto.MTLCategory = textFormat(materialDto.MTLCategory);

            var material = await _dataContext.Material.FirstOrDefaultAsync(i => i.MTLDescript == materialDto.MTLDescript);
            if (material != null)
            {
                //var oldQuantity = material.MTLQOH;
                //material.MTLQOH += materialDto.MTLQOH;
                //material.UpdatedAt = DateTimeOffset.UtcNow;


                //UserLogs log = new UserLogs
                //{
                //    Action = "Update",
                //    EntityName = "Material",
                //    EntityId = material.MTLId.ToString(),
                //    UserIPAddress = materialDto.UserIpAddress,
                //    Details = $"Updated material '{material.MTLDescript}'. Old Quantity: {oldQuantity}, New Quantity: {material.MTLQOH}.",
                //    UserId = user.Id,
                //    UserName = user.NormalizedUserName,
                //    UserRole = userRole.FirstOrDefault(),
                //    User = user,
                //};
                //_dataContext.UserLogs.Add(log);
                //await Save();

                return "Material already exist!";
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
            await Save();

            UserLogs logs = new UserLogs
            {
                Action = "Create",
                EntityName = "Material",
                EntityId = materialMap.MTLId.ToString(),
                UserIPAddress = materialDto.UserIpAddress,
                Details = $"New material named {materialMap.MTLDescript} added. With a quantity of {materialMap.MTLQOH}",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<(bool, string)> DeleteMaterial(int mtlId, string adminEmail, string ipAdd)
        {
            var material = await _dataContext.Material.FindAsync(mtlId);
            if (material == null) return (false, "Material not found!");


            var suppliedMaterial = await _dataContext.Supply
                .FirstOrDefaultAsync(m => m.Material == material);

            if (material.MTLQOH > 0 || suppliedMaterial == null)
                return (false, "Material cannot be deleted!");

            // Check if the material is used in any finished project
            var isUsed = await _dataContext.Supply
                .AnyAsync(s => s.Material == material && s.Project.Status == "OnGoing" && s.Material.MTLStatus == "Good");

            if (isUsed) return (false, "Material cannot be deleted because it is used in ongoing projects.");

            // Remove the material
            _dataContext.Material.Remove(material);

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(adminEmail);
            if (user == null) return (false, "Invalid User!");

            var userRole = await _userManager.GetRolesAsync(user);

            UserLogs logs = new UserLogs
            {
                Action = "Delete",
                EntityName = "Material",
                EntityId = material.MTLId.ToString(),
                UserIPAddress = ipAdd,
                Details = $"Deleted material '{material.MTLDescript}'. " +
                  $"Quantity: {material.MTLQOH}, " +
                  $"Price: ₱{material.MTLPrice}, " +
                  $"Unit: {material.MTLUnit}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);


            return (await Save(), "Material successfully deleted!");
        }

        public async Task<ICollection<GetAllMaterialCategory>> GetMaterialCategories()
        {
            return await _dataContext.Material
                .Where(m => !string.IsNullOrEmpty(m.MTLCategory))
                .GroupBy(m => m.MTLCategory)
                .Select(g => new GetAllMaterialCategory { Category = g.Key })
                .ToListAsync();
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
                    MTLCtgry = material.MTLCategory,
                    MTLPrice = material.MTLPrice,
                    MTLQOH = material.MTLQOH,
                    MTLUnit = material.MTLUnit,
                    MTLStatus = material.MTLStatus,
                    UpdatedAt = material.UpdatedAt.ToString("MMM dd, yyyy"),
                    CreatedAt = material.CreatedAt.ToString("MMM dd, yyyy"),

                });
            }

            return materialsList.OrderBy(o => o.MTLCtgry).ThenBy(c => c.CreatedAt).ToList();
        }

        public async Task<ICollection<AvailableByCategoryMaterialDTO>> GetMaterialsByCategory(string projId, string category)
        {
            return await _dataContext.Material
                .Where(m => m.MTLStatus == "Good" && m.MTLCategory == category)
                .Where(m => !_dataContext.Supply
                    .Where(s => s.Project.ProjId == projId)
                    .Select(s => s.Material.MTLId)
                    .Contains(m.MTLId))
                .Select(a => new AvailableByCategoryMaterialDTO
                {
                    Code = a.MTLCode,
                    Description = a.MTLDescript,
                    MtlId = a.MTLId,
                    Quantity = a.MTLQOH

                })
                .OrderBy(a => a.Description)
                .ToListAsync();
        }

        public async Task<int> GetQOHMaterial(int mtlId)
        {
            return await _dataContext.Material
                .Where(m => m.MTLId == mtlId) // Filter by the specified material ID
                .Select(m => m.MTLQOH) // Select the QOH property
                .FirstOrDefaultAsync(); // Get the first match or default value (0 if not found)
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

        public async Task<(bool, string)> UpdateQAndPMaterial(UpdateQAndPMaterialDTO updateMaterial)
        {
            var material = await _dataContext.Material
                .FirstOrDefaultAsync(m => m.MTLId == updateMaterial.MTLId);

            if (material == null) return (false, "Material does not exist");

            // Check if the material is used in any ongoing project
            var isUsed = await _dataContext.Supply
                .AnyAsync(s => s.Material == material && s.Project.Status == "OnGoing" && s.Material.MTLStatus == "Good");

            // Store old values
            var oldQuantity = material.MTLQOH;
            var oldPrice = material.MTLPrice;

            material.MTLQOH = updateMaterial.MTLQOH;
            if (!isUsed) material.MTLPrice = updateMaterial.MTLPrice; // Update price only if not used
            material.UpdatedAt = DateTimeOffset.UtcNow;

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(updateMaterial.UserEmail);
            if (user == null) return (false, "Invalid User!");

            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();

            // Log details
            var details = isUsed
                ? $"Updated material '{material.MTLDescript}'. Old Quantity: {oldQuantity}, New Quantity: {material.MTLQOH}. Price was not updated because it is used in ongoing projects."
                : $"Updated material '{material.MTLDescript}'. Old Quantity: {oldQuantity}, New Quantity: {material.MTLQOH}. Old Price: ₱{oldPrice}, New Price: ₱{material.MTLPrice}";

            _dataContext.UserLogs.Add(new UserLogs
            {
                Action = "Update",
                EntityName = "Material",
                EntityId = material.MTLId.ToString(),
                UserIPAddress = updateMaterial.UserIpAddress,
                Details = details,
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole,
                User = user,
            });

            await Save();
            if (isUsed) return (true, "The material quantity was sccuessfully updated!");

            return (true, "The material was successfully updated!");
        }

        public async Task<(bool, string)> UpdateUAndDMaterial(UpdateMaterialUAndC updateMaterial)
        {
            var material = await _dataContext.Material
                .FirstOrDefaultAsync(m => m.MTLCode == updateMaterial.MTLCode);

            if (material == null) return (false, "Material not exist");

            updateMaterial.MTLDescript = textFormat(updateMaterial.MTLDescript);

            var isMaterialExist = await IsMaterialExist(updateMaterial.MTLDescript);
            if (isMaterialExist)
                return (false, "Material cannot update");

            //// Check if the material is used in any finished project
            //var isUsed = await _dataContext.Supply
            //    .AnyAsync(s => s.Material == material && s.Project.Status == "OnGoing" && s.Material.MTLStatus == "Good");

            //if (isUsed) return (false, "Material cannot be update because it is used in ongoing projects.");

            // Store old values
            var oldDesc = material.MTLDescript;
            var oldUnit = material.MTLUnit;

            material.MTLDescript = updateMaterial.MTLDescript;
            material.MTLUnit = updateMaterial.MTLUnit;
            material.UpdatedAt = DateTimeOffset.UtcNow;


            // Check User Existence
            var user = await _userManager.FindByEmailAsync(updateMaterial.UserEmail);
            if (user == null) return (false, "Invalid User!");
            var userRole = await _userManager.GetRolesAsync(user);

            UserLogs logs = new UserLogs
            {
                Action = "Update",
                EntityName = "Material",
                EntityId = material.MTLId.ToString(),
                UserIPAddress = updateMaterial.UserIpAddress,
                Details = $"Updated material '{material.MTLDescript}'. " +
                  $"Old Description: {oldDesc}, New Description: {material.MTLDescript}. " +
                  $"Old Unit: {oldUnit}, New Unit: {material.MTLUnit}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);

            await Save();

            return (true, "The material was successfully updated!");
        }

        public async Task<(bool, string)> UpdateQOHMaterial(UpdateQOHMaterialDTO updateQOH)
        {
            var material = await _dataContext.Material
                .FirstOrDefaultAsync(m => m.MTLId == updateQOH.MTLId);

            if (material == null) return (false, "Material not exist!");
            if (updateQOH.MTLQOH <= 0)
                return (false, "Invalid Quantity!");

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(updateQOH.UserEmail);
            if (user == null) return (false, "Invalid User!");

            var userRole = (await _userManager.GetRolesAsync(user)).FirstOrDefault();


            // Store old values
            var oldQuantity = material.MTLQOH;

            material.MTLQOH += updateQOH.MTLQOH;
            material.UpdatedAt = DateTimeOffset.UtcNow;
            _dataContext.UserLogs.Add(new UserLogs
            {
                Action = "Update",
                EntityName = "Material",
                EntityId = material.MTLId.ToString(),
                UserIPAddress = updateQOH.UserIpAddress,
                Details = $"Updated material '{material.MTLDescript}'. Old Quantity: {oldQuantity}, New Quantity: {material.MTLQOH}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole,
                User = user,
            });

            await Save();

            return (true, "The material quantity updated!");

        }
    }
}

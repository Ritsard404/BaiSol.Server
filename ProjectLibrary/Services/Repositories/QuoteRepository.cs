
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace ProjectLibrary.Services.Repositories
{
    public class QuoteRepository(DataContext _dataContext) : IQuote
    {
        public async Task<string> AddNewLaborCost(LaborQuoteDto laborQuoteDto)
        {
            var clientProject = await _dataContext.Project
                .FirstOrDefaultAsync(proj => proj.ProjId == laborQuoteDto.ProjId);


            //if (clientProject == null)
            //{
            //    return "Project not found.";
            //}

            var newLabor = new Labor
            {
                LaborDescript = laborQuoteDto.Description,
                LaborQOH = laborQuoteDto.Quantity,
                LaborUnit = laborQuoteDto.Unit,
                LaborNumUnit = laborQuoteDto.UnitNum,
                LaborCost = laborQuoteDto.Quantity * laborQuoteDto.UnitCost * laborQuoteDto.UnitNum,
            };

            // Add the new Supply entity to the context
            _dataContext.Labor.Add(newLabor);

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<string> AddNewMaterialSupply(MaterialQuoteDto materialQuoteDto)
        {
            var clientProject = await _dataContext.Project
                .FirstOrDefaultAsync(proj => proj.ProjId == materialQuoteDto.ProjId);


            //if (clientProject == null)
            //{
            //    return "Project not found.";
            //}

            var projectMaterial = await _dataContext.Material
                .FirstOrDefaultAsync(m => m.MTLId == materialQuoteDto.MTLId);


            //if (projectMaterial == null)
            //{
            //    return "Material not found.";
            //}

            var newSupply = new Supply
            {
                MTLQuantity = materialQuoteDto.MTLQuantity,
                Material = projectMaterial,
                Project = clientProject

            };

            // Add the new Supply entity to the context
            _dataContext.Supply.Add(newSupply);

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";

        }

        public Task<ICollection<LaborCostDto>> GetLaborCostQuote(LaborCostDto laborCostDto)
        {
            throw new NotImplementedException();
        }

        public async Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(int? projectID)
        {
            //var materialSupply = await _dataContext.Supply
            //    .Where(p => p.Project.ProjId == projectID)
            //    .Include(i => i.Material)
            //    .ToListAsync();

            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == null)
                .Include(i => i.Material)
                .ToListAsync();

            var materialCostList = materialSupply
                .GroupBy(material => material.Material.MTLDescript)
                .Select(group => new MaterialCostDto
                {
                    Description = group.Key,
                    Quantity = group.Sum(m => m.MTLQuantity ?? 0), // Use null-coalescing to handle possible null values
                    Unit = group.First().Material.MTLUnit, // Assuming unit is the same for all items in the group
                    Category = group.First().Material.MTLCategory, // Add this if you have a category in your DTO
                    UnitCost = group.First().Material.MTLPrice, // Assuming price is the same for all items in the group
                    TotalUnitCost = (decimal)(group.Sum(m => m.MTLQuantity ?? 0) * group.First().Material.MTLPrice),
                    BuildUpCost = ((decimal)(group.Sum(m => m.MTLQuantity ?? 0) * group.First().Material.MTLPrice) * 1.2m)
                })
                .OrderBy(o => o.Category) // Order by category
                .ToList();

            return materialCostList;
        }

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }
    }
}

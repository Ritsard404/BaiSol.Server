
using AutoMapper;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace ProjectLibrary.Services.Repositories
{
    public class QuoteRepository(DataContext _dataContext
        ) : IQuote
    {

        public async Task<string> AddNewLaborCost(LaborQuoteDto laborQuoteDto)
        {
            var clientProject = await _dataContext.Project
                .FirstOrDefaultAsync(proj => proj.ProjId == laborQuoteDto.ProjId);


            if (clientProject == null)
            {
                return "Project not found.";
            }

            //var predefinedCosts = new[]
            //{
            //    new Labor { LaborDescript = "Manpower", LaborUnit = "Days", Project = clientProject },
            //    new Labor { LaborDescript = "Project Manager - Electrical Engr.", LaborUnit = "Days", Project = clientProject },
            //    new Labor { LaborDescript = "Mobilization/Demob", LaborUnit = "Lot", Project = clientProject },
            //    new Labor { LaborDescript = "Tools & Equipment", LaborUnit = "Lot", Project = clientProject },
            //    new Labor { LaborDescript = "Other Incidental Costs", LaborUnit = "Lot", Project = clientProject }
            //};

            //foreach (var labor in predefinedCosts)
            //{
            //    if (!await _dataContext.Labor
            //        .Include(p => p.Project)
            //        .AnyAsync(proj => proj.Project.ProjDescript == clientProject.ProjDescript && proj.LaborDescript == labor.LaborDescript))
            //    {
            //        _dataContext.Labor.Add(labor);
            //    }
            //}

            var newLabor = new Labor
            {
                LaborDescript = laborQuoteDto.Description.Trim(),
                LaborQuantity = laborQuoteDto.Quantity,
                LaborUnit = laborQuoteDto.Unit.Trim(),
                LaborUnitCost = laborQuoteDto.UnitCost,
                LaborNumUnit = laborQuoteDto.UnitNum,
                LaborCost = laborQuoteDto.Quantity * laborQuoteDto.UnitCost * laborQuoteDto.UnitNum,
                Project = clientProject
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


            if (clientProject == null)
            {
                return "Project not found.";
            }

            var projectMaterial = await _dataContext.Material
                .Where(s => s.MTLStatus == "Good")
                .FirstOrDefaultAsync(m => m.MTLCode == materialQuoteDto.MTLCode);

            if (projectMaterial == null)
            {
                return "Material not found.";
            }

            projectMaterial.MTLQOH -= materialQuoteDto.MTLQuantity;


            var newSupply = new Supply
            {
                MTLQuantity = materialQuoteDto.MTLQuantity,
                Material = projectMaterial,
                Project = clientProject

            };

            _dataContext.Material.Update(projectMaterial);

            // Add the new Supply entity to the context
            _dataContext.Supply.Add(newSupply);

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";

        }

        public async Task<bool> DeleteLaborQuote(int laborId)
        {
            var labor = await _dataContext.Labor
                .FindAsync(laborId);

            if (labor == null) return false;

            _dataContext.Labor.Remove(labor);

            // Save changes to the database
            return await Save();
        }

        public async Task<bool> DeleteMaterialSupply(int suppId, int mtlId)
        {
            var supply = await _dataContext.Supply
                .FirstOrDefaultAsync(i => i.SuppId == suppId);

            // Retrieve the Material entity by mtlID
            var material = await _dataContext.Material
                .FirstOrDefaultAsync(i => i.MTLId == mtlId);

            // Check if the material entity exists
            // Material not found, return false
            if (material == null) return false;

            if (supply == null) return false;

            material.MTLQOH = material.MTLQOH + (supply.MTLQuantity ?? 0);

            _dataContext.Material.Update(material);

            _dataContext.Supply.Remove(supply);

            // Save changes to the database
            return await Save();
        }

        public async Task<ICollection<LaborCostDto>> GetLaborCostQuote(string? projectID)
        {
            var quoteMaterialSupply = await _dataContext.Labor
                .Where(p => p.Project.ProjId == projectID)
                .ToListAsync();

            //var quoteMaterialSupply = await _dataContext.Labor
            //    .Where(p => p.Project.ProjId == null)
            //    .ToListAsync();

            var laborCostList = quoteMaterialSupply
                .Select(l => new LaborCostDto
                {
                    LaborId = l.LaborId,
                    Description = l.LaborDescript,
                    Quantity = l.LaborQuantity,
                    Unit = l.LaborUnit,
                    UnitCost = l.LaborUnitCost,
                    UnitNum = l.LaborNumUnit,
                    TotalCost = l.LaborCost,
                })
                .ToList();


            return laborCostList;
        }

        public async Task<ICollection<AllMaterialCategoriesExpense>> GetMaterialCategoryCostQuote(string? projectID)
        {
            return await _dataContext.Supply
                .Where(p => p.Project.ProjId == projectID)
                .Include(i => i.Material)
                .GroupBy(c => c.Material.MTLCategory)
                .Select(g => new AllMaterialCategoriesExpense
                {
                    Category = g.Key,
                    TotalCategory = g.Count(),
                    TotalExpense = g.Sum(s => (decimal)(s.MTLQuantity ?? 0) * s.Material.MTLPrice * 1.3m) // Calculate the total expense

                })
                .ToListAsync();

        }

        public async Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(string? projectID)
        {
            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == projectID)
                .Include(i => i.Material)
                .ToListAsync();

            //var materialSupply = await _dataContext.Supply
            //    .Where(p => p.Project.ProjId == null)
            //    .Include(i => i.Material)
            //    .ToListAsync();

            var materialCostList = materialSupply
            .Select(material => new MaterialCostDto
            {
                SuppId = material.SuppId, // Assuming SuppId is available and needs to be included
                MtlId = material.Material.MTLId,
                Description = material.Material.MTLDescript,
                Quantity = material.MTLQuantity ?? 0, // Use null-coalescing to handle possible null values
                Unit = material.Material.MTLUnit,
                Category = material.Material.MTLCategory, // Include if needed in DTO
                UnitCost = material.Material.MTLPrice,
                TotalUnitCost = (decimal)((material.MTLQuantity ?? 0) * material.Material.MTLPrice),
                BuildUpCost = (decimal)((material.MTLQuantity ?? 0) * material.Material.MTLPrice * 1.3m),
                CreatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy"),
                UpdatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy"),
            })
            .OrderByDescending(o => o.CreatedAt) // Order by CreatedAt or another property if necessary
            .ToList();

            return materialCostList;
        }

        public async Task<ICollection<AllMaterialCategoriesCostDto>> GetProjectAndMaterialsTotalCostQuote(string? projectID)
        {
            // Fetch the material supply data with the required joins
            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == projectID)
                .Include(i => i.Material)
                .ToListAsync();

            // Group by category and calculate the required totals
            var categoryExpenses = materialSupply
                .GroupBy(p => p.Material.MTLCategory)
                .Select(g => new AllMaterialCategoriesCostDto
                {
                    Category = g.Key,
                    TotalCategory = g.Count(),
                    TotalExpense = g.Sum(s => (decimal)(s.MTLQuantity ?? 0) * s.Material.MTLPrice * 1.3m),
                    MaterialCostDtos = g.Select(material => new MaterialCostDto
                    {
                        SuppId = material.SuppId,
                        MtlId = material.Material.MTLId,
                        Description = material.Material.MTLDescript,
                        Quantity = material.MTLQuantity ?? 0,
                        Unit = material.Material.MTLUnit,
                        Category = material.Material.MTLCategory,
                        UnitCost = material.Material.MTLPrice,
                        TotalUnitCost = (decimal)((material.MTLQuantity ?? 0) * material.Material.MTLPrice),
                        BuildUpCost = (decimal)((material.MTLQuantity ?? 0) * material.Material.MTLPrice * 1.3m),
                        CreatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy"),
                        UpdatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy"),
                    }).OrderBy(o => o.Description)
                    .ThenBy(o => o.Unit) // Secondary sort by Description
                    .ToList()
                })
                .OrderBy(c => c.Category)
                .ToList();

            return categoryExpenses;
        }

        public async Task<ProjectCostDto> GetProjectTotalCostQuote(string? projectID)
        {
            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == projectID)
                .Include(i => i.Material)
                .ToListAsync();

            //// Retrieve material supply data
            //var materialSupply = await _dataContext.Supply
            //    .Where(p => p.Project.ProjId == null)
            //    .Include(i => i.Material)
            //    .ToListAsync();

            // Calculate total unit cost and build-up cost in one pass
            var (totalUnitCostSum, buildUpCostSum) = materialSupply
                .GroupBy(m => m.Material.MTLDescript)
                .Aggregate(
                    (totalUnitCost: 0m, buildUpCost: 0m),
                    (acc, group) =>
                    {
                        var quantity = group.Sum(m => m.MTLQuantity ?? 0);
                        var price = group.First().Material.MTLPrice;
                        var unitCost = quantity * price;
                        var buildUpCost = unitCost * 1.2m;

                        return (acc.totalUnitCost + unitCost, acc.buildUpCost + buildUpCost);
                    }
                );

            // Calculate profit and overall totals
            var profitPercentage = 0.3m;
            var profit = totalUnitCostSum * profitPercentage;
            var overallMaterialTotal = totalUnitCostSum + profit;

            // Retrieve labor costs and calculate profit and totals
            var totalLaborCost = await _dataContext.Labor
                .Where(p => p.Project.ProjId == projectID)
                .SumAsync(o => o.LaborCost);

            var laborProfit = totalLaborCost * profitPercentage;
            var overallLaborProjectTotal = totalLaborCost + laborProfit;

            // Calculate the total project cost
            var totalProjectCost = overallMaterialTotal + overallLaborProjectTotal;

            // Return the project cost DTO
            return new ProjectCostDto
            {
                TotalCost = totalUnitCostSum,
                Profit = profit,
                OverallMaterialTotal = overallMaterialTotal,
                OverallProjMgtCost = overallLaborProjectTotal,
                NetMeteringCost = null,
                TotalProjectCost = totalProjectCost,

            };

        }

        public async Task<TotalLaborCostDto> GetTotalLaborCostQuote(string? projectID)
        {
            var totalLaborCost = await _dataContext.Labor
                .Where(p => p.Project.ProjId == projectID)
                .SumAsync(o => o.LaborCost);

            //// Retrieve labor costs for the specified project
            //var totalLaborCost = await _dataContext.Labor
            //    .Where(p => p.Project.ProjId == null)
            //    .SumAsync(o => o.LaborCost);


            // Calculate profit and overall total
            var profitPercentage = 0.3m; // Example profit percentage
            var profit = totalLaborCost * profitPercentage;
            var overallLaborProjectTotal = totalLaborCost + profit;

            // Return the DTO wrapped in a list
            return new TotalLaborCostDto
            {
                TotalCost = totalLaborCost,
                Profit = profit,
                OverallLaborProjectTotal = overallLaborProjectTotal

            };
        }


        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }

        public async Task<bool> UpdateLaborQuoote(UpdateLaborQuote updateLaborQuote)
        {
            // Retrieve the labor entity
            var labor = await _dataContext.Labor
                .FindAsync(updateLaborQuote.LaborId);

            // Return false if labor entity not found
            if (labor == null) return false;

            // Update labor properties
            labor.LaborDescript = updateLaborQuote.Description.Trim();
            labor.LaborQuantity = updateLaborQuote.Quantity;
            labor.LaborUnit = updateLaborQuote.Unit.Trim();
            labor.LaborUnitCost = updateLaborQuote.UnitCost;
            labor.LaborNumUnit = updateLaborQuote.UnitNum;
            labor.LaborCost = updateLaborQuote.Quantity * updateLaborQuote.UnitCost * updateLaborQuote.UnitNum;
            labor.UpdatedAt = DateTimeOffset.UtcNow;

            // Mark entity as modified
            _dataContext.Labor.Update(labor);

            // Save changes and return the result
            return await Save();
        }

        public async Task<bool> UpdateMaterialQuantity(UpdateMaterialSupplyQuantity materialSupplyQuantity)
        {
            // Retrieve the Supply entity by suppId
            var supply = await _dataContext.Supply.FirstOrDefaultAsync(i => i.SuppId == materialSupplyQuantity.SuppId);

            // Check if the supply entity exists
            if (supply == null)
            {
                // Supply not found, return false
                return false;
            }

            // Retrieve the Material entity by mtlID
            var material = await _dataContext.Material.FirstOrDefaultAsync(i => i.MTLId == materialSupplyQuantity.MTLId);

            // Check if the material entity exists
            if (material == null)
            {
                // Material not found, return false
                return false;
            }

            // Calculate the total quantity on hand (QOH) including the quantity from the supply
            // Use null-coalescing operator to handle possible null values
            var materialQOH = material.MTLQOH + (supply.MTLQuantity ?? 0);

            // Check if the available quantity is less than the requested quantity
            if (materialQOH < materialSupplyQuantity.Quantity)
            {
                // Not enough material available, return false
                return false;
            }

            // Update the material quantity on hand by subtracting the requested quantity
            material.MTLQOH = materialQOH - materialSupplyQuantity.Quantity;

            // Update the supply quantity to the new value
            supply.MTLQuantity = materialSupplyQuantity.Quantity;

            // Mark both entities as modified
            _dataContext.Material.Update(material);
            _dataContext.Supply.Update(supply);

            // Save changes to the database and return the result
            return await Save();
        }
    }
}

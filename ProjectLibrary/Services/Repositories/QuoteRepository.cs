
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
    public class QuoteRepository(DataContext _dataContext, UserManager<AppUsers> _userManager, IMapper _mapper) : IQuote
    {
        public async Task<string> AddNewClientProject(ProjectDto projectDto)
        {
            if (projectDto == null)
            {
                throw new ArgumentNullException(nameof(projectDto));
            }

            // Check if the client exists
            var isClientExist = await _userManager.FindByIdAsync(projectDto.ClientId);
            if (isClientExist == null)
            {
                return "Client does not exist";
            }

            // Map DTO to model
            var projectMap = _mapper.Map<Project>(projectDto);

            // Generate unique ProjId
            string uniqueProjId;
            do
            {
                uniqueProjId = Guid.NewGuid().ToString();
            } while (await IsProjIdExist(uniqueProjId));

            projectMap.ProjId = uniqueProjId;

            // Add the new Supply entity to the context
            _dataContext.Project.Add(projectMap);

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

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
                LaborQuantity = laborQuoteDto.Quantity,
                LaborUnit = laborQuoteDto.Unit,
                LaborUnitCost = laborQuoteDto.UnitCost,
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
                .FirstOrDefaultAsync(m => m.MTLCode == materialQuoteDto.MTLCode);


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

        public async Task<ICollection<LaborCostDto>> GetLaborCostQuote(string? projectID)
        {
            //var quoteMaterialSupply = await _dataContext.Labor
            //    .Where(p => p.Project.ProjId == projectID)
            //    .ToListAsync();

            var quoteMaterialSupply = await _dataContext.Labor
                .Where(p => p.Project.ProjId == null)
                .ToListAsync();

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

        public async Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(string? projectID)
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

        public async Task<ICollection<ProjectCostDto>> GetProjectTotalCostQuote(string? projectID)
        {
            //var materialSupply = await _dataContext.Supply
            //    .Where(p => p.Project.ProjId == projectID)
            //    .Include(i => i.Material)
            //    .ToListAsync();

            // Retrieve material supply data
            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == null)
                .Include(i => i.Material)
                .ToListAsync();

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
            var profitPercentage = 1.3m;
            var profit = totalUnitCostSum * (profitPercentage - 1);
            var overallMaterialTotal = totalUnitCostSum + profit;

            // Retrieve labor costs and calculate profit and totals
            var totalLaborCost = await _dataContext.Labor
                .Where(p => p.Project.ProjId == null)
                .SumAsync(o => o.LaborCost);

            var laborProfit = totalLaborCost * (profitPercentage - 1);
            var overallLaborProjectTotal = totalLaborCost + laborProfit;

            // Calculate the total project cost
            var totalProjectCost = overallMaterialTotal + overallLaborProjectTotal;

            // Return the project cost DTO
            return new List<ProjectCostDto>
{
            new ProjectCostDto
                {
                    TotalCost = totalUnitCostSum,
                    Profit = profit,
                    OverallMaterialTotal = overallMaterialTotal,
                    OverallProjMgtCost = totalLaborCost,
                    NetMeteringCost = null,
                    TotalProjectCost = totalProjectCost,
                }
            };

        }

        public async Task<ICollection<TotalLaborCostDto>> GetTotalLaborCostQuote(string? projectID)
        {
            //var totalLaborCost = await _dataContext.Labor
            //    .Where(p => p.Project.ProjId == projectID)
            //    .ToListAsync();

            // Retrieve labor costs for the specified project
            var totalLaborCost = await _dataContext.Labor
                .Where(p => p.Project.ProjId == null)
                .SumAsync(o => o.LaborCost);


            // Calculate profit and overall total
            var profitPercentage = 1.3m; // Example profit percentage
            var profit = totalLaborCost * (profitPercentage - 1);
            var overallLaborProjectTotal = totalLaborCost + profit;

            // Return the DTO wrapped in a list
            return new List<TotalLaborCostDto>
            {
                new TotalLaborCostDto
                {
                    TotalCost = totalLaborCost,
                    Profit = profit,
                    OverallLaborProjectTotal = overallLaborProjectTotal
                }
            };
        }

        public async Task<bool> IsProjIdExist(string projId)
        {
            return await _dataContext.Project.AnyAsync(p => p.ProjId == projId);
        }

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }
    }
}


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
    public class QuoteRepository(DataContext _dataContext, UserManager<AppUsers> _userManager
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

            var isExistDescript = await _dataContext.Labor
                .Include(p => p.Project)
                .AnyAsync(d => d.LaborDescript == laborQuoteDto.Description && d.Project.ProjId == laborQuoteDto.ProjId);

            if (isExistDescript)
                return "Labor already exists.";

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


            var isMaterialExist = await _dataContext.Supply
                .FirstOrDefaultAsync(i => i.Material.MTLCode == materialQuoteDto.MTLCode && i.Project == clientProject);
            if (isMaterialExist != null)
                return "Material supply already exist!";

            if (projectMaterial.MTLQOH < materialQuoteDto.MTLQuantity)
                return "Invalid quantity!";

            projectMaterial.MTLQOH -= materialQuoteDto.MTLQuantity;


            var newSupply = new Supply
            {
                MTLQuantity = materialQuoteDto.MTLQuantity,
                Price = projectMaterial.MTLPrice,
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

        public async Task<(bool, string)> AssignNewEquipment(AssignEquipmentDto assignEquipmentDto)
        {
            // Fetch the project based on ProjId
            var clientProject = await _dataContext.Project
                .FirstOrDefaultAsync(proj => proj.ProjId == assignEquipmentDto.ProjId);

            if (clientProject == null)
            {
                return (false, "Project not found.");
            }

            // Fetch the equipment based on EQPTId
            var projectEquipment = await _dataContext.Equipment
                .FirstOrDefaultAsync(equip => equip.EQPTId == assignEquipmentDto.EQPTId);

            if (projectEquipment == null)
            {
                return (false, "Equipment not found.");
            }

            if (projectEquipment.EQPTQOH < assignEquipmentDto.EQPTQuantity)
                return (false, "Invalid quanity!");

            var isEquipmentExist = await _dataContext.Supply
                .FirstOrDefaultAsync(i => i.Equipment.EQPTId == assignEquipmentDto.EQPTId && i.Project == clientProject);
            if (isEquipmentExist != null)
                return (false, "Equipment supply already exist!");

            // Decrease the equipment quantity on hand (QOH)
            projectEquipment.EQPTQOH -= assignEquipmentDto.EQPTQuantity;

            // Create a new supply entry
            var newSupply = new Supply
            {
                EQPTQuantity = assignEquipmentDto.EQPTQuantity,
                Price = projectEquipment.EQPTPrice,
                Equipment = projectEquipment,
                Project = clientProject
            };

            // Update the equipment and add the new supply to the database
            _dataContext.Equipment.Update(projectEquipment);
            _dataContext.Supply.Add(newSupply);

            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(assignEquipmentDto.UserEmail);
            if (user == null)
            {
                return (false, "Invalid User!");
            }

            // Fetch the user's roles
            var userRole = await _userManager.GetRolesAsync(user);

            // Log the action
            var logs = new UserLogs
            {
                Action = "Create",
                EntityName = "Supply",
                EntityId = projectEquipment.EQPTCode,
                UserIPAddress = assignEquipmentDto.UserIpAddress,
                Details = $"Equipment named {projectEquipment.EQPTDescript} assigned to project {clientProject.ProjName} with a quantity of {assignEquipmentDto.EQPTQuantity}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);

            // Save changes to the database
            await Save();

            return (true, "Equipment successfully assigned!");
        }

        public async Task<bool> DeleteEquipmentSupply(DeleteEquipmentSupplyDto deleteEquipmentSupply)
        {
            var supply = await _dataContext.Supply
                .FirstOrDefaultAsync(i => i.SuppId == deleteEquipmentSupply.SuppId);

            var equipment = await _dataContext.Equipment
                .FirstOrDefaultAsync(i => i.EQPTId == deleteEquipmentSupply.EQPTId);

            var isSupplyUsed = await _dataContext.Requisition
                .FirstOrDefaultAsync(i => i.RequestSupply.SuppId == deleteEquipmentSupply.SuppId);

            if (supply == null || equipment == null || isSupplyUsed == null)
                return false;

            var logMessage = $"Equipment named {equipment?.EQPTDescript ?? "unknown"} that is assigned to project {supply?.Project?.ProjName ?? "unknown"} was deleted with a quantity of {supply?.EQPTQuantity ?? 0}.";


            var log = await LogUserActionAsync(
                deleteEquipmentSupply.UserEmail,
                "Delete",
                "Supply",
                supply.SuppId.ToString(),
                logMessage,
                deleteEquipmentSupply.UserIpAddress
                );

            if (!log)
                return false;

            equipment.EQPTQOH += supply.EQPTQuantity ?? 0;
            _dataContext.Equipment.Update(equipment);
            _dataContext.Supply.Remove(supply);



            return await Save();
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

            var isSupplyUsed = await _dataContext.Requisition
                .FirstOrDefaultAsync(i => i.RequestSupply.SuppId == suppId);

            // Check if the material entity exists
            // Material not found, return false
            if (material == null) return false;

            if (supply == null) return false;

            if (isSupplyUsed == null) return false;

            material.MTLQOH = material.MTLQOH + (supply.MTLQuantity ?? 0);

            _dataContext.Material.Update(material);

            _dataContext.Supply.Remove(supply);

            // Save changes to the database
            return await Save();
        }

        public async Task<ICollection<AssignedEquipmentDto>> GetAssignedEquipment(string projectID)
        {
            var equipmentSupply = await _dataContext.Supply
                .Where(p => p.Project != null && p.Project.ProjId == projectID)
                .Include(i => i.Equipment)
                .Where(e => e.Equipment != null) // Ensure Equipment is not null
                .ToListAsync();

            var category = equipmentSupply
                .GroupBy(c => c.Equipment?.EQPTCategory) // Group by category, allowing null categories
                .Select(s => new AssignedEquipmentDto
                {
                    EQPTCategory = s.Key ?? "Unknown", // Handle null categories
                    Details = s
                        .Where(e => e.Equipment != null) // Check if Equipment is not null
                        .Select(e => new EquipmentDetails
                        {
                            SuppId = e.SuppId,
                            EQPTId = e.Equipment.EQPTId,
                            EQPTCode = e.Equipment.EQPTCode,
                            EQPTDescript = e.Equipment.EQPTDescript,
                            EQPTUnit = e.Equipment.EQPTUnit,
                            EQPTPrice = e.Equipment.EQPTPrice,
                            EQPTQOH = e.EQPTQuantity ?? 0,
                        })
                        .OrderBy(n => n.EQPTDescript)
                        .ThenBy(o => o.EQPTUnit)
                        .ToList()
                })
                .OrderBy(c => c.EQPTCategory)
                .ToList();

            return category;
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
                .Include(i => i.Project)
                .GroupBy(c => c.Material.MTLCategory)
                .Select(g => new AllMaterialCategoriesExpense
                {
                    Category = g.Key,
                    TotalCategory = g.Count(),
                    TotalExpense = g.Sum(s => (decimal)(s.MTLQuantity ?? 0) * s.Material.MTLPrice * (1 + s.Project.ProfitRate)) // Calculate the total expense

                })
                .ToListAsync();

        }

        public async Task<ICollection<MaterialCostDto>> GetMaterialCostQuote(string? projectID)
        {
            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == projectID)
                .Include(i => i.Material)
                .Include(i => i.Project)
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
                BuildUpCost = (decimal)((material.MTLQuantity ?? 0) * material.Material.MTLPrice * (1 + material.Project.ProfitRate)),
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
                .Include(i => i.Project)
                .ToListAsync();

            // Group by category and calculate the required totals
            var categoryExpenses = materialSupply
                .Where(p => p.Material != null) // Ensure Material is not null
                .GroupBy(p => p.Material.MTLCategory ?? "Unknown") // Handle null categories
                .Select(g => new AllMaterialCategoriesCostDto
                {
                    Category = g.Key,
                    TotalCategory = g.Count(),
                    TotalExpense = g.Sum(s =>
                        (decimal)(s.MTLQuantity ?? 0) *
                        (s.Material?.MTLPrice ?? 0) * (1 + s.Project.ProfitRate)), // Handle null prices
                    MaterialCostDtos = g
                        .Where(material => material.Material != null) // Ensure Material is not null
                        .Select(material => new MaterialCostDto
                        {
                            SuppId = material.SuppId,
                            MtlId = material.Material?.MTLId ?? 0, // Handle null MTLId
                            Description = material.Material?.MTLDescript ?? "No Description", // Handle null description
                            Quantity = material.MTLQuantity ?? 0,
                            Unit = material.Material?.MTLUnit ?? "Unknown", // Handle null units
                            Category = material.Material?.MTLCategory ?? "Unknown", // Handle null categories
                            UnitCost = material.Material?.MTLPrice ?? 0, // Handle null prices
                            TotalUnitCost = (decimal)((material.MTLQuantity ?? 0) * (material.Material?.MTLPrice ?? 0)),
                            BuildUpCost = (decimal)((material.MTLQuantity ?? 0) * (material.Material?.MTLPrice ?? 0) * (1+material.Project.ProfitRate)),
                            CreatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy"),
                            UpdatedAt = DateTime.UtcNow.ToString("MMM dd, yyyy"),
                        })
                        .OrderBy(o => o.Description)
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
                .Include(i => i.Project)
                .ToListAsync();

            //// Retrieve material supply data
            //var materialSupply = await _dataContext.Supply
            //    .Where(p => p.Project.ProjId == null)
            //    .Include(i => i.Material)
            //    .ToListAsync();

            var profitPercentage = materialSupply.Select(r => r.Project.ProfitRate).FirstOrDefault();

            // Calculate total unit cost and build-up cost in one pass
            var (totalUnitCostSum, buildUpCostSum) = materialSupply
                .Where(m => m.Material != null)
                .GroupBy(m => m.Material.MTLDescript)
                .Aggregate(
                    (totalUnitCost: 0m, buildUpCost: 0m),
                    (acc, group) =>
                    {
                        var quantity = group.Sum(m => m.MTLQuantity ?? 0);
                        var price = group.First().Material.MTLPrice;
                        var unitCost = quantity * price;
                        var buildUpCost = unitCost * (1 + profitPercentage);

                        return (acc.totalUnitCost + unitCost, acc.buildUpCost + buildUpCost);
                    }
                );

            // Calculate profit and overall totals
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
                ProfitPercentage = profitPercentage * 100,
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
            var profitPercentage = _dataContext.Labor
                .Where(p => p.Project.ProjId == projectID)
                .Select(p => p.Project.ProfitRate)
                .FirstOrDefault();

            var profit = totalLaborCost * profitPercentage;
            var overallLaborProjectTotal = totalLaborCost + profit;

            // Return the DTO wrapped in a list
            return new TotalLaborCostDto
            {
                TotalCost = totalLaborCost,
                Profit = profit,
                ProfitPercentage = profitPercentage * 100,
                OverallLaborProjectTotal = overallLaborProjectTotal

            };
        }

        public async Task<bool> LogUserActionAsync(string userEmail, string action, string entityName, string entityId, string details, string userIpAddress)
        {
            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null) return false;

            // Fetch the user's roles
            var userRole = await _userManager.GetRolesAsync(user);

            // Log the action
            var logs = new UserLogs
            {
                Action = action,
                EntityName = entityName,
                EntityId = entityId,
                UserIPAddress = userIpAddress,
                Details = details,
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };

            // Add the logs to the database
            _dataContext.UserLogs.Add(logs);

            // Save changes to the database (assuming you have a Save method)
            await Save();

            return true;
        }

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }

        public async Task<bool> UpdateEquipmentQuantity(UpdateEquipmentSupply updateEquipmentSupply)
        {
            // Retrieve the Supply entity by suppId
            var supply = await _dataContext.Supply
                .Include(p => p.Project)
                .FirstOrDefaultAsync(i => i.SuppId == updateEquipmentSupply.SuppId);
            if (supply == null)
                return false;

            var equipment = await _dataContext.Equipment.FirstOrDefaultAsync(i => i.EQPTId == updateEquipmentSupply.EQPTId);
            if (equipment == null)
                return false;

            var equipmentQOH = equipment.EQPTQOH + (supply.EQPTQuantity ?? 0);
            if (equipmentQOH < updateEquipmentSupply.Quantity)
                return false;

            equipment.EQPTQOH = equipmentQOH - updateEquipmentSupply.Quantity;

            // Update the supply quantity to the new value
            supply.EQPTQuantity = updateEquipmentSupply.Quantity;


            // Mark both entities as modified
            _dataContext.Equipment.Update(equipment);
            _dataContext.Supply.Update(supply);


            // Fetch the user by email and ensure the user exists
            var user = await _userManager.FindByEmailAsync(updateEquipmentSupply.UserEmail);
            if (user == null)
                return false;

            // Fetch the user's roles
            var userRole = await _userManager.GetRolesAsync(user);

            // Log the action
            var logs = new UserLogs
            {
                Action = "Update",
                EntityName = "Supply",
                EntityId = updateEquipmentSupply.SuppId.ToString(),
                UserIPAddress = updateEquipmentSupply.UserIpAddress,
                Details = $"Equipment named {equipment.EQPTDescript} that is assigned to project {supply.Project.ProjName} was updated to a quantity of {updateEquipmentSupply.Quantity}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);

            return await Save();
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
            var supply = await _dataContext.Supply
                .FirstOrDefaultAsync(i => i.SuppId == materialSupplyQuantity.SuppId);

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

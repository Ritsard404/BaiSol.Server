using DataLibrary.Data;
using DataLibrary.Models;
using FacilitatorLibrary.DTO.Supply;
using FacilitatorLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.Services.Repositories
{
    public class AssignedSupplyRepository(DataContext _dataContext, UserManager<AppUsers> _userManager) : IAssignedSupply
    {
        public async Task<ICollection<AssignedEquipmentDTO>> GetAssignedEquipment(string? userEmail)
        {
            // Retrieve the assigned facilitator's project information based on the user email
            var assignedFacilitatorProjId = await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status != "Finished")
                .Select(e => e.Project.ProjId) // Only select the project ID
                .FirstOrDefaultAsync();

            // If no assigned facilitator is found, return an empty list
            if (assignedFacilitatorProjId  == null)
                return new List<AssignedEquipmentDTO>();

            var equipmentSupply = await _dataContext.Supply
                .Where(p => p.Project != null && p.Project.ProjId == assignedFacilitatorProjId )
                .Include(i => i.Equipment)
                .Where(e => e.Equipment != null) // Ensure Equipment is not null
                .ToListAsync();

            var category = equipmentSupply
                .GroupBy(c => c.Equipment?.EQPTCategory) // Group by category, allowing null categories
                .Select(s => new AssignedEquipmentDTO
                {
                    EqptCategory = s.Key ?? "Unknown", // Handle null categories
                    Details = s
                        .Where(e => e.Equipment != null) // Check if Equipment is not null
                        .Select(e => new EquipmentDetails
                        {
                            SuppId = e.SuppId,
                            EqptCode = e.Equipment.EQPTCode,
                            EqptDescript = e.Equipment.EQPTDescript,
                            EqptUnit = e.Equipment.EQPTUnit,
                            Quantity = e.EQPTQuantity ?? 0,
                        })
                        .OrderBy(n => n.EqptDescript)
                        .ThenBy(o => o.EqptUnit)
                        .ToList()
                })
                .OrderBy(c => c.EqptCategory)
                .ToList();

            return category;
        }

        public async Task<ICollection<AssignedMaterialsDTO>> GetAssignedMaterials(string? userEmail)
        {
            // Retrieve the assigned facilitator's project information based on the user email
            var assignedFacilitatorProjId = await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status != "Finished")
                .Select(e => e.Project.ProjId) // Only select the project ID
                .FirstOrDefaultAsync();

            // If no assigned facilitator is found, return an empty list
            if (assignedFacilitatorProjId  == null)
                return new List<AssignedMaterialsDTO>();

            // Fetch the material supply data with the required joins
            var materialSupply = await _dataContext.Supply
                .Where(p => p.Project.ProjId == assignedFacilitatorProjId )
                .Include(i => i.Material)
                .ToListAsync();

            var category = materialSupply
                .GroupBy(c => c.Material?.MTLCategory) // Group by category, allowing null categories
                .Select(s => new AssignedMaterialsDTO
                {
                    MTLCategory = s.Key ?? "Unknown", // Handle null categories
                    Details = s
                        .Where(e => e.Material != null) // Check if Material is not null
                        .Select(e => new MaterialsDetails
                        {
                            SuppId = e.SuppId,
                            MtlId = e.Material.MTLId,
                            MtlDescription = e.Material.MTLDescript,
                            MtlUnit = e.Material.MTLUnit,
                            MtlQuantity = e.MTLQuantity ?? 0,
                        })
                        .OrderBy(n => n.MtlDescription)
                        .ThenBy(o => o.MtlUnit)
                        .ToList()
                })
                .OrderBy(c => c.MTLCategory)
                .ToList();

            return category;
        }

        public async Task<string> GetAssignedProject(string? userEmail)
        {
            // Retrieve the assigned facilitator's project information based on the user email
            return await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status != "Finished")
                .Select(e => e.Project.ProjId) // Only select the project ID
                .FirstOrDefaultAsync() ?? ""; // Return an empty string if no match is found
        }
    }
}

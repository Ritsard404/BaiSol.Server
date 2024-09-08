using AutoMapper;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Equipment;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.Services.Repositories
{
    public class EquipmentRepository(DataContext _dataContext, UserManager<AppUsers> _userManager, IMapper _mapper) : IEquipment
    {
        public async Task<string> AddNewEquipment(EquipmentDTO equipment)
        {
            if (equipment == null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            // Check if the equpment already exists
            var isEquipmentExist = await IsEQPTDescriptExist(equipment.EQPTDescript);
            if (isEquipmentExist)
            {
                return "Equipment Already Exists";
            }
            // Check User Existence
            var user = await _userManager.FindByEmailAsync(equipment.UserEmail);
            if (user == null) return "Invalid User!";
            var userRole = await _userManager.GetRolesAsync(user);

            // Map DTO to model
            var equipmentMap = _mapper.Map<Equipment>(equipment);

            // Ensure the EQPTCode is unique
            string uniqueEQPTCode;
            do
            {
                uniqueEQPTCode = Guid.NewGuid().ToString();
            } while (await IsEQPTCodeExist(uniqueEQPTCode));

            equipmentMap.EQPTCode = uniqueEQPTCode;

            // Add the equipment to the database
            _dataContext.Equipment.Add(equipmentMap);


            UserLogs logs = new UserLogs
            {
                Action = "Create",
                EntityName = "Equipment",
                EntityId = equipmentMap.EQPTId.ToString(),
                UserIPAddress = equipment.UserIpAddress,
                Details = $"New equipment named {equipment.EQPTDescript} added. With a quantity of {equipment.EQPTQOH}",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };

            _dataContext.UserLogs.Add(logs);

            return await Save() ? null : "Something went wrong while saving";

        }

        public async Task<(bool, string)> DeleteEquipment(int eqptId, string adminEmail, string ipAdd)
        {

            var equipment = await _dataContext.Equipment.FindAsync(eqptId);
            if (equipment == null) return (false, "Equipment not found!");


            // Check if the equipment is used in any finished project
            var isUsed = await _dataContext.Supply
                .AnyAsync(s => s.Equipment == equipment && s.Project.Status == "OnGoing" && s.Equipment.EQPTStatus == "Good");

            if (isUsed) return (false, "Equipment cannot be deleted because it is used in ongoing projects.");

            // Remove the equipment
            _dataContext.Equipment.Remove(equipment);


            // Check User Existence
            var user = await _userManager.FindByEmailAsync(adminEmail);
            if (user == null) return (false, "Invalid User!");

            var userRole = await _userManager.GetRolesAsync(user);

            UserLogs logs = new UserLogs
            {
                Action = "Delete",
                EntityName = "Equipment",
                EntityId = equipment.EQPTId.ToString(),
                UserIPAddress = ipAdd,
                Details = $"Deleted equipment '{equipment.EQPTDescript}'. " +
                  $"Quantity: {equipment.EQPTQOH}, " +
                  $"Price: ₱{equipment.EQPTPrice}, " +
                  $"Unit: {equipment.EQPTUnit}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);


            return (await Save(), "Equipment successfully deleted!");

        }

        public async Task<ICollection<GetEquipmentDTO>> GetAllEquipment()
        {
            var equipment = await _dataContext.Equipment.ToListAsync();

            var equipmentList = new List<GetEquipmentDTO>();

            foreach (var eqpt in equipment)
            {
                equipmentList.Add(new GetEquipmentDTO
                {
                    EQPTId = eqpt.EQPTId,
                    EQPTCode = eqpt.EQPTCode,
                    EQPTDescript = eqpt.EQPTDescript,
                    EQPTCtgry = eqpt.EQPTCategory,
                    EQPTPrice = eqpt.EQPTPrice,
                    EQPTQOH = eqpt.EQPTQOH,
                    EQPTUnit = eqpt.EQPTUnit,
                    EQPTStatus = eqpt.EQPTStatus,
                    UpdatedAt = eqpt.UpdatedAt.ToString("MMM dd, yyyy"),
                    CreatedAt = eqpt.CreatedAt.ToString("MMM dd, yyyy"),

                });
            }

            return equipmentList.OrderBy(o => o.EQPTCtgry).ThenBy(c => c.CreatedAt).ToList();

        }

        public async Task<ICollection<AvailableByCategoryEquipmentDTO>> GetEquipmentByCategory(string projId, string category)
        {
            return await _dataContext.Equipment
                .Where(m => m.EQPTStatus == "Good" && m.EQPTCategory == category)
                .Where(m => !_dataContext.Supply
                    .Where(s => s.Project.ProjId == projId)
                    .Select(s => s.Material.MTLId)
                    .Contains(m.EQPTId))
                .Select(a => new AvailableByCategoryEquipmentDTO
                {
                    Code = a.EQPTCode,
                    Description = a.EQPTDescript,
                    EQPTId = a.EQPTId,
                    Quantity = a.EQPTQOH

                })
                .OrderBy(a => a.Description)
                .ToListAsync();
        }

        public async Task<ICollection<GetAllEquipmentCategory>> GetEquipmentCategories()
        {
            return await _dataContext.Equipment
                .Where(m => !string.IsNullOrEmpty(m.EQPTCategory))
                .GroupBy(m => m.EQPTCategory)
                .Select(g => new GetAllEquipmentCategory { Category = g.Key })
                .ToListAsync();
        }

        public async Task<bool> IsEQPTCodeExist(string eqptCode)
        {
            return await _dataContext.Equipment.AnyAsync(m => m.EQPTCode == eqptCode);

        }

        public async Task<bool> IsEQPTDescriptExist(string eqptDescript)
        {
            return await _dataContext.Equipment.AnyAsync(i => i.EQPTDescript == eqptDescript);
        }

        public async Task<(bool, string)> ReturnDamagedEquipment(ReturnEquipmentDto returnEquipment)
        {
            var suppliedEquipment = await _dataContext.Supply
                .Include(i => i.Project)
                .Include(i => i.Equipment)
                .Where(p => p.Project.ProjId == returnEquipment.ProjId && p.Project.Status == "Finished")
                .ToListAsync();
            if (suppliedEquipment == null)
                return (false, "Project not finished yet!");

            // Retrieve the list of equipment from the database based on codes provided in the DTO
            var codesToCheck = returnEquipment.EquipmentDetails.Select(d => d.Code).ToList();
            var equipmentList = await _dataContext.Equipment
                .Where(e => codesToCheck.Contains(e.EQPTCode))
                .ToListAsync();

            // Determine if all codes are valid
            var validCodes = equipmentList.Select(e => e.EQPTCode).ToHashSet();
            var invalidCodes = codesToCheck.Except(validCodes).ToList();

            // If there are any codes not found in the database, return false
            if (invalidCodes.Any())
                return (false, $"The following equipment codes are invalid or not found in the database: {string.Join(", ", invalidCodes)}");

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(returnEquipment.UserEmail);
            if (user == null)
                return (false, "Invalid User!");


            // Iterate and create new entries with updated QOH and price
            foreach (var item in equipmentList)
            {
                // Find the matching detail based on EQPTCode
                var equipmentDetail = returnEquipment.EquipmentDetails
                    .FirstOrDefault(d => d.Code == item.EQPTCode);

                if (equipmentDetail == null) continue; // Skip if no matching detail found

                // Create a new Equipment object with the required properties
                Equipment newEquipment = new Equipment
                {
                    EQPTCode = item.EQPTCode,
                    EQPTDescript = item.EQPTDescript,
                    EQPTCategory = item.EQPTCategory,
                    EQPTUnit = item.EQPTUnit,
                    EQPTStatus = "Damaged",                  // Set the status as needed
                    EQPTQOH = equipmentDetail.QOH,           // Set the Quantity on Hand
                    EQPTPrice = equipmentDetail.Price        // Set the Price
                };

                UserLogs logs = new UserLogs
                {
                    Action = "Return",
                    EntityName = "Equipment",
                    EntityId = item.EQPTCode,
                    UserIPAddress = returnEquipment.UserIpAddress,
                    Details = $"Returned damaged equipment '{item.EQPTDescript}'. " +
                        $"Price: {equipmentDetail.Price}. " +
                        $"Quantity: {equipmentDetail.QOH}.",
                    UserId = user.Id,
                    UserName = user.NormalizedUserName,
                    UserRole = "Admin",
                    User = user,
                };

                _dataContext.UserLogs.Add(logs);

                // Add the new equipment object to the context
                _dataContext.Equipment.Add(newEquipment);
            }
            await Save();
            return (true, "Equipment Successfully Returned!");
        }

        public async Task<(bool, string)> ReturnGoodEquipment(ReturnEquipmentDto returnEquipment)
        {
            var equipmentCodes = returnEquipment.EquipmentDetails.Select(e => e.Code).ToList();

            var suppliedEquipment = await _dataContext.Supply
                .Include(i => i.Project)
                .Include(i => i.Equipment)
                .Where(p => p.Project.ProjId == returnEquipment.ProjId &&
                            p.Project.Status == "Finished" &&
                            equipmentCodes.Contains(p.Equipment.EQPTCode))
                .ToListAsync();

            if (suppliedEquipment == null)
                return (false, "Project not finished yet!");

            // Retrieve the list of equipment from the database based on codes provided in the DTO
            var codesToCheck = returnEquipment.EquipmentDetails.Select(d => d.Code).ToList();
            var goodEquipmentList = await _dataContext.Equipment
                .Where(e => codesToCheck.Contains(e.EQPTCode) && e.EQPTStatus == "Good")
                .ToListAsync();

            // Determine if all codes are valid
            var validCodes = goodEquipmentList.Select(e => e.EQPTCode).ToHashSet();
            var invalidCodes = codesToCheck.Except(validCodes).ToList();

            // If there are any codes not found in the database, return false
            if (invalidCodes.Any())
                return (false, $"The following equipment codes are invalid or not found in the database: {string.Join(", ", invalidCodes)}");

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(returnEquipment.UserEmail);
            if (user == null)
                return (false, "Invalid User!");


            // Iterate and create new entries with updated QOH and price
            foreach (var item in goodEquipmentList)
            {
                // Find the matching detail based on EQPTCode
                var equipmentDetail = returnEquipment.EquipmentDetails
                    .FirstOrDefault(d => d.Code == item.EQPTCode);

                if (equipmentDetail == null) continue; // Skip if no matching detail found

                // Update existing equipment record
                item.EQPTQOH = equipmentDetail.QOH;

                UserLogs logs = new UserLogs
                {
                    Action = "Return",
                    EntityName = "Equipment",
                    EntityId = item.EQPTCode,
                    UserIPAddress = returnEquipment.UserIpAddress,
                    Details = $"Returned good equipment '{item.EQPTDescript}'. " +
                        $"Price: {equipmentDetail.Price}. " +
                        $"Quantity: {equipmentDetail.QOH}.",
                    UserId = user.Id,
                    UserName = user.NormalizedUserName,
                    UserRole = "Admin",
                    User = user,
                };

                _dataContext.UserLogs.Add(logs);
            }

            await Save();
            return (true, "Equipment Successfully Returned!");
        }

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }

        public async Task<string> UpdateQAndPEquipment(UpdateQAndPDTO updateEquipment)
        {
            var equipment = await _dataContext.Equipment
                .FirstOrDefaultAsync(m => m.EQPTId == updateEquipment.EQPTId);

            if (equipment == null) return "Equipment not exist";

            // Store old values
            var oldQuantity = equipment.EQPTQOH;
            var oldPrice = equipment.EQPTPrice;

            equipment.EQPTQOH = updateEquipment.EQPTQOH;
            equipment.EQPTPrice = updateEquipment.EQPTPrice;
            equipment.UpdatedAt = DateTimeOffset.UtcNow;


            // Check User Existence
            var user = await _userManager.FindByEmailAsync(updateEquipment.UserEmail);
            if (user == null) return "Invalid User!";
            var userRole = await _userManager.GetRolesAsync(user);

            UserLogs logs = new UserLogs
            {
                Action = "Update",
                EntityName = "Equipment",
                EntityId = equipment.EQPTId.ToString(),
                UserIPAddress = updateEquipment.UserIpAddress,
                Details = $"Updated equipment '{equipment.EQPTDescript}'. " +
                  $"Old Quantity: {oldQuantity}, New Quantity: {equipment.EQPTQOH}. " +
                  $"Old Price: ₱{oldPrice}, New Price: ₱{equipment.EQPTPrice}",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);

            await Save();

            return "THe equipment was successfully updated!";

        }

        public async Task<string> UpdateUAndDEquipment(UpdateUAndDDTO updateEquipment)
        {
            var equipment = await _dataContext.Equipment
                .FirstOrDefaultAsync(m => m.EQPTCode == updateEquipment.EQPTCode);

            if (equipment == null) return "Equipment not exist";

            // Store old values
            var oldDesc = equipment.EQPTDescript;
            var oldUnit = equipment.EQPTUnit;

            equipment.EQPTDescript = updateEquipment.EQPTDescript;
            equipment.EQPTUnit = updateEquipment.EQPTUnit;
            equipment.UpdatedAt = DateTimeOffset.UtcNow;

            // Check User Existence
            var user = await _userManager.FindByEmailAsync(updateEquipment.UserEmail);
            if (user == null) return "Invalid User!";
            var userRole = await _userManager.GetRolesAsync(user);

            UserLogs logs = new UserLogs
            {
                Action = "Update",
                EntityName = "Equipment",
                EntityId = equipment.EQPTId.ToString(),
                UserIPAddress = updateEquipment.UserIpAddress,
                Details = $"Updated equipment '{equipment.EQPTDescript}'. " +
                  $"Old Description: {oldDesc}, New Description: {equipment.EQPTDescript}. " +
                  $"Old Unit: {oldUnit}, New Unit: {equipment.EQPTUnit}.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole.FirstOrDefault(),
                User = user,
            };
            _dataContext.UserLogs.Add(logs);

            await Save();

            return "THe equipment was successfully updated!";
        }
    }
}

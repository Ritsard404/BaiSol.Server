using ProjectLibrary.DTO.Equipment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IEquipment
    {
        Task<string> AddNewEquipment(EquipmentDTO equipment);
        Task<ICollection<GetEquipmentDTO>> GetAllEquipment();
        Task<ICollection<GetAllEquipmentCategory>> GetEquipmentCategories();
        Task<ICollection<AvailableByCategoryEquipmentDTO>> GetEquipmentByCategory(string projId, string category);
        Task<int> GetQOHEquipment(int eqptId);


        Task<(bool, string)> UpdateQAndPEquipment(UpdateQAndPDTO updateEquipment);
        Task<(bool, string)> UpdateUAndDEquipment(UpdateUAndDDTO updateEquipment);
        Task<bool> IsEQPTCodeExist(string eqptCode);
        Task<bool> IsEQPTDescriptExist(string eqptDescript);
        Task<(bool, string)> DeleteEquipment(int eqptId, string adminEmail, string ipAdd);
        Task<(bool, string)> ReturnDamagedEquipment(ReturnEquipmentDto returnEquipment);
        Task<(bool, string)> ReturnGoodEquipment(ReturnEquipmentDto returnEquipment);
        Task<bool> Save();
    }
}

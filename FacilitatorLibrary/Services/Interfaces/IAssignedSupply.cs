using FacilitatorLibrary.DTO.Supply;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.Services.Interfaces
{
    public interface IAssignedSupply
    {
        Task<ICollection<AssignedMaterialsDTO>> GetAssignedMaterials(string? userEmail);
        Task<ICollection<AssignedEquipmentDTO>> GetAssignedEquipment(string? userEmail);
        Task<string> GetAssignedProject(string? userEmail);
    }
}

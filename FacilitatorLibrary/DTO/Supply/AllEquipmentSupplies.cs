using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.DTO.Supply
{
    public class AllEquipmentSupplies
    {
        public required string EqptCode { get; set; }
        public required string EqptDescript { get; set; }
        public int Quantity { get; set; }
        public required string EqptUnit { get; set; }
    }
    public class AllAssignedEquipmentDTO
    {
        public required string EqptCategory { get; set; }
        public List<AllEquipmentSupplies>? Details { get; set; }

    }
}

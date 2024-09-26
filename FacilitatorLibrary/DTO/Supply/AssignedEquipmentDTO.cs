using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.DTO.Supply
{
    public class EquipmentDetails
    {
        public required int SuppId { get; set; }
        public required string EqptCode { get; set; }
        public required string EqptDescript { get; set; }
        public int Quantity { get; set; }
        public required string EqptUnit { get; set; }
    }
    public class AssignedEquipmentDTO
    {
        public required string EqptCategory { get; set; }
        public List<EquipmentDetails>? Details { get; set; }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Equipment
{
    public class AvailableByCategoryEquipmentDTO
    {
        public required int EQPTId { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required int Quantity { get; set; }
    }
}

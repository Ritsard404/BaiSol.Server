using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Quote
{
    public class AssignedEquipmentDto
    {
        [Key]
        public int EQPTId { get; set; }
        public string EQPTCode { get; set; }
        public required string EQPTDescript { get; set; }
        public decimal EQPTPrice { get; set; }
        public int EQPTQOH { get; set; }
        public required string EQPTUnit { get; set; }
        public required string EQPTCategory { get; set; }
    }
}

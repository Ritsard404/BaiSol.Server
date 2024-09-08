using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Equipment
{
    public class GetEquipmentDTO
    {
        [Key]
        public int EQPTId { get; set; }
        public required string EQPTCode { get; set; }
        public required string EQPTDescript { get; set; }
        public required string EQPTCtgry { get; set; }
        public decimal EQPTPrice { get; set; }
        public int EQPTQOH { get; set; }
        public required string EQPTUnit { get; set; }
        public string EQPTStatus { get; set; } = "Good";
        public string? UpdatedAt { get; set; }
        public string? CreatedAt { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Material
{
    public class GetMaterialDTO
    {
        [Key]
        public int MTLId { get; set; }
        public required string MTLCode { get; set; }
        public required string MTLDescript { get; set; }
        public required string MTLCtgry { get; set; }
        public decimal MTLPrice { get; set; }
        public int MTLQOH { get; set; }
        public required string MTLUnit { get; set; }
        public string MTLStatus { get; set; } = "Good";
        public string? UpdatedAt { get; set; }
        public string? CreatedAt { get; set; }
    }
}

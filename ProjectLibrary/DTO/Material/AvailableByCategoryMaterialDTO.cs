using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Material
{
    public class AvailableByCategoryMaterialDTO
    {
        public required int MtlId { get; set; }
        public required string Code { get; set; }
        public required string Description { get; set; }
        public required int Quantity { get; set; }
    }
}

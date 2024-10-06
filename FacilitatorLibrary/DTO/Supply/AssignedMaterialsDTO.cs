using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.DTO.Supply
{
    public class MaterialsDetails
    {
        public required int SuppId { get; set; }
        public required int MtlId { get; set; }
        public required string MtlDescription { get; set; }
        public int? MtlQuantity { get; set; }
        public required string MtlUnit { get; set; }

    }
    public class AssignedMaterialsDTO
    {
        public required string MTLCategory { get; set; }
        public List<MaterialsDetails>? Details { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.DTO.Supply
{
    public class ReturnSupplyDTO
    {
        public required string EqptCode { get; set; }
        public required int returnedQuantity { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Requisition
{
    public class AvailableRequestSupplies
    {
        public required int  suppId { get; set; }
        public required string supplyName { get; set; }
    }
}

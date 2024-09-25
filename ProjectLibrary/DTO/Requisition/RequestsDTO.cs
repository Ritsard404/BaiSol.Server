using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Requisition
{
    public class RequestsDTO
    {
        public required int  ReqId { get; set; }
        public required string  SubmittedAt { get; set; }
        public string? ReviewedAt { get; set; }
        public required string Status { get; set; }
        public required int QuantityRequested { get; set; }
        public required string RequestSupply { get; set; }
        public required string SupplyCategory { get; set; }
        public required string SubmittedBy { get; set; }
        public string? ReviewedBy { get; set; }
    }
}

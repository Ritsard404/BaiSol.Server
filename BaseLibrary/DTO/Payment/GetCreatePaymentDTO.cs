using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Payment
{
    public class GetCreatePaymentDTO
    {
        public required string id { get; set; }
        public required string checkout_url { get; set; }
        public required string reference_number { get; set; }
    }
}

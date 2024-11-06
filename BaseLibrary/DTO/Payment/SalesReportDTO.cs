using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Payment
{
    public class SalesReportDTO
    {
        public required string Date { get; set; }
        public required decimal Amount { get; set; }
    }
}

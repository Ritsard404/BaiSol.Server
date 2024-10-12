using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Project
{
    public class ProjectQuotationTotalExpense
    {
        public required string QuoteId { get; set; }
        public required string SubTotal { get; set; }
        public required string Discount { get; set; }
        public required string SubTotalAfterDiscount { get; set; }
        public required string VAT { get; set; }
        public required string VatRate { get; set; }
        public required string Total { get; set; }

        public required ProjectQuotationSupply TotalMaterialCost { get; set; }
        public required ProjectQuotationSupply TotalLaborCost { get; set; }
    }
}

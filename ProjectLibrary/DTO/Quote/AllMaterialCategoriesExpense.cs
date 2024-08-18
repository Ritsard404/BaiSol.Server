using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Quote
{
    public class AllMaterialCategoriesExpense
    {
        public required string Category { get; set; }
        public required int TotalCategory { get; set; }
        public required decimal TotalExpense { get; set; }
    }
}

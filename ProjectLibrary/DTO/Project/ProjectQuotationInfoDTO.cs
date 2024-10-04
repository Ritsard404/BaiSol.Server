using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Project
{

    public class ProjectQuotationInfoDTO
    {
        public string? customerId { get; set; }
        public string? customerName { get; set; }
        public string? customerEmail { get; set; }
        public string? customerAddress { get; set; }
        public string? projectDescription { get; set; }
        public string? projectDateCreation { get; set; }
        public string? projectDateValidity { get; set; }


    }
}

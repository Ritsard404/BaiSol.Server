using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Project
{
    public class UpdateProfitRate
    {
        [EmailAddress]
        public required string userEmail { get; set; }
        public required string projId { get; set; }
        public required decimal profitRate { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class ProjectDateInfo
    {
        public required string StartDate { get; set; }
        public required string EndDate { get; set; }
        public required string EstimatedStartDate { get; set; }
        public required string EstimatedEndDate { get; set; }
        public required string EstimatedProjectDays { get; set; }
        public required string AssignedFacilitator { get; set; }
    }
}

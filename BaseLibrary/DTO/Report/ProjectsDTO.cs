using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Report
{
    public class ProjectsDTO
    {
        public required string projId { get; set; }
        public required string kWCapacity { get; set; }
        public required string systemType { get; set; }
        public required string customer { get; set; }
        public required string facilitator { get; set; }
        public required string plannedStarted { get; set; }
        public required string plannedEnded { get; set; }
        public required string actualStarted { get; set; }
        public required string actualEnded { get; set; }
        public required string cost { get; set; }
        public required string status { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Report
{
    public class ProjectReportDTO
    {
        public required string ProjectName { get; set; }
        public required List<ProjectDetail> ProjectDetails { get; set; }
    }

    public class ProjectDetail
    {
        public required string Date { get; set; }
        public required int Progress { get; set; }
    }
}

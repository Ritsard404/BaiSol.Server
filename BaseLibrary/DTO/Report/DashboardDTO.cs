using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Report
{
    public class DashboardDTO
    {
        public int TotalPersonnel { get; set; }
        public int FinishedProjects { get; set; }
        public int PendingProjects { get; set; }
        public int OnWorkProjects { get; set; }
    }
}

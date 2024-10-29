using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class ProjectTasks
    {
        public int Id { get; set; }
        public string? TaskName { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? StartProofImage { get; set; }
        public string? FinishProofImage { get; set; }
        public bool? IsFinished { get; set; }
        public bool? IsStarting { get; set; }
    }
    public class ProjectStatusDTO
    {
        public ProjectDateInfo? Info { get; set; }
        public List<ProjectTasks>? Tasks { get; set; }
    }
}

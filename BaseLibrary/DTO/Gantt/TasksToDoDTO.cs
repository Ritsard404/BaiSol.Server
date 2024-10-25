using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class TasksToDoDTO
    {
        public int Id { get; set; }
        public string? TaskName { get; set; }
        public string? PlannedStartDate { get; set; }
        public string? PlannedEndDate { get; set; }
        public bool? IsEnable { get; set; }
        public bool? IsFinished { get; set; }
        public bool? IsStarting { get; set; }
    }
}

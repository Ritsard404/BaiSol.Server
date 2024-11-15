using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class TaskToDoDTO
    {
        public int Id { get; set; }
        public string? TaskName { get; set; }
        public string? PlannedStartDate { get; set; }
        public string? PlannedEndDate { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public bool? IsEnable { get; set; }
        public bool? IsFinished { get; set; }
        public bool? IsStarting { get; set; }
        public int DaysLate { get; set; }
        public List<TaskDTO>? TaskList { get; set; }
    }

    public class TaskDTO
    {
        public int id { get; set; }
        public string? ProofImage { get; set; }
        public string? ActualStart { get; set; }
        public required string EstimationStart { get; set; }
        public required int TaskProgress { get; set; }
        public bool IsFinish { get; set; }
        public bool? IsEnable { get; set; }
        public bool? IsLate { get; set; }
        public int DaysLate { get; set; }
    }
}

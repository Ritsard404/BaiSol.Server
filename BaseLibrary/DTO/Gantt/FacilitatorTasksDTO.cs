using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class Subtask
    {
        public int Id { get; set; }

        public int TaskId { get; set; }
        public string? TaskName { get; set; }
        public DateTime? PlannedStartDate { get; set; }

        public DateTime? PlannedEndDate { get; set; }

        public DateTime? ActualStartDate { get; set; }

        public DateTime? ActualEndDate { get; set; }
        public string? StartProofImage { get; set; }
        public string? EndProofImage { get; set; }
        public int? Progress { get; set; }
        public List<Subtask>? Subtasks { get; set; }
    }

    public class FacilitatorTasksDTO
    {
        public int Id { get; set; }

        public int TaskId { get; set; }

        public string? TaskName { get; set; }
        public DateTime? PlannedStartDate { get; set; }

        public DateTime? PlannedEndDate { get; set; }

        public DateTime? ActualStartDate { get; set; }

        public DateTime? ActualEndDate { get; set; }
        public string? StartProofImage { get; set; }
        public string? EndProofImage { get; set; }

        public int? Progress { get; set; }

        public string? ProjId { get; set; }
        public List<Subtask>? Subtasks { get; set; }

    }
}

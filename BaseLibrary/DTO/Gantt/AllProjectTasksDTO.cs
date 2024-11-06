using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class AllProjectTasksDTO
    {
        public int Id { get; set; }
        public string? ProjId { get; set; }
        public string? TaskName { get; set; }
        public string? PlannedStartDate { get; set; }
        public string? PlannedEndDate { get; set; }
        public string? StartDate { get; set; }
        public string? EndDate { get; set; }
        public string? StartProofImage { get; set; }
        public string? FinishProofImage { get; set; }
        public bool? IsFinished { get; set; }

        public string? FacilitatorName { get; set; }
        public string? FacilitatorEmail { get; set; }
    }
}

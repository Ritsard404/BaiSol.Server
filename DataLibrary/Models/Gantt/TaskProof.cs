using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models.Gantt
{
    public class TaskProof
    {
        [Key]
        public int id { get; set; }
        public string? ProofImage { get; set; }
        public bool IsFinish { get; set; } = false;
        public DateTimeOffset? ActualStart { get; set; }
        public DateTimeOffset EstimationStart { get; set; } = DateTimeOffset.UtcNow;
        public int? TaskProgress { get; set; }
        public GanttData? Task { get; set; }
    }
}

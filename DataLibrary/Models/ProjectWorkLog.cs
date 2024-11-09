using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class ProjectWorkLog
    {
        [Key]
        public int Id { get; set; }
        public DateTimeOffset DateAssigned { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? WorkStarted { get; set; }
        public DateTimeOffset? WorkEnded { get; set; }
        public string? WorkEndedReason { get; set; }
        public required AppUsers AssignedByAdmin { get; set; }

        public AppUsers? Facilitator { get; set; }
        public Installer? Installer { get; set; }
        public required Project Project { get; set; }
    }
}

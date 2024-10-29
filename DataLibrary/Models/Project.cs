
using DataLibrary.Models.Gantt;
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Project
    {
        [Key]
        public string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public string Status { get; set; } = "OnGoing";
        public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public decimal? Discount { get; set; }
        public decimal? VatRate { get; set; }
        public decimal ProfitRate { get; set; } = 0.3m;
        public required string SystemType { get; set; }
        public required decimal kWCapacity { get; set; }
        public AppUsers? Client { get; set; }
        public virtual ICollection<ProjectWorkLog>? Facilitator { get; set; }
        public virtual ICollection<GanttData>? GanttData { get; set; }

        public Project()
        {
            ProjId = Guid.NewGuid().ToString();
        }
    }

}

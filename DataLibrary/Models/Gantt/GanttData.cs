using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataLibrary.Models.Gantt
{
    public class GanttData
    {
        [JsonIgnore]
        [Key]
        public int Id { get; set; }

        [JsonPropertyName("TaskId")]
        public int TaskId { get; set; }

        [JsonPropertyName("TaskName")]
        public string? TaskName { get; set; }

        [JsonPropertyName("PlannedStartDate")]
        public DateTime? PlannedStartDate { get; set; }

        [JsonPropertyName("PlannedEndDate")]
        public DateTime? PlannedEndDate { get; set; }

        [JsonPropertyName("ActualStartDate")]
        public DateTime? ActualStartDate { get; set; }

        [JsonPropertyName("ActualEndDate")]
        public DateTime? ActualEndDate { get; set; }

        [JsonPropertyName("Progress")]
        public int? Progress { get; set; }

        [JsonPropertyName("Duration")]
        public int? Duration { get; set; }

        [JsonPropertyName("Predecessor")]
        public string? Predecessor { get; set; }

        [JsonPropertyName("ParentId")]
        public int? ParentId { get; set; }

        [JsonIgnore]
        public string? ProjId { get; set; }
    }
}

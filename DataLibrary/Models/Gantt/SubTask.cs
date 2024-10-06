using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DataLibrary.Models.Gantt
{
    public class SubTask
    {
        [Key]

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

        [JsonPropertyName("Week")]
        public int? Week { get; set; }

        [JsonPropertyName("Predecessor")]
        public string? Predecessor { get; set; }
    }
}

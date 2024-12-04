using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Gantt
{
    public class UpdateTaskProgress
    {
        public int? id { get; set; }
        public IFormFile? ProofImage { get; set; }
        public int Progress { get; set; }
        public required string EstimationStart { get; set; }

        [JsonIgnore]
        public string? ipAddress { get; set; }
    }
}

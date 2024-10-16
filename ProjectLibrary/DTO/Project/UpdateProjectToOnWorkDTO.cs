using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Project
{
    public class UpdateProjectToOnWorkDTO
    {
        public required string projId { get; set; }
        [EmailAddress]
        public required string userEmail { get; set; }

        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Quote
{
    public class AssignEquipmentDto
    {
        public required int EQPTId { get; set; }
        public required int EQPTQuantity { get; set; }
        public required string ProjId { get; set; }
        [EmailAddress]
        public required string UserEmail { get; set; }

        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

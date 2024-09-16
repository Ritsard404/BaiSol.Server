using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Quote
{
    public class DeleteEquipmentSupplyDto
    {
        public required int SuppId { get; set; }
        public required int EQPTId { get; set; }
        [EmailAddress]
        public required string UserEmail { get; set; }

        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

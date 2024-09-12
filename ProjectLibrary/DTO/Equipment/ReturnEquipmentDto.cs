using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLibrary.DTO.Equipment
{
    public class EquipmentDetailDto
    {
        public required string Code { get; set; }
        public required int QOH { get; set; }
        public required decimal Price { get; set; }
    }

    public class ReturnEquipmentDto
    {
        public required string ProjId { get; set; }
        public required List<EquipmentDetailDto> EquipmentDetails { get; set; } = new();
        [EmailAddress]
        public required string UserEmail { get; set; }
        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

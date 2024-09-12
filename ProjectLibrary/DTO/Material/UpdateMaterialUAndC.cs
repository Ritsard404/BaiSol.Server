using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Material
{
    public class UpdateMaterialUAndC
    {
        public required string MTLCode { get; set; }
        public required string MTLDescript { get; set; }
        public required string MTLUnit { get; set; }
        [EmailAddress]
        public required string UserEmail { get; set; }

        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

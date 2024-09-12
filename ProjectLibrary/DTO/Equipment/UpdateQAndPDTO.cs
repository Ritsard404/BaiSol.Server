using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Equipment
{
    public class UpdateQAndPDTO
    {
        public int EQPTId { get; set; }
        public decimal EQPTPrice { get; set; }
        public int EQPTQOH { get; set; }
        [EmailAddress]
        public required string UserEmail { get; set; }
        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

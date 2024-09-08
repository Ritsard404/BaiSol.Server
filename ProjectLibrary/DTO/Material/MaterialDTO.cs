

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLibrary.DTO.Material
{
    public class MaterialDTO
    {
        public required string MTLDescript { get; set; }
        public decimal MTLPrice { get; set; }
        public int MTLQOH { get; set; }
        public required string MTLCategory { get; set; }
        public required string MTLUnit { get; set; }
        [EmailAddress]
        public required string UserEmail { get; set; }
        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProjectLibrary.DTO.Material
{
    public class UpdateQAndPMaterialDTO
    {
        public int MTLId { get; set; }
        public decimal MTLPrice { get; set; }
        public int MTLQOH { get; set; }
        public required string UserEmail { get; set; }
        [JsonIgnore]
        public required string UserIpAddress { get; set; }
    }
}

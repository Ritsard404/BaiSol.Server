using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FacilitatorLibrary.DTO.Request
{
    public class SendRequestsDTO
    {
        public class RequestDetail
        {
            public required int QuantityRequested { get; set; }
            public required int SuppId { get; set; }
        }

        public class AddRequestDTO
        {
            [EmailAddress]
            public required string SubmittedBy { get; set; }
            public required List<RequestDetail> RequestDetails { get; set; } = new();
            [JsonIgnore]
            public string? UserIpAddress { get; set; }
        }
    }
}

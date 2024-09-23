
using DataLibrary.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLibrary.DTO.Requisition
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

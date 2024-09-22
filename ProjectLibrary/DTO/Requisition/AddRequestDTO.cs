
using DataLibrary.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProjectLibrary.DTO.Requisition
{
    public class RequestDetail
    {
        public int QuantityRequested { get; set; }
        public int MtlId { get; set; }
        public int EqptId { get; set; }
    }

    public class AddRequestDTO
    {
        public required string ProjId { get; set; }
        [EmailAddress]
        public required string SubmittedBy { get; set; }
        public required List<RequestDetail>? RequestDetails { get; set; } = new();
        [JsonIgnore]
        public string? UserIpAddress { get; set; }
    }
}

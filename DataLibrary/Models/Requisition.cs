

using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Requisition
    {
        [Key]
        public int ReqId { get; set; }
        public DateTimeOffset SubmittedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? ReviewedAt { get; set; }
        public string Status { get; set; } = "OnReview";
        public int QuantityRequested { get; set; }
        public required Supply RequestSupply { get; set; }
        public required AppUsers SubmittedBy { get; set; }
        public AppUsers? ReviewedBy { get; set; }

    }
}

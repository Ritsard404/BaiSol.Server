using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class Payment
    {
        [Key]
        public required string  Id { get; set; }
        public required string checkoutUrl { get; set; }
        public  bool IsAcknowledged { get; set; } = false;
        public DateTimeOffset? AcknowledgedAt { get; set; }
        public AppUsers? AcknowledgedBy { get; set; }
        public required Project Project { get; set; }
    }
}

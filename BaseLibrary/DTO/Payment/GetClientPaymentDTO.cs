using DataLibrary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Payment
{
    public class GetClientPaymentDTO
    {
        public required string referenceNumber { get; set; }
        public required string checkoutUrl { get; set; }
        public required string amount { get; set; }
        public required string description { get; set; }
        public required string status { get; set; }
        public required string sourceType { get; set; }
        public required string createdAt { get; set; }
        public required string updatedAt { get; set; }
        public required string paidAt { get; set; }
        public required string paymentFee { get; set; }
        public bool IsAcknowledged { get; set; }
        public required string AcknowledgedBy { get; set; }
        public required string acknowledgedAt { get; set; }
    }
}

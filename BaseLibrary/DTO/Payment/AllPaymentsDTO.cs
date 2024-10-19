using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Payment
{
    public class AllPaymentsDTO
    {
        public required string referenceNumber { get; set; }
        public required string checkoutUrl { get; set; }
        public required string amount { get; set; }
        public required string netAmount { get; set; }
        public required string description { get; set; }
        public required string status { get; set; }

        public required string sourceType { get; set; }
        public required string createdAt { get; set; }
        public required string paidAt { get; set; }
        public required string paymentFee { get; set; }
        public required string paymentFeePercent { get; set; }

        public bool isAcknowledged { get; set; }
        public required string acknowledgedBy { get; set; }
        public required string acknowledgedAt { get; set; }

        public required string projId { get; set; }
        public required string projName { get; set; }
        public required string billingEmail { get; set; }
        public required string billingName { get; set; }
        public required string billingPhone { get; set; }
    }
}

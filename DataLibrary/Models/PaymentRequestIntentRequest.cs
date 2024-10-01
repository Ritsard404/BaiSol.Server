using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class PaymentRequestIntentRequest
    {
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
        public string ReceiptEmail { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class StripeAPI
    {
        public string SecretKey { get; set; }
        public string PublishableKey { get; set; }
        public string WebhookSecret { get; set; }
    }
}

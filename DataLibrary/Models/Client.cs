using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLibrary.Models
{
    public class Client
    {
        [Key]
        public int Id { get; set; }
        public required bool IsMale { get; set; }
        public required string ClientContactNum { get; set; }
        public required string  ClientAddress { get; set; }
        public virtual ICollection<AppUsers>? Admin { get; set; }


    }
}

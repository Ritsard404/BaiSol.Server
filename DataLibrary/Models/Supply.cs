
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Supply
    {
        [Key]
        public int SuppId { get; set; }
        public int? MTLQuantity { get; set; }
        public int? EQPTQuantity { get; set; }
        public virtual ICollection<Material>? Material { get; set; }
        public virtual ICollection<Equipment>? Equipment { get; set; }
        public virtual ICollection<Project>? Project { get; set; }
    }
}

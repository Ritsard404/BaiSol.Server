
using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Supply
    {
        [Key]
        public int SuppId { get; set; }
        public int? MTLQuantity { get; set; }
        public int? EQPTQuantity { get; set; }
        public required decimal Price { get; set; }
        public Material? Material { get; set; }
        public Equipment? Equipment { get; set; }
        public Project? Project { get; set; }
    }
}



using System.ComponentModel.DataAnnotations;

namespace DataLibrary.Models
{
    public class Labor
    {
        [Key]
        public int LaborId { get; set; }
        public required string LaborDescript { get; set; }
        public int LaborQOH { get; set; }
        public int LaborNumUnit { get; set; }
        public required string LaborUnit { get; set; }
        public decimal LaborCost { get; set; }
        public Project? Project { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.DTO.Report
{
    public class EquipmentReportDTO
    {
        public int SuppId { get; set; }
        public int? EQPTQuantity { get; set; }
        public required string AssignedPrice { get; set; }


        public required string ProjId { get; set; }
        public required string EQPTCode { get; set; }
        public required string EQPTDescript { get; set; }
        public required string CurrentPrice { get; set; }
        public int EQPTQOH { get; set; }
        public required string EQPTUnit { get; set; }
        public required string EQPTCategory { get; set; }
        public required string UpdatedAt { get; set; }
        public required string CreatedAt { get; set; }
    }
}

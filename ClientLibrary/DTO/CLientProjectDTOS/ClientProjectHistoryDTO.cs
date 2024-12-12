using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.DTO.CLientProjectDTOS
{
    public class ClientProjectHistoryDTO
    {
        public required string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public required decimal Discount { get; set; }
        public required decimal VatRate { get; set; }
        public required string clientId { get; set; }
        public required string clientFName { get; set; }
        public required string clientLName { get; set; }
        public required string clientEmail { get; set; }
        public required string clientContactNum { get; set; }
        public required string clientAddress { get; set; }
        public required string SystemType { get; set; }
        public required decimal kWCapacity { get; set; }
        public required string Sex { get; set; }
        public required bool isMale { get; set; }
        public decimal? PaymentProgress { get; set; }
        public decimal? ProjectProgress { get; set; }
        public string? Status { get; set; }
        public string? ProjectStarted { get; set; }
        public string? ProjectEnded { get; set; }
        public string? TotalDays { get; set; }

        public List<InstallerInfo>? Installers { get; set; }
        public string? FacilitatorName { get; set; }
        public string? FacilitatorEmail { get; set; }



    }
    public class InstallerInfo
    {
        public string? Name { get; set; }
        public string? Position { get; set; }
    }
}

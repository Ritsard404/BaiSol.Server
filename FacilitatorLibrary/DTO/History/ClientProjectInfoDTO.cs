using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.DTO.History
{
    public class ClientProjectInfoDTO
    {
        public required string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public required decimal Discount { get; set; }
        public required decimal VatRate { get; set; }
        public required string clientId { get; set; }
        public required string clientFName { get; set; }
        public required string clientLName { get; set; }
        public required string clientContactNum { get; set; }
        public required string clientAddress { get; set; }
        public required string SystemType { get; set; }
        public required decimal kWCapacity { get; set; }
        public required string Sex { get; set; }
        public required bool isMale { get; set; }
        public decimal? ProjectProgress { get; set; }
        public string? Status { get; set; }
        public required List<InstallerInfo> Installers { get; set; }
    }

    public class InstallerInfo
    {
        public required string Name { get; set; }
        public required string Position { get; set; }
    }
}

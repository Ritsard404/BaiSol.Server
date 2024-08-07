
using DataLibrary.Models;

namespace ProjectLibrary.DTO.Project
{
    public class GetProjects
    {
        public required string ProjId { get; set; }
        public required string ProjName { get; set; }
        public required string ProjDescript { get; set; }
        public required string Status { get; set; } 
        public required string UpdatedAt { get; set; }
        public required string CreatedAt { get; set; }
        public required string ClientId { get; set; }
        public required string ClientName { get; set; }
        public required string ClientAddress { get; set; }
    }
}

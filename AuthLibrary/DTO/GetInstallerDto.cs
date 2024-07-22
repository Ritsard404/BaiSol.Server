
namespace AuthLibrary.DTO
{
    public class GetInstallerDto
    {
        public int InstallerId { get; set; }
        public required string Name { get; set; }
        public required string Position { get; set; }
        public required string Status { get; set; }
        public int? AssignedProj { get; set; } // Project
        public required string AdminEmail { get; set; }
        public required string UpdatedAt { get; set; } 
        public required string CreatedAt { get; set; }

    }
}

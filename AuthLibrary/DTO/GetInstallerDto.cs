
namespace AuthLibrary.DTO
{
    public class GetInstallerDto
    {
        public int InstallerId { get; set; }
        public required string Name { get; set; }
        public required string Position { get; set; }
        public required string Status { get; set; }
        public int? AssignedProj { get; set; } // Project
        public string? AdminEmail { get; set; }
        public DateTimeOffset UpdatedAt { get; set; } 
        public DateTimeOffset CreatedAt { get; set; }

    }
}

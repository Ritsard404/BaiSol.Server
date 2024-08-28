using DataLibrary.Models;

namespace AuthLibrary.DTO.Installer
{
    public class AssignInstallerToProjectDto
    {
        public required List<int> InstallerId { get; set; }
        public required string AdminId { get; set; }
        public required string ProjectId { get; set; }
    }
}

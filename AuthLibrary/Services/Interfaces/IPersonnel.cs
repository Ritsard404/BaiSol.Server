
using AuthLibrary.DTO;
using AuthLibrary.DTO.Facilitator;
using AuthLibrary.DTO.Installer;
using DataLibrary.Models;
using static AuthLibrary.Services.Responses.PersonnelResponse;

namespace AuthLibrary.Services.Interfaces
{
    public interface IPersonnel
    {
        Task<ICollection<Installer>> GetInstallers();
        Task<ICollection<GetInstallerDto>> GetInstallersInfo();
        Task<string> AddInstaller(InstallerDto installerDto);
        Task<bool> IsInstallerExist(string name);
        Task<bool> UpdateInstallerStatus(int id, string status);
        Task<ICollection<AvailableInstallerDto>> GetAvailableInstaller();
        Task<ICollection<AvailableInstallerDto>> GetAssignednstaller(string projectId);
        Task<ICollection<AvailableFacilitatorDto>> GetAvailableFacilitator();
        Task<AvailableFacilitatorDto> GetAssignedFacilitator(string projectId);
        Task<string> AssignInstallers(AssignInstallerToProjectDto assignInstallerToProject);
        Task<string> AssignFacilitator(AssignFacilitatorToProjectDto assignFacilitatorToProject);
        Task<bool> RemoveInstallers(List<int> installerId,string projectId);
        Task<bool> RemoveFacilitator(string facilitatorId,string projectId);
        Task<bool> Save();
    }
}

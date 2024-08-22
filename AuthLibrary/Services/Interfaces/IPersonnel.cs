
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
        Task<ICollection<AvailableFacilitatorDto>> GetAvailableFacilitator();
        Task<bool> Save();
    }
}

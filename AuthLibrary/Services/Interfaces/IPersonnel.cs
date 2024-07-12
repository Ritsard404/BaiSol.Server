
using AuthLibrary.DTO;
using DataLibrary.Models;
using static AuthLibrary.Services.Responses.PersonnelResponse;

namespace AuthLibrary.Services.Interfaces
{
    public interface IPersonnel
    {
        Task<ICollection<Installer>> GetInstallers();
        Task<ICollection<GetInstallerDto>> GetInstallersInfo();
        Task<bool> AddInstaller(Installer installer);
        Task<bool> IsInstallerExist(string name);
        Task<bool> UpdateInstallerStatus(int id, string status);
        Task<bool> Save();
    }
}

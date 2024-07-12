using AuthLibrary.DTO;
using AuthLibrary.Services.Interfaces;
using AuthLibrary.Services.Responses;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthLibrary.Services.Repositories
{
    public class PersonnelRepository(DataContext _dataContext) : IPersonnel
    {
        public async Task<bool> AddInstaller(Installer installer)
        {
            _dataContext.Add(installer);
            return await Save();
        }

        public async Task<ICollection<Installer>> GetInstallers()
        {
            return await _dataContext.Installer.ToListAsync();
        }

        public async Task<ICollection<GetInstallerDto>> GetInstallersInfo()
        {
            var installers = await _dataContext.Installer.ToListAsync();
            var installerList = new List<GetInstallerDto>();

            foreach (var installer in installers)
            {
                installerList.Add(new GetInstallerDto
                {
                    InstallerId = installer.InstallerId,
                    Name = installer.Name,
                    Position = installer.Position,
                    Status = installer.Status,
                    AdminEmail = installer.Admin?.AdminEmail,
                    UpdatedAt = installer.UpdatedAt,
                    CreatedAt = installer.CreatedAt,

                });
            }
            return installerList.OrderBy(o => o.Position).ToList();
        }

        public async Task<bool> IsInstallerExist(string name)
        {
            return await _dataContext.Installer.AnyAsync(i => i.Name == name);
        }

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }

        public async Task<bool> UpdateInstallerStatus(int id, string status)
        {
            var installer = await _dataContext.Installer.FirstOrDefaultAsync(i => i.InstallerId == id);
            if (installer == null)
            {
                // Installer not found
                return false;
            }

            // Update the installer's status
            installer.Status = status;
            installer.UpdatedAt = DateTimeOffset.UtcNow;

            // Save changes to the database
            _dataContext.Installer.Update(installer);
            return await Save();
        }
    }
}

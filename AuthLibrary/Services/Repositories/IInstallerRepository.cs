
using AuthLibrary.DTO.Installer;
using AuthLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AuthLibrary.Services.Repositories
{
    public class IInstallerRepository
        (DataContext _dataContext,
        UserManager<AppUsers> _userManager
        ) : IInstaller
    {
        public async Task<ICollection<AvailableInstallerDto>> GetAvailableInstaller()
        {
            return await _dataContext.Installer
                .Where(a => a.Status == "Active")
                .OrderBy(n => n.Name)
                .ThenBy(a => a.Position)
                .Select(s => new AvailableInstallerDto { InstallerId = s.InstallerId, Name = s.Name, Position = s.Position })
                .ToListAsync();
        }
    }
}

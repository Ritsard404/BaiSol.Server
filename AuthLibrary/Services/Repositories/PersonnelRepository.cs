using AuthLibrary.DTO;
using AuthLibrary.Services.Interfaces;
using AuthLibrary.Services.Responses;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoMapper;
using AuthLibrary.DTO.Installer;
using AuthLibrary.DTO.Facilitator;
using AuthLibrary.Models;

namespace AuthLibrary.Services.Repositories
{
    public class PersonnelRepository(DataContext _dataContext,
        UserManager<AppUsers> _userManager, IMapper _mapper) : IPersonnel
    {
        public async Task<string> AddInstaller(InstallerDto installerDto)
        {
            if (installerDto == null)
            {
                throw new ArgumentNullException(nameof(installerDto));
            }

            // Check if the installer already exists
            var isInstallerExist = await IsInstallerExist(installerDto.Name);
            if (isInstallerExist)
            {
                return "Installer Already Exists";
            }

            // Fetch the AppUsers entity based on AdminEmail
            var adminCreator = await _userManager.FindByEmailAsync(installerDto.AdminEmail);
            if (adminCreator == null)
            {
                return "Admin does not exist!";
            }
            //if (adminCreator == null || !adminCreator.EmailConfirmed)
            //{
            //    return "Admin does not exist!";
            //}

            // Map DTO to model
            var installerMap = _mapper.Map<Installer>(installerDto);
            installerMap.Admin = adminCreator;

            // Add the installer to the database
            _dataContext.Installer.Add(installerMap);
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<ICollection<AvailableFacilitatorDto>> GetAvailableFacilitator()
        {
            var facilitators = await _dataContext.Users
      .Where(a => a.Status == "Active")
      .ToListAsync();

            var facilitatorList = await Task.WhenAll(facilitators
                .Select(async facilitator =>
                {
                    var roles = await _userManager.GetRolesAsync(facilitator);
                    if (roles.Contains(UserRoles.Facilitator))
                    {
                        return new AvailableFacilitatorDto
                        {
                            Id = facilitator.Id,
                            Email = facilitator.Email,
                            UserName = facilitator.NormalizedUserName
                        };
                    }
                    return null;
                }));

            return facilitatorList
                .Where(f => f != null)
                .OrderBy(n => n.UserName)
                .ToList();
        }

        public async Task<ICollection<AvailableInstallerDto>> GetAvailableInstaller()
        {
            return await _dataContext.Installer
                .Where(a => a.Status == "Active")
                .OrderBy(n => n.Name)
                .ThenBy(a => a.Position)
                .Select(s => new AvailableInstallerDto { InstallerId = s.InstallerId, Name = s.Name, Position = s.Position })
                .ToListAsync();
        }

        public async Task<ICollection<Installer>> GetInstallers()
        {
            return await _dataContext.Installer.ToListAsync();
        }

        public async Task<ICollection<GetInstallerDto>> GetInstallersInfo()
        {
            // Include the Admin property to ensure it is loaded with the Installer entities
            var installers = await _dataContext.Installer
                .Include(i => i.Admin)
                .ToListAsync();
            var installerList = new List<GetInstallerDto>();

            foreach (var installer in installers)
            {
                installerList.Add(new GetInstallerDto
                {
                    InstallerId = installer.InstallerId,
                    Name = installer.Name,
                    Position = installer.Position,
                    Status = installer.Status,
                    AdminEmail = installer.Admin?.Email,
                    UpdatedAt = installer.UpdatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                    CreatedAt = installer.CreatedAt.ToString("MMM dd, yyyy HH:mm:ss"),

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

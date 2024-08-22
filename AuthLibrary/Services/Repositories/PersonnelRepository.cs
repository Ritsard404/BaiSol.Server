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

            return await Save() ? null : "Something went wrong while saving";
        }

        public async Task<string> AssignFacilitator(AssignFacilitatorToProjectDto assignFacilitatorToProject)
        {
            if (assignFacilitatorToProject == null)
                throw new ArgumentNullException(nameof(assignFacilitatorToProject));

            // Find the facilitator by Id
            var facilitator = await _userManager.FindByIdAsync(assignFacilitatorToProject.FacilitatorId);
            if (facilitator == null)
                return "Facilitator does not exist!";

            // Find the admin by Id
            var admin = await _userManager.FindByIdAsync(assignFacilitatorToProject.AdminId);
            if (admin == null)
                return "Admin does not exist!";

            // Check if the facilitator is already assigned and working
            var isFacilitatorAssigned = await _dataContext.ProjectWorkLog
                .AnyAsync(o => o.Facilitator.Id == facilitator.Id && o.Project.ProjId == assignFacilitatorToProject.ProjectId);

            if (isFacilitatorAssigned)
                return "Facilitator is already assigned to this project";

            // Find the project by Id
            var project = await _dataContext.Project.FindAsync(assignFacilitatorToProject.ProjectId);
            if (project == null)
                return "Project does not exist!";

            // Add a new entry to the ProjectWorkLog
            _dataContext.ProjectWorkLog.Add(new ProjectWorkLog
            {
                Facilitator = facilitator,
                AssignedByAdmin = admin,
                Project = project
            });

            // Update the facilitator's status
            facilitator.Status = "OnWork";
            _dataContext.Users.Update(facilitator);

            // Save changes
            return await Save() ? null : "Something went wrong while saving";
        }

        public async Task<string> AssignInstallers(List<AssignInstallerToProjectDto> assignInstallersToProject)
        {
            // Check if the input list is null or empty
            if (assignInstallersToProject == null || !assignInstallersToProject.Any())
                return "No installers provided";

            // Retrieve the project by ID from the first DTO
            var projectId = assignInstallersToProject.First().ProjectId;
            var project = await _dataContext.Project.FindAsync(projectId);
            if (project == null)
                return "Project does not exist!";

            // Get a list of unique installer IDs and fetch their details
            var installerIds = assignInstallersToProject.Select(x => x.InstallerId).Distinct().ToList();
            var installers = await _dataContext.Installer
                .Where(i => installerIds.Contains(i.InstallerId))
                .ToDictionaryAsync(i => i.InstallerId);

            // Get a list of unique admin IDs
            var adminIds = assignInstallersToProject.Select(x => x.AdminId).Distinct().ToList();
            var results = new List<string>();

            // Iterate through each DTO to process assignments
            foreach (var dto in assignInstallersToProject)
            {
                // Check if the installer exists in the dictionary
                if (!installers.TryGetValue(dto.InstallerId, out var installer))
                {
                    results.Add($"Installer with ID {dto.InstallerId} does not exist!");
                    continue;
                }

                // Fetch admin details by ID
                var admin = await _userManager.FindByIdAsync(dto.AdminId);
                if (admin == null)
                {
                    results.Add($"Admin with ID {dto.AdminId} does not exist!");
                    continue;
                }

                // Check if the installer is already assigned to another project
                var isInstallerAssigned = await _dataContext.Installer
                    .AnyAsync(o => o.Status == "OnWork" && o.InstallerId == installer.InstallerId);

                if (isInstallerAssigned)
                {
                    results.Add($"Installer with ID {dto.InstallerId} is already assigned");
                    continue;
                }

                // Add a new project work log entry and update the installer status
                _dataContext.ProjectWorkLog.Add(new ProjectWorkLog
                {
                    Installer = installer,
                    AssignedByAdmin = admin,
                    Project = project
                });

                installer.Status = "OnWork";
                _dataContext.Installer.Update(installer);
            }

            // Save changes to the database
            await Save();

            // Return a summary of results or a success message
            return results.Any() ? string.Join("; ", results) : "Installers assigned successfully";
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

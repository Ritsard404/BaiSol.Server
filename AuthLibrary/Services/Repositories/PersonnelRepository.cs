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
            var admin = await _userManager.FindByEmailAsync(assignFacilitatorToProject.AdminEmail);
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

        public async Task<string> AssignInstallers(AssignInstallerToProjectDto assignInstallersToProject)
        {
            // Check if the input list is null or empty
            if (assignInstallersToProject.InstallerId == null || !assignInstallersToProject.InstallerId.Any())
                return "No installers provided";

            // Retrieve the project by ID from the first DTO
            var project = await _dataContext.Project.FindAsync(assignInstallersToProject.ProjectId);
            if (project == null)
                return "Project does not exist!";


            // Fetch admin details by ID
            var admin = await _userManager.FindByIdAsync(assignInstallersToProject.AdminId);
            if (admin == null)
                return "Admin does not exist!";

            // Fetch installers for this specific DTO
            var installers = await _dataContext.Installer
                .Where(i => assignInstallersToProject.InstallerId.Contains(i.InstallerId))
                .ToListAsync();

            var results = new List<string>();

            // Check for installers that are not found
            var foundInstallerIds = installers.Select(i => i.InstallerId).ToHashSet();
            var missingInstallerIds = assignInstallersToProject.InstallerId.Except(foundInstallerIds).ToList();
            if (missingInstallerIds.Any())
            {
                results.AddRange(missingInstallerIds.Select(id => $"Installer with ID {id} does not exist!"));
            }

            // Iterate through each installer to process assignments
            foreach (var installer in installers)
            {
                // Check if the installer is already assigned to another project
                if (installer.Status == "OnWork")
                {
                    results.Add($"Installer with ID {installer.InstallerId} is already assigned");
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

        public async Task<AvailableFacilitatorDto> GetAssignedFacilitator(string projectId)
        {
            // Get active facilitators who have the "Facilitator" role
            var facilitators = await _dataContext.ProjectWorkLog
                .Include(f => f.Facilitator)
                .Where(p => p.Project.ProjId == projectId)
                .ToListAsync();

            var facilitatorList = new List<AvailableFacilitatorDto>();

            // Loop through each facilitator to check if they have the 'Facilitator' role
            foreach (var facilitator in facilitators)
            {
                var roles = await _userManager.GetRolesAsync(facilitator.Facilitator);
                if (roles.Contains(UserRoles.Facilitator))
                {
                    facilitatorList.Add(new AvailableFacilitatorDto
                    {
                        Id = facilitator.Facilitator.Id,
                        Email = facilitator.Facilitator.Email,
                        UserName = facilitator.Facilitator.NormalizedUserName
                    });
                }
            }

            // Return the list, ordered by the user name
            return facilitatorList
                .OrderBy(n => n.UserName)
                .FirstOrDefault();
        }

        public async Task<ICollection<AvailableInstallerDto>> GetAssignednstaller(string projectId)
        {

            return await _dataContext.ProjectWorkLog
                .Include(i => i.Installer)
                .Where(p => p.Project.ProjId == projectId)
                .OrderBy(n => n.Installer.Name)
                .ThenBy(a => a.Installer.Position)
                .Select(s => new AvailableInstallerDto { InstallerId = s.Installer.InstallerId, Name = s.Installer.Name, Position = s.Installer.Position })
                .ToListAsync();
        }

        public async Task<ICollection<AvailableFacilitatorDto>> GetAvailableFacilitator()
        {
            // Get active facilitators who have the "Facilitator" role
            var facilitators = await _dataContext.Users
                .Where(a => a.Status == "Active")
                .ToListAsync();

            var facilitatorList = new List<AvailableFacilitatorDto>();

            // Loop through each facilitator to check if they have the 'Facilitator' role
            foreach (var facilitator in facilitators)
            {
                var roles = await _userManager.GetRolesAsync(facilitator);
                if (roles.Contains(UserRoles.Facilitator))
                {
                    facilitatorList.Add(new AvailableFacilitatorDto
                    {
                        Id = facilitator.Id,
                        Email = facilitator.Email,
                        UserName = facilitator.NormalizedUserName
                    });
                }
            }

            // Return the list, ordered by the user name
            return facilitatorList
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

        public async Task<bool> RemoveFacilitator(string facilitatorId, string projectId)
        {
            // Retrieve the first matching ProjectWorkLog entry for the specified project and facilitator
            var assignedFacilitator = await _dataContext.ProjectWorkLog
                .Include(i => i.Facilitator) // Include related Facilitator entity in the query
                .Include(p => p.Project)     // Include related Project entity in the query
                .Where(p => p.Project.ProjId == projectId && p.Facilitator.Id == facilitatorId) // Filter by project and facilitator
                .FirstOrDefaultAsync();      // Retrieve the first or default result (null if not found)

            // Check if a matching facilitator was found
            if (assignedFacilitator == null)
                return false; // Return false if no matching facilitator was found

            // Remove the facilitator entry from the ProjectWorkLog
            _dataContext.ProjectWorkLog.Remove(assignedFacilitator);

            // Retrieve the facilitator entity from the user manager
            var updateFacilitator = await _userManager.FindByIdAsync(facilitatorId);

            // Check if the facilitator entity was found
            if (updateFacilitator == null)
                return false; // Return false if no matching facilitator was found

            // Update the status of the facilitator
            updateFacilitator.Status = "Active";

            // Save the changes to the database
            return await Save();
        }

        public async Task<bool> RemoveInstallers(List<int> installerId, string projectId)
        {
            // Retrieve all ProjectWorkLog entries for the specified project and installers
            var assignedInstallers = await _dataContext.ProjectWorkLog
                .Include(i => i.Installer)
                .Include(p => p.Project)
                .Where(p => p.Project.ProjId == projectId && installerId.Contains(p.Installer.InstallerId))
                .ToListAsync();

            // Check if any matching installers exist
            if (!assignedInstallers.Any()) return false;


            // Remove all the matching ProjectWorkLog entries
            _dataContext.ProjectWorkLog.RemoveRange(assignedInstallers);

            // Retrieve the installers to update
            var installersToUpdate = await _dataContext.Installer
                .Where(i => installerId.Contains(i.InstallerId)) // Filter installers by IDs
                .ToListAsync(); // Execute the query and retrieve the installers

            // Update the status of each installer
            foreach (var installer in installersToUpdate)
            {
                installer.Status = "Active"; // Set the desired status or update any other property as needed
            }

            // Save the changes to the database and return the result
            return await Save();
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

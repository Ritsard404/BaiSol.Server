

using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace ProjectLibrary.Services.Repositories
{
    public class ProjectRepository(UserManager<AppUsers> _userManager, DataContext _dataContext) : IProject
    {
        public async Task<string> AddNewClientProject(ProjectDto projectDto)
        {
            if (projectDto == null)
            {
                throw new ArgumentNullException(nameof(projectDto));
            }

            // Check if the client exists
            var isClientExist = await _userManager.FindByIdAsync(projectDto.ClientId);
            if (isClientExist == null)
            {
                return "Client does not exist";
            }

            //// Map DTO to model
            //var projectMap = _mapper.Map<Project>(projectDto);

            // Generate unique ProjId
            string uniqueProjId;
            do
            {
                uniqueProjId = Guid.NewGuid().ToString();
            } while (await IsProjIdExist(uniqueProjId));

            //projectMap.ProjId = uniqueProjId;

            var newProject = new Project
            {
                ProjDescript = projectDto.ProjDescript,
                ProjName = projectDto.ProjName,
                ProjId = uniqueProjId,
                Client = isClientExist
            };

            // Add the new Supply entity to the context
            _dataContext.Project.Add(newProject);


            var predefinedCosts = new[]
            {
                new Labor { LaborDescript = "Manpower", LaborUnit = "Days", Project = newProject },
                new Labor { LaborDescript = "Project Manager - Electrical Engr.", LaborQuantity = 1, LaborUnit = "Days", Project = newProject },
                new Labor { LaborDescript = "Mobilization/Demob", LaborUnit = "Lot", Project = newProject },
                new Labor { LaborDescript = "Tools & Equipment", LaborUnit = "Lot", Project = newProject },
                new Labor { LaborDescript = "Other Incidental Costs", LaborUnit = "Lot", Project = newProject }
            };

            foreach (var labor in predefinedCosts)
            {
                if (!await _dataContext.Labor
                    .Include(p => p.Project)
                    .AnyAsync(proj => proj.Project.ProjDescript == newProject.ProjDescript && proj.LaborDescript == labor.LaborDescript))
                {
                    _dataContext.Labor.Add(labor);
                }
            }

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<bool> DeleteClientProject(string projId)
        {
            // Retrieve the project entity
            var project = await _dataContext.Project
                .FindAsync(projId);

            // Check if project exists and if it has associated materials or labor
            if (project == null ||
                await _dataContext.Supply.AnyAsync(s => s.Project.ProjId == projId) ||
                await _dataContext.Labor.AnyAsync(l => l.Project.ProjId == projId))
            {
                return false;
            }

            _dataContext.Project.Remove(project);

            return await Save();
        }

        public async Task<ICollection<GetProjects>> GetClientProject(string clientId)
        {
            return await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(p => p.Client.Id == clientId)
                .Select(p => new GetProjects
                {
                    ProjId = p.ProjId,
                    ProjName = p.ProjName,
                    ProjDescript = p.ProjDescript,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt.ToString("MMM dd, yyyy"),
                    UpdatedAt = p.UpdatedAt.ToString("MMM dd, yyyy"),
                    ClientId = p.Client.Id,
                    ClientName = p.Client.NormalizedUserName,
                    ClientAddress = p.Client.Client.ClientAddress
                })
                .ToListAsync();
        }

        public async Task<ICollection<GetProjects>> GetClientsProject()
        {
            var projects = await _dataContext.Project
                .Include(p => p.Client) // Include related Client data
                .Include(p => p.Client.Client)
                .OrderBy(p => p.CreatedAt) // Order by CreatedAt first
                .ToListAsync(); // Fetch the data

            // Map to DTO after fetching data
            var projectDtos = projects.Select(p => new GetProjects
            {
                ProjId = p.ProjId,
                ProjName = p.ProjName,
                ProjDescript = p.ProjDescript,
                Status = p.Status,
                CreatedAt = p.CreatedAt.ToString("MMM dd, yyyy"),
                UpdatedAt = p.UpdatedAt.ToString("MMM dd, yyyy"),
                ClientId = p.Client?.Id, // Handle potential null reference
                ClientName = p.Client.NormalizedUserName,
                ClientAddress = p.Client.Client.ClientAddress
            }).ToList();

            return projectDtos;
        }
        public async Task<bool> IsProjIdExist(string projId)
        {
            return await _dataContext.Project.AnyAsync(p => p.ProjId == projId);
        }


        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }

        public async Task<bool> UpdateClientProject(UpdateProject updateProject)
        {
            // Retrieve the project entity
            var project = await _dataContext.Project
                .FindAsync(updateProject.ProjId);

            if (project == null) return false;

            project.ProjName = updateProject.ProjName;
            project.ProjDescript = updateProject.ProjDescript;
            project.UpdatedAt = DateTimeOffset.Now;

            _dataContext.Project.Update(project);

            return await Save();
        }

        public async Task<bool> UpdatePersonnelWorkEnded(string projId, string reasonEnded)
        {
            // Find the ProjectWorkLog entries by the foreign key
            var workLogs = await _dataContext.ProjectWorkLog
                .Where(w => w.Project.ProjId == projId)
                .ToListAsync();

            if (workLogs == null || !workLogs.Any())
                return false; // Return false if no work logs were found

            // Update the WorkEnded property for each entry in the list
            foreach (var log in workLogs)
            {
                log.WorkEnded = DateTimeOffset.Now;
                log.WorkEndedReason = reasonEnded;
            }

            // Save changes to the database
            return await Save();
        }

        public async Task<bool> UpdatePersonnelWorkStarted(string projId)
        {
            // Find the ProjectWorkLog entries by the foreign key
            var workLogs = await _dataContext.ProjectWorkLog
                .Where(w => w.Project.ProjId == projId)
                .ToListAsync();

            if (workLogs == null || !workLogs.Any())
                return false; // Return false if no work logs were found

            // Update the WorkStarted property for each entry in the list
            foreach (var log in workLogs)
            {
                log.WorkStarted = DateTimeOffset.Now;
            }

            // Save changes to the database
            return await Save();
        }
    }
}

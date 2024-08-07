

using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectLibrary.DTO.Project;
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

            // Save changes to the database
            var saveResult = await Save();

            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<ICollection<GetProjects>> GetClientProject(string clientId)
        {
            return await _dataContext.Project
                .Include(p => p.Client)
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
                })
                .ToListAsync();
        }

        public async Task<ICollection<GetProjects>> GetClientsProject()
        {
            var projects = await _dataContext.Project
                .Include(p => p.Client) // Include related Client data
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
    }
}

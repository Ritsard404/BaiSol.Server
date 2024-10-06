using ClientLibrary.DTO.CLientProjectDTOS;
using ClientLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientLibrary.Services.Repositories
{
    public class ClientProjectRepository(DataContext _dataContext, UserManager<AppUsers> _userManager) : IClientProject
    {
        public async Task<ProjectId> GetClientProject(string userEmail)
        {
            return await _dataContext.Project
                .Where(p => p.Client.Email == userEmail && p.Status != "Finished") // Filter by client email first
                .Select(i => new ProjectId { projId = i.ProjId }) // Project to ProjectId after filtering
                .FirstOrDefaultAsync();
        }
    }
}

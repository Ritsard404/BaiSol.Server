using DataLibrary.Data;
using FacilitatorLibrary.DTO.History;
using FacilitatorLibrary.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.Services.Repositories
{
    public class HistoryRepository(DataContext _dataContext, IAssignedSupply _assignedProject) : IHistoryRepository
    {
        public async Task<ICollection<ClientProjectInfoDTO>> GetProjectHistories(string userEmail)
        {
            List<ClientProjectInfoDTO> clientProjectInfoList = new List<ClientProjectInfoDTO>();

            // Fetch assigned projects (only the project IDs for better performance)
            var assignedProjects = await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status == "Finished")
                .Select(e => e.Project.ProjId)
                .ToListAsync();

            // If no assigned projects are found, return an empty list
            if (assignedProjects.Count == 0)
                return clientProjectInfoList;

            // Fetch project data along with related client and other properties
            var projectDataList = await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(i => assignedProjects.Contains(i.ProjId) && i.Client.EmailConfirmed)
                .Select(d => new
                {
                    d.ProjId,
                    d.ProjName,
                    d.SystemType,
                    d.Status,
                    d.ProjDescript,
                    boolSex = d.Client.Client.IsMale,
                    fName = d.Client.FirstName,
                    lName = d.Client.LastName,
                    sex = d.Client.Client.IsMale ? "Male" : "Female",
                    Discount = d.Discount ?? 0,
                    VatRate = (d.VatRate ?? 0) * 100,
                    clientId = d.Client.Id,
                    clientContactNum = d.Client.Client.ClientContactNum,
                    clientAddress = d.Client.Client.ClientAddress,
                    kWCapacity = d.kWCapacity
                })
                .ToListAsync();

            // Loop through each project data and fetch related task info
            foreach (var projectData in projectDataList)
            {
                // Fetch tasks related to the current project
                var tasks = await _dataContext.GanttData
                    .Where(i => i.ProjId == projectData.ProjId && i.ParentId == null)
                    .ToListAsync();

                // Calculate the total progress and number of tasks
                decimal tasksProgress = tasks.Sum(p => p.Progress) ?? 0;  // Ensure Progress is not null
                int taskCount = tasks.Count;

                // Calculate the average progress
                decimal averageProgress = taskCount > 0 ? tasksProgress / taskCount : 0;

                var installers = await _dataContext.ProjectWorkLog
                   .Include(i => i.Installer)
                   .Where(w => w.Project.ProjId == projectData.ProjId && w.Installer != null)
                   .OrderBy(p => p.Installer.Position)
                   .ToListAsync();

                List<InstallerInfo> installerList = new List<InstallerInfo>();

                foreach (var installer in installers)
                {
                    installerList.Add(new InstallerInfo
                    {
                        Name = installer.Installer.Name, // Adjust based on your model properties
                        Position = installer.Installer.Position // Adjust based on your model properties
                    });
                }

                // Add project info to the list
                clientProjectInfoList.Add(new ClientProjectInfoDTO
                {
                    ProjId = projectData.ProjId,
                    ProjName = projectData.ProjName,
                    ProjDescript = projectData.ProjDescript,
                    Discount = projectData.Discount,
                    VatRate = projectData.VatRate,
                    clientId = projectData.clientId,
                    clientFName = projectData.fName,
                    clientLName = projectData.lName,
                    clientContactNum = projectData.clientContactNum,
                    clientAddress = projectData.clientAddress,
                    kWCapacity = projectData.kWCapacity,
                    Sex = projectData.sex,
                    SystemType = projectData.SystemType,
                    isMale = projectData.boolSex,
                    Status = projectData.Status,
                    ProjectProgress = averageProgress,
                    Installers= installerList
                });
            }

            return clientProjectInfoList;
        }

    }
}

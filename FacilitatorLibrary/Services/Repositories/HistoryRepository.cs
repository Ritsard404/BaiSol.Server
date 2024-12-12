﻿using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using FacilitatorLibrary.DTO.History;
using FacilitatorLibrary.DTO.Supply;
using FacilitatorLibrary.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FacilitatorLibrary.Services.Repositories
{
    public class HistoryRepository(DataContext _dataContext, IAssignedSupply _assignedProject, IGanttRepository _gantt) : IHistoryRepository
    {
        public async Task<ICollection<AllAssignedEquipmentDTO>> GetAllAssignedEquipment(string userEmail)
        {
            // Retrieve the assigned facilitator's project information based on the user email
            var assignedFacilitatorProjId = await _dataContext.ProjectWorkLog
                .Where(e => e.Facilitator.Email == userEmail && e.Project.Status != "Finished")
                .Select(e => e.Project.ProjId)
                .FirstOrDefaultAsync();

            if (assignedFacilitatorProjId == null)
                return new List<AllAssignedEquipmentDTO>();


            //var equipmentSupply = await _dataContext.Supply
            //    .Where(p => p.Project != null && p.Project.ProjId == assignedFacilitatorProjId)
            //    .Include(i => i.Equipment)
            //    .Where(e => e.Equipment != null) // Ensure Equipment is not null
            //    .ToListAsync();

            var requestsEquipment = await _dataContext.Requisition
                .Where(p => p.RequestSupply.Project != null && p.RequestSupply.Project.ProjId == assignedFacilitatorProjId)
                .Include(s => s.RequestSupply)
                .Include(s => s.RequestSupply.Equipment)
                .Include(s => s.RequestSupply.Project)
                .ToListAsync();

            var category = requestsEquipment
               .GroupBy(c => c.RequestSupply.Equipment?.EQPTCategory) // Group by category, allowing null categories
               .Select(s => new AllAssignedEquipmentDTO
               {
                   EqptCategory = s.Key ?? "Unknown", // Handle null categories
                   Details = s
                       .Where(e => e.RequestSupply.Equipment != null) // Check if Equipment is not null
                       .Select(e => new AllEquipmentSupplies
                       {

                           EqptCode = e.RequestSupply.Equipment.EQPTCode,
                           EqptDescript = e.RequestSupply.Equipment.EQPTDescript,
                           EqptUnit = e.RequestSupply.Equipment.EQPTUnit,
                           Quantity = e.RequestSupply.EQPTQuantity + e.QuantityRequested ?? 0,

                       })
                       .OrderBy(n => n.EqptDescript)
                       .ThenBy(o => o.EqptUnit)
                       .ToList()
               })
               .OrderBy(c => c.EqptCategory)
               .ToList();

            return category;
        }

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
                    kWCapacity = d.kWCapacity,
                    clientEmail = d.Client.Email
                })
                .ToListAsync();

            // Loop through each project data and fetch related task info
            foreach (var projectData in projectDataList)
            {
                // Fetch tasks for the current project
                var tasksProof = await _dataContext.TaskProof
                .Include(t => t.Task)
                    .Where(i => i.Task.ProjId == projectData.ProjId)
                    .ToListAsync();

                // Calculate the total progress
                var tasksProgress = tasksProof.Where(i => i.IsFinish).Count();

                // Calculate the number of tasks
                var taskCount = tasksProof.Count();

                // Calculate the average progress
                decimal averageProgress = (decimal)await _gantt.ProjectTaskProgress(projectData.ProjId);
                //decimal averageProgress = taskCount > 0 ? (decimal)tasksProgress / taskCount * 100 : 0;

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


                var ganttDates = await _dataContext.GanttData
                    .Where(i => i.ProjId == projectData.ProjId)
                    .Select(g => new
                    {
                        g.ActualStartDate,
                        g.ActualEndDate
                    })
                    .ToListAsync();

                var earliestStartDate = ganttDates
                    .Where(g => g.ActualStartDate.HasValue)
                    .Min(g => g.ActualStartDate);

                var latestEndDate = ganttDates
                    .Where(g => g.ActualEndDate.HasValue)
                    .Max(g => g.ActualEndDate);


                var plannedDate = await _gantt.ProjectDateInfo(projectData.ProjId);


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
                    Installers = installerList,
                    plannedStarted = plannedDate.EstimatedStartDate,
                    plannedEnded = plannedDate.EstimatedEndDate,
                    plannedWorkingDays = plannedDate.EstimatedProjectDays,
                    actualStarted = earliestStartDate.HasValue ? earliestStartDate.Value.ToString("MMMM dd, yyyy") : "",
                    actualEnded = latestEndDate.HasValue && projectData.Status == "Finished" ? latestEndDate.Value.ToString("MMMM dd, yyyy") : "",
                    actualdWorkingDays = latestEndDate.HasValue && projectData.Status == "Finished" ? CalculateActualWorkingDays(earliestStartDate.Value, latestEndDate.Value).ToString() : "",
                    clientEmail = projectData.clientEmail
                });
            }

            return clientProjectInfoList;
        }
        public int CalculateActualWorkingDays(DateTime? startDate, DateTime? endDate)
        {
            if (!startDate.HasValue || !endDate.HasValue)
                return 0;

            DateTime start = startDate.Value.Date;
            DateTime end = endDate.Value.Date;

            int daysLate = 0;

            // Loop through all days between start and end
            for (DateTime date = start; date < end; date = date.AddDays(1))
            {
                // Exclude weekends (Saturday and Sunday)
                if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                {
                    daysLate++;
                }
            }

            return daysLate;
        }

    }
}

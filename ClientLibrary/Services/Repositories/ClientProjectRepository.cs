using ClientLibrary.DTO.CLientProjectDTOS;
using ClientLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RestSharp;
using BaseLibrary.Services.Interfaces;
using System.Globalization;
using Microsoft.IdentityModel.Tokens;
using BaseLibrary.DTO.Gantt;
using ClientLibrary.DTO.Notification;

namespace ClientLibrary.Services.Repositories
{
    public class ClientProjectRepository(DataContext _dataContext, IConfiguration _config,IGanttRepository _gantt) : IClientProject
    {
        public async Task<ProjectId> GetClientProject(string userEmail)
        {
            return await _dataContext.Project
                .Where(p => p.Client.Email == userEmail && p.Status != "Finished") // Filter by client email first
                .Select(i => new ProjectId { projId = i.ProjId }) // Project to ProjectId after filtering
                .FirstOrDefaultAsync();
        }

        public async Task<ICollection<ClientProjectHistoryDTO>> GetClientProjectHistory(string userEmail)
        {
            var projects = await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(i => i.Client.EmailConfirmed == true && i.Client.NormalizedEmail == userEmail.ToUpper())
                .OrderBy(i => i.CreatedAt)
                .ToListAsync();


            var projectHistory = new List<ClientProjectHistoryDTO>();

            foreach (var project in projects)
            {
                // Fetch tasks for the current project
                var tasksProof = await _dataContext.TaskProof
                    .Include(t => t.Task)
                    .Where(i => i.Task.ProjId == project.ProjId)
                    .ToListAsync();

                // Calculate the total progress
                var tasksProgress = tasksProof.Where(i => i.IsFinish).Count();

                // Calculate the number of tasks
                var taskCount = tasksProof.Count();

                // Calculate the average progress
                decimal averageProgress = (decimal)await _gantt.ProjectTaskProgress(project.ProjId);
                //decimal averageProgress = taskCount > 0 ? (decimal)tasksProgress / taskCount * 100 : 0;

                // Step 3: Calculate payment progress
                var paymentReferences = await _dataContext.Payment
                    .Where(p => p.Project.ProjId == project.ProjId) // Ensure you use the correct navigation property
                    .ToListAsync();

                int paid = 0;

                foreach (var reference in paymentReferences)
                {
                    var options = new RestClientOptions($"{_config["Payment:API"]}/{reference.Id}");
                    var client = new RestClient(options);
                    var request = new RestRequest("");

                    request.AddHeader("accept", "application/json");
                    request.AddHeader("authorization", $"Basic {_config["Payment:Key"]}");

                    var response = await client.GetAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = JsonDocument.Parse(response.Content);
                        var data = responseData.RootElement.GetProperty("data");
                        var attributes = data.GetProperty("attributes");

                        string currentStatus = attributes.GetProperty("status").GetString();

                        if (currentStatus == "paid" || reference.IsCashPayed)
                            paid++;
                    }
                }

                decimal paymentProgress = paymentReferences.Any() ? (decimal)paid / paymentReferences.Count * 100 : 0;

                var facilitator = await _dataContext.ProjectWorkLog
                  .Include(i => i.Facilitator)
                  .FirstOrDefaultAsync(w => w.Project.ProjId == project.ProjId && w.Facilitator != null);

                var installers = await _dataContext.ProjectWorkLog
                    .Include(i => i.Installer)
                    .Where(w => w.Project.ProjId == project.ProjId && w.Installer != null)
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

                var allTask = await TasksToDo(project.ProjId);
                var startDate = allTask.FirstOrDefault();
                var endDate = allTask.LastOrDefault();

                var actualStart = await _gantt.ProjectActualWorkedDate(project.ProjId);

                // Parse the strings back to DateTime objects
                DateTime? startInDate = string.IsNullOrEmpty(startDate.StartDate)
                    ? (DateTime?)null
                    : DateTime.ParseExact(startDate.StartDate, "MMM dd, yyyy", CultureInfo.InvariantCulture);

                DateTime? endInDate = string.IsNullOrEmpty(endDate.EndDate)
                    ? (DateTime?)null
                    : DateTime.ParseExact(endDate.EndDate, "MMM dd, yyyy", CultureInfo.InvariantCulture);

                // Calculate total days if both dates are valid
                int? totalDays = (startInDate.HasValue && endInDate.HasValue)
                    ? (int?)(endInDate.Value - startInDate.Value).TotalDays
                    : null;

                // Add to the result list
                projectHistory.Add(new ClientProjectHistoryDTO
                {
                    ProjId = project.ProjId,
                    ProjName = project.ProjName,
                    ProjDescript = project.ProjDescript,
                    Discount = project.Discount ?? 0,
                    VatRate = project.VatRate ?? 0,
                    clientId = project.Client.Id,
                    clientFName = project.Client.FirstName,
                    clientLName = project.Client.LastName,
                    clientContactNum = project.Client.Client.ClientContactNum,
                    clientAddress = project.Client.Client.ClientAddress,
                    kWCapacity = project.kWCapacity,
                    Sex = project.Client.Client.IsMale ? "Male" : "Female",
                    SystemType = project.SystemType,
                    isMale = project.Client.Client.IsMale,
                    ProjectProgress = averageProgress, // Include average progress
                    PaymentProgress = paymentProgress, // Include payment progress
                    Status = project.Status,
                    Installers = installerList,
                    FacilitatorEmail = facilitator?.Facilitator?.Email,
                    FacilitatorName = $"{facilitator?.Facilitator?.FirstName} {facilitator?.Facilitator?.LastName}",
                    ProjectStarted = actualStart.ActualStartDate,
                    ProjectEnded =actualStart.ActualEndDate,
                    TotalDays = actualStart.ActualProjectDays,
                    clientEmail=project.Client.Email
                });
            }

            return projectHistory;
        }

        public async Task<NotificationDTO> NotificationMessage(int notifId)
        {
            var notification = await _dataContext.Notification
                .Include(p => p.Project)
                .FirstOrDefaultAsync(u => u.NotifId == notifId && u.Project != null);

            var facilitator = await _dataContext.ProjectWorkLog
                   .Include(f => f.Facilitator)
                   .FirstOrDefaultAsync(p => p.Project == notification.Project && p.Facilitator != null);

            return new NotificationDTO
            {
                NotifId = notification.NotifId,
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                CreatedAt = notification.CreatedAt.ToString("MMM dd, yyyy"),
                isRead = notification.isRead,
                FacilitatorEmail = facilitator?.Facilitator?.Email,
                FacilitatorName = $"{facilitator?.Facilitator?.FirstName} {facilitator?.Facilitator?.LastName}"
            };
        }

        public async Task<ICollection<NotificationDTO>> NotificationMessages(string userEmail)
        {
            var notifications = await _dataContext.Notification
                .Include(p => p.Project)
                .Where(u => u.Project.Client.Email == userEmail && u.Project != null)
                .OrderByDescending(c => c.CreatedAt)
                .ThenByDescending(p => p.Project.CreatedAt)
                .ToListAsync();

            var notifs = new List<NotificationDTO>();

            foreach (var notification in notifications)
            {
                var facilitator = await _dataContext.ProjectWorkLog
                    .Include(f => f.Facilitator)
                    .FirstOrDefaultAsync(p => p.Project == notification.Project && p.Facilitator != null);

                notifs.Add(new NotificationDTO
                {
                    NotifId = notification.NotifId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    CreatedAt = notification.CreatedAt.ToString("MMM dd, yyyy"),
                    isRead = notification.isRead,
                    FacilitatorEmail = facilitator?.Facilitator?.Email,
                    FacilitatorName = $"{facilitator?.Facilitator?.FirstName} {facilitator?.Facilitator?.LastName}"
                });
            }

            return notifs;
        }

        public async Task ReadNotif(int notifId)
        {

            var notification = await _dataContext.Notification
                .FirstOrDefaultAsync(u => u.NotifId == notifId);

            if (notification != null)
            {
                notification.isRead = true;
                await _dataContext.SaveChangesAsync();
            }
        }

        private async Task<ICollection<TasksToDoDTO>> TasksToDo(string projId)
        {

            var isProjectOnWork = await _dataContext.Project
                .AnyAsync(i => i.ProjId == projId && i.Status == "Finished");

            // If the project is not "OnWork," return an empty list
            if (!isProjectOnWork)
                return new List<TasksToDoDTO>();

            var tasks = await _dataContext.GanttData
                .Where(p => p.ProjId == projId)
                .Include(t => t.TaskProofs)
                .OrderBy(s => s.PlannedStartDate)
                .ToListAsync();

            // Find tasks that are referenced as ParentId (tasks with subtasks)
            var taskIdsWithSubtasks = tasks
                .Where(t => tasks.Any(sub => sub.ParentId == t.TaskId))
                .Select(t => t.TaskId)
                .ToHashSet();

            var toDoList = new List<TasksToDoDTO>();
            bool previousTaskCompleted = true; // To enable the first task

            foreach (var task in tasks)
            {
                // Skip tasks that have subtasks
                if (taskIdsWithSubtasks.Contains(task.TaskId))
                    continue;

                // Set IsEnable to true if the previous task is completed, otherwise false
                bool isEnable = previousTaskCompleted;


                // Create the DTO object
                var toDo = new TasksToDoDTO
                {
                    Id = task.Id,
                    TaskName = task.TaskName,
                    PlannedStartDate = task.PlannedStartDate?.ToString("MMM dd, yyyy") ?? "",
                    PlannedEndDate = task.PlannedEndDate?.ToString("MMM dd, yyyy") ?? "",
                    StartDate = task.ActualStartDate?.ToString("MMM dd, yyyy") ?? "",
                    EndDate = task.ActualEndDate?.ToString("MMM dd, yyyy") ?? "",
                    IsEnable = isEnable || (task.PlannedStartDate.HasValue && (task.PlannedStartDate.Value - DateTime.Today).Days <= 2),
                    IsFinished = task.Progress == 100,
                    IsStarting = task.ActualStartDate != null,
                };

                // Add to the final list
                toDoList.Add(toDo);

                // Update previousTaskCompleted based on the current task's progress
                previousTaskCompleted = task.Progress == 100;
            }

            return toDoList;
        }

    }
}

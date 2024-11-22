﻿using BaiSol.Server.Models.Email;
using BaseLibrary.DTO.Gantt;
using BaseLibrary.DTO.Report;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Repositories
{
    public class GanttRepository(DataContext _dataContext, IPayment _payment, IConfiguration _config, IEmailRepository _email, IUserLogs _logs) : IGanttRepository
    {
        public async Task<ICollection<FacilitatorTasksDTO>> FacilitatorTasks(string projId)
        {
            var tasks = await _dataContext.GanttData
                .Where(p => p.ProjId == projId)
                .Include(t => t.TaskProofs)
                .ToListAsync();

            var taskMap = tasks.Select(task => new FacilitatorTasksDTO
            {
                Id = task.TaskId,
                TaskId = task.TaskId,
                TaskName = task.TaskName,
                PlannedEndDate = task.PlannedStartDate,
                PlannedStartDate = task.PlannedEndDate,
                ActualStartDate = task.ActualEndDate,
                ActualEndDate = task.ActualStartDate,
                Progress = task.Progress,
                ProjId = projId,
                Subtasks = new List<Subtask>(),
                StartProofImage = task.TaskProofs?.FirstOrDefault(tp => !tp.IsFinish)?.ProofImage, // Get the start proof image
                EndProofImage = task.TaskProofs?.FirstOrDefault(tp => tp.IsFinish)?.ProofImage // Get the end proof image
            }).ToDictionary(t => t.TaskId);

            foreach (var task in tasks)
            {
                if (task.ParentId != null && taskMap.ContainsKey((int)task.ParentId))
                {
                    // Find the parent task and add the current task as a subtask
                    var parentTask = taskMap[(int)task.ParentId];
                    var subtask = new Subtask
                    {
                        Id = task.TaskId,
                        TaskId = task.TaskId,
                        TaskName = task.TaskName,
                        PlannedEndDate = task.PlannedStartDate,
                        PlannedStartDate = task.PlannedEndDate,
                        ActualStartDate = task.ActualEndDate,
                        ActualEndDate = task.ActualStartDate,
                        Progress = task.Progress,
                        Subtasks = new List<Subtask>(),
                        StartProofImage = task.TaskProofs?.FirstOrDefault(tp => !tp.IsFinish)?.ProofImage, // Get the start proof image
                        EndProofImage = task.TaskProofs?.FirstOrDefault(tp => tp.IsFinish)?.ProofImage // Get the end proof image
                    };

                    parentTask.Subtasks.Add(subtask);
                }
            }

            foreach (var task in tasks)
            {
                if (taskMap.ContainsKey(task.TaskId))
                {
                    var taskDTO = taskMap[task.TaskId];
                    taskDTO.Subtasks = GetNestedSubtasks(taskDTO.TaskId, taskMap, tasks);
                }
            }

            var topLevelTasks = taskMap.Values
                .Where(t => tasks.First(task => task.TaskId == t.TaskId).ParentId == null)
                .ToList();

            return topLevelTasks;
        }

        public async Task<(bool, string)> HandleTask(UploadTaskDTO taskDto, bool isStarting)
        {
            var task = await _dataContext.GanttData.FirstOrDefaultAsync(i => i.Id == taskDto.id);
            if (task == null)
                return (false, "Task not exist!");

            var project = await _dataContext.Project
                .Include(c => c.Client)
                .Include(c => c.Client.Client)
                .FirstOrDefaultAsync(i => i.ProjId == task.ProjId);
            if (project == null)
                return (false, "Project not exist!");


            // Update task based on whether it's starting or finishing
            if (isStarting)
            {
                task.ActualStartDate = DateTime.UtcNow;

                string notifMessage = $"Dear {project.Client.FirstName}, the task '{task.TaskName}' for your project '{project.ProjName}' has started on {task.ActualStartDate:MMMM dd, yyyy}. We’re excited to begin this phase!";
                await AddNotification($"Your Task Has Started: {task.TaskName}", notifMessage, "Task Progress Update", project);

            }
            else
            {
                task.Progress = 100;
                task.ActualEndDate = DateTime.UtcNow;

                string notifMessage = $"Dear {project.Client.FirstName}, we are happy to inform you that the task '{task.TaskName}' for your project '{project.ProjName}' has been successfully completed on {task.ActualEndDate:MMMM dd, yyyy}. Thank you for your continued trust!";
                await AddNotification($"Your Task is Complete: {task.TaskName}", notifMessage, "Task Progress Update", project);

            }

            // Save proof image
            string[] allowedFileExtentions = { ".jpg", ".jpeg", ".png" };
            string createdImageName = await SaveFileAsync(taskDto.ProofImage, allowedFileExtentions);

            var proof = new TaskProof
            {
                ProofImage = createdImageName,
                IsFinish = !isStarting,
                Task = task
            };

            // Update task and save proof
            _dataContext.GanttData.Update(task);
            await _dataContext.TaskProof.AddAsync(proof);
            await _dataContext.SaveChangesAsync();

            // If task has a parent, update the parent's actual dates based on all child levels
            if (task.ParentId.HasValue)
            {
                await UpdateParentDatesRecursive(task.ParentId.Value);
            }

            EmailMessage message;

            string greeting = $"<p>Hello {(project.Client.Client.IsMale ? "Mr." : "Ms.")} {project.Client.LastName},</p>";

            string startContent = $@"
                <p>We're excited to inform you that the task <strong>{task.TaskName}</strong> for your project <strong>{project.ProjName}</strong> has just started!</p>
                <p>Start Date: <strong>{task.ActualStartDate:MMMM dd, yyyy}</strong></p>
                <p>Please log in to our site to monitor progress and stay updated.</p>
            ";

            string finishContent = $@"
                <p>We’re pleased to inform you that the task <strong>{task.TaskName}</strong> for your project <strong>{project.ProjName}</strong> has been successfully completed!</p>
                <p>Completion Date: <strong>{task.ActualEndDate:MMMM dd, yyyy}</strong></p>
                <p>Feel free to check the site for a detailed update on your project.</p>
            ";

            string footer = @"
                <p>Best regards,<br>BaiSol Team</p>
            ";

            if (isStarting)
            {
                message = new EmailMessage(
                    new string[] { project.Client.Email },
                    "Project Task Started: " + task.TaskName,
                    $"{greeting}{startContent}{footer}"
                );
            }
            else
            {
                message = new EmailMessage(
                    new string[] { project.Client.Email },
                    "Project Task Completed: " + task.TaskName,
                    $"{greeting}{finishContent}{footer}"
                );
            }


            // Send the email if message is initialized.
            if (message != null)
            {
                _email.SendEmail(message);
            }


            var allTask = await TasksToDo(task.ProjId);
            var lastTask = allTask.Last();

            string finishProjectMessage = $@"
                    <p>We’re delighted to inform you that your project <strong>{project.ProjName}</strong> has been successfully completed!</p>
                    <p>Completion Date: <strong>{project.UpdatedAt:MMMM dd, yyyy}</strong></p>
                    <p>Thank you for trusting us with your project. We hope you’re satisfied with the results!</p>
                    <p>Feel free to review all project details on our website.</p>
                ";


            if (!string.IsNullOrEmpty(lastTask.StartDate))
            {
                project.Status = "Finished";
                project.isDemobilization = true;
                project.UpdatedAt = DateTimeOffset.UtcNow.UtcDateTime;


                await _dataContext.SaveChangesAsync();

                string notifMessage = $"Dear {project.Client.FirstName}, we are thrilled to inform you that your project '{project.ProjName}' has been successfully completed as of {project.UpdatedAt:MMMM dd, yyyy}. We truly appreciate your trust in us and look forward to working with you again in the future!";
                await AddNotification(
                        "Project Finished: " + project.ProjName,  // Title of the notification
                        notifMessage,                               // Message content
                        "Project Update",                           // Type of notification
                        project
                    );

                message = new EmailMessage(
                    new string[] { project.Client.Email },
                    "Project Finished",
                    $"{greeting}{finishProjectMessage}{footer}");

                return (true, "All the tasks complete! The project is Finished.");
            }

            // Return appropriate message
            return isStarting
                ? (true, "Task started! Your report submitted to the admin.")
                : (true, "Task finished! Your report submitted to the admin.");
        }

        public async Task<(bool, string)> SubmitTaskReport(UploadTaskDTO taskDto)
        {

            var task = await _dataContext.TaskProof
                .Include(g => g.Task)
                .FirstOrDefaultAsync(i => i.id == taskDto.id);
            if (task == null)
                return (false, "Task not exist!");

            var project = await _dataContext.Project
                .Include(c => c.Client)
                .Include(c => c.Client.Client)
                .FirstOrDefaultAsync(i => i.ProjId == task.Task.ProjId);
            if (project == null)
                return (false, "Project not exist!");

            // Save proof image
            string[] allowedFileExtentions = { ".jpg", ".jpeg", ".png" };
            string createdImageName = await SaveFileAsync(taskDto.ProofImage, allowedFileExtentions);

            task.ProofImage = createdImageName;
            task.IsFinish = true;
            task.ActualStart = DateTimeOffset.UtcNow;

            // Update gantt task
            task.Task.Progress = task.TaskProgress;
            if (!task.Task.ActualStartDate.HasValue)
            {
                task.Task.ActualStartDate = DateTime.UtcNow;
            }

            if (task.TaskProgress == 100)
            {
                task.Task.ActualEndDate = DateTime.UtcNow;
            }

            await _dataContext.SaveChangesAsync();

            // If task has a parent, update the parent's actual dates based on all child levels
            if (task.Task.ParentId.HasValue)
            {
                await UpdateParentDatesRecursive(task.Task.ParentId.Value);
            }

            EmailMessage message;

            string greeting = $"<p>Hello {(project.Client.Client.IsMale ? "Mr." : "Ms.")} {project.Client.LastName},</p>";

            string messageContent = $@"
                <p>We are excited to inform you that the task <strong>{task.Task.TaskName}</strong> for your solar installation project <strong>{project.ProjName}</strong> is now at <strong>{task.TaskProgress}%</strong> completion!</p>
                <p>The latest update was made on <strong>{task.ActualStart:MMMM dd, yyyy}</strong>. We are making great progress, and your project is moving forward as planned!</p>
                <p>Please log in to your account on our site to monitor the ongoing progress and get the latest updates on your project.</p>
            ";

            string footer = @"
                <p>Best regards,<br>The BaiSol Team</p>
                <p>If you have any questions, feel free to reach out to us.</p>
            ";

            message = new EmailMessage(
                   new string[] { project.Client.Email },
                   "Project Progress Update: " + task.Task.TaskName,
                   $"{greeting}{messageContent}{footer}"
               );

            _email.SendEmail(message);


            //var allTask = await TaskToDo(project.ProjId);

            //var lastTaskGroup = allTask.LastOrDefault();
            //if (lastTaskGroup == null || lastTaskGroup.TaskList == null || !lastTaskGroup.TaskList.Any())
            //{
            //    return (true, "Task finished! Your report submitted to the admin.");
            //}

            //var lastTask = lastTaskGroup.TaskList.LastOrDefault();

            var allFinished = await _dataContext.TaskProof
               .Where(t => t.Task.ProjId == project.ProjId)
               .AllAsync(t => t.IsFinish);

            string finishProjectMessage = $@"
                    <p>We’re delighted to inform you that your project <strong>{project.ProjName}</strong> has been successfully completed!</p>
                    <p>Completion Date: <strong>{project.UpdatedAt:MMMM dd, yyyy}</strong></p>
                    <p>Thank you for trusting us with your project. We hope you’re satisfied with the results!</p>
                    <p>Feel free to review all project details on our website.</p>
                ";

            if (allFinished)
            {
                project.Status = "Finished";
                project.isDemobilization = true;
                project.UpdatedAt = DateTimeOffset.UtcNow.UtcDateTime;


                await _dataContext.SaveChangesAsync();

                string notifMessageFinish = $"Dear {project.Client.FirstName}, we are thrilled to inform you that your project '{project.SystemType} {project.kWCapacity}' has been successfully completed as of {project.UpdatedAt:MMMM dd, yyyy}. We truly appreciate your trust in us and look forward to working with you again in the future!";
                await AddNotification(
                        "Project Finished: " + project.ProjName,  // Title of the notification
                        notifMessageFinish,                               // Message content
                        "Project Update",                           // Type of notification
                        project
                    );

                message = new EmailMessage(
                    new string[] { project.Client.Email },
                    "Project Finished",
                    $"{greeting}{finishProjectMessage}{footer}");

                return (true, "All the tasks complete! The project is Finished.");
            }

            string notifMessage = $"Dear {project.Client.FirstName}, we are pleased to inform you that the task '{task.Task.TaskName}' for your project '{project.SystemType} {project.kWCapacity} kW' is now {task.TaskProgress}% complete as of {task.ActualStart:MMMM dd, yyyy}. We deeply value your trust and partnership. Thank you!";
            await AddNotification($"Project Update: {task.Task.TaskName}", notifMessage, "Task Progress Update", project);

            return (true, "Task finished! Your report submitted to the admin.");
        }

        public async Task<string> SaveFileAsync(IFormFile imageFile, string[] allowedFileExtensions)
        {
            // Validate the input file
            if (imageFile == null)
            {
                throw new ArgumentNullException(nameof(imageFile), "The uploaded file cannot be null.");
            }

            // Get the content root path from the environment
            var contentPath = @"C:\Users\Acer\Documents\Capstone\Sunvoltage System\Images";

            // Combine the content path with the desired uploads directory
            var uploadsPath = Path.Combine(contentPath, "Uploads");

            // Create the uploads directory if it doesn't exist
            if (!Directory.Exists(uploadsPath))
            {
                Directory.CreateDirectory(uploadsPath);
            }

            // Check the allowed file extensions
            var ext = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
            if (!allowedFileExtensions.Contains(ext))
            {
                throw new ArgumentException($"Only the following file extensions are allowed: {string.Join(", ", allowedFileExtensions)}.", nameof(imageFile));
            }

            // Generate a unique filename
            var fileName = $"{Guid.NewGuid()}{ext}";
            var fileNameWithPath = Path.Combine(uploadsPath, fileName);

            // Save the file to the specified path
            using var stream = new FileStream(fileNameWithPath, FileMode.Create);
            await imageFile.CopyToAsync(stream);

            // Return the unique filename (without path) for reference
            return fileName;
        }

        private async Task UpdateParentDatesRecursive(int parentId)
        {
            var parentTask = await _dataContext.GanttData.FirstOrDefaultAsync(p => p.TaskId == parentId);
            if (parentTask == null) return;

            // Fetch all descendant tasks
            var allDescendants = await GetAllDescendants(parentTask.TaskId);

            // Find the earliest ActualStartDate and latest ActualEndDate among descendants
            var earliestStartDate = allDescendants
                                    .Where(ct => ct.ActualStartDate.HasValue)
                                    .Min(ct => ct.ActualStartDate);

            var latestEndDate = allDescendants
                                .Where(ct => ct.ActualEndDate.HasValue)
                                .Max(ct => ct.ActualEndDate);

            // Update parent's ActualStartDate if necessary
            if (earliestStartDate.HasValue &&
                (!parentTask.ActualStartDate.HasValue || parentTask.ActualStartDate > earliestStartDate.Value))
            {
                parentTask.ActualStartDate = earliestStartDate.Value;
            }

            // Update parent's ActualEndDate if necessary
            if (latestEndDate.HasValue &&
                (!parentTask.ActualEndDate.HasValue || parentTask.ActualEndDate < latestEndDate.Value))
            {
                parentTask.ActualEndDate = latestEndDate.Value;
            }

            // Save parent task if updated
            _dataContext.GanttData.Update(parentTask);
            await _dataContext.SaveChangesAsync();

            // Continue updating up the hierarchy if the parent has its own parent
            if (parentTask.ParentId.HasValue)
            {
                await UpdateParentDatesRecursive(parentTask.ParentId.Value);
            }
        }

        // Helper method to get all descendant tasks of a parent task
        private async Task<List<GanttData>> GetAllDescendants(int parentId)
        {
            var descendants = new List<GanttData>();
            var directChildren = await _dataContext.GanttData
                                                   .Where(ct => ct.ParentId == parentId)
                                                   .ToListAsync();
            descendants.AddRange(directChildren);

            foreach (var child in directChildren)
            {
                var childDescendants = await GetAllDescendants(child.TaskId);
                descendants.AddRange(childDescendants);
            }

            return descendants;
        }

        public async Task<TaskProof> TaskById(int id)
        {
            var task = await _dataContext.TaskProof
                .FirstOrDefaultAsync(i => i.id == id);

            return task;
        }

        public async Task<ICollection<TasksToDoDTO>> TasksToDo(string projId)
        {

            var isProjectOnWork = await _dataContext.Project
                .AnyAsync(i => i.ProjId == projId && i.Status == "OnWork");

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
        public async Task<ICollection<TaskToDoDTO>> TaskToDo(string projId)
        {

            var isProjectOnWork = await _dataContext.Project
                //.AnyAsync(i => i.ProjId == projId && i.Status == "OnWork");
                .AnyAsync(i => i.ProjId == projId);

            // If the project is not "OnWork," return an empty list
            if (!isProjectOnWork)
                return new List<TaskToDoDTO>();

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

            var toDoList = new List<TaskToDoDTO>();
            bool previousTaskCompleted = true;

            foreach (var task in tasks)
            {
                // Skip tasks that have subtasks
                if (taskIdsWithSubtasks.Contains(task.TaskId))
                    continue;

                // Set IsEnable to true if the previous task is completed, otherwise false
                bool isEnable = previousTaskCompleted;

                var taskToDo = await _dataContext.TaskProof
                    .Where(t => t.Task == task)
                    .OrderBy(s => s.EstimationStart)
                    .ToListAsync();

                var tasksList = new List<TaskDTO>();
                var isPrevTaskComplete = false;

                foreach (var taskItem in taskToDo)
                {
                    bool isEnableTask = isPrevTaskComplete;
                    // Ensure the first subtask is enabled if the parent task is enabled
                    if ((isEnable || task.PlannedStartDate.HasValue && (task.PlannedStartDate.Value - DateTime.Today).Days <= 2) && taskToDo.IndexOf(taskItem) == 0)
                    {
                        isEnableTask = true; // Enable the first subtask regardless of its completion
                    }

                    var daysLate = 0;

                    // Calculate days late only if ActualStart and EstimationStart are not null
                    if (taskItem.ActualStart.HasValue)
                    {
                        daysLate = (taskItem.ActualStart.Value - taskItem.EstimationStart).Days;
                    }

                    tasksList.Add(new TaskDTO
                    {
                        id = taskItem.id,
                        ProofImage = taskItem.ProofImage,
                        ActualStart = taskItem.ActualStart?.ToString("MMM dd, yyyy") ?? "",
                        EstimationStart = taskItem.EstimationStart.ToString("MMM dd, yyyy") ?? "",
                        TaskProgress = taskItem.TaskProgress ?? 0,
                        IsFinish = taskItem.IsFinish,
                        IsEnable = isEnableTask,
                        IsLate = taskItem.EstimationStart < taskItem.ActualStart,
                        DaysLate = daysLate


                    });
                    isPrevTaskComplete = taskItem.IsFinish;

                }

                // Create the DTO object
                var toDo = new TaskToDoDTO
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
                    DaysLate = task.PlannedStartDate.HasValue && task.ActualStartDate.HasValue && task.ActualStartDate > task.PlannedStartDate
                        ? (task.PlannedStartDate.Value - task.ActualStartDate.Value).Days
                        : 0,
                    TaskList = tasksList
                };

                // Add to the final list
                toDoList.Add(toDo);

                // Update previousTaskCompleted based on the current task's progress
                previousTaskCompleted = task.Progress == 100;
            }

            return toDoList;
        }

        private List<Subtask> GetNestedSubtasks(int parentId, Dictionary<int, FacilitatorTasksDTO> taskMap, List<GanttData> tasks)
        {
            var subtasks = tasks
                .Where(t => t.ParentId == parentId)
                .Select(t => new Subtask
                {
                    Id = t.TaskId,
                    TaskId = t.TaskId,
                    TaskName = t.TaskName,
                    PlannedEndDate = t.PlannedStartDate,
                    PlannedStartDate = t.PlannedEndDate,
                    ActualStartDate = t.ActualEndDate,
                    ActualEndDate = t.ActualStartDate,
                    Progress = t.Progress,
                    StartProofImage = t.TaskProofs?.FirstOrDefault(tp => !tp.IsFinish)?.ProofImage, // Get the start proof image
                    EndProofImage = t.TaskProofs?.FirstOrDefault(tp => tp.IsFinish)?.ProofImage, // Get the end proof image
                    Subtasks = GetNestedSubtasks(t.TaskId, taskMap, tasks)
                })
                .ToList();

            return subtasks;
        }

        public async Task<ProjectDateInfo> ProjectDateInfo(string projId)
        {
            var project = await _dataContext.Project
                .Include(f => f.Facilitator)
                .FirstOrDefaultAsync(id => id.ProjId == projId);
            if (project == null)
                return null;

            var paymentReference = await _dataContext.Payment
                .OrderBy(p => p.AcknowledgedAt)
                .FirstOrDefaultAsync(p => p.Project == project && p.AcknowledgedAt != null);
            if (paymentReference == null)
                return null;

            var facilitator = await _dataContext.ProjectWorkLog
                .Include(f => f.Facilitator)
                .FirstOrDefaultAsync(p => p.Project.ProjId == projId && p.Facilitator != null);

            if (facilitator?.Facilitator == null)
                return null;

            var paymentReferences = await _dataContext.Payment
                .Where(p => p.Project == project)
                .ToListAsync();

            var totalAmount = await _payment.GetTotalProjectExpense(projId: projId);

            string payRef = string.Empty;
            string status = string.Empty;
            int createdAt = 0;

            var options = new RestClientOptions($"{_config["Payment:API"]}/{paymentReference.Id}");
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

                decimal amount = attributes.GetProperty("amount").GetDecimal() / 100m;
                string currentStatus = attributes.GetProperty("status").GetString();
                createdAt = attributes.GetProperty("created_at").GetInt32();

                status = currentStatus;
                payRef = paymentReference.Id;
            }

            int estimatedDaysToEnd = project.kWCapacity <= 5 ? 7
                   : project.kWCapacity >= 6 && project.kWCapacity <= 10 ? 15
                   : project.kWCapacity >= 11 && project.kWCapacity <= 15 ? 25
                   : 35;

            DateTimeOffset CalculateEndDateExcludingWeekends(DateTimeOffset startDate, int daysToAdd)
            {
                DateTimeOffset endDate = startDate;
                int daysAdded = 0;

                while (daysAdded < daysToAdd)
                {
                    endDate = endDate.AddDays(1);
                    if (endDate.DayOfWeek != DayOfWeek.Saturday && endDate.DayOfWeek != DayOfWeek.Sunday)
                    {
                        daysAdded++;
                    }
                }

                return endDate;
            }

            ProjectDateInfo info;

            if (paymentReference.IsCashPayed)
            {
                DateTimeOffset startDate = paymentReference.CashPaidAt ?? DateTimeOffset.UtcNow;
                DateTimeOffset endDate = CalculateEndDateExcludingWeekends(startDate, estimatedDaysToEnd);

                info = new ProjectDateInfo
                {
                    AssignedFacilitator = facilitator.Facilitator.Email ?? "No Facilitator Assigned",
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd"),
                    EstimatedStartDate = startDate.ToString("MMMM dd, yyyy"),
                    EstimatedEndDate = endDate.ToString("MMMM dd, yyyy"),
                    EstimatedProjectDays = estimatedDaysToEnd.ToString()
                };
            }
            else
            {
                DateTimeOffset startDate = DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime;
                DateTimeOffset endDate = CalculateEndDateExcludingWeekends(startDate, estimatedDaysToEnd);

                info = new ProjectDateInfo
                {
                    AssignedFacilitator = facilitator.Facilitator.Email ?? "No Facilitator Assigned",
                    StartDate = startDate.ToString("yyyy-MM-dd"),
                    EndDate = endDate.ToString("yyyy-MM-dd"),
                    EstimatedStartDate = startDate.ToString("MMMM dd, yyyy"),
                    EstimatedEndDate = endDate.ToString("MMMM dd, yyyy"),
                    EstimatedProjectDays = estimatedDaysToEnd.ToString()
                };
            }

            return info;
        }

        public async Task<ProjProgressDTO> ProjectProgress(string projId)
        {
            // Get the tasks that match the criteria
            var tasks = await _dataContext.GanttData
                .Where(i => i.ProjId == projId && i.ParentId == null)
                .ToListAsync();

            // Calculate the total progress
            var tasksProgress = tasks.Sum(p => p.Progress) ?? 0;

            // Calculate the number of tasks
            var taskCount = tasks.Count;

            // Calculate the average progress
            decimal averageProgress = taskCount > 0 ? tasksProgress / taskCount : 0;

            // Return the result
            return new ProjProgressDTO { progress = averageProgress };
        }

        public async Task<ProjectStatusDTO> ProjectStatus(string projId)
        {
            var projectDateInfo = await ProjectDateInfo(projId);


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

            var toDoList = new List<ProjectTasks>();

            foreach (var task in tasks)
            {
                // Skip tasks that have subtasks
                if (taskIdsWithSubtasks.Contains(task.TaskId))
                    continue;


                // Get the first proof marked as finished
                var finishProof = task.TaskProofs?.FirstOrDefault(proof => proof.IsFinish);

                // Get the first proof marked as not finished
                var startProof = task.TaskProofs?.FirstOrDefault(proof => !proof.IsFinish);

                // Create the DTO object
                var toDo = new ProjectTasks
                {
                    Id = task.Id,
                    TaskName = task.TaskName,
                    PlannedStartDate = task.PlannedStartDate?.ToString("MMM dd, yyyy"),
                    PlannedEndDate = task.PlannedEndDate?.ToString("MMM dd, yyyy"),
                    StartDate = task.ActualStartDate?.ToString("MMM dd, yyyy"),
                    EndDate = task.ActualEndDate?.ToString("MMM dd, yyyy"),
                    IsFinished = task.Progress == 100,
                    FinishProofImage = finishProof?.ProofImage,
                    StartProofImage = startProof?.ProofImage
                };

                // Add to the final list
                toDoList.Add(toDo);

            }


            return new ProjectStatusDTO
            {
                Info = projectDateInfo,
                Tasks = toDoList

            };
        }

        private async Task AddNotification(string title, string message, string type, Project project)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                CreatedAt = DateTimeOffset.UtcNow,
                Project = project
            };

            _dataContext.Notification.Add(notification);
            await _dataContext.SaveChangesAsync();
        }

        public async Task<(bool, string)> UpdateTaskProgress(UpdateTaskProgress taskDto)
        {

            var task = await _dataContext.GanttData
                .FirstOrDefaultAsync(i => i.Id == taskDto.id);
            if (task == null)
                return (false, "Task not exist!");

            var project = await _dataContext.Project
                .Include(c => c.Client)
                .Include(c => c.Client.Client)
                .FirstOrDefaultAsync(i => i.ProjId == task.ProjId);
            if (project == null)
                return (false, "Project not exist!");

            if (taskDto.Progress < 0 || taskDto.Progress > 100 || taskDto.Progress < task.Progress)
                return (false, "Invalid inputted progress!");

            var assignedFacilitator = await _dataContext.ProjectWorkLog
                .Include(i => i.Facilitator)
                .FirstOrDefaultAsync(f => f.Project == project && f.Facilitator != null);
            if (assignedFacilitator == null)
                return (false, "No facilitator assigned!");


            task.Progress = taskDto.Progress;
            if (!task.ActualStartDate.HasValue)
            {
                task.ActualStartDate = DateTime.UtcNow;
            }
            task.ActualEndDate = DateTime.UtcNow;

            string[] allowedFileExtentions = { ".jpg", ".jpeg", ".png" };
            string createdImageName = await SaveFileAsync(taskDto.ProofImage, allowedFileExtentions);

            var taskProof = new TaskProof
            {
                ProofImage = createdImageName,
                TaskProgress = taskDto.Progress,
                Task = task,
                IsFinish = true,
                ActualStart = DateTimeOffset.UtcNow
            };

            await _dataContext.TaskProof.AddAsync(taskProof);

            await _dataContext.SaveChangesAsync();


            EmailMessage message;

            string greeting = $"<p>Hello {(project.Client.Client.IsMale ? "Mr." : "Ms.")} {project.Client.LastName},</p>";

            string messageContent = $@"
                <p>We are excited to inform you that the task <strong>{task.TaskName}</strong> for your solar installation project <strong>{project.ProjName}</strong> is now at <strong>{task.Progress}%</strong> completion!</p>
                <p>The latest update was made on <strong>{DateTimeOffset.UtcNow:MMMM dd, yyyy}</strong>. We are making great progress, and your project is moving forward as planned!</p>
                <p>Please log in to your account on our site to monitor the ongoing progress and get the latest updates on your project.</p>
            ";

            string footer = @"
                <p>Best regards,<br>The BaiSol Team</p>
                <p>If you have any questions, feel free to reach out to us.</p>
            ";

            message = new EmailMessage(
                   new string[] { project.Client.Email },
                   "Project Progress Update: " + task.TaskName,
                   $"{greeting}{messageContent}{footer}"
               );

            _email.SendEmail(message);

            var tasks = await _dataContext.GanttData
              .Where(p => p.ProjId == project.ProjId)
              .Include(t => t.TaskProofs)
              .OrderBy(s => s.PlannedStartDate)
              .ToListAsync();

            // Find tasks that are referenced as ParentId (tasks with subtasks)
            var taskIdsWithSubtasks = tasks
                .Where(t => tasks.Any(sub => sub.ParentId == t.TaskId))
                .Select(t => t.TaskId)
                .ToHashSet();

            var totalProgress = 0;
            var taskCount = 0;

            foreach (var taskItem in tasks)
            {
                // Skip tasks that have subtasks
                if (taskIdsWithSubtasks.Contains(taskItem.TaskId) || !taskItem.PlannedStartDate.HasValue)
                    continue;

                taskCount++;
                totalProgress += task.Progress ?? 0;
            }

            var isFinishTask = taskCount > 0 && (totalProgress / taskCount) == 100;

            //var isFinishTask = await _dataContext.GanttData
            //    .AnyAsync(p => p.Progress == 100);

            string finishProjectMessage = $@"
                    <p>We’re delighted to inform you that your project <strong>{project.ProjName}</strong> has been successfully completed!</p>
                    <p>Completion Date: <strong>{project.UpdatedAt:MMMM dd, yyyy}</strong></p>
                    <p>Thank you for trusting us with your project. We hope you’re satisfied with the results!</p>
                    <p>Feel free to review all project details on our website.</p>
                ";

            if (isFinishTask)
            {
                project.Status = "Finished";
                project.isDemobilization = true;
                project.UpdatedAt = DateTimeOffset.UtcNow.UtcDateTime;

                await _dataContext.SaveChangesAsync();

                string notifMessageFinish = $"Dear {project.Client.FirstName}, we are thrilled to inform you that your project '{project.SystemType} {project.kWCapacity}' has been successfully completed as of {project.UpdatedAt:MMMM dd, yyyy}. We truly appreciate your trust in us and look forward to working with you again in the future!";
                await AddNotification(
                        "Project Finished: " + project.ProjName,  // Title of the notification
                        notifMessageFinish,                               // Message content
                        "Project Update",                           // Type of notification
                        project
                    );

                message = new EmailMessage(
                    new string[] { project.Client.Email },
                    "Project Finished",
                    $"{greeting}{finishProjectMessage}{footer}");

                await _logs.LogUserActionAsync(assignedFacilitator.Facilitator.Email, "Update", "project", task.Id.ToString(), "Finish project", taskDto.ipAddress);

                return (true, "All the tasks complete! The project is Finished.");
            }

            string notifMessage = $"Dear {project.Client.FirstName}, we are pleased to inform you that the task '{task.TaskName}' for your project '{project.SystemType} {project.kWCapacity} kW' is now {task.Progress}% complete as of {DateTimeOffset.UtcNow:MMMM dd, yyyy}. We deeply value your trust and partnership. Thank you!";
            await AddNotification($"Project Update: {task.TaskName}", notifMessage, "Task Progress Update", project);



            await _logs.LogUserActionAsync(assignedFacilitator.Facilitator.Email, "Update", "Task", task.Id.ToString(), "Update project task progress", taskDto.ipAddress);
            return (true, "Task progress updated! Your report submitted to the admin.");
        }
    }
}

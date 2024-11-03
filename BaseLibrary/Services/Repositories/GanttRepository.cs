using BaiSol.Server.Models.Email;
using BaseLibrary.DTO.Gantt;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Text.Json;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Repositories
{
    public class GanttRepository(DataContext _dataContext, IPayment _payment, IConfiguration _config, IEmailRepository _email) : IGanttRepository
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
            }
            else
            {
                task.Progress = 100;
                task.ActualEndDate = DateTime.UtcNow;
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


            //var message = new EmailMessage(new string[] { "richardquirante98@gmail.com" }, "Test", "<h1>Test ra goy</h1>");
            //_emailRepository.SendEmail(message);

            // Return appropriate message
            return isStarting
                ? (true, "Task started! Your report submitted to the admin.")
                : (true, "Task finished! Your report submitted to the admin.");
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

            //var assignedFacilitatorProjId = await _dataContext.ProjectWorkLog
            //    .Where(e => e.Facilitator.Email == userEmail && e.Project.Status != "Finished")
            //    .Select(e => e.Project.ProjId) // Only select the project ID
            //    .FirstOrDefaultAsync();

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
                    PlannedStartDate = task.PlannedStartDate?.ToString("MMM dd, yyyy"),
                    PlannedEndDate = task.PlannedEndDate?.ToString("MMM dd, yyyy"),
                    StartDate = task.ActualStartDate?.ToString("MMM dd, yyyy"),
                    EndDate = task.ActualEndDate?.ToString("MMM dd, yyyy"),
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
                .ThenInclude(wl => wl.Facilitator)
                .FirstOrDefaultAsync(id => id.ProjId == projId);
            if (project == null)
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
            //totalAmount = 100000;

            string payRef = string.Empty;
            string status = string.Empty;
            int createdAt = 0;


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

                    decimal amount = attributes.GetProperty("amount").GetDecimal() / 100m;
                    string currentStatus = attributes.GetProperty("status").GetString();
                    createdAt = attributes.GetProperty("created_at").GetInt32();

                    // Update largest amount and status if a larger amount is found
                    if (amount == (totalAmount * 0.6m))
                    {
                        status = currentStatus;
                        payRef = reference.Id;
                    }
                }
            }

            var payment = await _dataContext.Payment
                .FirstOrDefaultAsync(i => i.Id == payRef);
            if (payment == null)
                return null;

            if (status != "paid" && !payment.IsCashPayed)
                return null;

            int estimatedDaysToEnd = project.kWCapacity <= 5 ? 7
                   : project.kWCapacity >= 6 && project.kWCapacity <= 10 ? 15
                   : project.kWCapacity >= 11 && project.kWCapacity <= 15 ? 25
                   : 35;

            ProjectDateInfo info;

            if (payment.IsCashPayed)
            {
                info = new ProjectDateInfo
                {
                    AssignedFacilitator = facilitator.Facilitator.Email ?? "No Facilitator Assigned",
                    StartDate = payment.CashPaidAt?.ToString("yyyy-MM-dd"),
                    EstimatedStartDate = payment.CashPaidAt?.AddDays(2).ToString("MMMM dd, yyyy"),
                    EstimatedEndDate = payment.CashPaidAt?.AddDays(estimatedDaysToEnd + 2).ToString("MMMM dd, yyyy")
                };
            }
            else
            {
                info = new ProjectDateInfo
                {
                    AssignedFacilitator = facilitator.Facilitator.Email ?? "No Facilitator Assigned",
                    StartDate = DateTimeOffset.FromUnixTimeSeconds(createdAt).UtcDateTime.ToString("yyyy-MM-dd"),
                    EstimatedStartDate = DateTimeOffset.FromUnixTimeSeconds(createdAt).AddDays(2).UtcDateTime.ToString("MMMM dd, yyyy"),
                    EstimatedEndDate = project.CreatedAt.AddDays(9).AddDays(estimatedDaysToEnd + 2).UtcDateTime.ToString("MMMM dd, yyyy")
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
    }
}

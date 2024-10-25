using BaseLibrary.DTO.Gantt;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Repositories
{
    public class GanttRepository(DataContext _dataContext) : IGanttRepository
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

            // Update task based on whether it's starting or finishing
            if (isStarting)
            {
                task.ActualStartDate = DateTime.Now;
            }
            else
            {
                task.Progress = 100;
                task.ActualEndDate = DateTime.Now;
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


        public async Task<TaskProof> TaskById(int id)
        {
            var task = await _dataContext.TaskProof
                .FirstOrDefaultAsync(i => i.id == id);

            return task;
        }

        public async Task<ICollection<TasksToDoDTO>> TasksToDo(string projId)
        {
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
                    IsEnable = isEnable,
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
    }
}

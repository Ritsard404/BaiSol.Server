using BaseLibrary.DTO.Gantt;
using BaseLibrary.DTO.Report;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseLibrary.Services.Repositories
{
    public class ReportRepository(DataContext _dataContext) : IReportRepository
    {
        public async Task<ProjectCounts> AllProjectsCount()
        {
            var projects = await _dataContext.Project.CountAsync();

            var finishedProjects = await _dataContext.Project.CountAsync(w => w.Status == "Finished");

            return new ProjectCounts { AllProject = projects, FinishedProject = finishedProjects };
        }

        public async Task<ICollection<AllProjectTasksDTO>> AllProjectTasksReport()
        {
            var tasks = await _dataContext.GanttData
                .Include(t => t.TaskProofs)
                .OrderBy(s => s.ProjId)
                .ThenBy(s => s.PlannedStartDate)
                .ToListAsync();

            var parentIds = tasks.Select(t => t.ParentId).Where(id => id.HasValue).Select(id => id.Value).ToHashSet();

            var projectLogs = await _dataContext.ProjectWorkLog
                .Include(p => p.Facilitator)
                .Where(pl => tasks.Select(t => t.ProjId).Contains(pl.Project.ProjId))
                .ToListAsync();

            var reportTasksLists = tasks
                .Where(t => !parentIds.Contains(t.TaskId)) // Skip tasks with subtasks
                .Select(task =>
                {
                    var project = projectLogs.FirstOrDefault(pl => pl.Project.ProjId == task.ProjId);
                    var finishProof = task.TaskProofs?.FirstOrDefault(proof => proof.IsFinish);
                    var startProof = task.TaskProofs?.FirstOrDefault(proof => !proof.IsFinish);

                    return new AllProjectTasksDTO
                    {
                        Id = task.Id,
                        ProjId = task.ProjId,
                        TaskName = task.TaskName,
                        PlannedStartDate = FormatDate(task.PlannedStartDate),
                        PlannedEndDate = FormatDate(task.PlannedEndDate),
                        StartDate = FormatDate(task.ActualStartDate),
                        EndDate = FormatDate(task.ActualEndDate),
                        IsFinished = task.Progress == 100,
                        FinishProofImage = finishProof?.ProofImage,
                        StartProofImage = startProof?.ProofImage,
                        FacilitatorName = project?.Facilitator != null
                            ? $"{project.Facilitator.FirstName} {project.Facilitator.LastName}"
                            : null,
                        FacilitatorEmail = project?.Facilitator?.Email,
                    };
                })
                .ToList();

            return reportTasksLists;
        }

        public async Task<TaskCounts> AllProjectTasksReportCount()
        {
            // Call AllProjectTasksReport to get the list of all project tasks
            var reportTasksLists = await AllProjectTasksReport();

            // Calculate the counts based on the returned tasks
            var allTasksCount = reportTasksLists.Count;
            var finishedTasksCount = reportTasksLists.Count(task => task.IsFinished == true);

            // Return the counts in a new TaskCounts instance
            return new TaskCounts
            {
                AllTasks = allTasksCount,
                FinishedTasks = finishedTasksCount
            };
        }


        private string FormatDate(DateTime? date) => date?.ToString("MMM dd, yyyy");
    }
}

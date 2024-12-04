using BaseLibrary.DTO.Gantt;
using BaseLibrary.DTO.Report;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseLibrary.Services.Repositories
{
    public class ReportRepository(DataContext _dataContext, IPayment _payment, IGanttRepository _gantt) : IReportRepository
    {
        public async Task<ICollection<EquipmentReportDTO>> AllEquipmentReport()
        {
            var result = await _dataContext.Supply
                .Include(x => x.Equipment)
                .Include(x => x.Project)
                .Where(m => m.Equipment != null)
                .Select(s => new EquipmentReportDTO
                {
                    SuppId = s.SuppId,
                    EQPTQuantity = s.EQPTQuantity,
                    ProjId = s.Project.ProjId,
                    AssignedPrice = s.Price.ToString("#,##0.##"),
                    EQPTCode = s.Equipment.EQPTCode,
                    EQPTDescript = s.Equipment.EQPTDescript,
                    CurrentPrice = s.Equipment.EQPTPrice.ToString("#,##0.##"),
                    EQPTQOH = s.Equipment.EQPTQOH,
                    EQPTUnit = s.Equipment.EQPTUnit,
                    EQPTCategory = s.Equipment.EQPTCategory,
                    UpdatedAt = s.Equipment.UpdatedAt.ToString("MMM dd, yyyy"),
                    CreatedAt = s.Equipment.CreatedAt.ToString("MMM dd, yyyy")
                })
                .OrderBy(c => c.ProjId)
                .ToListAsync();

            return result;
        }

        public async Task<ICollection<MaterialReportDTO>> AllMaterialReport()
        {
            var result = await _dataContext.Supply
                .Include(x => x.Material)
                .Include(x => x.Project)
                .Where(m => m.Material != null)
                .Select(s => new MaterialReportDTO
                {
                    SuppId = s.SuppId,
                    MTLQuantity = s.MTLQuantity,
                    ProjId = s.Project.ProjId,
                    AssignedPrice = s.Price.ToString("#,##0.##"),
                    MTLCode = s.Material.MTLCode,
                    MTLDescript = s.Material.MTLDescript,
                    CurrentPrice = s.Material.MTLPrice.ToString("#,##0.##"),
                    MTLQOH = s.Material.MTLQOH,
                    MTLUnit = s.Material.MTLUnit,
                    MTLCategory = s.Material.MTLCategory,
                    UpdatedAt = s.Material.UpdatedAt.ToString("MMM dd, yyyy"),
                    CreatedAt = s.Material.CreatedAt.ToString("MMM dd, yyyy")
                })
                .OrderBy(c => c.ProjId)
                .ToListAsync();

            return result;
        }

        public async Task<ICollection<ProjectsDTO>> AllProjectReport()
        {
            var projects = await _dataContext.Project
                .Include(f => f.Facilitator)
                .ThenInclude(f => f.Facilitator)
                .OrderByDescending(s => s.Status)
                .ToListAsync();

            var result = new List<ProjectsDTO>();

            foreach (var project in projects)
            {
                var ganttDates = await _dataContext.GanttData
                    .Where(i => i.ProjId == project.ProjId)
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


                var plannedDate = await _gantt.ProjectDateInfo(project.ProjId);

                var cost = await _payment.GetTotalProjectExpense(project.ProjId);

                var projectInfo = new ProjectsDTO
                {
                    projId = project.ProjId,
                    kWCapacity = project.kWCapacity.ToString(),
                    systemType = project.SystemType,
                    customer=project.ProjName,
                    facilitator = project.Facilitator?
                        .Where(f => f.Facilitator != null)
                        .Select(f => f.Facilitator!.Email)
                        .FirstOrDefault() ?? "",
                    plannedStarted = plannedDate.EstimatedStartDate,
                    plannedEnded = plannedDate.EstimatedEndDate,
                    actualStarted = earliestStartDate.HasValue ? earliestStartDate.Value.ToString("MMMM dd, yyyy") : "",
                    actualEnded = latestEndDate.HasValue && project.Status == "Finished" ? latestEndDate.Value.ToString("MMMM dd, yyyy") : "",
                    cost = "₱ " + cost.ToString("#,##0.00"),
                    status = project.Status
                };

                result.Add(projectInfo);

            }

            return result;
        }

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

            var parentIds = tasks
                .Select(t => t.ParentId)
                .Where(id => id.HasValue)
                .Select(id => id.Value)
                .ToHashSet();

            var taskProjIds = tasks.Select(t => t.ProjId).ToHashSet();

            var projectLogs = await _dataContext.ProjectWorkLog
                .Include(p => p.Facilitator)
                .Include(p => p.Project)
                .Where(pl => taskProjIds.Contains(pl.Project.ProjId))
                .ToListAsync();

            var reportTasksLists = tasks
                .Where(t => !parentIds.Contains(t.TaskId)) // Skip tasks with subtasks
                .Select(task =>
                {
                    var project = projectLogs.FirstOrDefault(pl => pl.Project != null && pl.Project.ProjId == task.ProjId);
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

        public async Task<DashboardDTO> DashboardData()
        {
            var totalPersonnel = await _dataContext.Users.CountAsync() + await _dataContext.Installer.CountAsync();

            var projectCounts = await _dataContext.Project
                .GroupBy(p => p.Status)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            var finishedProjects = projectCounts.FirstOrDefault(p => p.Status == "Finished")?.Count ?? 0;
            var pendingProjects = projectCounts.FirstOrDefault(p => p.Status == "OnGoing")?.Count ?? 0;
            var onWorkProjects = projectCounts.FirstOrDefault(p => p.Status == "OnWork")?.Count ?? 0;

            return new DashboardDTO
            {
                TotalPersonnel = totalPersonnel,
                FinishedProjects = finishedProjects,
                PendingProjects = pendingProjects,
                OnWorkProjects = onWorkProjects
            };
        }


        private string FormatDate(DateTime? date) => date?.ToString("MMM dd, yyyy");
    }
}

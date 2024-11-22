using BaseLibrary.DTO.Gantt;
using DataLibrary.Models.Gantt;

namespace BaseLibrary.Services.Interfaces
{
    public interface IGanttRepository
    {
        Task<ICollection<FacilitatorTasksDTO>> FacilitatorTasks(string projId);
        Task<ICollection<TasksToDoDTO>> TasksToDo(string projId);
        Task<ICollection<TaskToDoDTO>> TaskToDo(string projId);
        Task<(bool, string)> HandleTask(UploadTaskDTO taskDto, bool isStarting);
        Task<(bool, string)> SubmitTaskReport(UploadTaskDTO taskDto);
        Task<(bool, string)>UpdateTaskProgress(UpdateTaskProgress taskDto);
        Task<TaskProof> TaskById(int id);
        Task<ProjectDateInfo> ProjectDateInfo(string projId);
        Task<ProjProgressDTO> ProjectProgress(string projId);
        Task<ProjectStatusDTO> ProjectStatus(string projId);
    }
}

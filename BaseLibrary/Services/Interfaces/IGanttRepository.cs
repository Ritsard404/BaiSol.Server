using BaseLibrary.DTO.Gantt;
using DataLibrary.Models.Gantt;

namespace BaseLibrary.Services.Interfaces
{
    public interface IGanttRepository
    {
        Task<ICollection<FacilitatorTasksDTO>> FacilitatorTasks(string projId);
        Task<(bool, string)> HandleTask(UploadTaskDTO taskDto, bool isStarting);
        Task<TaskProof> TaskById(int id);
    }
}

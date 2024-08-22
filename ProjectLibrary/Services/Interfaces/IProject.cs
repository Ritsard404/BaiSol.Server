using ProjectLibrary.DTO.Project;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IProject
    {
        Task<ICollection<GetProjects>> GetClientsProject();
        Task<ICollection<GetProjects>> GetClientProject(string clientId);
        Task<bool> UpdateClientProject(UpdateProject updateProject);
        Task<string> AddNewClientProject(ProjectDto projectDto);
        Task<bool> DeleteClientProject(string projId);
        Task<bool> IsProjIdExist(string projId);
        Task<bool> UpdatePersonnelWorkStarted(string projId);
        Task<bool> UpdatePersonnelWorkEnded(string projId, string reasonEnded);
        Task<bool> Save();

    }
}

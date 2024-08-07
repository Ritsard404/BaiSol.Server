using ProjectLibrary.DTO.Project;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IProject
    {
        Task<ICollection<GetProjects>> GetClientsProject();
        Task<ICollection<GetProjects>> GetClientProject(string clientId);
        Task<string> AddNewClientProject(ProjectDto projectDto);
        Task<bool> IsProjIdExist(string projId);
        Task<bool> Save();

    }
}

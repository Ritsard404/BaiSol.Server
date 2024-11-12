using ProjectLibrary.DTO.Project;

namespace ProjectLibrary.Services.Interfaces
{
    public interface IProject
    {
        Task<ICollection<GetProjects>> GetClientsProject();
        Task<ICollection<ClientProjectInfoDTO>> GetClientsProjectInfos();
        Task<ICollection<GetProjects>> GetClientProject(string clientId);
        Task<ClientProjectInfoDTO> GetClientProjectInfo(string projId);
        Task<(bool, string)> UpdateClientProject(UpdateClientProjectInfoDTO updateProject);
        Task<string> AddNewClientProject(ProjectDto projectDto);
        Task<bool> DeleteClientProject(string projId);
        Task<bool> IsProjIdExist(string projId);
        Task<bool> UpdatePersonnelWorkStarted(string projId);
        Task<bool> UpdatePersonnelWorkEnded(string projId, string reasonEnded);
        Task<(bool, string)> UpdateProjectToOnProcess(UpdateProjectStatusDTO updateProjectToOnProcess);
        Task<(bool, string)> UpdateProjectToOnWork(UpdateProjectStatusDTO updateProjectToOnWork);
        Task<bool> Save();

        // Project Quotation
        Task<ProjectQuotationInfoDTO> ProjectQuotationInfo(string? projId, string? customerEmail);
        Task<ICollection<ProjectQuotationSupply>> ProjectQuotationSupply(string? projId);
        Task<ProjectQuotationTotalExpense> ProjectQuotationExpense(string? projId, string? customerEmail);

        Task<(bool, string)> UpdateProfit(UpdateProfitRate updateProfit);

        Task<bool> IsProjectOnGoing(string projId);
        Task<bool> IsProjectOnProcess(string projId);
        Task<bool> IsProjectOnWork(string projId);
        Task<bool> IsProjectOnFinished(string projId);

    }
}

using BaseLibrary.DTO.Gantt;
using BaseLibrary.DTO.Report;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseLibrary.Services.Interfaces
{
    public interface IReportRepository
    {
        Task<ICollection<AllProjectTasksDTO>> AllProjectTasksReport();
        Task<TaskCounts> AllProjectTasksReportCount();
        Task<ProjectCounts> AllProjectsCount();

        Task<DashboardDTO> DashboardData();

        Task<ICollection<MaterialReportDTO>> AllMaterialReport();
        Task<ICollection<EquipmentReportDTO>> AllEquipmentReport();
    }
}

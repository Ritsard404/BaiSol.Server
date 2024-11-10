using BaseLibrary.DTO.Gantt;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiSol.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ReportController(IReportRepository _report,DataContext _dataContext) : ControllerBase
    {

        [HttpGet()]
        public async Task<IActionResult> AllProjectTasksReport()
        {
            var task = await _report.AllProjectTasksReport();

            return Ok(task);
        }

        [HttpGet()]
        public async Task<IActionResult> TasksAndProjectCounts()
        {
            var taskCount = await _report.AllProjectTasksReportCount();
            var projectCount = await _report.AllProjectsCount();

            return Ok(new
            {
                taskCount,
                projectCount
            });
        }
        
        [HttpGet()]
        public async Task<IActionResult> AllMaterialReport()
        {
            var materialReport = await _report.AllMaterialReport();

            return Ok(materialReport);
        }
        
        [HttpGet()]
        public async Task<IActionResult> AllEquipmentReport()
        {
            var equipmentReport = await _report.AllEquipmentReport();

            return Ok(equipmentReport);
        }
        
        [HttpGet()]
        public async Task<IActionResult> DashboardData()
        {
            var dashboard = await _report.DashboardData();

            return Ok(dashboard);
        }

    }
}

using BaseLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaiSol.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportController(IReportRepository _report) : ControllerBase
    {

        [HttpGet("[action]")]
        public async Task<IActionResult> AllProjectTasksReport()
        {
            var task = await _report.AllProjectTasksReport();

            return Ok(task);
        }
        
        [HttpGet("[action]")]
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

    }
}

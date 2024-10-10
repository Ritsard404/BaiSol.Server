using BaseLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaiSol.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class LogsController(IUserLogs _userLogs) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetInventoryLogs(string supplyCategory, string id)
        {
            var logs = await _userLogs.GetInventoryLogs(supplyCategory, id);
            return Ok(logs);
        }
    }
}

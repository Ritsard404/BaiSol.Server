using ClientLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaiSol.Server.Controllers.Client
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class ClientController(IClientProject _clientProject) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetClientProjectId( string userEmail)
        {
            var id = await _clientProject.GetClientProject(userEmail);
            return Ok(id);
        }

    }
}

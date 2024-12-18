using AuthLibrary.DTO;
using AuthLibrary.Models;
using ClientLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Project;

namespace BaiSol.Server.Controllers.Client
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    //[Authorize(Roles = UserRoles.Client)]
    public class ClientController(IClientProject _clientProject) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> GetClientProjectId(string userEmail)
        {
            var id = await _clientProject.GetClientProject(userEmail);
            return Ok(id);
        }

        [HttpGet]
        public async Task<IActionResult> GetClientProjectHistory(string userEmail)
        {
            var history = await _clientProject.GetClientProjectHistory(userEmail);
            return Ok(history);
        }

        [HttpGet]
        public async Task<IActionResult> NotificationMessages(string userEmail)
        {
            var notifs = await _clientProject.NotificationMessages(userEmail);


            return Ok(new
            {
                notifs = notifs,
                notifCount = notifs.Count(w => !w.isRead)
            });
        }

        [HttpGet]
        public async Task<IActionResult> NotificationMessage(int notifId)
        {
            var notifs = await _clientProject.NotificationMessage(notifId);
            return Ok(notifs);
        }

        [HttpGet]
        public async Task<IActionResult> IsProjectApprovedQuotation(string projId)
        {
            var projects = await _clientProject.IsProjectApprovedQuotation(projId);
            return Ok(projects); // Wrap the result in an Ok result
        }

        [HttpPut]
        public async Task<IActionResult> ReadNotif(int notifId, string clientEmail)
        {
            await _clientProject.ReadNotif(notifId);
            return Ok();
        }

        [HttpPut]
        public async Task<IActionResult> ApproveProjectQuotation(UpdateProjectStatusDTO approveProjectQuotation)
        {
            var (success, message) = await _clientProject.ApproveProjectQuotation(approveProjectQuotation);

            if (!success) return BadRequest(message);

            return Ok(message);
        }

    }
}

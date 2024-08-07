using DataLibrary.Models;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Project;
using ProjectLibrary.Services.Interfaces;

namespace BaiSol.Server.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProjectController(IProject _project) : ControllerBase
    {

        [HttpGet("Get-All-Projects")]
        public async Task<IActionResult> GetClientProjects()
        {
            var projects = await _project.GetClientsProject();
            return Ok(projects); // Wrap the result in an Ok result
        }

        [HttpGet("Get-Client-Project")]
        public async Task<IActionResult> GetClientProject(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId)) return BadRequest("Client ID is required.");

            var clientProject = await _project.GetClientProject(clientId);

            if (clientProject == null || !clientProject.Any()) return NotFound("No projects found for the given client ID.");

            return Ok(clientProject);
        }

        [HttpPost("Add-Client-Project")]
        public async Task<IActionResult> NewClientProject(ProjectDto projectDto)
        {
            if (projectDto == null) return BadRequest(ModelState);

            var result = await _project.AddNewClientProject(projectDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Client Project Added");
        }
    }
}

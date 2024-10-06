﻿using DataLibrary.Models;
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

        [HttpGet("Get-Client-Info")]
        public async Task<IActionResult> GetClientProjectInfo(string projId)
        {
            if (string.IsNullOrWhiteSpace(projId)) return BadRequest("Client ID is required.");

            var clientInfo = await _project.GetClientProjectInfo(projId);

            if (clientInfo == null) return NotFound("No client found for the given client ID.");

            return Ok(clientInfo);
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

        [HttpPut("Update-Client-Project")]
        public async Task<IActionResult> UpdateClientProject(ClientProjectInfoDTO updateProject)
        {
            if (updateProject == null) return BadRequest(ModelState);

            var (success, message) = await _project.UpdateClientProject(updateProject);

            if (!success) return BadRequest(message);

            return Ok(message);
        }

        [HttpDelete("Delete-Client-Project")]
        public async Task<IActionResult> DeleteClientProject(string projectId)
        {
            var project = await _project.DeleteClientProject(projectId);

            if (!project) return BadRequest("Unable to delete the project!");

            return Ok("Project deleted!");
        }

        [HttpPut("Update-WorkStarted")]
        public async Task<IActionResult> UpdatePersonnelWorkStart(string projectId)
        {
            var updatePersonnel = await _project.UpdatePersonnelWorkStarted(projectId);

            if (!updatePersonnel) return BadRequest("Unable to update the work start!");

            return Ok("Work start updated!");
        }


        [HttpPut("Update-WorkEnded")]
        public async Task<IActionResult> UpdatePersonnelWorkEnd(string projectId, string workEndReason)
        {
            var updatePersonnel = await _project.UpdatePersonnelWorkEnded(projectId, workEndReason);

            if (!updatePersonnel) return BadRequest("Unable to update the work end!");

            return Ok("Work end updated!");
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectQuotationInfo(string? projId, string? customerEmail)
        {
            var info = await _project.ProjectQuotationInfo(projId, customerEmail);
            return Ok(info); // Wrap the result in an Ok result
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectQuotationSupply(string? projId, string? customerEmail)
        {
            var supply = await _project.ProjectQuotationSupply(projId, customerEmail);
            return Ok(supply); // Wrap the result in an Ok result
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> ProjectQuotationExpense(string? projId, string? customerEmail)
        {
            var expense = await _project.ProjectQuotationExpense(projId, customerEmail);
            return Ok(expense); // Wrap the result in an Ok result
        }
    }
}

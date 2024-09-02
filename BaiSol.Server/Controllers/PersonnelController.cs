using AuthLibrary.DTO;
using AuthLibrary.DTO.Facilitator;
using AuthLibrary.DTO.Installer;
using AuthLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BaiSol.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PersonnelController(IPersonnel _personnel) : ControllerBase
    {
        [HttpPost("Add-Installer")]
        public async Task<IActionResult> NewInstaller([FromBody] InstallerDto installerDto)
        {
            if (installerDto == null) return BadRequest(ModelState);

            // Call the repository method
            var result = await _personnel.AddInstaller(installerDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Installer Added");
        }

        [HttpGet("Get-Installers")]
        public async Task<IActionResult> GetInstallers()
        {
            var installer = await _personnel.GetInstallersInfo();


            if (installer == null || !installer.Any())
            {
                return StatusCode(400, "Empty Installers");
            }

            return Ok(installer);
        }

        [HttpPut("Update-Installer-Status")]
        public async Task<IActionResult> UpdateInstallerStatus(int id, string status)
        {
            var updateInstaller = await _personnel.UpdateInstallerStatus(id, status);
            if (!updateInstaller) return BadRequest("Installer Not Exist");

            return Ok("Status Updated Successfully");
        }

        [HttpGet("Get-Available-Facilitators")]
        public async Task<IActionResult> GetAvailableFacilitators()
        {
            var allFacilitator = await _personnel.GetAvailableFacilitator();
            if (allFacilitator == null || !allFacilitator.Any()) return NoContent();

            return Ok(allFacilitator);
        }

        [HttpGet("Get-Available-Installers")]
        public async Task<IActionResult> GetAvailableInstallers()
        {
            var allInstaller = await _personnel.GetAvailableInstaller();
            if (allInstaller == null || !allInstaller.Any()) return NoContent();

            return Ok(allInstaller);
        }

        [HttpPost("Assign-Installers")]
        public async Task<IActionResult> AssignInstallers(AssignInstallerToProjectDto assignInstallerToProject)
        {
            // Call the service to assign installers and get the result
            var result = await _personnel.AssignInstallers(assignInstallerToProject);

            // Check if the result contains any messages or errors
            if (result == "No installers provided" || result.Contains("does not exist") || result.Contains("already assigned"))
            {
                // Return a BadRequest if the result indicates a problem
                return BadRequest(result);
            }

            // Return OK with the result if everything went well
            return Ok(result);
        }


        [HttpPost("Assign-Facilitator")]
        public async Task<IActionResult> AssignFacilitator(AssignFacilitatorToProjectDto facilitatorToProjectDto)
        {
            // Call the service to assign the facilitator and get the result
            var result = await _personnel.AssignFacilitator(facilitatorToProjectDto);

            // Check for specific error messages and return BadRequest if any issues are found
            if (result != null)
            {
                return BadRequest(result); // Return NoContent if the result is empty or null
            }

            // If the result contains a success message or other information, return Ok
            return Ok("Facilitator assigned successfully!");
        }

        [HttpGet("Get-Assigned-Installers")]
        public async Task<IActionResult> GetAssignedInstallers(string projId)
        {
            var assignedInstaller = await _personnel.GetAssignednstaller(projId);
            if (assignedInstaller == null || !assignedInstaller.Any()) return NoContent();

            return Ok(assignedInstaller);
        }

        [HttpGet("Get-Assigned-Facilitator")]
        public async Task<IActionResult> GetAssignedFacilitator(string projId)
        {
            var assignedFacilitator = await _personnel.GetAssignedFacilitator(projId);
            if (assignedFacilitator == null) return NoContent();

            return Ok(assignedFacilitator);
        }

        [HttpDelete("Remove-Assigned-Installers")]
        public async Task<IActionResult> RemoveInstallers(RemoveInstallersDto removeInstallersDto)
        {
            var removeInstallers = await _personnel.RemoveInstallers(removeInstallersDto.InstallerIds, removeInstallersDto.ProjectId);
            if (!removeInstallers) return BadRequest("Installer not found or could not be removed.");

            return Ok("Installers removed successfully.");
        }

        [HttpDelete("Remove-Assigned-Facilitator")]
        public async Task<IActionResult> RemoveFacilitator(RemoveFacilitatorDto removeFacilitatorDto)
        {
            var removeFacilitator = await _personnel.RemoveFacilitator(removeFacilitatorDto.facilitatorId, removeFacilitatorDto.projectId);
            if (!removeFacilitator) return BadRequest("Facilitator not found or could not be removed.");

            return Ok("Facilitator removed successfully.");
        }
    }
}

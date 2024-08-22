using AuthLibrary.DTO;
using AuthLibrary.Services.Interfaces;
using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
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

        [HttpGet]
        public async Task<IActionResult> GetAvailableFacilitators()
        {
            var allFacilitator = await _personnel.GetAvailableFacilitator();
            if (allFacilitator == null || !allFacilitator.Any()) return NoContent();

            return Ok(allFacilitator);
        }

        [HttpGet]
        public async Task<IActionResult> GetAvailableInstallers()
        {
            var allInstaller = await _personnel.GetAvailableInstaller();
            if (allInstaller == null || !allInstaller.Any()) return NoContent();

            return Ok(allInstaller);
        }

    }
}

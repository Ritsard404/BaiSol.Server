using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.Services.Interfaces;

namespace BaiSol.Server.Controllers.Projects
{
    [Route("material/[controller]")]
    [ApiController]
    public class MaterialController(IMaterial _material) : ControllerBase
    {

        [HttpPost("Add-Material")]
        public async Task<IActionResult> NewMaterial(MaterialDTO getMaterialDTO)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (getMaterialDTO == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            getMaterialDTO.UserIpAddress = ipAddress;

            var result = await _material.AddNewMaterial(getMaterialDTO);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Material Added");
        }

        [HttpGet("Get-Materials")]
        public async Task<IActionResult> GetMaterials()
        {
            var material = await _material.GetMaterials();

            //if (material == null || !material.Any())
            //{
            //    return StatusCode(400, "Empty Materials");
            //}

            return Ok(material);
        }

        [HttpGet("Get-Categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _material.GetMaterialCategories();

            return Ok(categories);
        }

        [HttpGet("Get-Available-Materials")]
        public async Task<IActionResult> GetAvailableMaterials(string projId, string category)
        {
            var availableMaterials = await _material.GetMaterialsByCategory(projId, category);


            return Ok(availableMaterials);
        }

        [HttpGet("Get-Material-QOH")]
        public async Task<IActionResult> GetAvailableMaterials(int mtlId)
        {
            var qoh = await _material.GetQOHMaterial(mtlId);

            //if (qoh < 0)
            //{
            //    return StatusCode(400, "Empty Material");
            //}

            return Ok(new { QOH = qoh });
        }

        [HttpPut("Update-MaterialPAndQ")]
        public async Task<IActionResult> UpdateQAndPMaterial(UpdateQAndPMaterialDTO updateMaterial)
        {

            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            updateMaterial.UserIpAddress = ipAddress;
            var updateMtaerial = await _material.UpdateQAndPMaterial(updateMaterial);


            return Ok(updateMtaerial);
        }

        [HttpPut("Update-MaterialUAndD")]
        public async Task<IActionResult> UpdateUAndDMaterial(UpdateMaterialUAndC updateMaterial)
        {

            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            updateMaterial.UserIpAddress = ipAddress;
            var updateMtaerial = await _material.UpdateUAndDMaterial(updateMaterial);


            return Ok(updateMtaerial);
        }

        [HttpDelete("Delete-Material")]
        public async Task<IActionResult> DeleteMaterialById(int mtlId, string adminEmail)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            var (success, message) = await _material.DeleteMaterial(mtlId, adminEmail, ipAddress);

            if (success)
            {
                return Ok(message);
            }
            else
            {
                return BadRequest(message);
            }
        }


    }
}

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
            if (getMaterialDTO == null) return BadRequest(ModelState);

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

            if (material == null || !material.Any())
            {
                return StatusCode(400, "Empty Materials");
            }

            return Ok(material);
        }
    }
}

using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace BaiSol.Server.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuotationController(IQuote _materialQuote) : ControllerBase
    {
        [HttpPost("Add-MaterialSuplly")]
        public async Task<IActionResult> NewMaterialSupply(MaterialQuoteDto materialQuoteDto)
        {
            if (materialQuoteDto == null) return BadRequest(ModelState);

            var result = await _materialQuote.AddNewMaterialSupply(materialQuoteDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Supply Added");
        }

        [HttpGet("Get-Material-Cost")]
        public async Task<IActionResult> GetMaterialCostQuote(int projectId)
        {
            var materialCost = await _materialQuote.GetMaterialCostQuote(projectId);

            if (materialCost == null || !materialCost.Any())
            {
                return StatusCode(400, "Empty Material Cost");
            }

            return Ok(materialCost);
        }
    }
}

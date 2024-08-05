
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace BaiSol.Server.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class QuotationController(IQuote _quote) : ControllerBase
    {
        [HttpPost("Add-Client-Project")]
        public async Task<IActionResult> NewClientProject(ProjectDto projectDto)
        {
            if (projectDto == null) return BadRequest(ModelState);

            var result = await _quote.AddNewClientProject(projectDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Client Project Added");
        }

        [HttpPost("Add-Materia-Supply")]
        public async Task<IActionResult> NewMaterialSupply(MaterialQuoteDto materialQuoteDto)
        {
            if (materialQuoteDto == null) return BadRequest(ModelState);

            var result = await _quote.AddNewMaterialSupply(materialQuoteDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Supply Added");
        }

        [HttpGet("Get-Project-Cost")]
        public async Task<IActionResult> GetProjectCostQuote(string projectId)
        {
            var materialCost = await _quote.GetMaterialCostQuote(projectId);
            var totalProjectCost = await _quote.GetProjectTotalCostQuote(projectId);

            if (materialCost == null || !materialCost.Any())
            {
                return StatusCode(400, "Empty Material Cost");
            }

            return Ok(new
            {
                materialCostList = materialCost,
                totalProjectCostList = totalProjectCost
            });
        }

        [HttpPost("Add-Project-Labor")]
        public async Task<IActionResult> NewProjectLabor(LaborQuoteDto laborQuoteDto)
        {
            if (laborQuoteDto == null) return BadRequest(ModelState);

            var result = await _quote.AddNewLaborCost(laborQuoteDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Labor Added");
        }

        [HttpGet("Get-Labor-Cost")]
        public async Task<IActionResult> GetLaborCostQuote(string projectId)
        {
            var laborCost = await _quote.GetLaborCostQuote(projectId);
            var totalLaborCost = await _quote.GetTotalLaborCostQuote(projectId);

            if (laborCost == null || !laborCost.Any())
            {
                return StatusCode(400, "Empty Material Cost");
            }

            return Ok(new
            {
                LaborCost = laborCost,
                TotalLaborCost = totalLaborCost
            });
        }
    }
}

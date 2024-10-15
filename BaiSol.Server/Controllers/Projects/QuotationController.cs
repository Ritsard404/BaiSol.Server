﻿
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

        [HttpPost("Add-Material-Supply")]
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
            var categoryCost = await _quote.GetMaterialCategoryCostQuote(projectId);
            var totalProjectCost = await _quote.GetProjectTotalCostQuote(projectId);

            //if (materialCost == null || !materialCost.Any())
            //{
            //    return StatusCode(400, "Empty Material Cost");
            //}

            return Ok(new
            {
                materialCostList = materialCost,
                categoryCostList = categoryCost,
                totalProjectCostList = totalProjectCost
            });
        }
        [HttpGet("Get-Project-Material-Cost")]
        public async Task<IActionResult> GetProjectAndMaterialCostQuote(string projectId)
        {
            var materialAndCategoryCost = await _quote.GetProjectAndMaterialsTotalCostQuote(projectId);
            var totalProjectCost = await _quote.GetProjectTotalCostQuote(projectId);

            //if (materialAndCategoryCost == null || !materialAndCategoryCost.Any())
            //{
            //    return StatusCode(400, "Empty Material Cost");
            //}

            return Ok(new
            {
                materialAndCategoryCostList = materialAndCategoryCost,
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
                return BadRequest(result);
            }

            return Ok("New Labor Added");
        }

        [HttpGet("Get-Labor-Cost")]
        public async Task<IActionResult> GetLaborCostQuote(string projectId)
        {
            var laborCost = await _quote.GetLaborCostQuote(projectId);
            var totalLaborCost = await _quote.GetTotalLaborCostQuote(projectId);

            //if (laborCost == null || !laborCost.Any())
            //{
            //    return StatusCode(400, "Empty Labor Cost");
            //}

            return Ok(new
            {
                LaborCost = laborCost,
                TotalLaborCost = totalLaborCost
            });
        }

        [HttpGet("Get-Assigned-Equipment")]
        public async Task<IActionResult> GetAssignedEquipment(string projectId)
        {
            var assignedEquipment = await _quote.GetAssignedEquipment(projectId);

            return Ok(assignedEquipment);
        }

        [HttpPost("Assign-Equipment")]
        public async Task<IActionResult> AssignNewEquipment(AssignEquipmentDto assignEquipmentDto)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            assignEquipmentDto.UserIpAddress = ipAddress;

            var (isAssign, message) = await _quote.AssignNewEquipment(assignEquipmentDto);

            if (isAssign)
                return Ok(message);
            else
                return BadRequest(message);
        }


        [HttpPut("Update-Material-Supply-Quantity")]
        public async Task<IActionResult> UpdateMaterialSupplyQuantity(UpdateMaterialSupplyQuantity materialSupplyQuantity)
        {
            if (materialSupplyQuantity == null) return BadRequest(ModelState);

            var updateSupply = await _quote.UpdateMaterialQuantity(materialSupplyQuantity);

            // Check if the update was unsuccessful due to insufficient inventory
            if (!updateSupply)
            {
                // Return a BadRequest with a specific error message
                return BadRequest("Insufficient material inventory");
            }

            // Return a success response
            return Ok("Material quantity updated successfully");
        }

        [HttpPut("Update-Labor-Quote")]
        public async Task<IActionResult> UpdateLaborQuote(UpdateLaborQuote updateLaborQuote)
        {
            if (updateLaborQuote == null) return BadRequest(ModelState);

            var updateLabor = await _quote.UpdateLaborQuoote(updateLaborQuote);

            if (!updateLabor) return BadRequest("Labor not found!");

            return Ok("Labor updated successfully");
        }

        [HttpPut("Update-Equipment-Quantity")]
        public async Task<IActionResult> UpdateLaborQuote(UpdateEquipmentSupply updateEquipmentSupply)
        {
            if (updateEquipmentSupply == null) return BadRequest(ModelState);

            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            updateEquipmentSupply.UserIpAddress = ipAddress;

            var updateEquipment = await _quote.UpdateEquipmentQuantity(updateEquipmentSupply);

            if (!updateEquipment) return BadRequest("Invalid quantity!");

            return Ok("Equipment updated successfully");
        }


        [HttpDelete("Delete-Material-Supply")]
        public async Task<IActionResult> DeleteMaterialSupply(int suppId, int mtlId)
        {
            var deleteSupply = await _quote.DeleteMaterialSupply(suppId, mtlId);

            if (!deleteSupply) return BadRequest("Supply don\'t exist!");

            return Ok("Supply deleted!");
        }

        [HttpDelete("Delete-Labor-Quote")]
        public async Task<IActionResult> DeleteLaborQuote(int laborId)
        {
            var deleteLabor = await _quote.DeleteLaborQuote(laborId);

            if (!deleteLabor) return BadRequest("Labor don\'t exist!");

            return Ok("Labor deleted!");
        }

        [HttpDelete("Delete-Equipment-Supply")]
        public async Task<IActionResult> DeleteLaborQuote(DeleteEquipmentSupplyDto deleteEquipmentSupply)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            deleteEquipmentSupply.UserIpAddress = ipAddress;

            var deleteEquipment = await _quote.DeleteEquipmentSupply(deleteEquipmentSupply);

            if (!deleteEquipment) return BadRequest("Supply don\'t exist!");

            return Ok("Labor deleted!");
        }
    }
}

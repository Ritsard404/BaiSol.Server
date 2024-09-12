using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Equipment;
using ProjectLibrary.Services.Interfaces;

namespace BaiSol.Server.Controllers.Projects
{
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentController(IEquipment _equipment) : ControllerBase
    {

        [HttpPost("Add-Equipment")]
        public async Task<IActionResult> NewEquipment(EquipmentDTO equipmentDto)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (equipmentDto == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            equipmentDto.UserIpAddress = ipAddress;

            var result = await _equipment.AddNewEquipment(equipmentDto);

            if (result != null)
            {
                ModelState.AddModelError("", result);
                return StatusCode(500, ModelState);
            }

            return Ok("New Equipment Added");
        }

        [HttpGet("Get-Equipment")]
        public async Task<IActionResult> GetEquipment()
        {
            var equipment = await _equipment.GetAllEquipment();

            if (equipment == null || !equipment.Any())
            {
                return StatusCode(400, "Empty Equipment");
            }

            return Ok(equipment);
        }

        [HttpGet("Get-Available-Equipment")]
        public async Task<IActionResult> GetAvailableEquipment(string projId, string category)
        {
            var availableMaterials = await _equipment.GetEquipmentByCategory(projId, category);

            return Ok(availableMaterials);
        }

        [HttpGet("Get-Categories")]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _equipment.GetEquipmentCategories();

            return Ok(categories);
        }

        [HttpPut("Update-EquipmentPAndQ")]
        public async Task<IActionResult> UpdateQAndPEquipment(UpdateQAndPDTO updateEquipment)
        {

            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            updateEquipment.UserIpAddress = ipAddress;
            var updateMtaerial = await _equipment.UpdateQAndPEquipment(updateEquipment);


            return Ok(updateMtaerial);
        }

        [HttpPut("Update-EquipmentUAndD")]
        public async Task<IActionResult> UpdateUAndDEquipment(UpdateUAndDDTO updateEquipment)
        {

            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            updateEquipment.UserIpAddress = ipAddress;
            var updateMtaerial = await _equipment.UpdateUAndDEquipment(updateEquipment);


            return Ok(updateMtaerial);
        }

        [HttpDelete("Delete-Equipment")]
        public async Task<IActionResult> DeleteEquipmentById(int eqptId, string adminEmail)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");

            var (success, message) = await _equipment.DeleteEquipment(eqptId, adminEmail, ipAddress);

            if (success)
            {
                return Ok(message);
            }
            else
            {
                return BadRequest(message);
            }
        }


        [HttpPost("Return-Damaged-Equipment")]
        public async Task<IActionResult> ReturnDamagedEquipment(ReturnEquipmentDto returnEquipment)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (returnEquipment == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            returnEquipment.UserIpAddress = ipAddress;

            var (success, message) = await _equipment.ReturnDamagedEquipment(returnEquipment);

            if (success)
            {
                return Ok(message);
            }
            else
            {
                return BadRequest(message);
            }
        }

        [HttpPut("Return-Good-Equipment")]
        public async Task<IActionResult> ReturnGoodEquipment(ReturnEquipmentDto returnEquipment)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (returnEquipment == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            returnEquipment.UserIpAddress = ipAddress;

            var (success, message) = await _equipment.ReturnGoodEquipment(returnEquipment);

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

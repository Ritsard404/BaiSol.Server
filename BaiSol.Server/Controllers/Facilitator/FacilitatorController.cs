using FacilitatorLibrary.DTO.Request;
using FacilitatorLibrary.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BaiSol.Server.Controllers.Facilitator
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class FacilitatorController(IRequestSupply _requestSupply, IAssignedSupply _assignedSupply) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> SentRequestByProj(string userEmail)
        {
            var requests = await _requestSupply.SentRequestByProj(userEmail);
            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> RequestSupplies(string userEmail, string supplyCtgry)
        {
            var requests = await _requestSupply.RequestSupplies(userEmail, supplyCtgry);
            return Ok(requests);
        }

        [HttpPut]
        public async Task<IActionResult> AcknowledgeRequest(AcknowledgeRequestDTO acknowledgeRequest)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (acknowledgeRequest == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            acknowledgeRequest.UserIpAddress = ipAddress;

            var (success, message) = await _requestSupply.AcknowledgeRequest(acknowledgeRequest);

            if (success)
            {
                return Ok(message);
            }
            else
            {
                return BadRequest(message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AssignedMaterialsSupply(string userEmail)
        {
            var requests = await _assignedSupply.GetAssignedMaterials(userEmail);
            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> AssignedEquipmentSupply(string userEmail)
        {
            var requests = await _assignedSupply.GetAssignedEquipment(userEmail);
            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> GetAssignedProject(string userEmail)
        {
            var requests = await _assignedSupply.GetAssignedProject(userEmail);
            return Ok(requests);
        }

    }
}

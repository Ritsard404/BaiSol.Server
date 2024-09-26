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
    }
}

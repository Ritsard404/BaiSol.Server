using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Equipment;
using ProjectLibrary.DTO.Requisition;
using ProjectLibrary.Services.Interfaces;

namespace BaiSol.Server.Controllers.Projects
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class RequisitionController(IRequisition _requisition) : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> AllRequest()
        {
            var requests = await _requisition.AllRequest();
            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> SentRequestByProj(string projId)
        {
            var requests = await _requisition.SentRequestByProj(projId);
            return Ok(requests);
        }

        [HttpGet]
        public async Task<IActionResult> RequestSupplies(string projId, string supplyCtgry)
        {
            var requests = await _requisition.RequestSupplies(projId, supplyCtgry);
            return Ok(requests);
        }


        [HttpPost]
        public async Task<IActionResult> RequestSupply(AddRequestDTO approveRequest)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (approveRequest == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            approveRequest.UserIpAddress = ipAddress;

            var (isSuccess, message) = await _requisition.RequestSupply(approveRequest);
            if (!isSuccess)
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPut]
        public async Task<IActionResult> ApproveRequest(StatusRequestDTO approveRequest)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (approveRequest == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            approveRequest.UserIpAddress = ipAddress;

            var (isSuccess, message) = await _requisition.ApproveRequest(approveRequest);
            if (!isSuccess)
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPut]
        public async Task<IActionResult> DeclineRequest(StatusRequestDTO declineRequest)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (declineRequest == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            declineRequest.UserIpAddress = ipAddress;

            var (isSuccess, message) = await _requisition.DeclineRequest(declineRequest);
            if (!isSuccess)
                return BadRequest(message);

            return Ok(message);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateRequestQuantity(UpdateQuantity updateQuantity)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (updateQuantity == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            updateQuantity.UserIpAddress = ipAddress;

            var (isSuccess, message) = await _requisition.UpdateRequest(updateQuantity);
            if (!isSuccess)
                return BadRequest(message);

            return Ok(message);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteRequest(DeleteRequest deleteRequest)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (deleteRequest == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            deleteRequest.UserIpAddress = ipAddress;

            var (isSuccess, message) = await _requisition.DeleteRequest(deleteRequest);
            if (!isSuccess)
                return BadRequest(message);

            return Ok(message);
        }

    }
}

using BaseLibrary.DTO.Payment;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace BaiSol.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PaymentController(IConfiguration _config, DataContext _dataContext, IPayment _payment) : ControllerBase
    {

        [HttpGet]
        public async Task<IActionResult> GetClientPayments(string projId)
        {

            var payments = await _payment.GetClientPayments(projId);

            return Ok(payments);

        }

        [HttpGet]
        public async Task<IActionResult> IsProjectPayedDownpayment(string projId)
        {

            var payments = await _payment.IsProjectPayedDownpayment(projId);
            return Ok(payments);

        }

        //[HttpGet]
        //public async Task<IActionResult> GetAllPayments()
        //{

        //    var payments = await _payment.GetAllPayments();

        //    return Ok(payments);

        //}

        [HttpGet]
        public async Task<IActionResult> GetAllPayment()
        {

            var payments = await _payment.GetAllPayment();

            return Ok(payments);

        }

        [HttpPost]
        public async Task<IActionResult> CreatePayment(CreatePaymentDTO createPayment)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (createPayment == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            createPayment.UserIpAddress = ipAddress;

            var (isSuccessful, message) = await _payment.CreatePayment(createPayment);
            if(!isSuccessful) 
                return BadRequest(message);

            return Ok(message);

        }

        [HttpPut]
        public async Task<IActionResult> AcknowledgePayment(AcknowledgePaymentDTO acknowledgePayment)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (acknowledgePayment == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            acknowledgePayment.UserIpAddress = ipAddress;

            var (isSuccessful, message) = await _payment.AcknowledgePayment(acknowledgePayment);
            if (!isSuccessful)
                return BadRequest(message);

            return Ok(message);

        }
    }
}

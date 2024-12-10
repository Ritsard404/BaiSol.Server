using BaseLibrary.DTO.Payment;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RestSharp;

namespace BaiSol.Server.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    //[Authorize]
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

        [HttpGet]
        public async Task<IActionResult> PaymentProgress(string projId)
        {

            var progress = await _payment.GetPaymentProgress(projId);
            return Ok(progress);

        }

        [HttpGet]
        public async Task<IActionResult> GetAllPayment()
        {

            var payments = await _payment.GetAllPayment();

            return Ok(payments);

        }

        [HttpGet]
        public async Task<IActionResult> SalesReport()
        {

            var payments = await _payment.SalesReport();

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

        [HttpPut]
        public async Task<IActionResult> PayOnCash(PayOnCashDTO payOnCash)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            if (payOnCash == null) return BadRequest(ModelState);

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            payOnCash.UserIpAddress = ipAddress;

            var (isSuccessful, message) = await _payment.PayOnCash(payOnCash);
            if (!isSuccessful)
                return BadRequest(message);

            return Ok(message);

        }

        [HttpPost]
        public async Task<IActionResult> TestCreate()
        {
            var options = new RestClientOptions("https://api.paymongo.com/v1/links");
            var client = new RestClient(options);
            var request = new RestRequest("");
            request.AddHeader("accept", "application/json");
            request.AddHeader("authorization", "Basic c2tfdGVzdF9CbXllbTJ6cHUxMUhFdmF5Qk0zOXF0WjI6");
            request.AddJsonBody("{\"data\":{\"attributes\":{\"amount\":500000,\"description\":\"FDBDB\"}}}", false);

            int retryCount = 0;
            const int maxRetries = 5;
            TimeSpan delay = TimeSpan.FromSeconds(2);

            while (retryCount < maxRetries)
            {
                var response = await client.PostAsync(request);

                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    retryCount++;
                    Console.WriteLine($"Rate limit exceeded. Retrying in {delay.TotalSeconds} seconds...");
                    await Task.Delay(delay);
                    delay = delay * 2; // Exponential backoff
                }
                else if (response.IsSuccessful)
                {
                    Console.WriteLine("{0}", response.Content);
                    return Ok(response.Content);
                }
                else
                {
                    // Log or handle other error cases
                    Console.WriteLine($"Request failed with status code {response.StatusCode}");
                    break;
                }
            }

            return StatusCode(429, "Too many requests. Please try again later.");
        }


    }
}

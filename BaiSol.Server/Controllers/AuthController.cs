using AuthLibrary.DTO;
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProjectLibrary.DTO.Equipment;
namespace BaiSol.Server.Controllers
{
    [Route("auth/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class AuthController(IAuthAccount _userAccount,
        UserManager<AppUsers> _userManager,
        IEmailRepository _emailRepository,
        IConfiguration _config
        ) : ControllerBase

    {

        [HttpGet("ConfirmEmail")]

        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {

            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    return Ok("Your email is successfully verified!");
                }
            }

            ModelState.AddModelError("", "User doesn\'t exist");
            return StatusCode(500, ModelState);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto loginDto, bool? isMobile)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");
            loginDto.UserIpAddress = ipAddress;

            var response = await _userAccount.LoginAccount(loginDto, isMobile);
            if (!response.Flag)
            {
                return BadRequest(response.Message);
            }
            return Ok(response);
        }

        [HttpPost("LogOut")]
        public async Task<IActionResult> LogOut(string email)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");


            var (isLogOut, Message) = await _userAccount.LogOut(email, ipAddress);
            if (!isLogOut)
            {
                return BadRequest(Message);
            }
            return Ok(Message);
        }

        [HttpPost("Login-2FA")]
        public async Task<IActionResult> OTPLogin(OTPDto otp)
        {
            // Retrieve the client IP address
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            // Validate IP address
            if (string.IsNullOrWhiteSpace(ipAddress)) return BadRequest("IP address is required and cannot be empty");


            var login = await _userAccount.Login2FA(otp.Code, otp.Email, ipAddress);
            if (login.AccessToken == null)
            {
                return BadRequest(login);
            }

            return Ok(login);
        }

        [HttpPost("ResendOTP")]
        public async Task<IActionResult> ResendOTP([FromBody] string email)
        {
            var resend = await _userAccount.ResendOTP(email);
            if (resend)
            {
                return Ok(new { message = "We've resent you a new OTP" });
            }
            return BadRequest();
        }


        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null && _config["OwnerEmail"] != user.Email)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var reactAppUrl = _config["FrontEnd_Url:Web_Url"];
                var forgotPasswordLink = $"{reactAppUrl}/change-password?token={token}&email={user.Email}";
                //var forgotPasswordLink = Url.Action(nameof(ResetPassword), "Account", new { token, user.Email }, Request.Scheme);

                var message = new EmailMessage(new string[] { user.Email! }, "Forgot Password Link", $"Click this link to reset password: <a href='{forgotPasswordLink}'>Reset Password</a>");
                _emailRepository.SendEmail(message);
                return Ok($"Password changed request is sent on your email. Please open your email & click the link.");
            }


            ModelState.AddModelError("", "Couldn't send link to email, please try again.");
            return StatusCode(500, ModelState);
        }

        [HttpGet("Reset-Password")]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            return Ok(new { Token = token, Email = email });
        }

        [HttpPost("New-Password")]
        [AllowAnonymous]
        public async Task<IActionResult> NewPassword(ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user != null)
            {
                var resetPassResult = await _userManager.ResetPasswordAsync(user, resetPasswordDto.Token, resetPasswordDto.Password);
                if (!resetPassResult.Succeeded)
                {
                    foreach (var error in resetPassResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return StatusCode(500, ModelState);
                }
                return Ok("Password has been successfully changed!");
            }
            return BadRequest("Couldn\'t send link to email, please try again.");
        }

        [HttpPost("Refresh-Token")]
        public async Task<IActionResult> RefreshToken([FromBody] Token tokenModel)
        {
            if (tokenModel == null || string.IsNullOrEmpty(tokenModel.AccessToken) || string.IsNullOrEmpty(tokenModel.RefreshToken))
            {
                return BadRequest("Invalid client request");
            }

            var refreshTokenResponse = await _userAccount.RefreshToken(tokenModel);

            if (refreshTokenResponse == null || refreshTokenResponse.AccessToken == null)
            {
                return Unauthorized();
            }

            return Ok(refreshTokenResponse);
        }

        [HttpGet("TestEmail")]
        public IActionResult TestEmail()
        {
            var message = new EmailMessage(new string[] { "richardquirante98@gmail.com" }, "Test", "<h1>Test ra goy</h1>");
            _emailRepository.SendEmail(message);
            return Ok(new { Status = "Success", Message = "Email sent successfully!" });
        }

    }
}

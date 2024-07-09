using AuthLibrary.DTO;
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using System.ComponentModel.DataAnnotations;
namespace BaiSol.Server.Controllers
{
    [Route("auth/[controller]")]
    [ApiController]
    public class AccountController(IUserAccount _userAccount,
        UserManager<AppUsers> _userManager,
        IEmailRepository _emailRepository,
        SignInManager<AppUsers> _signInManager,
        RoleManager<IdentityRole> _roleManager
        ) : ControllerBase

    {
        [HttpPost("RegisterAdmin")]
        public async Task<IActionResult> RegisterAdmin(AdminDto adminDto)
        {
            var addAdmin = await _userAccount.CreateAdminAccount(adminDto);

            if (addAdmin.Flag)
            {
                // Add Token to Verify the email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(addAdmin.AppUsers);

                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Account", new { token, addAdmin.AppUsers.Email }, Request.Scheme);

                var message = new EmailMessage(new string[] { addAdmin.AppUsers.Email! }, "Confirmation email link", $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirmation Link</a>");
                _emailRepository.SendEmail(message);
                return Ok($"An email has been sent to {addAdmin.AppUsers.Email} for confirmation.");

            }
            return StatusCode(500, addAdmin);
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token, string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    return Ok("Admin Account Verified");
                }
            }

            ModelState.AddModelError("", "User doesn\'t exist");
            return StatusCode(500, ModelState);
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(LoginDto loginDto)
        {
            var response = await _userAccount.LoginAccount(loginDto);
            return Ok(response);
        }

        [HttpPost("Login-2FA")]
        public async Task<IActionResult> OTPLogin(OTPDto otp)
        {
            var login = await _userAccount.Login2FA(otp.Code, otp.Email);
            if (login.AccessToken == null)
            {
                return StatusCode(500, login);
            }

            return Ok(login);
        }

        [HttpPost("ForgotPassword")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([Required] string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var forgotPasswordLink = Url.Action(nameof(ResetPassword), "Account", new { token, user.Email }, Request.Scheme);

                var message = new EmailMessage(new string[] { user.Email! }, "Forgot Password Link", $"Click this link to reset password: <a href='{forgotPasswordLink}'>Reset Password</a>");
                _emailRepository.SendEmail(message);
                return Ok($"Password changed request is sent on Email {user.Email}. Please open your email & click the link.");
            }
            return BadRequest("Couldn\'t send link to email, please try again.");
        }

        [HttpGet("Reset-Password")]
        public async Task<IActionResult> ResetPassword(string token, string email)
        {
            var modelDto = new ResetPasswordDto { Token = token, Email = email };
            return Ok(modelDto);
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
                return Ok("Password has been changed!");
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

        [HttpPut("Suspend-User")]
        public async Task<IActionResult> SuspendUser(string id)
        {
            await _userAccount.SuspendUser(id);
            return Ok();
        }

        [HttpPut("UnSuspend-User")]
        public async Task<IActionResult> UnSuspendUser(string id)
        {
            await _userAccount.UnSuspendUser(id);
            return Ok();
        }

        [HttpPut("Activate-User")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            await _userAccount.ActivateUser(id);
            return Ok();
        }

        [HttpPut("Deactivate-User")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            await _userAccount.DeactivateUser(id);
            return Ok();
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

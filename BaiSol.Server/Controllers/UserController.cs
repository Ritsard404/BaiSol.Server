using AuthLibrary.DTO;
using AuthLibrary.Services.Interfaces;
using AuthLibrary.Services.Repositories;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace BaiSol.Server.Controllers
{
    [Route("user/[controller]")]
    [ApiController]
    public class UserController(IUserAccount _userAccount,
        UserManager<AppUsers> _userManager,
        IEmailRepository _emailRepository,
        IConfiguration _config
        ) : ControllerBase
    {
        [HttpPost("Register-Admin")]
        public async Task<IActionResult> RegisterAdmin(AdminDto adminDto)
        {
            var addAdmin = await _userAccount.CreateAdminAccount(adminDto);

            if (addAdmin.Flag)
            {
                // Add Token to Verify the email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(addAdmin.AppUsers);
                var reactAppUrl = _config["FrontEnd_Url"];
                var confirmationLink = $"{reactAppUrl}/Confirm-Email?token={token}&email={addAdmin.AppUsers.Email}";

                var message = new EmailMessage(new string[] { addAdmin.AppUsers.Email! }, "Confirmation email link", $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirmation Link</a>");
                _emailRepository.SendEmail(message);
                return Ok($"An email has been sent to {addAdmin.AppUsers.Email} for confirmation.");

            }
            return StatusCode(500, addAdmin);
        }

        [HttpPost("Register-Facilitator")]
        public async Task<IActionResult> RegisterFacilitator(FacilitatorDto facilitatorDto)
        {
            var addFacilitator = await _userAccount.CreateFacilitatorAccount(facilitatorDto);

            if (addFacilitator.Flag)
            {
                // Add Token to Verify the email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(addFacilitator.AppUsers);
                var reactAppUrl = _config["FrontEnd_Url"];
                var confirmationLink = $"{reactAppUrl}/Confirm-Email?token={token}&email={addFacilitator.AppUsers.Email}";

                var message = new EmailMessage(new string[] { addFacilitator.AppUsers.Email! }, "Confirmation email link", $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirmation Link</a>");
                _emailRepository.SendEmail(message);
                return Ok($"An email has been sent to {addFacilitator.AppUsers.Email} for confirmation.");

            }
            return StatusCode(500, addFacilitator);
        }

        [HttpPost("Register-Client")]
        public async Task<IActionResult> RegisterClient(ClientDto clientDto)
        {
            var addClient = await _userAccount.CreateClientAccount(clientDto);

            if (addClient.Flag)
            {
                // Add Token to Verify the email
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(addClient.AppUsers);
                var reactAppUrl = _config["FrontEnd_Url"];
                var confirmationLink = $"{reactAppUrl}/Confirm-Email?token={token}&email={addClient.AppUsers.Email}";

                var message = new EmailMessage(new string[] { addClient.AppUsers.Email! }, "Confirmation email link", $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirmation Link</a>");
                _emailRepository.SendEmail(message);
                return Ok($"An email has been sent to {addClient.AppUsers.Email} for confirmation.");

            }
            return StatusCode(500, addClient);
        }


        [HttpGet]
        public async Task<IActionResult> GetUsers()
        {
            var user = await _userAccount.GetUsersAsync();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }
            return Ok(user);
        }

        [HttpGet("Admin-Users")]
        public async Task<IActionResult> GetUserByRole()
        {
            var admins = await _userAccount.GetAdminUsers();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }
            return Ok(admins);
        }

        [HttpGet("Users-By-Role")]

        public async Task<IActionResult> GetUsersByRole(string role)
        {
            var users = await _userAccount.GetUsersByRole(role);

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);

            }

            if (users == null || !users.Any())
            {
                return StatusCode(400, "Empty Users");
            }

            return Ok(users);
        }

        [HttpPut("Suspend-User")]
        public async Task<IActionResult> SuspendUser(string id)
        {
            await _userAccount.SuspendUser(id);
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
    }
}

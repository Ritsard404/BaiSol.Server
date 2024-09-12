using AuthLibrary.DTO;
using AuthLibrary.Services.Interfaces;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

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
                var message = new EmailMessage(
                    new string[] { addClient.AppUsers.Email! },
                    "Successful Account Registration",
                    "Dear User,\n\nCongratulations! Your account has been successfully registered with BaiSol. We are excited to have you on board. Please note that your account is currently pending approval. Once approved, you'll be able to fully explore and enjoy our services as you embark on your project journey.\n\nThank you for choosing BaiSol!\n\nBest regards,\nThe BaiSol Team"
                );
                _emailRepository.SendEmail(message);


                return Ok("You have successfully registered your account. Please check your email for further instructions.");
            }

            return StatusCode(500, addClient);
        }

        [HttpPut("Approve-Client-Account")]
        public async Task<IActionResult> ApproveClientAccount(string clientId)
        {
            var approveClient = await _userAccount.ApproveClient(clientId);
            if (!approveClient.Flag) return BadRequest("Client does not exist.");


            // Generate the email confirmation token
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(approveClient.ClientUser);

            // URL encode token and email to ensure proper handling of special characters
            var encodedToken = Uri.EscapeDataString(token);
            var encodedEmail = Uri.EscapeDataString(approveClient.ClientUser.Email!);

            // Construct the confirmation link
            var reactAppUrl = _config["FrontEnd_Url"];
            var confirmationLink = $"{reactAppUrl}/Confirm-Email?token={encodedToken}&email={encodedEmail}";

            // Create the email message
            var message = new EmailMessage(
                new string[] { approveClient.ClientUser.Email! },
                "Email Confirmation Link",
                $"Dear {approveClient.ClientUser.UserName},<br/><br/>" +
                $"Your registration is approved by the company.<br/>" +
                $"Please confirm your account by clicking the link below:<br/>" +
                $"<a href='{confirmationLink}'>Confirm Your Email</a><br/><br/>" +
                $"If you did not create an account, please disregard this email.<br/><br/>" +
                $"Best regards,<br/>The BaiSol Team"
            );

            // Send the email
            _emailRepository.SendEmail(message);

            return Ok($"Client with email {approveClient.ClientUser.Email} has been approved. A confirmation email has been sent.");
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

            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);

            //}

            //if (users == null || !users.Any())
            //{
            //    return StatusCode(400, "Empty Users");
            //}

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

        [HttpGet("Available-Clients")]
        public async Task<IActionResult> GetAvailableClients()
        {
            return Ok(await _userAccount.GetAvailableClients());
        }
    }
}

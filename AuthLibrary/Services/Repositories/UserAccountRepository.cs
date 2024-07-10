using AuthLibrary.DTO;
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Repositories
{
    public class UserAccountRepository(UserManager<AppUsers> _userManager,
        RoleManager<IdentityRole> _roleManager,
        SignInManager<AppUsers> _signInManager,
        IEmailRepository _emailRepository,
        IConfiguration _config,
        DataContext _dataContext
        ) : IUserAccount
    {
        public async Task<RegisterResponse> CreateAdminAccount(AdminDto adminDto)
        {
            // Check if the provided adminDto is null and return a response if it is.
            if (adminDto is null) return new RegisterResponse("Model is empty", false, null);

            // Create a new AppUsers object using the data from the provided adminDto.
            AppUsers newAdminUser = new AppUsers()
            {
                UserName = adminDto.FirstName + "_" + adminDto.LastName, // Create a username by concatenating first and last names.
                Email = adminDto.Email, // Set the email from adminDto.
                PasswordHash = adminDto.Password, // Set the password hash (assuming it's already hashed).
                TwoFactorEnabled = true,
            };

            // Check if an admin user with the same email already exists.
            var adminUser = await _userManager.FindByEmailAsync(newAdminUser.Email);
            if (adminUser != null) return new RegisterResponse("Admin already exist!", false, null);

            // Check if an admin user with the same username already exists.
            var adminName = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if (adminName != null) return new RegisterResponse("Username already exist!", false, null);

            // Attempt to create the new admin user.
            var createAdminUser = await _userManager.CreateAsync(newAdminUser!, adminDto.Password);
            if (!createAdminUser.Succeeded)
            {
                // If user creation failed, collect all error messages and return a response.
                var errors = string.Join(", ", createAdminUser.Errors.Select(e => e.Description));
                return new RegisterResponse("Error occurred: " + errors, false, null);
            }

            // Ensure the roles exists in the system.
            await EnsureRoleExists(UserRoles.Admin);
            await EnsureRoleExists(UserRoles.Client);
            await EnsureRoleExists(UserRoles.Facilitator);

            // If the Admin role exists, add the new user to the Admin role.
            if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                await _userManager.AddToRoleAsync(newAdminUser, UserRoles.Admin);
            }

            // Return a success response after the admin user has been successfully created and added to the Admin role.
            return new RegisterResponse("Admin created successfully.", true, newAdminUser);
        }

        // This method ensures that a role exists in the system, creating it if it does not.
        public async Task EnsureRoleExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public string GenerateRefreshToken()
        {
            var ranNum = new Byte[64];
            var generator = RandomNumberGenerator.Create();
            generator.GetBytes(ranNum);
            return Convert.ToBase64String(ranNum);
        }

        public string GenerateAccessToken(UserSession user)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var credential = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);
            var userClaims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id!.ToString()),
                new Claim(ClaimTypes.Name, user.Name!),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Role, user.Role!),
            };

            var token = new JwtSecurityToken(
                    issuer: _config["Jwt:Issuer"],
                    audience: _config["Jwt:Audience"],
                    claims: userClaims,
                    expires: DateTime.Now.AddMinutes(15),
                    signingCredentials: credential
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var secret = _config["Jwt:Key"] ?? throw new InvalidOperationException("Secret not configured");

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _config["Jwt:Issuer"],
                ValidAudience = _config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                ValidateLifetime = false
            };


            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

                if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                    !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                {
                    throw new SecurityTokenException("Invalid token");
                }

                return principal;
            }
            catch (Exception ex)
            {
                // Log the exception and rethrow
                throw new SecurityTokenException("Invalid token", ex);
            }
        }

        public async Task<bool> IsUserExist(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null;
        }

        public async Task<LoginResponse> Login2FA(string code, string email)
        {
            // Check if code or email is empty or null
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
            {
                return new LoginResponse(null, null, "Invalid parameters");
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(email);

            // Attempt two-factor sign-in using email as the provider
            var signInResult = await _userManager.VerifyTwoFactorTokenAsync(user,"Email", code);

            // Check if two-factor sign-in succeeded
            if (signInResult)
            {
                // If user exists
                if (user != null)
                {
                    // Get user roles
                    var getUserRoles = await _userManager.GetRolesAsync(user);
                    var userRole = getUserRoles.FirstOrDefault();

                    // Create a user session object
                    var userSession = new UserSession(user.Id, user.UserName, user.Email, userRole);

                    // Sign in the user using SignInManager
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    // Generate Refresh Token
                    var refreshToken = GenerateRefreshToken();
                    user.RefreshToken = refreshToken;
                    user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);

                    // Update user with new refresh token
                    await _userManager.UpdateAsync(user);

                    // Generate access token
                    var accessToken = GenerateAccessToken(userSession);

                    // Return successful login response with token and refresh token
                    return new LoginResponse(accessToken, refreshToken, "Login Successfully");
                }
            }

            // Return unsuccessful login response
            return new LoginResponse(null, null, $"Invalid OTP! {signInResult.ToString()}");
        }

        public async Task<GeneralResponse> LoginAccount(LoginDto loginDto)
        {
            // Get user from the database
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new GeneralResponse("User not found!", false);
            }

            // Sign out any existing users
            await _signInManager.SignOutAsync();
            await _signInManager.PasswordSignInAsync(user, loginDto.Password, false, true);

            // Check if the user's password is correct
            var checkUserPass = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!checkUserPass || user.IsSuspend || !user.IsActive)
            {
                return new GeneralResponse("Invalid credentials!", false);
            }

            // Check if the user's email is confirmed
            if (!user.EmailConfirmed)
            {
                return new GeneralResponse("Email not confirmed. Please check your email for confirmation link.", false);
            }

            // Generate OTP token for 2FA using email
            var tokenOTP = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            // Send OTP via email
            var message = new EmailMessage(new string[] { loginDto.Email }, "OTP Confirmation", tokenOTP);
            _emailRepository.SendEmail(message);

            // Get user roles
            var getUserRole = await _userManager.GetRolesAsync(user);
            var userRole = getUserRole.FirstOrDefault();

            // Create a user session object
            var userSession = new UserSession(user.Id, user.UserName, user.Email, userRole);

            // Generate access token
            string token = GenerateAccessToken(userSession);

            // Return response indicating OTP sent
            return new GeneralResponse("We've sent you an OTP", true);
        }

        public async Task<LoginResponse> RefreshToken(Token token)
        {
            // Validate and retrieve principal from the expired token
            var principal = GetPrincipalFromExpiredToken(token.AccessToken);

            // Check if principal or its identity is null
            if (principal?.Identity?.Name == null)
            {
                return new LoginResponse(null, null, "Unauthorized!");
            }

            // Find user by username (typically stored in token's Identity.Name)
            var user = await _userManager.FindByNameAsync(principal.Identity.Name);

            // Check if user exists, refresh token matches, and refresh token is not expired
            if (user == null || user.RefreshToken != token.RefreshToken || user.RefreshTokenExpiryTime < DateTime.UtcNow)
            {
                return new LoginResponse(null, null, "Unauthorized!");
            }

            // Get user roles
            var getUserRoles = await _userManager.GetRolesAsync(user);
            var userRole = getUserRoles.FirstOrDefault();

            // Create a user session object
            var userSession = new UserSession(user.Id, user.UserName, user.Email, userRole);

            // Generate a new access token
            var newToken = GenerateAccessToken(userSession);

            // Return successful response with new access token and existing refresh token
            return new LoginResponse(newToken, token.RefreshToken, "Successfully refreshed token!");
        }

        public async Task<bool> SuspendUser(string id)
        {
            var user = await _userManager.FindByEmailAsync(id);

            if (user != null)
            {
                // Update suspension logic
                user.IsSuspend = true;

                // Update user in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return await Save();
                }
            }

            return false; // User not found
        }


        public async Task<bool> UnSuspendUser(string id)
        {
            var user = await _userManager.FindByEmailAsync(id);

            if (user != null)
            {
                // Update suspension logic
                user.IsSuspend = false;

                // Update user in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return await Save();
                }
            }

            return false; // User not found
        }

        public async Task<bool> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                // Update suspension logic
                user.IsActive = false;

                // Update user in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return await Save();
                }
            }

            return false; // User not found
        }

        public async Task<bool> ActivateUser(string id)
        {
            var user = await _userManager.FindByEmailAsync(id);

            if (user != null)
            {
                // Update suspension logic
                user.IsActive = true;

                // Update user in the database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return await Save();
                }
            }

            return false; // User not found
        }

        public async Task<bool> Save()
        {

            var saved = await _dataContext.SaveChangesAsync();
            return saved > 0 ? true : false;
        }

        public async Task<bool> ResendOTP(string email)
        {

            // Get user from the database
            var user = await _userManager.FindByEmailAsync(email);

            // Generate OTP token for 2FA using email
            var tokenOTP = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            // Send OTP via email
            var message = new EmailMessage(new string[] { email }, "OTP Confirmation", tokenOTP);
            _emailRepository.SendEmail(message);

            return true;
        }
    }
}

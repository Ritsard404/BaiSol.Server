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
    public class AuthAccountRepository(UserManager<AppUsers> _userManager,
        SignInManager<AppUsers> _signInManager,
        IEmailRepository _emailRepository,
        IConfiguration _config,
        DataContext _dataContext
        ) : IAuthAccount
    {


        // Generate Refresh Token as JWT
        public string GenerateRefreshToken()
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var credential = new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                expires: DateTime.UtcNow.AddDays(7), // Set appropriate expiration time for refresh tokens
                signingCredentials: credential
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
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

        public async Task<LoginResponse> Login2FA(string code, string email, string ipAddress)
        {
            // Check if code or email is empty or null
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(email))
            {
                return new LoginResponse(null, null, "Invalid parameters");
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(email);

            // Attempt two-factor sign-in using email as the provider
            var signInResult = await _userManager.VerifyTwoFactorTokenAsync(user, "Email", code);

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

                    // Update user with new refresh token
                    await _userManager.UpdateAsync(user);

                    // Generate access token
                    var accessToken = GenerateAccessToken(userSession);


                    _dataContext.UserLogs.Add(new UserLogs
                    {
                        Action = "Logged In",
                        EntityName = "App",
                        EntityId = user.Id,
                        UserIPAddress = ipAddress,
                        Details = $"{userRole} user with email {user.Email} logged in.",
                        UserId = user.Id,
                        UserName = user.NormalizedUserName,
                        UserRole = userRole,
                        User = user,
                    });
                    _dataContext.SaveChangesAsync();

                    // Return successful login response with token and refresh token
                    return new LoginResponse(accessToken, refreshToken, "Login Successfully");
                }
            }

            // Return unsuccessful login response
            return new LoginResponse(null, null, $"Invalid OTP!");
        }
        public async Task<GeneralResponse> LoginAccount(LoginDto loginDto)
        {
            // Get user from the database
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return new GeneralResponse("User not found!", false, false, null, null);
            }

            // Sign out any existing users and sign in with the provided credentials
            await _signInManager.SignOutAsync();
            var isPasswordValid = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!isPasswordValid || await IsUserSuspend(user.Id) || !await IsUserActive(user.Id))
            {
                return new GeneralResponse("Invalid credentials!", false, false, null, null);
            }

            // Check if the user's email is confirmed
            if (!user.EmailConfirmed)
            {
                return new GeneralResponse("Email not confirmed. Please check your email for the confirmation link.", false, false, null, null);
            }

            // Check if the logged-in user is the default admin
            if (IsDefaultAdmin(user.Email))
            {
                return await HandleDefaultAdminLogin(user, loginDto.UserIpAddress);
            }


            // Handle regular user login with OTP
            return await HandleRegularUserLogin(user, loginDto.Email);
        }

        // Helper method to check if the user is the default admin
        private bool IsDefaultAdmin(string userEmail)
        {
            return _config["OwnerEmail"] == userEmail;
        }

        // Helper method to handle login for the default admin
        private async Task<GeneralResponse> HandleDefaultAdminLogin(AppUsers user, string ipAddress)
        {
            // Get user roles
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();

            // Create a user session object
            var userSession = new UserSession(user.Id, user.UserName, user.Email, userRole);

            // Sign in the user using SignInManager
            await _signInManager.SignInAsync(user, isPersistent: false);

            // Generate and update refresh token
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            await _userManager.UpdateAsync(user);

            // Generate access token
            var accessToken = GenerateAccessToken(userSession);


            _dataContext.UserLogs.Add(new UserLogs
            {
                Action = "Logged In",
                EntityName = "App",
                EntityId = user.Id,
                UserIPAddress = ipAddress,
                Details = $"{userRole} user with email {user.Email} logged in.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole,
                User = user,
            });
            await _dataContext.SaveChangesAsync();

            // Return response directly for the default admin without sending OTP
            return new GeneralResponse("Welcome admin.", true, true, accessToken, refreshToken);
        }

        // Helper method to handle regular user login with OTP
        private async Task<GeneralResponse> HandleRegularUserLogin(AppUsers user, string email)
        {
            // Generate OTP token for 2FA using email
            var tokenOTP = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            // Send OTP via email
            var message = new EmailMessage(new string[] { email }, "OTP Confirmation", tokenOTP);
            _emailRepository.SendEmail(message);


            // Return response indicating OTP sent
            return new GeneralResponse("We've sent you an OTP", true, false, null, null);
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
            if (user == null || user.RefreshToken != token.RefreshToken)
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

        public async Task<bool> IsUserSuspend(string id)
        {
            // Get user from the database
            var user = await _userManager.FindByIdAsync(id);

            // Check if the user is suspended
            return user?.Status == "Suspend";
        }

        public async Task<bool> IsUserActive(string id)
        {
            // Get user from the database
            var user = await _userManager.FindByIdAsync(id);


            return user?.Status == "Active" || user?.Status == "OnWork";
        }

        public async Task<(bool, string)> LogOut(string email, string ipAddress)
        {
            // Get user from the database
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return (false, "Invalid User!");
            }

            // Sign out any existing users and sign in with the provided credentials
            await _signInManager.SignOutAsync();
            var userRoles = await _userManager.GetRolesAsync(user);
            var userRole = userRoles.FirstOrDefault();

            _dataContext.UserLogs.Add(new UserLogs
            {
                Action = "Logged Out",
                EntityName = "App",
                EntityId = user.Id,
                UserIPAddress = ipAddress,
                Details = $"{userRole} user with email {user.Email} logged out.",
                UserId = user.Id,
                UserName = user.NormalizedUserName,
                UserRole = userRole,
                User = user,
            });
            await _dataContext.SaveChangesAsync();

            return (true, "You've successfully logged out!");
        }
    }
}

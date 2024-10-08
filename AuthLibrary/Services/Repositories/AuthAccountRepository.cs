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
        IConfiguration _config
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
            if (!checkUserPass || await IsUserSuspend(user.Id) || !await IsUserActive(user.Id))
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


            return user?.Status == "Active"|| user?.Status == "OnWork";
        }
    }
}

using AuthLibrary.DTO;
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using AuthLibrary.Services.Responses;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Repositories
{
    public class UserAccountRepository(DataContext _dataContext,
        UserManager<AppUsers> _userManager,
        RoleManager<IdentityRole> _roleManager,
        ILogger<UserAccountRepository> _logger
        ) : IUserAccount
    {
        public async Task<ICollection<AdminUsersDto>> GetAdminUsers()
        {
            var users = await _dataContext.Users.ToListAsync();
            var adminList = new List<AdminUsersDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (await _userManager.IsEmailConfirmedAsync(user))
                {
                    if (UserRoles.Admin == roles.FirstOrDefault())
                    {
                        adminList.Add(new AdminUsersDto
                        {
                            Id = user.Id,
                            Email = user.Email,
                            UserName = user.NormalizedUserName,
                            Status = user.Status
                        });
                    }
                }
            }

            return adminList;
        }

        public async Task<ICollection<UsersDto>> GetUsersAsync()
        {
            var users = await _dataContext.Users.ToListAsync();
            var userList = new List<UsersDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UsersDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    UserName = user.NormalizedUserName,
                    Role = roles.FirstOrDefault(),
                    Status = user.Status
                });
            }

            return userList;
        }

        public async Task<ICollection<UsersDto>> GetUsersByRole(string role)
        {
            var users = await _dataContext.Users
                .Include(user => user.Client)
                //.Where(user => user.EmailConfirmed) // Consider re-enabling if needed
                .ToListAsync();

            var userList = new List<UsersDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.FirstOrDefault() == role)
                {
                    var userDto = new UsersDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        UserName = user.NormalizedUserName,
                        Role = roles.FirstOrDefault(),
                        AdminEmail = user.AdminEmail,
                        Status = user.Status,
                        UpdatedAt = user.UpdatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                        CreatedAt = user.CreatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                    };

                    if (role == UserRoles.Client)
                    {
                        userDto.ClientContactNum = user.Client?.ClientContactNum;
                        userDto.ClientAddress = user.Client?.ClientAddress;
                        userDto.ClientMonthlyElectricBill = user.Client?.ClientMonthlyElectricBill;
                    }

                    userList.Add(userDto);
                }
            }

            return userList;
        }

        public async Task<bool> SuspendUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                user.Status = "Suspended";
                user.UpdatedAt = DateTimeOffset.UtcNow;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return await Save();
                }
            }

            return false;
        }

        public async Task<bool> DeactivateUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                // Update suspension logic
                user.Status = "InActive";
                user.UpdatedAt = DateTimeOffset.UtcNow;

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
            var user = await _userManager.FindByIdAsync(id);

            if (user != null)
            {
                // Update suspension logic
                user.Status = "Active";
                user.UpdatedAt = DateTimeOffset.UtcNow;

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
                AdminEmail = adminDto.AdminEmail,
                TwoFactorEnabled = true,
            };

            // Check if an admin user with the same email already exists.
            var adminUser = await _userManager.FindByEmailAsync(newAdminUser.Email);
            if (adminUser != null) return new RegisterResponse("Email already exist! Try another.", false, null);

            // Check if an admin user with the same username already exists.
            var adminName = await _userManager.FindByNameAsync(newAdminUser.UserName);
            if (adminName != null) return new RegisterResponse("Username already exist! Try another.", false, null);

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
            return new RegisterResponse("New Admin added successfully.", true, newAdminUser);
        }

        // This method ensures that a role exists in the system, creating it if it does not.
        public async Task EnsureRoleExists(string roleName)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        public async Task<RegisterResponse> CreateFacilitatorAccount(FacilitatorDto facilitatorDto)
        {
            // Check if the provided adminDto is null and return a response if it is.
            if (facilitatorDto is null) return new RegisterResponse("Model is empty", false, null);

            // Check the creator email.
            var adminCreator = await _userManager.FindByEmailAsync(facilitatorDto.AdminEmail);
            if (adminCreator == null) return new RegisterResponse("Admin not exist!", false, null);

            // Create a new AppUsers object using the data from the provided adminDto.
            AppUsers newFacilitatorUser = new AppUsers()
            {
                UserName = facilitatorDto.FirstName + "_" + facilitatorDto.LastName, // Create a username by concatenating first and last names.
                Email = facilitatorDto.Email,
                PasswordHash = facilitatorDto.Password,
                TwoFactorEnabled = true,
                AdminEmail = facilitatorDto.AdminEmail
            };

            // Check if an facilitator user with the same email already exists.
            var facilitatorUser = await _userManager.FindByEmailAsync(newFacilitatorUser.Email);
            if (facilitatorUser != null) return new RegisterResponse("Email already exist! Try another.", false, null);

            // Check if an facilitator user with the same username already exists.
            var facilitatorName = await _userManager.FindByNameAsync(newFacilitatorUser.UserName);
            if (facilitatorName != null) return new RegisterResponse("Username already exist! Try another.", false, null);

            // Attempt to create the new admin user.
            var createFacilitatorUser = await _userManager.CreateAsync(newFacilitatorUser!, facilitatorDto.Password);
            if (!createFacilitatorUser.Succeeded)
            {
                // If user creation failed, collect all error messages and return a response.
                var errors = string.Join(", ", createFacilitatorUser.Errors.Select(e => e.Description));
                return new RegisterResponse("Error occurred: " + errors, false, null);
            }

            // If the Admin role not exists, add the roles.
            if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                // Ensure the roles exists in the system.
                await EnsureRoleExists(UserRoles.Admin);
                await EnsureRoleExists(UserRoles.Client);
                await EnsureRoleExists(UserRoles.Facilitator);
            }

            await _userManager.AddToRoleAsync(newFacilitatorUser, UserRoles.Facilitator);

            // Return a success response after the admin user has been successfully created and added to the Admin role.
            return new RegisterResponse("New facilitator added successfully.", true, newFacilitatorUser);
        }

        public async Task<RegisterResponse> CreateClientAccount(ClientDto clientDto)
        {
            // Check if the provided clientDto is null and return a response if it is.
            if (clientDto is null) return new RegisterResponse("Model is empty", false, null);

            var adminCreator = await _userManager.FindByEmailAsync(clientDto.AdminEmail);
            if (adminCreator == null) return new RegisterResponse("Admin not exist!", false, null);

            Client newClient = new Client()
            {
                ClientContactNum = clientDto.ClientContactNum,
                ClientAddress = clientDto.ClientAddress,
                ClientMonthlyElectricBill = clientDto.ClientMonthlyElectricBill
            };

            // Create a new AppUsers object using the data from the provided clientDto.
            AppUsers newClientUser = new AppUsers()
            {
                UserName = clientDto.FirstName + "_" + clientDto.LastName, // Create a username by concatenating first and last names.
                Email = clientDto.Email,
                PasswordHash = clientDto.Password,
                TwoFactorEnabled = true,
                AdminEmail = clientDto.AdminEmail,
                Client = newClient
            };

            // Check if an client user with the same email already exists.
            var clientUser = await _userManager.FindByEmailAsync(newClientUser.Email);
            if (clientUser != null) return new RegisterResponse("Email already exist! Try another.", false, null);

            // Check if an client user with the same username already exists.
            var clientName = await _userManager.FindByNameAsync(newClientUser.UserName);
            if (clientName != null) return new RegisterResponse("Username already exist! Try another.", false, null);


            CreateClient(newClient);
            // Attempt to create the new client user.
            var createFacilitatorUser = await _userManager.CreateAsync(newClientUser!, clientDto.Password);
            if (!createFacilitatorUser.Succeeded)
            {
                // If user creation failed, collect all error messages and return a response.
                var errors = string.Join(", ", createFacilitatorUser.Errors.Select(e => e.Description));
                return new RegisterResponse("Error occurred: " + errors, false, null);
            }

            // If the Admin role not exists, add the roles.
            if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                // Ensure the roles exists in the system.
                await EnsureRoleExists(UserRoles.Admin);
                await EnsureRoleExists(UserRoles.Client);
                await EnsureRoleExists(UserRoles.Facilitator);
            }

            await _userManager.AddToRoleAsync(newClientUser, UserRoles.Client);

            // Return a success response after the client user has been successfully created and added to the Client role.
            return new RegisterResponse("New client added successfully.", true, newClientUser);
        }

        public void CreateClient(Client client)
        {
            _dataContext.Client.Add(client);
            _dataContext.SaveChanges();
        }
    }
}

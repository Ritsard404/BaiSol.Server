using AuthLibrary.DTO;
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;
using static AuthLibrary.Services.Responses.AuthResponse;

namespace AuthLibrary.Services.Repositories
{
    public class UserAccountRepository(DataContext _dataContext,
        UserManager<AppUsers> _userManager,
        IConfiguration _configuration,
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
                    Name = $"{user.FirstName} {user.LastName}",
                    Role = roles.FirstOrDefault(),
                    Status = user.Status
                });
            }

            return userList;
        }

        public async Task<ICollection<UsersDto>> GetUsersByRole(string role)
        {
            var ownerEmail = _configuration["OwnerEmail"];

            var users = await _dataContext.Users
                .Include(user => user.Client)
                .Where(user => user.Email != ownerEmail) // Consider re-enabling if needed
                                                         //.Where(u => u.Email != ownerEmail)
                .ToListAsync();

            var userList = new List<UsersDto>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                if (roles.Contains(role))
                {
                    var userDto = new UsersDto
                    {
                        Id = user.Id,
                        Email = user.Email,
                        UserName = $"{user.FirstName} {user.LastName}",
                        Name = $"{user.FirstName} {user.LastName}",
                        Role = roles.FirstOrDefault(),
                        AdminEmail = user.AdminEmail,
                        Status = user.Status,
                        UpdatedAt = user.UpdatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                        CreatedAt = user.CreatedAt.ToString("MMM dd, yyyy HH:mm:ss"),
                    };

                    if (role == UserRoles.Facilitator)
                    {
                        var currentProject = await _dataContext.ProjectWorkLog
                            .Include(p => p.Project)
                            .FirstOrDefaultAsync(c => c.Facilitator == user && c.Project.Status != "Finished");

                        var handledProjects = await _dataContext.ProjectWorkLog
                            .Include(p => p.Project)
                            .OrderBy(s => s.Project.Status)
                            .Where(c => c.Facilitator == user)
                            .Select(s => new ProjectInfo { ProjId = s.Project.ProjId })
                            .ToListAsync();


                        userDto.CurrentProjId = currentProject?.Project?.ProjId;
                        userDto.ClientProjects = handledProjects;

                    }

                    if (role == UserRoles.Client)
                    {
                        var project = await _dataContext.Project
                            .FirstOrDefaultAsync(c => c.Client == user);

                        var currentProj = await _dataContext.Project
                            .FirstOrDefaultAsync(c => c.Client == user && c.Status != "Finished");

                        var projects = await _dataContext.Project
                            .Where(c => c.Client == user)
                            .OrderBy(s => s.Status)
                            .Select(s => new ProjectInfo { ProjId = s.ProjId })
                            .ToListAsync();

                        userDto.ClientContactNum = user.Client?.ClientContactNum;
                        userDto.ClientAddress = user.Client?.ClientAddress;
                        userDto.Sex = user.Client?.IsMale == true ? "Male" : "Female";
                        userDto.kWCapacity = project?.kWCapacity;
                        userDto.CurrentProjId = project?.SystemType + " " + project?.kWCapacity + "kW";
                        userDto.ClientProjects = projects;
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
            return saved > 0;
        }

        public async Task<RegisterResponse> CreateAdminAccount(AdminDto adminDto)
        {
            // Check if the provided adminDto is null and return a response if it is.
            if (adminDto is null) return new RegisterResponse("Model is empty", false, null);

            // Check the creator email.
            var adminCreator = await _userManager.FindByEmailAsync(adminDto.AdminEmail);
            if (adminCreator == null) return new RegisterResponse("Admin not exist!", false, null);

            // Create a new AppUsers object using the data from the provided adminDto.
            AppUsers newAdminUser = new AppUsers()
            {
                UserName = adminDto.FirstName + "_" + adminDto.Email, // Create a username by concatenating first and last names.
                Email = adminDto.Email, // Set the email from adminDto.
                LastName = adminDto.LastName,
                FirstName = adminDto.FirstName,
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

            //// Ensure the roles exists in the system.
            //await EnsureRoleExists(UserRoles.Admin);
            //await EnsureRoleExists(UserRoles.Client);
            //await EnsureRoleExists(UserRoles.Facilitator);

            //// If the Admin role exists, add the new user to the Admin role.
            //if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            //{
            //    await _userManager.AddToRoleAsync(newAdminUser, UserRoles.Admin);
            //}

            await _userManager.AddToRoleAsync(newAdminUser, UserRoles.Admin);

            // Return a success response after the admin user has been successfully created and added to the Admin role.
            return new RegisterResponse("New Admin added successfully.", true, newAdminUser);
        }

        // This method ensures that a role exists in the system, creating it if it does not.
        //public async Task EnsureRoleExists(string roleName)
        //{
        //    if (!await _roleManager.RoleExistsAsync(roleName))
        //    {
        //        await _roleManager.CreateAsync(new IdentityRole(roleName));
        //    }
        //}

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
                UserName = facilitatorDto.FirstName + "_" + facilitatorDto.Email, // Create a username by concatenating first and last names.
                FirstName = facilitatorDto.FirstName,
                LastName = facilitatorDto.LastName,
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

            //// If the Admin role not exists, add the roles.
            //if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
            //{
            //    // Ensure the roles exists in the system.
            //    await EnsureRoleExists(UserRoles.Admin);
            //    await EnsureRoleExists(UserRoles.Client);
            //    await EnsureRoleExists(UserRoles.Facilitator);
            //}

            await _userManager.AddToRoleAsync(newFacilitatorUser, UserRoles.Facilitator);

            // Return a success response after the admin user has been successfully created and added to the Admin role.
            return new RegisterResponse("New facilitator added successfully.", true, newFacilitatorUser);
        }

        public async Task<RegisterResponse> CreateClientAccount(ClientDto clientDto)
        {
            try
            {
                if (clientDto == null) return new RegisterResponse("Model is empty", false, null);

                //var adminCreator = await _userManager.FindByEmailAsync(clientDto.AdminEmail);
                //if (adminCreator == null) return new RegisterResponse("Admin not exist!", false, null);

                var clientUser = await _userManager.FindByEmailAsync(clientDto.Email);
                if (clientUser != null) return new RegisterResponse("Email already exists!", false, null);

                var clientName = await _userManager.FindByNameAsync(clientDto.FirstName + "_" + clientDto.LastName);
                if (clientName != null) return new RegisterResponse("Username already exists!", false, null);

                if (clientDto.kWCapacity < 1 || clientDto.kWCapacity > 20)
                    return new RegisterResponse("Invalid kW Capacity!", false, null);

                var newClient = new Client
                {
                    ClientContactNum = clientDto.ClientContactNum,
                    IsMale = clientDto.IsMale,
                    ClientAddress = clientDto.ClientAddress,
                };

                _dataContext.Client.Add(newClient);

                var newClientUser = new AppUsers
                {
                    UserName = clientDto.FirstName + "_" + clientDto.Email,
                    FirstName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clientDto.FirstName.ToLower()),
                    LastName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(clientDto.LastName.ToLower()),
                    Email = clientDto.Email,
                    //AdminEmail = clientDto.AdminEmail,
                    Client = newClient,
                    Status = "Pending"
                };

                var createClientUser = await _userManager.CreateAsync(newClientUser, clientDto.Password);
                if (!createClientUser.Succeeded)
                {
                    var errors = string.Join(", ", createClientUser.Errors.Select(e => e.Description));
                    return new RegisterResponse("Error occurred: " + errors, false, null);
                }

                await _userManager.AddToRoleAsync(newClientUser, UserRoles.Client);


                Project newProject = new Project
                {
                    ProjDescript = $"To provide a {clientDto.kWCapacity} kW {clientDto.SystemType} system for a residential home.",
                    ProjName = $"{(clientDto.IsMale == true ? "Mr." : "Ms./Mrs.")} {newClientUser.FirstName} {newClientUser.LastName}",
                    Client = newClientUser,
                    kWCapacity = clientDto.kWCapacity,
                    SystemType = clientDto.SystemType,
                };

                _dataContext.Project.Add(newProject);

                var manPowerQTY = 0;
                var estimationDate = 0;

                if (clientDto.kWCapacity <= 5)
                {
                    manPowerQTY = 5;
                    estimationDate = 7;
                }
                else if (clientDto.kWCapacity > 5 && clientDto.kWCapacity <= 10)
                {
                    manPowerQTY = 7;
                    estimationDate = 15;
                }
                else if (clientDto.kWCapacity > 10 && clientDto.kWCapacity <= 15)
                {
                    manPowerQTY = 10;
                    estimationDate = 25;
                }
                else
                {
                    manPowerQTY = 12;
                    estimationDate = 35;
                }


                var predefinedCosts = new[]
                {
                new Labor { LaborDescript = "Manpower", LaborQuantity = manPowerQTY, LaborUnit = "Days", LaborNumUnit=estimationDate, Project = newProject },
                new Labor { LaborDescript = "Project Manager - Electrical Engr.", LaborQuantity = 1, LaborUnit = "Days", LaborNumUnit=estimationDate, Project = newProject },
                new Labor { LaborDescript = "Mobilization/Demob", LaborUnit = "Lot", Project = newProject },
                new Labor { LaborDescript = "Tools & Equipment", LaborUnit = "Lot", Project = newProject },
                new Labor { LaborDescript = "Other Incidental Costs", LaborUnit = "Lot", Project = newProject }
                 };

                foreach (var labor in predefinedCosts)
                {
                    if (!await _dataContext.Labor
                        .Include(p => p.Project)
                        .AnyAsync(proj => proj.Project.ProjDescript == newProject.ProjDescript && proj.LaborDescript == labor.LaborDescript))
                    {
                        _dataContext.Labor.Add(labor);
                    }
                }



                await _dataContext.SaveChangesAsync();
                return new RegisterResponse("New client added successfully.", true, newClientUser);
            }
            catch (Exception ex)
            {
                return new RegisterResponse($"An error occurred: {ex.Message}", false, null);
            }
        }

        public void CreateClient(Client client, Project project)
        {
            _dataContext.Client.Add(client);
            _dataContext.Project.Add(project);
            _dataContext.SaveChanges();
        }

        public async Task<ICollection<AvailableClients>> GetAvailableClients()
        {
            var clients = await _dataContext.Project
                .Include(p => p.Client) // Include Client to ensure client details are loaded
                .GroupBy(p => p.Client) // Group projects by each Client
                .Where(g => g.All(p => p.Status == "Finished" && p.Client.EmailConfirmed)) // Only include clients whose all projects are finished
                .Select(g => g.Key) // Select the client from each group
                .ToListAsync();

            var clientList = new List<AvailableClients>();


            foreach (var client in clients)
            {
                //var roles = await _userManager.GetRolesAsync(client.Client);
                //if (UserRoles.Client == roles.FirstOrDefault())
                //{
                clientList.Add(new AvailableClients
                {
                    ClientId = client.Id,
                    ClientEmail = client.NormalizedEmail
                });
                //}
            }

            return clientList.OrderBy(a => a.ClientEmail).ToList();
        }

        public async Task<ApprovalResponse> ApproveClient(string clientId, string adminEmail)
        {
            var newClient = await _userManager.Users
                .Include(c => c.Client)
                .FirstOrDefaultAsync(u => u.Id == clientId);

            if (newClient == null) return new ApprovalResponse(false, null);

            var admin = await _userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
                return new ApprovalResponse(false, null);


            //Project newProject = new Project
            //{
            //    ProjDescript = $"{newClient.Client.ClientAddress} Solar Project",
            //    ProjName = $"{newClient.NormalizedUserName} Project",
            //    Client = newClient
            //};

            //_dataContext.Project.Add(newProject);



            //var predefinedCosts = new[]
            //{
            //    new Labor { LaborDescript = "Manpower", LaborUnit = "Days", Project = newProject },
            //    new Labor { LaborDescript = "Project Manager - Electrical Engr.", LaborQuantity = 1, LaborUnit = "Days", Project = newProject },
            //    new Labor { LaborDescript = "Mobilization/Demob", LaborUnit = "Lot", Project = newProject },
            //    new Labor { LaborDescript = "Tools & Equipment", LaborUnit = "Lot", Project = newProject },
            //    new Labor { LaborDescript = "Other Incidental Costs", LaborUnit = "Lot", Project = newProject }
            //};

            //foreach (var labor in predefinedCosts)
            //{
            //    if (!await _dataContext.Labor
            //        .Include(p => p.Project)
            //        .AnyAsync(proj => proj.Project.ProjDescript == newProject.ProjDescript && proj.LaborDescript == labor.LaborDescript))
            //    {
            //        _dataContext.Labor.Add(labor);
            //    }
            //}

            newClient.Status = "Active";
            newClient.AdminEmail = admin.Email;
            return new ApprovalResponse(await Save(), newClient);
        }
    }
}

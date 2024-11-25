

using AutoMapper.Configuration.Annotations;
using BaseLibrary.Services.Interfaces;
using DataLibrary.Data;
using DataLibrary.Models;
using DataLibrary.Models.Gantt;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using ProjectLibrary.DTO.Material;
using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;
using RestSharp;
using System.Text.Json;

namespace ProjectLibrary.Services.Repositories
{
    public class ProjectRepository(UserManager<AppUsers> _userManager, DataContext _dataContext, IUserLogs _userLogs, IConfiguration _config) : IProject
    {
        public async Task<string> AddNewClientProject(ProjectDto projectDto)
        {
            //if (projectDto == null)
            //{
            //    throw new ArgumentNullException(nameof(projectDto));
            //}

            // Check if the client exists
            var client = await _userManager.FindByIdAsync(projectDto.ClientId);
            if (client == null)
            {
                return "Client does not exist";
            }

            //// Map DTO to model
            //var projectMap = _mapper.Map<Project>(projectDto);

            // Generate unique ProjId
            string uniqueProjId;
            do
            {
                uniqueProjId = Guid.NewGuid().ToString();
            } while (await IsProjIdExist(uniqueProjId));

            //projectMap.ProjId = uniqueProjId;

            var newProject = new Project
            {
                ProjDescript = projectDto.ProjDescript,
                ProjName = projectDto.ProjName,
                ProjId = uniqueProjId,
                Client = client,
                kWCapacity = projectDto.kWCapacity,
                SystemType = projectDto.SystemType,
            };

            // Add the new Supply entity to the context
            _dataContext.Project.Add(newProject);


            var predefinedCosts = new[]
            {
                new Labor { LaborDescript = "Manpower", LaborUnit = "Days", Project = newProject },
                new Labor { LaborDescript = "Project Manager - Electrical Engr.", LaborQuantity = 1, LaborUnit = "Days", Project = newProject },
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


            // Save changes to the database
            var saveResult = await Save();

            await _userLogs.LogUserActionAsync(client.Email, "Create", "Project", newProject.ProjId, $"New project created", projectDto.ipAddress);


            return saveResult ? null : "Something went wrong while saving";
        }

        public async Task<bool> DeleteClientProject(string projId)
        {
            // Retrieve the project entity
            var project = await _dataContext.Project
                .FindAsync(projId);

            // Check if project exists and if it has associated materials or labor
            if (project == null ||
                await _dataContext.Supply.AnyAsync(s => s.Project.ProjId == projId) ||
                await _dataContext.Labor.AnyAsync(l => l.Project.ProjId == projId))
            {
                return false;
            }

            _dataContext.Project.Remove(project);

            return await Save();
        }

        public async Task<ICollection<GetProjects>> GetClientProject(string clientId)
        {
            return await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(p => p.Client.Id == clientId && p.Client.EmailConfirmed == true)
                .Select(p => new GetProjects
                {
                    ProjId = p.ProjId,
                    ProjName = p.ProjName,
                    ProjDescript = p.ProjDescript,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt.ToString("MMM dd, yyyy"),
                    UpdatedAt = p.UpdatedAt.ToString("MMM dd, yyyy"),
                    ClientId = p.Client.Id,
                    ClientName = $"{(p.Client.Client.IsMale ? "Mr." : "Mrs./Ms.")} {p.Client.FirstName} {p.Client.LastName}",
                    ClientAddress = p.Client.Client.ClientAddress
                })
                .ToListAsync();
        }

        public async Task<ClientProjectInfoDTO> GetClientProjectInfo(string projId)
        {
            var projectData = await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(i => i.ProjId == projId && i.Client.EmailConfirmed == true)
                .Select(d => new
                {
                    d.ProjId,
                    d.ProjName,
                    d.SystemType,
                    d.ProjDescript,
                    boolSex = d.Client.Client.IsMale,
                    fName = d.Client.FirstName,
                    lName = d.Client.LastName,
                    sex = d.Client.Client.IsMale ? "Male" : "Female",
                    Discount = d.Discount ?? 0,
                    VatRate = (d.VatRate ?? 0) * 100,
                    clientId = d.Client.Id,
                    clientContactNum = d.Client.Client.ClientContactNum,
                    clientAddress = d.Client.Client.ClientAddress,
                    kWCapacity = d.kWCapacity
                })
                .FirstOrDefaultAsync();

            var installers = await _dataContext.ProjectWorkLog
                .Include(i => i.Installer)
                .Where(w => w.Project.ProjId == projId && w.Installer != null)
                .OrderBy(p => p.Installer.Position)
                .ToListAsync();

            List<InstallerInfo> installerList = new List<InstallerInfo>();

            foreach (var installer in installers)
            {
                installerList.Add(new InstallerInfo
                {
                    Name = installer.Installer.Name, // Adjust based on your model properties
                    Position = installer.Installer.Position // Adjust based on your model properties
                });
            }

            return new ClientProjectInfoDTO
            {
                ProjId = projectData?.ProjId ?? "",
                ProjName = projectData?.ProjName,
                ProjDescript = projectData?.ProjDescript,
                Discount = projectData?.Discount ?? 0,
                VatRate = projectData?.VatRate ?? 0,
                clientId = projectData?.clientId ?? "",
                clientFName = projectData?.fName,
                clientLName = projectData?.lName,
                clientContactNum = projectData?.clientContactNum,
                clientAddress = projectData?.clientAddress,
                kWCapacity = projectData.kWCapacity,
                Sex = projectData.sex,
                SystemType = projectData.SystemType,
                isMale = projectData.boolSex,
                Installers = installerList,

            };
        }

        public async Task<ICollection<GetProjects>> GetClientsProject()
        {
            var projects = await _dataContext.Project
                .Include(p => p.Client) // Include related Client data
                .Include(p => p.Client.Client)
                .Where(c => c.Client.EmailConfirmed == true)
                .OrderByDescending(f => f.Status)
                .ThenByDescending(p => p.CreatedAt) // Order by CreatedAt first
                .ToListAsync(); // Fetch the data

            // Map to DTO after fetching data
            var projectDtos = projects.Select(p => new GetProjects
            {
                ProjId = p.ProjId,
                ProjName = p.ProjName,
                ProjDescript = p.ProjDescript,
                Status = p.Status,
                CreatedAt = p.CreatedAt.ToString("MMM dd, yyyy"),
                UpdatedAt = p.UpdatedAt.ToString("MMM dd, yyyy"),
                ClientId = p.Client?.Id, // Handle potential null reference
                ClientName = p.Client.NormalizedUserName,
                ClientAddress = p.Client.Client.ClientAddress
            }).ToList();

            return projectDtos;
        }

        public async Task<bool> IsProjectOnGoing(string projId)
        {
            var project = await _dataContext.Project
                .FirstOrDefaultAsync(i => i.ProjId == projId && i.Status == "OnGoing");

            return project != null; // Returns true if project exists, false otherwise
        }

        public async Task<bool> IsProjIdExist(string projId)
        {
            return await _dataContext.Project.AnyAsync(p => p.ProjId == projId);
        }

        public async Task<ProjectQuotationTotalExpense> ProjectQuotationExpense(string? projId, string? customerEmail)
        {
            decimal overallMaterialTotal = 0;
            decimal overallLaborProjectTotal = 0;
            decimal total = 0;
            List<Project> projectInfo = new List<Project>();


            if (!string.IsNullOrEmpty(projId) || !string.IsNullOrEmpty(customerEmail))
            {
                // Fetch material supply using both projId and customerEmail
                var materialSupply = await _dataContext.Supply
                    .Include(i => i.Material)
                    .Include(p => p.Project.Client)
                    .Where(p =>
                        (projId != null && p.Project.ProjId == projId))
                    .ToListAsync();

                // Prioritize customerEmail data if projId is empty
                if (!materialSupply.Any())
                {
                    materialSupply = await _dataContext.Supply
                        .Include(i => i.Material)
                        .Include(p => p.Project.Client)
                        .Where(p => p.Project.Client.Email == customerEmail)
                        .ToListAsync();
                }

                // Calculate total unit cost and build-up cost in one pass
                var (totalUnitCostSum, buildUpCostSum) = materialSupply
                    .Where(m => m.Material != null)
                    .GroupBy(m => m.Material.MTLDescript)
                    .Aggregate(
                        (totalUnitCost: 0m, buildUpCost: 0m),
                        (acc, group) =>
                        {
                            var quantity = group.Sum(m => m.MTLQuantity ?? 0);
                            var price = group.First().Material.MTLPrice;
                            var unitCost = quantity * price;
                            var buildUpCost = unitCost * 1.2m;

                            return (acc.totalUnitCost + unitCost, acc.buildUpCost + buildUpCost);
                        }
                    );

                // Calculate profit and overall totals
                var profitPercentage = materialSupply.Select(p => p.Project.ProfitRate).FirstOrDefault();
                var profit = totalUnitCostSum * profitPercentage;
                overallMaterialTotal = totalUnitCostSum + profit;

                overallLaborProjectTotal = await _dataContext.Labor
                    .Where(p => projId != null ? p.Project.ProjId == projId : p.Project.Client.Email == customerEmail)
                    .SumAsync(o => o.LaborCost) * (profitPercentage + 1);

                projectInfo = await _dataContext.Project
                    .Include(c => c.Client)
                    .Include(c => c.Client.Client)
                    .Where(p => projId != null ? p.ProjId == projId : p.Client.Email == customerEmail)
                    .ToListAsync();

                total = overallMaterialTotal + overallLaborProjectTotal;
            }

            var result = projectInfo.Select(i =>
            {
                decimal discountRate = i.Discount ?? 0;
                decimal vatRate = i.VatRate ?? 0;

                // Calculate total
                decimal total = overallMaterialTotal + overallLaborProjectTotal;

                // Calculate subtotal after discount
                decimal subtotalAfterDiscount = total - discountRate;

                // Calculate VAT
                decimal vatAmount = subtotalAfterDiscount * vatRate;

                // Calculate final total
                decimal finalTotal = subtotalAfterDiscount + vatAmount;

                // Create an instance of ProjectQuotationSupply for TotalLaborCost
                var totalLaborCost = new ProjectQuotationSupply
                {
                    description = "Total Labor and Installation Cost", // You can modify this as needed
                    lineTotal = overallLaborProjectTotal.ToString("#,##0.00") // Ensure the line total is formatted as a string
                };

                // Create an instance of ProjectQuotationSupply for TotalLaborCost
                var totalMaterialCost = new ProjectQuotationSupply
                {
                    description = "Total Material Cost", // You can modify this as needed
                    lineTotal = overallMaterialTotal.ToString("#,##0.00") // Ensure the line total is formatted as a string
                };


                return new ProjectQuotationTotalExpense
                {
                    QuoteId = i.ProjId,
                    SubTotal = total.ToString("#,##0.00"),
                    Discount = discountRate.ToString("#,##0.00"),
                    SubTotalAfterDiscount = subtotalAfterDiscount.ToString("#,##0.00"),
                    VAT = vatAmount.ToString("#,##0.00"),
                    VatRate = (vatRate * 100).ToString("0.##") + "%",
                    Total = finalTotal.ToString("#,##0.00"),
                    TotalLaborCost = totalLaborCost,
                    TotalMaterialCost = totalMaterialCost,
                    EstimationDate = i.kWCapacity <= 5 ? 7 : i.kWCapacity >= 6 && i.kWCapacity <= 10 ? 15 : i.kWCapacity >= 11 && i.kWCapacity <= 15 ? 25 : 35
                };
            }).FirstOrDefault();

            return result;
        }

        public async Task<ProjectQuotationInfoDTO> ProjectQuotationInfo(string? projId, string? customerEmail)
        {
            List<Project> projectInfo = new List<Project>();

            if (!string.IsNullOrEmpty(projId) || !string.IsNullOrEmpty(customerEmail))
            {
                projectInfo = await _dataContext.Project
                    .Include(p => p.Client)
                    .Include(p => p.Client.Client)
                    .Where(p => projId != null ? p.ProjId == projId : p.Client.Email == customerEmail)
                    .ToListAsync();
            }


            // If no data is found, return an empty list
            if (projectInfo == null || !projectInfo.Any())
            {
                return new ProjectQuotationInfoDTO();  // Return an empty collection
            }

            var result = projectInfo.Select(i => new ProjectQuotationInfoDTO
            {
                customerId = i.Client.Id,
                customerEmail = i.Client.Email,
                customerName = $"{(i.Client.Client.IsMale ? "Mr." : "Mrs./Ms.")} {i.Client.FirstName} {i.Client.LastName}",
                customerAddress = i.Client.Client.ClientAddress,
                projectId = i.ProjId,
                projectDescription = i.ProjDescript,
                projectDateCreation = i.CreatedAt.ToString("MMMM dd, yyyy"),
                projectDateValidity = i.CreatedAt.AddDays(7).ToString("MMMM dd, yyyy")

            }).FirstOrDefault();

            return result;
        }

        public async Task<ICollection<ProjectQuotationSupply>> ProjectQuotationSupply(string? projId)
        {
            List<Supply> quotationData = new List<Supply>();


            return await _dataContext.Supply
                .Where(p => p.Project.ProjId == projId)
                .Include(i => i.Material)
                .GroupBy(c => c.Material.MTLCategory)
                .Select(g => new ProjectQuotationSupply
                {
                    description = g.Key,
                    lineTotal = (g.Sum(s => (decimal)(s.MTLQuantity ?? 0) * s.Material.MTLPrice * (s.Project.ProfitRate + 1))).ToString("#,##0.00") // Calculate the total expense

                })
                .ToListAsync();

            //if (!string.IsNullOrEmpty(projId))
            //{
            //    // Fetch only supplies where MTLQuantity and Material.MTLPrice are not null
            //    quotationData = await _dataContext.Supply
            //        .Include(m => m.Material)
            //        .Include(m => m.Equipment)
            //        .Include(m => m.Project)
            //        .Where(p => p.Project.ProjId == projId
            //                    && p.MTLQuantity != null // Ensure MTLQuantity is not null
            //                    && p.Material.MTLPrice != null) // Ensure MTLPrice is not null
            //        .OrderBy(c => c.Material.MTLCategory)
            //        .ToListAsync();
            //}

            //// If no data is found, return an empty list
            //if (quotationData == null || !quotationData.Any())
            //{
            //    return new List<ProjectQuotationSupply>();  // Return an empty collection
            //}

            //var result = quotationData.Select(supply => new ProjectQuotationSupply
            //{
            //    description = supply.Material?.MTLDescript ?? "No Description",  // Provide a default description if null
            //    lineTotal = (((decimal)(supply.MTLQuantity ?? 0) * (supply.Material?.MTLPrice ?? 0)) * supply.Project.ProfitRate).ToString("#,##0.00") // Calculate total safely
            //}).ToList();

            //return result;
        }


        //public async Task<ICollection<ProjectQuotationInfoDTO>> ProjectQuotationInfoByProjId(string projId)
        //{
        //    //return await _dataContext.Project
        //    //   .Include(p => p.Client)
        //    //   .Include(p => p.Client.Client)
        //    //   .Where(p => p.Client.Id == clientId)
        //    //   .Select(p => new GetProjects
        //    //   {
        //    //       ProjId = p.ProjId,
        //    //       ProjName = p.ProjName,
        //    //       ProjDescript = p.ProjDescript,
        //    //       Status = p.Status,
        //    //       CreatedAt = p.CreatedAt.ToString("MMM dd, yyyy"),
        //    //       UpdatedAt = p.UpdatedAt.ToString("MMM dd, yyyy"),
        //    //       ClientId = p.Client.Id,
        //    //       ClientName = p.Client.NormalizedUserName,
        //    //       ClientAddress = p.Client.Client.ClientAddress
        //    //   })
        //    //   .ToListAsync();

        //    var projectInfo = await _dataContext.Project
        //         .Include(p => p.Client)
        //         .Include(p => p.Client.Client )
        //         .Where(p => p.ProjId == projId)
        //         .ToListAsync();

        //    var quotationData = await _dataContext.Supply
        //        .Include(m => m.Material)
        //        .Include(m => m.Equipment)
        //        .Include(m => m.Project)
        //        .Where(p => p.Project.ProjId == projId)
        //        .ToListAsync();


        //    throw new NotImplementedException();

        //}

        public async Task<bool> Save()
        {
            var saved = _dataContext.SaveChangesAsync();
            return await saved > 0 ? true : false;
        }

        public async Task<(bool, string)> UpdateClientProject(UpdateClientProjectInfoDTO updateProject)
        {
            // Retrieve the project entity
            var project = await _dataContext.Project
                .FindAsync(updateProject.ProjId);

            if (project == null) return (false, "Project not found");

            // Retrieve the client entity using the client ID from the DTO, assuming it's available
            var client = await _userManager.Users
                .Include(c => c.Client) // Ensure the related Client entity is included
                .FirstOrDefaultAsync(c => c.Id == updateProject.clientId); // Use clientId instead of projId

            if (client == null) return (false, "Client not found");

            var laborManpower = await _dataContext.Labor
                .Include(p => p.Project)
                .FirstOrDefaultAsync(i => i.Project.ProjId == project.ProjId && i.LaborDescript == "Manpower");

            var laborFacilitator = await _dataContext.Labor
                .Include(p => p.Project)
                .FirstOrDefaultAsync(i => i.Project.ProjId == project.ProjId && i.LaborDescript == "Project Manager - Electrical Engr.");

            // Update laborManpower quantity based on kW capacity, if there's a change
            if (project.kWCapacity != updateProject.kWCapacity && laborManpower != null && laborFacilitator != null)
            {
                if (updateProject.kWCapacity <= 5)
                {
                    laborManpower.LaborQuantity = 5;
                    laborManpower.LaborNumUnit = 7;
                    laborFacilitator.LaborNumUnit = 7;
                }
                else if (updateProject.kWCapacity > 5 && updateProject.kWCapacity <= 10)
                {
                    laborManpower.LaborQuantity = 7;
                    laborManpower.LaborNumUnit = 15;
                    laborFacilitator.LaborNumUnit = 15;
                }
                else if (updateProject.kWCapacity > 10 && updateProject.kWCapacity <= 15)
                {
                    laborManpower.LaborQuantity = 10;
                    laborManpower.LaborNumUnit = 25;
                    laborFacilitator.LaborNumUnit = 25;
                }
                else
                {
                    laborManpower.LaborQuantity = 12;
                    laborManpower.LaborNumUnit = 35;
                    laborFacilitator.LaborNumUnit = 35;
                }

                laborManpower.UpdatedAt = DateTimeOffset.UtcNow;
                laborFacilitator.UpdatedAt = DateTimeOffset.UtcNow;

                _dataContext.Labor.Update(laborManpower);
                _dataContext.Labor.Update(laborFacilitator);
            }

            // Update project properties
            project.ProjName = updateProject.ProjName;
            project.ProjDescript = updateProject.ProjDescript;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            project.Discount = updateProject.Discount;
            project.VatRate = updateProject.VatRate / 100;
            project.kWCapacity = updateProject.kWCapacity;
            project.SystemType = updateProject.SystemType;

            // Update client properties
            client.UserName = updateProject.clientFName + "_" + updateProject.clientLName;
            client.Client.ClientContactNum = updateProject.clientContactNum;
            client.Client.ClientAddress = updateProject.clientAddress;
            client.Client.IsMale = updateProject.isMale;
            client.UpdatedAt = DateTimeOffset.UtcNow;


            // Update the project entity in the context
            _dataContext.Project.Update(project);


            // Update the client entity
            var updateResult = await _userManager.UpdateAsync(client);
            if (!updateResult.Succeeded) // Check if the update was successful
            {
                return (false, "Failed to update client: " + string.Join(", ", updateResult.Errors.Select(e => e.Description)));
            }

            // Save changes
            await _dataContext.SaveChangesAsync(); // Ensure changes are saved to the database

            return (true, "Client Info successfully updated!");
        }


        public async Task<bool> UpdatePersonnelWorkEnded(string projId, string reasonEnded)
        {
            // Find the ProjectWorkLog entries by the foreign key
            var workLogs = await _dataContext.ProjectWorkLog
                .Where(w => w.Project.ProjId == projId)
                .ToListAsync();

            if (workLogs == null || !workLogs.Any())
                return false; // Return false if no work logs were found

            // Update the WorkEnded property for each entry in the list
            foreach (var log in workLogs)
            {
                log.WorkEnded = DateTimeOffset.Now;
                log.WorkEndedReason = reasonEnded;
            }

            // Save changes to the database
            return await Save();
        }

        public async Task<bool> UpdatePersonnelWorkStarted(string projId)
        {
            // Find the ProjectWorkLog entries by the foreign key
            var workLogs = await _dataContext.ProjectWorkLog
                .Where(w => w.Project.ProjId == projId)
                .ToListAsync();

            if (workLogs == null || !workLogs.Any())
                return false; // Return false if no work logs were found

            // Update the WorkStarted property for each entry in the list
            foreach (var log in workLogs)
            {
                log.WorkStarted = DateTimeOffset.Now;
            }

            // Save changes to the database
            return await Save();
        }

        public async Task<(bool, string)> UpdateProfit(UpdateProfitRate updateProfit)
        {
            var user = await _userManager.FindByEmailAsync(updateProfit.userEmail);
            if (user == null)
                return (false, "Invalid User!");

            // Retrieve the project entity
            var project = await _dataContext.Project
                .FindAsync(updateProfit.projId);

            if (project == null)
                return (false, "Project not found!");

            if (updateProfit.profitRate < 1 || updateProfit.profitRate > 30)
                return (false, "Invalid profit rate!");

            project.ProfitRate = updateProfit.profitRate / 100;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            await _dataContext.SaveChangesAsync();

            return (true, "Profit successfully change.");
        }

        public async Task<(bool, string)> UpdateProjectToOnWork(UpdateProjectStatusDTO updateProjectToOnWork)
        {
            var project = await _dataContext.Project
                .FindAsync(updateProjectToOnWork.projId);

            if (project == null)
                return (false, "Invalid Project");

            project.Status = "OnWork";
            project.UpdatedAt = DateTimeOffset.UtcNow;

            var tasks = await _dataContext.GanttData
                .Where(p => p.ProjId == project.ProjId)
                .Include(t => t.TaskProofs)
                .OrderBy(s => s.PlannedStartDate)
                .ToListAsync();

            // Find tasks that are referenced as ParentId (tasks with subtasks)
            var taskIdsWithSubtasks = tasks
                .Where(t => tasks.Any(sub => sub.ParentId == t.TaskId))
                .Select(t => t.TaskId)
                .ToHashSet();

            if (tasks == null)
                return (false, "Empty Tasks");

            int? CalculateWorkDays(DateTime? startDate, DateTime? endDate)
            {
                if (!startDate.HasValue || !endDate.HasValue)
                    return null;

                DateTime start = startDate.Value;
                DateTime end = endDate.Value;
                int workDays = 0;

                while (start <= end)
                {
                    if (start.DayOfWeek != DayOfWeek.Saturday && start.DayOfWeek != DayOfWeek.Sunday)
                    {
                        workDays++;
                    }
                    start = start.AddDays(1);
                }

                return workDays;
            }

            foreach (var task in tasks)
            {
                // Skip tasks that have subtasks
                if (taskIdsWithSubtasks.Contains(task.TaskId) || !task.PlannedStartDate.HasValue)
                    continue;

                int? taskWorkDays = CalculateWorkDays(task.PlannedStartDate, task.PlannedEndDate);

                for (int i = 0; i < taskWorkDays; i++)
                {
                    var taskToDo = new TaskProof
                    {
                        EstimationStart = task.PlannedStartDate.Value.AddDays(i),
                        TaskProgress = (int)((double)(i + 1) / taskWorkDays.Value * 100),
                        Task = task
                    };

                    await _dataContext.TaskProof.AddAsync(taskToDo);
                }
            }

            _dataContext.Project.Update(project);
            await _dataContext.SaveChangesAsync();

            return (true, "Project set to OnWork now. You can't edit the tasks!");
        }

        public async Task<(bool, string)> UpdateProjectToOnProcess(UpdateProjectStatusDTO updateProjectToOnProcess)
        {
            // Retrieve the project entity
            var project = await _dataContext.Project
                .FindAsync(updateProjectToOnProcess.projId);

            if (project == null)
                return (false, "Invalid Project");

            var hasFacilitators = await _dataContext.ProjectWorkLog
                .Include(f => f.Facilitator)
                .Where(p => p.Project == project)
                .AnyAsync();

            if (!hasFacilitators)
                return (false, "Cannot seal the quotation. No Facilitator assigned yet!");

            var installers = await _dataContext.ProjectWorkLog
                .Include(i => i.Installer)
                .Where(p => p.Project == project && p.Installer != null)
                .CountAsync();

            if (installers == 0)
                return (false, "Cannot seal the quotation. Insufficient installers!");

            var labor = await _dataContext.Labor
              .Include(p => p.Project)
              .FirstOrDefaultAsync(i => i.Project.ProjId == project.ProjId && i.LaborDescript == "Manpower");

            if (labor == null)
                return (false, "Invalid labor quote");

            // Validate number of installers based on project capacity
            string validationMessage = ValidateInstallersByCapacity(project.kWCapacity, installers);
            if (validationMessage != null)
                return (false, validationMessage);

            var amount = await GetTotalProjectExpense(projId: project.ProjId);
            if (amount < 1)
                return (false, "No Quotation Cost Yet!");

            var material = await _dataContext.Supply
                .AnyAsync(i => i.Project == project && i.Material != null);
            if (!material)
                return (false, "No material supply!");

            var equipment = await _dataContext.Supply
                .AnyAsync(i => i.Project == project && i.Equipment != null);
            if (!equipment)
                return (false, "No equipment supply!");


            project.Status = "OnProcess";
            project.UpdatedAt = DateTimeOffset.UtcNow;

            _dataContext.Project.Update(project);
            await _dataContext.SaveChangesAsync();

            return (true, "Project set to OnProcess now. You can't edit the quotation!");
        }

        private async Task<decimal> GetTotalProjectExpense(string projId)
        {
            if (string.IsNullOrEmpty(projId))
                return 0;

            // Fetch material supply for the project
            var materialSupply = await _dataContext.Supply
                .Include(i => i.Material)
                .Where(p => p.Project.ProjId == projId)
                .ToListAsync();

            // Calculate total unit cost
            var totalUnitCost = materialSupply
                .Where(m => m.Material != null)
                .Sum(m => (m.MTLQuantity ?? 0) * m.Material.MTLPrice);

            // Calculate profit and material total
            var profitRate = materialSupply.Select(p => p.Project.ProfitRate).FirstOrDefault();
            var overallMaterialTotal = totalUnitCost * (1 + profitRate);

            // Calculate overall labor cost
            var overallLaborTotal = await _dataContext.Labor
                .Where(p => p.Project.ProjId == projId)
                .SumAsync(o => o.LaborCost) * (1 + profitRate);

            // Fetch discount and VAT rates
            var project = await _dataContext.Project
                .Where(p => p.ProjId == projId)
                .Select(p => new { Discount = p.Discount ?? 0, VatRate = p.VatRate ?? 0 })
                .FirstOrDefaultAsync();

            // Calculate final total with discount and VAT
            var subtotal = overallMaterialTotal + overallLaborTotal - project.Discount;
            var finalTotal = subtotal * (1 + project.VatRate);

            return (finalTotal);
        }


        // Helper method to validate the number of installers based on project capacity
        private string ValidateInstallersByCapacity(decimal kWCapacity, int installerCount)
        {
            if (kWCapacity <= 5 && installerCount != 5)
            {
                return "Cannot seal the quotation. Insufficient installers!";
            }
            if (kWCapacity > 5 && kWCapacity <= 10 && installerCount != 7)
            {
                return "Cannot seal the quotation. Insufficient installers!";
            }
            if (kWCapacity > 10 && kWCapacity <= 15 && installerCount != 10)
            {
                return "Cannot seal the quotation. Insufficient installers!";
            }

            return null; // Return null if valid
        }

        public async Task<bool> IsProjectOnProcess(string projId)
        {
            var project = await _dataContext.Project
                .FirstOrDefaultAsync(i => i.ProjId == projId && i.Status == "OnProcess");

            return project != null; // Returns true if project exists, false otherwise
        }

        public async Task<bool> IsProjectOnWork(string projId)
        {
            var project = await _dataContext.Project
                .FirstOrDefaultAsync(i => i.ProjId == projId && i.Status == "OnOwrk");

            return project != null; // Returns true if project exists, false otherwise
        }

        public async Task<bool> IsProjectOnFinished(string projId)
        {
            var project = await _dataContext.Project
                .FirstOrDefaultAsync(i => i.ProjId == projId && i.Status == "Finished");

            return project != null; // Returns true if project exists, false otherwise
        }

        public async Task<ICollection<ClientProjectInfoDTO>> GetClientsProjectInfos()
        {
            // Step 1: Get the projects with their client information
            var projectData = await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(i => i.Client.EmailConfirmed == true)
                .Select(d => new
                {
                    d.ProjId,
                    d.ProjName,
                    d.ProjDescript,
                    Discount = d.Discount ?? 0,
                    VatRate = (d.VatRate ?? 0) * 100,
                    clientId = d.Client.Id,
                    clientFName = d.Client.FirstName,
                    clientLName = d.Client.LastName,
                    clientContactNum = d.Client.Client.ClientContactNum,
                    clientAddress = d.Client.Client.ClientAddress,
                    kWCapacity = d.kWCapacity,
                    Sex = d.Client.Client.IsMale ? "Male" : "Female",
                    SystemType = d.SystemType,
                    isMale = d.Client.Client.IsMale,
                    status = d.Status
                })
                .OrderByDescending(s => s.status)
                .ToListAsync(); // Fetch all projects first

            // Step 2: Calculate the average progress and payment progress for each project
            var clientProjectInfos = new List<ClientProjectInfoDTO>();

            foreach (var project in projectData)
            {
                // Fetch tasks for the current project
                var tasksProof = await _dataContext.TaskProof
                    .Include(t => t.Task)
                    .Where(i => i.Task.ProjId == project.ProjId)
                    .ToListAsync();

                var averageProgress = (decimal)await _dataContext.GanttData
                    .Where(i => i.ProjId == project.ProjId && i.ParentId == null)
                    .AverageAsync(t => t.Progress);

                // Step 3: Calculate payment progress
                var paymentReferences = await _dataContext.Payment
                    .Where(p => p.Project.ProjId == project.ProjId) // Ensure you use the correct navigation property
                    .ToListAsync();

                int paid = 0;

                foreach (var reference in paymentReferences)
                {
                    var options = new RestClientOptions($"{_config["Payment:API"]}/{reference.Id}");
                    var client = new RestClient(options);
                    var request = new RestRequest("");

                    request.AddHeader("accept", "application/json");
                    request.AddHeader("authorization", $"Basic {_config["Payment:Key"]}");

                    var response = await client.GetAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = JsonDocument.Parse(response.Content);
                        var data = responseData.RootElement.GetProperty("data");
                        var attributes = data.GetProperty("attributes");

                        string currentStatus = attributes.GetProperty("status").GetString();

                        if (currentStatus == "paid" || reference.IsCashPayed)
                            paid++;
                    }
                }

                decimal paymentProgress = paymentReferences.Any() ? (decimal)paid / paymentReferences.Count * 100 : 0;

                var facilitator = await _dataContext.ProjectWorkLog
                  .Include(i => i.Facilitator)
                  .FirstOrDefaultAsync(w => w.Project.ProjId == project.ProjId && w.Facilitator != null);

                var installers = await _dataContext.ProjectWorkLog
                    .Include(i => i.Installer)
                    .Where(w => w.Project.ProjId == project.ProjId && w.Installer != null)
                    .OrderBy(p => p.Installer.Position)
                    .ToListAsync();

                List<InstallerInfo> installerList = new List<InstallerInfo>();

                foreach (var installer in installers)
                {
                    installerList.Add(new InstallerInfo
                    {
                        Name = installer.Installer.Name, // Adjust based on your model properties
                        Position = installer.Installer.Position // Adjust based on your model properties
                    });
                }

                // Add to the result list
                clientProjectInfos.Add(new ClientProjectInfoDTO
                {
                    ProjId = project.ProjId,
                    ProjName = project.ProjName,
                    ProjDescript = project.ProjDescript,
                    Discount = project.Discount,
                    VatRate = project.VatRate,
                    clientId = project.clientId,
                    clientFName = project.clientFName,
                    clientLName = project.clientLName,
                    clientContactNum = project.clientContactNum,
                    clientAddress = project.clientAddress,
                    kWCapacity = project.kWCapacity,
                    Sex = project.Sex,
                    SystemType = project.SystemType,
                    isMale = project.isMale,
                    ProjectProgress = averageProgress, // Include average progress
                    PaymentProgress = paymentProgress, // Include payment progress
                    Status = project.status,
                    Installers = installerList,
                    FacilitatorEmail = facilitator?.Facilitator?.Email,
                    FacilitatorName = $"{facilitator?.Facilitator?.FirstName} {facilitator?.Facilitator?.LastName}"
                });
            }

            return clientProjectInfos; // Return the final list
        }
    }
}

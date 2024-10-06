

using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ProjectLibrary.DTO.Project;
using ProjectLibrary.DTO.Quote;
using ProjectLibrary.Services.Interfaces;

namespace ProjectLibrary.Services.Repositories
{
    public class ProjectRepository(UserManager<AppUsers> _userManager, DataContext _dataContext) : IProject
    {
        public async Task<string> AddNewClientProject(ProjectDto projectDto)
        {
            if (projectDto == null)
            {
                throw new ArgumentNullException(nameof(projectDto));
            }

            // Check if the client exists
            var isClientExist = await _userManager.FindByIdAsync(projectDto.ClientId);
            if (isClientExist == null)
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
                Client = isClientExist
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
                .Where(p => p.Client.Id == clientId)
                .Select(p => new GetProjects
                {
                    ProjId = p.ProjId,
                    ProjName = p.ProjName,
                    ProjDescript = p.ProjDescript,
                    Status = p.Status,
                    CreatedAt = p.CreatedAt.ToString("MMM dd, yyyy"),
                    UpdatedAt = p.UpdatedAt.ToString("MMM dd, yyyy"),
                    ClientId = p.Client.Id,
                    ClientName = p.Client.NormalizedUserName,
                    ClientAddress = p.Client.Client.ClientAddress
                })
                .ToListAsync();
        }

        public async Task<ClientProjectInfoDTO> GetClientProjectInfo(string projId)
        {
            var projectData = await _dataContext.Project
                .Include(p => p.Client)
                .Include(p => p.Client.Client)
                .Where(i => i.ProjId == projId)
                .Select(d => new
                {
                    d.ProjId,
                    d.ProjName,
                    d.ProjDescript,
                    DiscountRate = (d.DiscountRate ?? 0) * 100,
                    VatRate = (d.VatRate ?? 0) * 100,
                    clientId = d.Client.Id,
                    UserName = d.Client.UserName,
                    clientContactNum = d.Client.Client.ClientContactNum,
                    clientAddress = d.Client.Client.ClientAddress,
                    clientMonthlyElectricBill = d.Client.Client.ClientMonthlyElectricBill
                })
                .FirstOrDefaultAsync();

            var usernameParts = projectData?.UserName?.Split('_') ?? new string[0];

            return new ClientProjectInfoDTO
            {
                ProjId = projectData?.ProjId ?? "",
                ProjName = projectData?.ProjName,
                ProjDescript = projectData?.ProjDescript,
                DiscountRate = projectData?.DiscountRate ?? 0,
                VatRate = projectData?.VatRate ?? 0,
                clientId = projectData?.clientId ?? "",
                clientFName = usernameParts.Length > 0 ? usernameParts[0] : string.Empty,
                clientLName = usernameParts.Length > 1 ? usernameParts[1] : string.Empty,
                clientContactNum = projectData?.clientContactNum,
                clientAddress = projectData?.clientAddress,
                clientMonthlyElectricBill = projectData.clientMonthlyElectricBill
            };
        }

        public async Task<ICollection<GetProjects>> GetClientsProject()
        {
            var projects = await _dataContext.Project
                .Include(p => p.Client) // Include related Client data
                .Include(p => p.Client.Client)
                .OrderBy(p => p.CreatedAt) // Order by CreatedAt first
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
                var profitPercentage = 0.3m;
                var profit = totalUnitCostSum * profitPercentage;
                overallMaterialTotal = totalUnitCostSum + profit;

                overallLaborProjectTotal = await _dataContext.Labor
                    .Where(p => projId != null ? p.Project.ProjId == projId : p.Project.Client.Email == customerEmail)
                    .SumAsync(o => o.LaborCost) * 1.3m;

                projectInfo = await _dataContext.Project
                    .Include(c => c.Client)
                    .Include(c => c.Client.Client)
                    .Where(p => projId != null ? p.ProjId == projId : p.Client.Email == customerEmail)
                    .ToListAsync();

                total = overallMaterialTotal + overallLaborProjectTotal;
            }

            var result = projectInfo.Select(i =>
            {
                decimal discountRate = i.DiscountRate ?? 0;
                decimal vatRate = i.VatRate ?? 0;

                // Calculate total
                decimal total = overallMaterialTotal + overallLaborProjectTotal;

                // Calculate subtotal after discount
                decimal subtotalAfterDiscount = total - (discountRate * total);

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
                    Discount = (discountRate * total).ToString("#,##0.00"),
                    DiscountRate = (discountRate * 100).ToString("0.##") + "%",
                    SubTotalAfterDiscount = subtotalAfterDiscount.ToString("#,##0.00"),
                    VAT = vatAmount.ToString("#,##0.00"),
                    VatRate = (vatRate * 100).ToString("0.##") + "%",
                    Total = finalTotal.ToString("#,##0.00"),
                    TotalLaborCost = totalLaborCost,
                    TotalMaterialCost = totalMaterialCost
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
                customerName = i.Client.UserName.Replace("_", " "),
                customerAddress = i.Client.Client.ClientAddress,
                projectId = i.ProjId,
                projectDescription = i.ProjDescript,
                projectDateCreation = i.CreatedAt.ToString("MMMM dd, yyyy"),
                projectDateValidity = i.CreatedAt.AddDays(7).ToString("MMMM dd, yyyy")

            }).FirstOrDefault();

            return result;
        }

        public async Task<ICollection<ProjectQuotationSupply>> ProjectQuotationSupply(string? projId, string? customerEmail)
        {

            List<Supply> quotationData = new List<Supply>();

            if (!string.IsNullOrEmpty(projId) || !string.IsNullOrEmpty(customerEmail))
            {

                quotationData = await _dataContext.Supply
                   .Include(m => m.Material)
                   .Include(m => m.Equipment)
                   .Include(m => m.Project)
                   .Where(p => projId != null ? p.Project.ProjId == projId : p.Project.Client.Email == customerEmail)
                   .OrderBy(c => c.Material.MTLCategory)
                   .ToListAsync();
            }

            // If no data is found, return an empty list
            if (quotationData == null || !quotationData.Any())
            {
                return new List<ProjectQuotationSupply>();  // Return an empty collection
            }

            var result = quotationData.Select(supply => new ProjectQuotationSupply
            {
                description = supply.Material?.MTLDescript ?? "No Description",  // Provide a default description if null
                lineTotal = (((decimal)(supply.MTLQuantity) * (supply.Material?.MTLPrice ?? 0)) * 1.3m).ToString("#,##0.00") // Ensure the multiplication is performed correctly
            }).ToList();

            return result;
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

        public async Task<(bool, string)> UpdateClientProject(ClientProjectInfoDTO updateProject)
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

            // Update project properties
            project.ProjName = updateProject.ProjName;
            project.ProjDescript = updateProject.ProjDescript;
            project.UpdatedAt = DateTimeOffset.UtcNow;
            project.DiscountRate = updateProject.DiscountRate / 100;
            project.VatRate = updateProject.VatRate / 100;

            // Update client properties
            client.UserName = updateProject.clientFName + "_" + updateProject.clientLName;
            client.Client.ClientContactNum = updateProject.clientContactNum;
            client.Client.ClientAddress = updateProject.clientAddress;
            client.Client.ClientMonthlyElectricBill = updateProject.clientMonthlyElectricBill;
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
    }
}

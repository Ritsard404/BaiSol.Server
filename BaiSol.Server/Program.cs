
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using AuthLibrary.Services.Repositories;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using BaseLibrary.Services.Repositories;
using ClientLibrary.Services.Interfaces;
using ClientLibrary.Services.Repositories;
using DataLibrary.Data;
using DataLibrary.Models;
using FacilitatorLibrary.Services.Interfaces;
using FacilitatorLibrary.Services.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProjectLibrary.Services.Interfaces;
using ProjectLibrary.Services.Repositories;
using Swashbuckle.AspNetCore.Filters;
using System.Text;

namespace BaiSol.Server
{
    public class Program
    {

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Connect Database
            builder.Services.AddDbContext<DataContext>(options =>
            {
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly("BaiSol.Server")); // Specify the migrations assembly here
            });

            // Add Scope of Interface and Repository
            builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
            builder.Services.AddScoped<IEmailRepository, EmailRepository>();
            builder.Services.AddScoped<IUserAccount, UserAccountRepository>();
            builder.Services.AddScoped<IAuthAccount, AuthAccountRepository>();
            builder.Services.AddScoped<IPersonnel, PersonnelRepository>();
            builder.Services.AddScoped<IMaterial, MaterialRepository>();
            builder.Services.AddScoped<IQuote, QuoteRepository>();
            builder.Services.AddScoped<IProject, ProjectRepository>();
            builder.Services.AddScoped<IEquipment, EquipmentRepository>();
            builder.Services.AddScoped<IRequisition, RequisitionRepository>();

            // Facilitator
            builder.Services.AddScoped<IRequestSupply, RequestSupplyRepository>();
            builder.Services.AddScoped<IAssignedSupply, AssignedSupplyRepository>();
            builder.Services.AddScoped<IHistoryRepository, HistoryRepository>();

            // Client
            builder.Services.AddScoped<IClientProject, ClientProjectRepository>();

            // Gantt
            builder.Services.AddScoped<IGanttRepository, GanttRepository>();

            // Logs
            builder.Services.AddScoped<IUserLogs, UserLogsRepository>();

            // Payment
            builder.Services.AddScoped<IPayment, PaymentRepository>();

            // Report
            builder.Services.AddScoped<IReportRepository, ReportRepository>();


            // Add Email Config
            var emailConfig = builder.Configuration
                .GetSection("EmailConfiguration")
                .Get<EmailModel>();
            builder.Services.AddSingleton(emailConfig);

            //Add Identity & JWT Authentication
            // Identity
            builder.Services.AddIdentity<AppUsers, IdentityRole>()
                .AddEntityFrameworkStores<DataContext>()
                .AddSignInManager()
                .AddRoles<IdentityRole>()
                .AddDefaultTokenProviders();

            // JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
                .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,

                    ValidIssuer = builder.Configuration["Jwt:Issuer"],
                    ValidAudience = builder.Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:key"]!))
                };
            });

            // Add Config for Required Email
            builder.Services.Configure<IdentityOptions>(
                options => options.SignIn.RequireConfirmedEmail = true
                );

            builder.Services.Configure<DataProtectionTokenProviderOptions>(options => options.TokenLifespan = TimeSpan.FromHours(5));

            // Add Authentication to Swagger UI
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "BaiSol API", Version = "v1" });
                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter a valid token",
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    BearerFormat = "JWT",
                    Scheme = "Bearer"
                });
                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type=ReferenceType.SecurityScheme,
                                Id="Bearer"
                            }
                        },
                        new string[]{}
                    }
                });
                options.OperationFilter<SecurityRequirementsOperationFilter>();

            });

            // Add CORS to services
            builder.Services.AddCors(options =>
            {
                var webUrl = builder.Configuration["FrontEnd_Url:Web_Url"];
                var mobileUrl = builder.Configuration["FrontEnd_Url:Mobile_Url"];
                var mobileWebUrl = builder.Configuration["FrontEnd_Url:Mobile_Web_Url"];

                //options.AddDefaultPolicy(policy =>
                //{
                //    policy.WithOrigins(webUrl, mobileUrl, mobileWebUrl) // Allow both URLs
                //          .AllowAnyMethod()
                //          .AllowAnyHeader();
                //});

                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin() // Allow any URL
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Seed Default Admin User
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var userManager = services.GetRequiredService<UserManager<AppUsers>>();
                    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                    SeedAdminUser(userManager, roleManager, builder.Configuration).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred while seeding the admin user: {ex.Message}");
                }
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles(); // Enable serving static files

            // Serve files from the "Uploads" directory
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(@"C:\Users\Angelie Gecole\Desktop\BAISOL_Capstone\Images", "Uploads")),
                RequestPath = "/uploads"
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }

        private static async Task SeedAdminUser(UserManager<AppUsers> userManager, RoleManager<IdentityRole> roleManager, IConfiguration configuration)
        {
            string adminEmail = configuration["OwnerEmail"] ?? "admin@domain.com";
            string adminPassword = "Admin@1234"; // You can set a stronger password or fetch from configuration

            // Check if the role exists, create if not
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Facilitator));
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Client));
            }

            // Check if any users exist
            if (!userManager.Users.Any())
            {
                var adminUser = new AppUsers
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Default admin user created successfully.");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating admin user: {error.Description}");
                    }
                }
            }
        }
    }
}

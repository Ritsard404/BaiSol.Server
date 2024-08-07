
using AuthLibrary.Models;
using AuthLibrary.Services.Interfaces;
using AuthLibrary.Services.Repositories;
using BaiSol.Server.Models.Email;
using BaseLibrary.Services.Interfaces;
using BaseLibrary.Services.Repositories;
using DataLibrary.Data;
using DataLibrary.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

            // JWT
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
                options.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth API", Version = "v1" });
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
                var frontEndUrl = builder.Configuration.GetValue<string>("FrontEnd_Url");
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins(frontEndUrl)
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            var app = builder.Build();

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
    }
}

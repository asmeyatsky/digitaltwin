using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DigitalTwin.Core.Data;
using DigitalTwin.API.Controllers;
using System.Text;

namespace DigitalTwin.API
{
    /// <summary>
    /// Digital Twin Emotional Companion API
    /// 
    /// Architectural Intent:
    /// - Provides RESTful API for emotional companion system
    /// - Integrates with ML services for emotion detection and conversation
    /// - Supports JWT authentication and authorization
    /// - Enables real-time conversational AI interactions
    /// 
    /// Key Features:
    /// 1. JWT-based authentication
    /// 2. Emotional conversation endpoints
    /// 3. Integration with external ML services
    /// 4. Comprehensive error handling
    /// 5. OpenAPI/Swagger documentation
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container
            builder.Services.AddControllers();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { 
                    Title = "Digital Twin Emotional Companion API", 
                    Version = "v1",
                    Description = "RESTful API for emotional companion AI system with real-time emotion detection and conversation capabilities"
                });
                
                // Include XML Comments
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Add JWT Authentication to Swagger
                c.AddSecurityDefinition("Bearer", new()
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new()
                {
                    {
                        new()
                        {
                            Reference = new() { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // Add Database Context
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
                ?? "Host=localhost;Database=digitaltwin;Username=postgres;Password=password";
            
            builder.Services.AddDbContext<DigitalTwinDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Add JWT Authentication
            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                ?? "ThisIsASecretKeyForDevelopmentUseOnly123456789012345678901234567890";
            
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "DigitalTwinAPI",
                        ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "DigitalTwinClients",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            builder.Services.AddAuthorization();

            // Add HttpClient for external service calls
            builder.Services.AddHttpClient();
            
            // Add JWT Authentication Service
            builder.Services.AddSingleton<API.Services.JwtAuthenticationService>();

            // Add CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Digital Twin Emotional Companion API v1");
                    c.RoutePrefix = string.Empty; // Serve Swagger at root
                });
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            // Add health check endpoint
            app.MapGet("/health", () => new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Digital Twin Emotional Companion API",
                version = "1.0.0"
            });

            // Add API info endpoint
            app.MapGet("/", () => new
            {
                name = "Digital Twin Emotional Companion API",
                version = "1.0.0",
                description = "RESTful API for emotional companion AI system",
                endpoints = new
                {
                    swagger = "/swagger",
                    health = "/health",
                    conversation = "/api/conversation"
                }
            });

            app.Run();
        }
    }
}
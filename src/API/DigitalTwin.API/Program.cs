using Microsoft.AspNetCore.Authentication.JwtBearer;
using Prometheus;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Security;
using DigitalTwin.Core.Interfaces;
using DigitalTwin.Core.Services;
using System.Text;

namespace DigitalTwin.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new()
                {
                    Title = "Digital Twin Emotional Companion API",
                    Version = "v1",
                    Description = "RESTful API for emotional companion AI system with real-time emotion detection and conversation capabilities"
                });

                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

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

            // Database — use standardized env var with fallback
            var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
                ?? builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                if (builder.Environment.IsDevelopment())
                    connectionString = "Host=localhost;Database=digitaltwin;Username=postgres;Password=password";
                else
                    throw new InvalidOperationException("ConnectionStrings__DefaultConnection must be set in production");
            }

            builder.Services.AddDbContext<DigitalTwinDbContext>(options =>
                options.UseNpgsql(connectionString));

            // JWT Authentication — standardized env vars
            var jwtKey = Environment.GetEnvironmentVariable("JwtConfiguration__SecretKey")
                ?? builder.Configuration["JwtConfiguration:SecretKey"];
            if (string.IsNullOrEmpty(jwtKey))
            {
                if (builder.Environment.IsDevelopment())
                    jwtKey = "ThisIsASecretKeyForDevelopmentUseOnly123456789012345678901234567890";
                else
                    throw new InvalidOperationException("JwtConfiguration__SecretKey must be set in production");
            }
            var jwtIssuer = Environment.GetEnvironmentVariable("JwtConfiguration__Issuer")
                ?? builder.Configuration["JwtConfiguration:Issuer"]
                ?? "DigitalTwin";
            var jwtAudience = Environment.GetEnvironmentVariable("JwtConfiguration__Audience")
                ?? builder.Configuration["JwtConfiguration:Audience"]
                ?? "DigitalTwin";

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
                    };
                });

            builder.Services.AddAuthorization();

            // Distributed cache — Redis in production, in-memory for development
            var redisConnection = Environment.GetEnvironmentVariable("Redis__ConnectionString");
            if (!string.IsNullOrEmpty(redisConnection))
            {
                builder.Services.AddStackExchangeRedisCache(options =>
                {
                    options.Configuration = redisConnection;
                    options.InstanceName = "digitaltwin:";
                });
            }
            else
            {
                builder.Services.AddDistributedMemoryCache();
            }

            // HttpClient with named clients and timeouts
            builder.Services.AddHttpClient("DeepFace", client =>
            {
                var baseUrl = Environment.GetEnvironmentVariable("Services__DeepFace__BaseUrl") ?? "http://localhost:8001";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("LLM", client =>
            {
                var baseUrl = Environment.GetEnvironmentVariable("Services__LLM__BaseUrl") ?? "http://localhost:8004";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
            builder.Services.AddHttpClient("Avatar", client =>
            {
                var baseUrl = Environment.GetEnvironmentVariable("Services__Avatar__BaseUrl") ?? "http://localhost:8002";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            builder.Services.AddHttpClient("Voice", client =>
            {
                var baseUrl = Environment.GetEnvironmentVariable("Services__Voice__BaseUrl") ?? "http://localhost:8003";
                client.BaseAddress = new Uri(baseUrl);
                client.Timeout = TimeSpan.FromSeconds(60);
            });
            builder.Services.AddHttpClient();

            // Core services
            builder.Services.AddSingleton<API.Services.JwtAuthenticationService>();
            builder.Services.AddScoped<PasswordHasher>();
            builder.Services.AddScoped<AuthenticationService>();
            builder.Services.AddScoped<RoleBasedAccessControlService>();
            builder.Services.AddScoped<SecurityEventLogger>();
            builder.Services.AddScoped<IAITwinService, AITwinService>();
            builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
            builder.Services.AddScoped<IPredictiveAnalyticsService, PredictiveAnalyticsService>();
            builder.Services.AddScoped<IAlertService, AlertService>();
            builder.Services.AddScoped<IReportService, ReportService>();
            builder.Services.AddScoped<IExportService, ExportService>();
            builder.Services.AddScoped<IWebhookService, WebhookService>();
            builder.Services.AddScoped<IConversationService, ConversationService>();
            builder.Services.AddScoped<IEmotionalStateService, EmotionalStateService>();

            // CORS — restrict in production
            var allowedOrigins = Environment.GetEnvironmentVariable("CORS__AllowedOrigins")?.Split(',')
                ?? new[] { "http://localhost:3000", "http://localhost:8081", "http://localhost:19006" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Default", policy =>
                {
                    policy.WithOrigins(allowedOrigins)
                          .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                          .WithHeaders("Content-Type", "Authorization", "X-Service-Key")
                          .AllowCredentials();
                });
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Digital Twin Emotional Companion API v1");
                    c.RoutePrefix = string.Empty;
                });
            }

            app.UseHttpsRedirection();
            app.UseCors("Default");
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseHttpMetrics();

            app.MapControllers();
            app.MapMetrics();

            app.MapGet("/health", () => new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "Digital Twin Emotional Companion API",
                version = "1.0.0"
            });

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

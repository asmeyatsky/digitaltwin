using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using DigitalTwin.API;
using DigitalTwin.API.Services;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;

namespace DigitalTwin.IntegrationTests.Fixtures;

/// <summary>
/// Custom WebApplicationFactory that replaces external dependencies with
/// test-safe in-memory alternatives: InMemory EF Core database, ephemeral
/// RSA key for JWT, and a stub HttpClient handler that returns canned
/// responses for external service calls (DeepFace, LLM, Avatar, Voice).
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>RSA key generated once per factory instance for signing test JWTs.</summary>
    private readonly RSA _rsa = RSA.Create(2048);

    /// <summary>Unique database name ensures test isolation when factories run in parallel.</summary>
    private readonly string _databaseName = $"DigitalTwinTest_{Guid.NewGuid():N}";

    public string TestUserId { get; } = "test-user-id-11111";
    public string TestUsername { get; } = "testuser";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            // ---- Replace PostgreSQL with InMemory database ----
            services.RemoveAll<DbContextOptions<DigitalTwinDbContext>>();
            services.RemoveAll<DigitalTwinDbContext>();

            // Remove any existing DbContext registrations (including the factory)
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<DigitalTwinDbContext>)
                         || d.ServiceType == typeof(DigitalTwinDbContext)
                         || (d.ServiceType == typeof(DbContextOptions))).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<DigitalTwinDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                // Suppress the warning about InMemory not supporting transactions
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
            });

            // ---- Replace JWT signing credentials with test RSA key ----
            services.RemoveAll<JwtSigningCredentials>();

            var testSigningKey = new RsaSecurityKey(_rsa);
            var testCredentials = new JwtSigningCredentials(
                new SigningCredentials(testSigningKey, SecurityAlgorithms.RsaSha256),
                "DigitalTwin",
                "DigitalTwin");
            services.AddSingleton(testCredentials);

            // ---- Replace named HttpClients with a stub handler ----
            // This prevents the test host from making real HTTP calls to
            // DeepFace, LLM, Avatar, and Voice microservices.
            services.AddHttpClient("DeepFace")
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());
            services.AddHttpClient("LLM")
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());
            services.AddHttpClient("Avatar")
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());
            services.AddHttpClient("Voice")
                .ConfigurePrimaryHttpMessageHandler(() => new StubHttpMessageHandler());

            // ---- Seed the database with basic test data ----
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DigitalTwinDbContext>();
            db.Database.EnsureCreated();
            SeedTestData(db);
        });
    }

    /// <summary>
    /// Creates an HttpClient that already carries a valid JWT Bearer token
    /// for the default test user.
    /// </summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = GenerateTestToken(TestUserId, TestUsername, new[] { "User" });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Creates an HttpClient with a JWT for a user with the given parameters.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(string userId, string username, string[] roles)
    {
        var client = CreateClient();
        var token = GenerateTestToken(userId, username, roles);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    /// <summary>
    /// Generates a valid JWT token signed with the test RSA key.
    /// </summary>
    public string GenerateTestToken(string userId, string username, IEnumerable<string> roles)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, username),
        };
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "DigitalTwin",
            Audience = "DigitalTwin",
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static void SeedTestData(DigitalTwinDbContext db)
    {
        // Seed a basic AI Twin profile for the test user
        if (!db.AITwinProfiles.Any())
        {
            db.AITwinProfiles.Add(new AITwinProfile
            {
                Id = Guid.NewGuid(),
                UserId = "test-user-id-11111",
                Name = "Test Companion",
                Description = "Integration test companion profile",
                ActivationLevel = 0.8,
                CreationDate = DateTime.UtcNow,
                LastInteraction = DateTime.UtcNow,
                PersonalityTraits = new AITwinPersonalityTraits
                {
                    Friendliness = 0.8,
                    Empathy = 0.7,
                    Curiosity = 0.6,
                    Humor = 0.9,
                    Patience = 0.3
                },
                BehavioralPatterns = new Dictionary<string, double>
                {
                    ["empathy"] = 0.9,
                    ["humor"] = 0.5
                },
                Preferences = new Dictionary<string, object>
                {
                    ["language"] = "en",
                    ["theme"] = "calm"
                }
            });
        }

        // Seed a subscription for the test user (free tier)
        if (!db.Subscriptions.Any())
        {
            db.Subscriptions.Add(new Subscription
            {
                Id = Guid.NewGuid(),
                UserId = "test-user-id-11111",
                Tier = "free",
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _rsa.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Stub HTTP message handler that returns canned 200 OK JSON responses
/// for any request, preventing real network calls to external services.
/// </summary>
internal class StubHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestUri = request.RequestUri?.PathAndQuery ?? "";

        string jsonBody;
        if (requestUri.Contains("detect-emotion"))
        {
            jsonBody = """{"emotion":"neutral","confidence":0.85}""";
        }
        else if (requestUri.Contains("generate-response"))
        {
            jsonBody = """{"response":"I hear you. How does that make you feel?","confidence":0.9}""";
        }
        else
        {
            jsonBody = """{"status":"ok"}""";
        }

        var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
        {
            Content = new StringContent(jsonBody, System.Text.Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}

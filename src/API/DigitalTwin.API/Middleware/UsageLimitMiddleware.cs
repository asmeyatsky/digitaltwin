using System.Security.Claims;
using System.Text.Json;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Middleware
{
    public class UsageLimitMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UsageLimitMiddleware> _logger;

        // Route prefix -> resource mapping
        private static readonly Dictionary<string, string> RouteResourceMap = new()
        {
            ["/api/conversation"] = "conversations",
            ["/api/voice/tts"] = "voice",
            ["/api/voice/clone"] = "voice",
            ["/api/voice/analyze-emotion"] = "voice_emotion",
            ["/api/emotion/analyze"] = "camera_emotion",
            ["/api/avatar/generate"] = "avatar_3d",
        };

        // Routes to skip
        private static readonly string[] SkipPrefixes = new[]
        {
            "/api/auth",
            "/api/subscription",
            "/api/webhook",
            "/health",
            "/swagger",
        };

        public UsageLimitMiddleware(RequestDelegate next, ILogger<UsageLimitMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower() ?? "";

            // Skip non-API and exempted routes
            if (!path.StartsWith("/api/") || SkipPrefixes.Any(p => path.StartsWith(p)))
            {
                await _next(context);
                return;
            }

            // Only check on mutating requests (POST/PUT)
            if (context.Request.Method != "POST" && context.Request.Method != "PUT")
            {
                await _next(context);
                return;
            }

            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await _next(context);
                return;
            }

            // Find matching resource
            string? resource = null;
            foreach (var (routePrefix, res) in RouteResourceMap)
            {
                if (path.StartsWith(routePrefix))
                {
                    resource = res;
                    break;
                }
            }

            if (resource == null)
            {
                await _next(context);
                return;
            }

            var usageService = context.RequestServices.GetRequiredService<IUsageLimitService>();
            var (allowed, remaining, limit) = await usageService.CheckLimitAsync(userId, resource);

            if (!allowed)
            {
                var resetAt = DateTime.UtcNow.Date.AddDays(1);
                context.Response.StatusCode = 429;
                context.Response.ContentType = "application/json";
                // AD-2 compliant envelope
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                {
                    success = false,
                    data = new { remaining = 0, limit, resetAt = resetAt.ToString("O") },
                    error = "RATE_LIMIT_EXCEEDED",
                    message = $"Usage limit exceeded for {resource}",
                    timestamp = DateTime.UtcNow
                }));
                return;
            }

            // Increment usage after successful processing
            await _next(context);

            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                await usageService.IncrementUsageAsync(userId, resource);
            }
        }
    }

    public static class UsageLimitMiddlewareExtensions
    {
        public static IApplicationBuilder UseUsageLimitMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UsageLimitMiddleware>();
        }
    }
}

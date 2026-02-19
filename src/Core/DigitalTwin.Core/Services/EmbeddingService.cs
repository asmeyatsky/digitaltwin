using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDistributedCache _cache;
        private readonly ILogger<EmbeddingService> _logger;

        private static readonly DistributedCacheEntryOptions CacheOptions = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        public EmbeddingService(
            IHttpClientFactory httpClientFactory,
            IDistributedCache cache,
            ILogger<EmbeddingService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            // Check cache first
            var cacheKey = $"embedding:{ComputeHash(text)}";
            var cached = await _cache.GetStringAsync(cacheKey);
            if (cached != null)
            {
                return JsonSerializer.Deserialize<float[]>(cached)!;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("LLM");
                var serviceKey = Environment.GetEnvironmentVariable("Services__ServiceKey") ?? "dev-service-key";
                client.DefaultRequestHeaders.Add("X-Service-Key", serviceKey);

                var response = await client.PostAsJsonAsync("/embedding", new { text });
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<EmbeddingResponse>();
                var embedding = result?.Embedding ?? throw new InvalidOperationException("No embedding returned from LLM service");

                // Cache result
                await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(embedding), CacheOptions);

                return embedding;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating embedding for text");
                throw;
            }
        }

        private static string ComputeHash(string input)
        {
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashBytes)[..16];
        }

        private class EmbeddingResponse
        {
            public float[] Embedding { get; set; } = Array.Empty<float>();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DigitalTwin.Core.DTOs;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BiometricController : ControllerBase
    {
        private readonly IBiometricService _biometricService;
        private readonly ILogger<BiometricController> _logger;

        public BiometricController(IBiometricService biometricService, ILogger<BiometricController> logger)
        {
            _biometricService = biometricService;
            _logger = logger;
        }

        private string GetUserId() =>
            User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;

        [HttpPost]
        public async Task<IActionResult> StoreReading([FromBody] BiometricReadingRequest request)
        {
            try
            {
                var reading = new BiometricReading
                {
                    UserId = GetUserId(),
                    Type = request.Type,
                    Value = request.Value,
                    Unit = request.Unit,
                    Source = request.Source,
                    Timestamp = request.Timestamp ?? DateTime.UtcNow
                };

                var result = await _biometricService.StoreReadingAsync(reading);
                return Ok(ApiResponse<BiometricReading>.Ok(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error storing biometric reading");
                return StatusCode(500, ApiResponse.Fail("Failed to store biometric reading"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReadings([FromQuery] string? type = null, [FromQuery] int limit = 50)
        {
            try
            {
                var readings = await _biometricService.GetReadingsAsync(GetUserId(), type, limit);
                return Ok(ApiResponse<List<BiometricReading>>.Ok(readings));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching biometric readings");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch biometric readings"));
            }
        }

        [HttpGet("latest/{type}")]
        public async Task<IActionResult> GetLatestReading(string type)
        {
            try
            {
                var reading = await _biometricService.GetLatestReadingAsync(GetUserId(), type);
                return Ok(ApiResponse<BiometricReading?>.Ok(reading));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching latest biometric reading");
                return StatusCode(500, ApiResponse.Fail("Failed to fetch latest biometric reading"));
            }
        }

        [HttpPost("sync")]
        public async Task<IActionResult> SyncReadings([FromBody] List<BiometricReadingRequest> readings)
        {
            try
            {
                var userId = GetUserId();
                var results = new List<BiometricReading>();

                foreach (var req in readings)
                {
                    var reading = new BiometricReading
                    {
                        UserId = userId,
                        Type = req.Type,
                        Value = req.Value,
                        Unit = req.Unit,
                        Source = req.Source,
                        Timestamp = req.Timestamp ?? DateTime.UtcNow
                    };

                    results.Add(await _biometricService.StoreReadingAsync(reading));
                }

                return Ok(ApiResponse<List<BiometricReading>>.Ok(results, $"Synced {results.Count} readings"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing biometric readings");
                return StatusCode(500, ApiResponse.Fail("Failed to sync biometric readings"));
            }
        }
    }

    public class BiometricReadingRequest
    {
        public string Type { get; set; } = string.Empty;
        public double Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Source { get; set; } = "manual";
        public DateTime? Timestamp { get; set; }
    }
}

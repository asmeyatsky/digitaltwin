using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;
using DigitalTwin.Core.Interfaces;

namespace DigitalTwin.Core.Services
{
    public class BiometricService : IBiometricService
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<BiometricService> _logger;

        public BiometricService(DigitalTwinDbContext context, ILogger<BiometricService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BiometricReading> StoreReadingAsync(BiometricReading reading)
        {
            _context.BiometricReadings.Add(reading);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Stored biometric reading {Type}={Value} for user {UserId}", reading.Type, reading.Value, reading.UserId);
            return reading;
        }

        public async Task<List<BiometricReading>> GetReadingsAsync(string userId, string? type = null, int limit = 50)
        {
            var query = _context.BiometricReadings
                .Where(r => r.UserId == userId);

            if (!string.IsNullOrEmpty(type))
                query = query.Where(r => r.Type == type);

            return await query
                .OrderByDescending(r => r.Timestamp)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<BiometricReading?> GetLatestReadingAsync(string userId, string type)
        {
            return await _context.BiometricReadings
                .Where(r => r.UserId == userId && r.Type == type)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();
        }

        public async Task<Emotion> InferEmotionFromBiometricsAsync(string userId)
        {
            var heartRate = await GetLatestReadingAsync(userId, "heart_rate");
            var hrv = await GetLatestReadingAsync(userId, "hrv");
            var sleepQuality = await GetLatestReadingAsync(userId, "sleep_quality");

            // High HR + low HRV → Anxious
            if (heartRate != null && hrv != null)
            {
                if (heartRate.Value > 100 && hrv.Value < 30)
                    return Emotion.Anxious;

                // Low HR + high HRV → Calm
                if (heartRate.Value < 70 && hrv.Value > 60)
                    return Emotion.Calm;

                // High HR + high HRV → Excited
                if (heartRate.Value > 90 && hrv.Value > 50)
                    return Emotion.Excited;
            }

            // Poor sleep → Sad
            if (sleepQuality != null && sleepQuality.Value < 40)
                return Emotion.Sad;

            return Emotion.Neutral;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DigitalTwin.Core.Entities;
using DigitalTwin.Core.Enums;

namespace DigitalTwin.Core.Interfaces
{
    public interface IBiometricService
    {
        Task<BiometricReading> StoreReadingAsync(BiometricReading reading);
        Task<List<BiometricReading>> GetReadingsAsync(string userId, string? type = null, int limit = 50);
        Task<BiometricReading?> GetLatestReadingAsync(string userId, string type);
        Task<Emotion> InferEmotionFromBiometricsAsync(string userId);
    }
}

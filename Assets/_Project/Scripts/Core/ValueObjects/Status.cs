using System;

namespace DigitalTwin.Core.ValueObjects
{
    /// <summary>
    /// Status Value Object
    /// 
    /// Architectural Intent:
    /// - Represents operational status with health indicators
    /// - Provides immutable status calculations and transitions
    /// - Encapsulates status validation and state management
    /// - Supports status aggregation and health scoring
    /// 
    /// Invariants:
    /// - Health score must be between 0 and 100
    /// - Status level cannot be null
    /// - Last updated timestamp cannot be in the future
    /// - Status transitions must follow business rules
    /// </summary>
    public readonly struct Status : IEquatable<Status>
    {
        public StatusLevel Level { get; }
        public HealthScore Health { get; }
        public string Message { get; }
        public DateTime LastUpdated { get; }
        public TimeSpan Duration { get; }

        public Status(StatusLevel level, HealthScore health, string message, DateTime lastUpdated, TimeSpan duration)
        {
            if (string.IsNullOrEmpty(message))
                throw new ArgumentNullException(nameof(message));
            if (lastUpdated > DateTime.UtcNow.AddMinutes(1)) // Allow 1 minute clock skew
                throw new ArgumentException("Last updated timestamp cannot be in the future", nameof(lastUpdated));
            if (duration < TimeSpan.Zero)
                throw new ArgumentException("Duration cannot be negative", nameof(duration));

            Level = level;
            Health = health;
            Message = message;
            LastUpdated = lastUpdated;
            Duration = duration;
        }

        public static Status Operational(string message = "System is operational") 
            => new Status(StatusLevel.Operational, HealthScore.Excellent, message, DateTime.UtcNow, TimeSpan.Zero);

        public static Status Warning(string message, HealthScore health) 
            => new Status(StatusLevel.Warning, health, message, DateTime.UtcNow, TimeSpan.Zero);

        public static Status Critical(string message, HealthScore health) 
            => new Status(StatusLevel.Critical, health, message, DateTime.UtcNow, TimeSpan.Zero);

        public static Status Offline(string message) 
            => new Status(StatusLevel.Offline, HealthScore.Poor, message, DateTime.UtcNow, TimeSpan.Zero);

        public bool IsHealthy => Level == StatusLevel.Operational && Health.Score >= 70;
        public bool HasIssues => Level == StatusLevel.Warning || Level == StatusLevel.Critical;
        public bool IsOffline => Level == StatusLevel.Offline;
        public bool RequiresAttention => Level == StatusLevel.Critical || Health.Score < 50;

        public Status WithMessage(string newMessage) 
            => new Status(Level, Health, newMessage, DateTime.UtcNow, Duration);

        public Status WithHealth(HealthScore newHealth) 
            => new Status(Level, newHealth, Message, DateTime.UtcNow, Duration);

        public Status WithLevel(StatusLevel newLevel) 
            => new Status(newLevel, Health, Message, DateTime.UtcNow, Duration);

        public Status UpdateDuration() 
            => new Status(Level, Health, Message, LastUpdated, DateTime.UtcNow - LastUpdated);

        public Status TransitionTo(StatusLevel newLevel, string message)
        {
            if (!IsValidTransition(Level, newLevel))
                throw new InvalidOperationException($"Cannot transition from {Level} to {newLevel}");

            var newHealth = CalculateHealthForLevel(newLevel, Health);
            return new Status(newLevel, newHealth, message, DateTime.UtcNow, TimeSpan.Zero);
        }

        public Status Degrade(string reason)
        {
            var newLevel = GetNextLowerLevel(Level);
            var newHealth = Health.Degrade();
            var message = string.IsNullOrEmpty(reason) ? $"Status degraded to {newLevel}" : reason;
            
            return new Status(newLevel, newHealth, message, DateTime.UtcNow, TimeSpan.Zero);
        }

        public Status Improve(string reason)
        {
            var newLevel = GetNextHigherLevel(Level);
            var newHealth = Health.Improve();
            var message = string.IsNullOrEmpty(reason) ? $"Status improved to {newLevel}" : reason;
            
            return new Status(newLevel, newHealth, message, DateTime.UtcNow, TimeSpan.Zero);
        }

        public Priority GetPriority()
        {
            return Level switch
            {
                StatusLevel.Critical => Priority.High,
                StatusLevel.Warning => Priority.Medium,
                StatusLevel.Operational => Priority.Low,
                StatusLevel.Offline => Priority.High,
                _ => Priority.Low
            };
        }

        public TimeSpan GetTimeSinceLastUpdate()
        {
            return DateTime.UtcNow - LastUpdated;
        }

        public bool IsStale(TimeSpan threshold)
        {
            return GetTimeSinceLastUpdate() > threshold;
        }

        private static bool IsValidTransition(StatusLevel from, StatusLevel to)
        {
            return (from, to) switch
            {
                (StatusLevel.Operational, StatusLevel.Warning) => true,
                (StatusLevel.Operational, StatusLevel.Critical) => true,
                (StatusLevel.Operational, StatusLevel.Offline) => true,
                (StatusLevel.Warning, StatusLevel.Operational) => true,
                (StatusLevel.Warning, StatusLevel.Critical) => true,
                (StatusLevel.Warning, StatusLevel.Offline) => true,
                (StatusLevel.Critical, StatusLevel.Warning) => true,
                (StatusLevel.Critical, StatusLevel.Offline) => true,
                (StatusLevel.Offline, StatusLevel.Operational) => true,
                (StatusLevel.Offline, StatusLevel.Warning) => true,
                (StatusLevel.Offline, StatusLevel.Critical) => true,
                _ => false
            };
        }

        private static HealthScore CalculateHealthForLevel(StatusLevel level, HealthScore currentHealth)
        {
            return level switch
            {
                StatusLevel.Operational => HealthScore.Excellent,
                StatusLevel.Warning => currentHealth.Score >= 70 ? HealthScore.Good : HealthScore.Fair,
                StatusLevel.Critical => currentHealth.Score >= 50 ? HealthScore.Poor : HealthScore.Critical,
                StatusLevel.Offline => HealthScore.Critical,
                _ => currentHealth
            };
        }

        private static StatusLevel GetNextLowerLevel(StatusLevel current)
        {
            return current switch
            {
                StatusLevel.Operational => StatusLevel.Warning,
                StatusLevel.Warning => StatusLevel.Critical,
                StatusLevel.Critical => StatusLevel.Offline,
                StatusLevel.Offline => StatusLevel.Offline,
                _ => StatusLevel.Offline
            };
        }

        private static StatusLevel GetNextHigherLevel(StatusLevel current)
        {
            return current switch
            {
                StatusLevel.Offline => StatusLevel.Critical,
                StatusLevel.Critical => StatusLevel.Warning,
                StatusLevel.Warning => StatusLevel.Operational,
                StatusLevel.Operational => StatusLevel.Operational,
                _ => StatusLevel.Operational
            };
        }

        public bool Equals(Status other) 
            => Level == other.Level && Health.Equals(other.Health) && Message == other.Message && LastUpdated == other.LastUpdated;

        public override bool Equals(object obj) 
            => obj is Status other && Equals(other);

        public override int GetHashCode() 
            => HashCode.Combine(Level, Health, Message, LastUpdated);

        public override string ToString() 
            => $"{Level}: {Message} (Health: {Health.Score}%)";
    }

    public enum StatusLevel
    {
        Operational,
        Warning,
        Critical,
        Offline
    }

    public enum Priority
    {
        Low,
        Medium,
        High,
        Critical
    }

    public readonly struct HealthScore : IEquatable<HealthScore>, IComparable<HealthScore>
    {
        public decimal Score { get; }
        public string Description { get; }

        public HealthScore(decimal score, string description)
        {
            if (score < 0 || score > 100)
                throw new ArgumentException("Health score must be between 0 and 100", nameof(score));
            
            Score = score;
            Description = description ?? throw new ArgumentNullException(nameof(description));
        }

        public static HealthScore Excellent => new HealthScore(100, "Excellent");
        public static HealthScore Good => new HealthScore(80, "Good");
        public static HealthScore Fair => new HealthScore(60, "Fair");
        public static HealthScore Poor => new HealthScore(40, "Poor");
        public static HealthScore Critical => new HealthScore(20, "Critical");

        public HealthRating Rating => Score switch
        {
            >= 90 => HealthRating.Excellent,
            >= 75 => HealthRating.Good,
            >= 60 => HealthRating.Fair,
            >= 40 => HealthRating.Poor,
            >= 20 => HealthRating.Critical,
            _ => HealthRating.Failed
        };

        public HealthScore Degrade(decimal amount = 10)
        {
            var newScore = Math.Max(0, Score - amount);
            var newDescription = GetDescriptionForScore(newScore);
            return new HealthScore(newScore, newDescription);
        }

        public HealthScore Improve(decimal amount = 10)
        {
            var newScore = Math.Min(100, Score + amount);
            var newDescription = GetDescriptionForScore(newScore);
            return new HealthScore(newScore, newDescription);
        }

        private static string GetDescriptionForScore(decimal score)
        {
            return score switch
            {
                >= 90 => "Excellent",
                >= 75 => "Good",
                >= 60 => "Fair",
                >= 40 => "Poor",
                >= 20 => "Critical",
                _ => "Failed"
            };
        }

        public int CompareTo(HealthScore other) 
            => Score.CompareTo(other.Score);

        public bool Equals(HealthScore other) 
            => Score == other.Score;

        public override bool Equals(object obj) 
            => obj is HealthScore other && Equals(other);

        public override int GetHashCode() 
            => Score.GetHashCode();

        public override string ToString() 
            => $"{Score}% ({Description})";
    }

    public enum HealthRating
    {
        Excellent,
        Good,
        Fair,
        Poor,
        Critical,
        Failed
    }
}
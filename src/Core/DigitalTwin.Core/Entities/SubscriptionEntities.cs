using System.ComponentModel.DataAnnotations;

namespace DigitalTwin.Core.Entities
{
    public class Subscription
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(50)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? StripeCustomerId { get; set; }

        [MaxLength(100)]
        public string? StripeSubscriptionId { get; set; }

        [MaxLength(100)]
        public string? StripePriceId { get; set; }

        [Required]
        [MaxLength(20)]
        public string Tier { get; set; } = "free";

        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "active";

        public DateTime? CurrentPeriodEnd { get; set; }

        public bool CancelAtPeriodEnd { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}

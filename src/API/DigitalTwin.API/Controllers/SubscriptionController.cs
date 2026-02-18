using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DigitalTwin.Core.Data;
using DigitalTwin.Core.Entities;
using Stripe;
using Stripe.Checkout;

namespace DigitalTwin.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionController : ControllerBase
    {
        private readonly DigitalTwinDbContext _context;
        private readonly ILogger<SubscriptionController> _logger;
        private readonly string _stripeWebhookSecret;

        private static readonly List<SubscriptionTierDto> Tiers = new()
        {
            new SubscriptionTierDto
            {
                Tier = "free",
                Name = "Free",
                Price = 0,
                Interval = "month",
                Features = new List<string>
                {
                    "5 conversations per day",
                    "Basic emotion detection",
                    "Text chat only",
                    "2D avatar"
                }
            },
            new SubscriptionTierDto
            {
                Tier = "plus",
                Name = "Plus",
                Price = 9.99m,
                Interval = "month",
                StripePriceId = Environment.GetEnvironmentVariable("Stripe__PlusPriceId") ?? "price_plus_monthly",
                Features = new List<string>
                {
                    "Unlimited conversations",
                    "Advanced emotion detection",
                    "Voice conversations",
                    "3D avatar generation",
                    "Voice cloning",
                    "Priority support"
                }
            },
            new SubscriptionTierDto
            {
                Tier = "premium",
                Name = "Premium",
                Price = 19.99m,
                Interval = "month",
                StripePriceId = Environment.GetEnvironmentVariable("Stripe__PremiumPriceId") ?? "price_premium_monthly",
                Features = new List<string>
                {
                    "Everything in Plus",
                    "Camera emotion detection",
                    "Custom personality tuning",
                    "Conversation export",
                    "Advanced analytics",
                    "Early access to features",
                    "Dedicated support"
                }
            }
        };

        public SubscriptionController(
            DigitalTwinDbContext context,
            ILogger<SubscriptionController> logger)
        {
            _context = context;
            _logger = logger;
            _stripeWebhookSecret = Environment.GetEnvironmentVariable("Stripe__WebhookSecret") ?? "";

            var stripeKey = Environment.GetEnvironmentVariable("Stripe__SecretKey");
            if (!string.IsNullOrEmpty(stripeKey))
            {
                StripeConfiguration.ApiKey = stripeKey;
            }
        }

        [HttpGet("tiers")]
        [AllowAnonymous]
        public IActionResult GetTiers()
        {
            var publicTiers = Tiers.Select(t => new
            {
                t.Tier,
                t.Name,
                t.Price,
                t.Interval,
                t.Features
            });

            return Ok(new { success = true, data = publicTiers });
        }

        [HttpGet("current")]
        [Authorize]
        public async Task<IActionResult> GetCurrentSubscription()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (subscription == null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            tier = "free",
                            status = "active",
                            cancelAtPeriodEnd = false
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = subscription.Id,
                        tier = subscription.Tier,
                        status = subscription.Status,
                        currentPeriodEnd = subscription.CurrentPeriodEnd,
                        cancelAtPeriodEnd = subscription.CancelAtPeriodEnd,
                        stripeCustomerId = subscription.StripeCustomerId
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current subscription");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("checkout")]
        [Authorize]
        public async Task<IActionResult> CreateCheckout([FromBody] CheckoutRequest request)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var tier = Tiers.FirstOrDefault(t => t.Tier == request.Tier);
                if (tier == null || string.IsNullOrEmpty(tier.StripePriceId))
                    return BadRequest(new { success = false, message = "Invalid subscription tier" });

                // Get or create Stripe customer
                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                string customerId;
                if (subscription?.StripeCustomerId != null)
                {
                    customerId = subscription.StripeCustomerId;
                }
                else
                {
                    var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? "";
                    var customerService = new CustomerService();
                    var customer = await customerService.CreateAsync(new CustomerCreateOptions
                    {
                        Email = userEmail,
                        Metadata = new Dictionary<string, string> { ["userId"] = userId }
                    });
                    customerId = customer.Id;

                    if (subscription == null)
                    {
                        subscription = new Core.Entities.Subscription
                        {
                            UserId = userId,
                            StripeCustomerId = customerId,
                            Tier = "free",
                            Status = "active"
                        };
                        _context.Subscriptions.Add(subscription);
                    }
                    else
                    {
                        subscription.StripeCustomerId = customerId;
                    }
                    await _context.SaveChangesAsync();
                }

                if (request.Platform == "web")
                {
                    // Create Stripe Checkout Session for web
                    var sessionService = new SessionService();
                    var session = await sessionService.CreateAsync(new SessionCreateOptions
                    {
                        Customer = customerId,
                        PaymentMethodTypes = new List<string> { "card" },
                        LineItems = new List<SessionLineItemOptions>
                        {
                            new()
                            {
                                Price = tier.StripePriceId,
                                Quantity = 1
                            }
                        },
                        Mode = "subscription",
                        SuccessUrl = Environment.GetEnvironmentVariable("Stripe__SuccessUrl") ?? "http://localhost:8081/settings/subscription?success=true",
                        CancelUrl = Environment.GetEnvironmentVariable("Stripe__CancelUrl") ?? "http://localhost:8081/settings/subscription?canceled=true",
                        Metadata = new Dictionary<string, string>
                        {
                            ["userId"] = userId,
                            ["tier"] = request.Tier
                        }
                    });

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            sessionId = session.Id,
                            clientSecret = "",
                            url = session.Url
                        }
                    });
                }
                else
                {
                    // Create PaymentIntent for mobile (Stripe Payment Sheet)
                    var ephemeralKeyService = new EphemeralKeyService();
                    var ephemeralKey = await ephemeralKeyService.CreateAsync(new EphemeralKeyCreateOptions
                    {
                        Customer = customerId
                    });

                    var subscriptionService = new SubscriptionService();
                    var stripeSubscription = await subscriptionService.CreateAsync(new SubscriptionCreateOptions
                    {
                        Customer = customerId,
                        Items = new List<SubscriptionItemOptions>
                        {
                            new() { Price = tier.StripePriceId }
                        },
                        PaymentBehavior = "default_incomplete",
                        PaymentSettings = new SubscriptionPaymentSettingsOptions
                        {
                            SaveDefaultPaymentMethod = "on_subscription"
                        },
                        Metadata = new Dictionary<string, string>
                        {
                            ["userId"] = userId,
                            ["tier"] = request.Tier
                        },
                        Expand = new List<string> { "latest_invoice.payment_intent" }
                    });

                    var invoice = stripeSubscription.LatestInvoice;
                    var paymentIntent = invoice?.PaymentIntent;

                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            sessionId = stripeSubscription.Id,
                            clientSecret = paymentIntent?.ClientSecret ?? "",
                            ephemeralKey = ephemeralKey.Secret,
                            customerId
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout session");
                return StatusCode(500, new { success = false, message = "Failed to create checkout session" });
            }
        }

        [HttpPost("cancel")]
        [Authorize]
        public async Task<IActionResult> CancelSubscription()
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var subscription = await _context.Subscriptions
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (subscription?.StripeSubscriptionId == null)
                    return BadRequest(new { success = false, message = "No active subscription" });

                var service = new SubscriptionService();
                await service.UpdateAsync(subscription.StripeSubscriptionId, new SubscriptionUpdateOptions
                {
                    CancelAtPeriodEnd = true
                });

                subscription.CancelAtPeriodEnd = true;
                subscription.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { success = true, data = (object?)null, message = "Subscription will cancel at period end" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling subscription");
                return StatusCode(500, new { success = false, message = "Failed to cancel subscription" });
            }
        }

        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _stripeWebhookSecret
                );

                switch (stripeEvent.Type)
                {
                    case EventTypes.CheckoutSessionCompleted:
                        await HandleCheckoutCompleted(stripeEvent);
                        break;
                    case EventTypes.CustomerSubscriptionUpdated:
                        await HandleSubscriptionUpdated(stripeEvent);
                        break;
                    case EventTypes.CustomerSubscriptionDeleted:
                        await HandleSubscriptionDeleted(stripeEvent);
                        break;
                    case EventTypes.InvoicePaymentFailed:
                        await HandlePaymentFailed(stripeEvent);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogWarning(ex, "Stripe webhook signature verification failed");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling Stripe webhook");
                return StatusCode(500);
            }
        }

        private async Task HandleCheckoutCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null) return;

            var userId = session.Metadata.GetValueOrDefault("userId");
            var tier = session.Metadata.GetValueOrDefault("tier");
            if (string.IsNullOrEmpty(userId)) return;

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (subscription == null)
            {
                subscription = new Core.Entities.Subscription
                {
                    UserId = userId,
                    StripeCustomerId = session.CustomerId,
                };
                _context.Subscriptions.Add(subscription);
            }

            subscription.StripeSubscriptionId = session.SubscriptionId;
            subscription.Tier = tier ?? "plus";
            subscription.Status = "active";
            subscription.CancelAtPeriodEnd = false;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task HandleSubscriptionUpdated(Event stripeEvent)
        {
            var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (stripeSubscription == null) return;

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription == null) return;

            subscription.Status = stripeSubscription.Status;
            subscription.CancelAtPeriodEnd = stripeSubscription.CancelAtPeriodEnd;
            subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd;
            subscription.UpdatedAt = DateTime.UtcNow;

            if (stripeSubscription.Items?.Data?.Count > 0)
            {
                subscription.StripePriceId = stripeSubscription.Items.Data[0].Price.Id;
            }

            await _context.SaveChangesAsync();
        }

        private async Task HandleSubscriptionDeleted(Event stripeEvent)
        {
            var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
            if (stripeSubscription == null) return;

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription == null) return;

            subscription.Status = "canceled";
            subscription.Tier = "free";
            subscription.CancelAtPeriodEnd = false;
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private async Task HandlePaymentFailed(Event stripeEvent)
        {
            var invoice = stripeEvent.Data.Object as Invoice;
            if (invoice == null) return;

            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeCustomerId == invoice.CustomerId);

            if (subscription == null) return;

            subscription.Status = "past_due";
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogWarning("Payment failed for user {UserId}, subscription {SubscriptionId}",
                subscription.UserId, subscription.StripeSubscriptionId);
        }
    }

    public class CheckoutRequest
    {
        public string Tier { get; set; } = string.Empty;
        public string Platform { get; set; } = "web";
    }

    public class SubscriptionTierDto
    {
        public string Tier { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Interval { get; set; } = "month";
        public string? StripePriceId { get; set; }
        public List<string> Features { get; set; } = new();
    }
}

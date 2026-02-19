using Prometheus;

namespace DigitalTwin.Core.Telemetry
{
    public static class MetricsRegistry
    {
        public static readonly Counter ConversationsTotal = Metrics.CreateCounter(
            "digitaltwin_conversations_total",
            "Total number of conversations started");

        public static readonly Counter EmotionDetectionsTotal = Metrics.CreateCounter(
            "digitaltwin_emotion_detections_total",
            "Total emotion detections",
            new CounterConfiguration { LabelNames = new[] { "emotion", "source" } });

        public static readonly Counter FusionOperationsTotal = Metrics.CreateCounter(
            "digitaltwin_fusion_operations_total",
            "Total emotion fusion operations");

        public static readonly Counter PluginExecutionsTotal = Metrics.CreateCounter(
            "digitaltwin_plugin_executions_total",
            "Total plugin executions",
            new CounterConfiguration { LabelNames = new[] { "plugin" } });

        public static readonly Counter CheckInEvaluationsTotal = Metrics.CreateCounter(
            "digitaltwin_checkin_evaluations_total",
            "Total check-in evaluations",
            new CounterConfiguration { LabelNames = new[] { "type" } });

        public static readonly Counter UsageLimitHitsTotal = Metrics.CreateCounter(
            "digitaltwin_usage_limit_hits_total",
            "Total usage limit hits",
            new CounterConfiguration { LabelNames = new[] { "resource" } });

        public static readonly Counter EncryptionOperationsTotal = Metrics.CreateCounter(
            "digitaltwin_encryption_operations_total",
            "Total encryption operations",
            new CounterConfiguration { LabelNames = new[] { "operation" } });

        public static readonly Histogram MessageLatencySeconds = Metrics.CreateHistogram(
            "digitaltwin_message_latency_seconds",
            "Message processing latency in seconds");

        public static readonly Gauge ActiveConversations = Metrics.CreateGauge(
            "digitaltwin_active_conversations",
            "Number of currently active conversations");
    }
}

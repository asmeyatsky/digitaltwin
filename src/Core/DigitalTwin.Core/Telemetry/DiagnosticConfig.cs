using System.Diagnostics;

namespace DigitalTwin.Core.Telemetry
{
    public static class DiagnosticConfig
    {
        public const string ServiceName = "DigitalTwin.API";
        public static readonly ActivitySource Source = new(ServiceName, "1.0.0");
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace AdventureWorksSalesClient
{
    /// <summary> Singleton access to the client-side Applications Insights telemetry client. </summary>
    internal class ClientAppInsights
    {
        /// <summary> The client-side Application Insights telemetry client. </summary>
        internal static TelemetryClient TelemetryClient;

        public static void Shutdown()
        {
            TelemetryClient.Flush();

            // Allow some time for flushing before shutdown.
            Thread.Sleep(1000);
        }

        static ClientAppInsights()
        {
            TelemetryClient = new TelemetryClient();
        }
    }
}

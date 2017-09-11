using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using Microsoft.ApplicationInsights;

namespace AdventureWorksSalesService
{
    /// <summary> Singleton access to the server-side Applications Insights telemetry client. </summary>
    internal static class ServerAppInsights
    {
        /// <summary> The server-side Application Insights telemetry client. </summary>
        internal static TelemetryClient Client;

        static ServerAppInsights()
        {
            Client = new TelemetryClient();
        }
    }

    /// <summary>
    /// Indicates that the annotated WCF service should report its internal errors to
    /// Application Insights.
    /// </summary>
    public class AiLogExceptionAttribute : Attribute, IErrorHandler, IServiceBehavior
    {
        public void AddBindingParameters(
            ServiceDescription serviceDescription,
            ServiceHostBase serviceHostBase,
            Collection<ServiceEndpoint> endpoints,
            BindingParameterCollection bindingParameters)
        {
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            foreach (ChannelDispatcher disp in serviceHostBase.ChannelDispatchers)
            {
                disp.ErrorHandlers.Add(this);
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            // no-op
        }

        bool IErrorHandler.HandleError(Exception error)
        {
            ServerAppInsights.Client.TrackException(error);
            return false;
        }

        void IErrorHandler.ProvideFault(Exception error, MessageVersion version, ref Message fault)
        {
            // no-op
        }
    }
}
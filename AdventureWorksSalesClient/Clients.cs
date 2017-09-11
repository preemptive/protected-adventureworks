using System;
using System.Configuration;
using System.Data.Services.Client;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using AdventureWorksSalesClient.AuthenticationServiceReference;
using AdventureWorksSalesClient.CustomerManagementServiceReference;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient
{
    /// <summary> Serves as a holder of various client objects to access the server. </summary>
    public class Clients
    {
        /// <summary> The URI for the sales host, e.g., http://servername.adventureworks.internal/Sales </summary>
        public Uri BaseEndpoint { get; }
        /// <summary> Provides access to the Authentication service. </summary>
        public AuthenticationServiceClient Auth { get; }

        /// <summary> Provides access to the Data service. </summary>
        public SalesEntities Data { get; private set; }
        /// <summary> Provides access to the Customer Management service. </summary>
        public CustomerManagementServiceClient Management { get; private set; }

        private const string AuthTokenHeaderName = "X-AdventureWorks-Auth";
        
        /// <summary> Constructs a Clients object as needed for authentication. </summary>
        public Clients()
        {
            BaseEndpoint = new Uri(ConfigurationManager.AppSettings["IntranetEndpoint"]);
            Auth = new AuthenticationServiceClient(
                new BasicHttpBinding(), 
                new EndpointAddress(new Uri(BaseEndpoint, "Authentication.svc")));
        }

        /// <summary>
        /// Begins an authenticated session, populating the <see cref="Data"/>
        /// and <see cref="Management"/> properties.
        /// </summary>
        /// <param name="authToken">the authentication token provided by <see cref="Auth"/></param>
        public void BeginSession(AuthToken authToken)
        {
            Data = new SalesEntities(new Uri(BaseEndpoint, "Data.svc"))
            {
                MergeOption = MergeOption.OverwriteChanges // needed to expand properties
            };
            Data.SendingRequest2 += (sender, args) =>
            {
                args.RequestMessage.SetHeader(AuthTokenHeaderName, authToken.Hash);
            };
            
            var management = new CustomerManagementServiceClient(
                new BasicHttpBinding(),
                new EndpointAddress(new Uri(BaseEndpoint, "CustomerManagement.svc")));
            management.Endpoint.Behaviors.Add(new AddHttpHeaderEndpointBehavior(AuthTokenHeaderName, authToken.Hash));
            Management = management;
        }
    }

    /// <summary>
    /// A behavior for a WCF client that instructs it to add a particular
    /// HTTP header to the outgoing requests.
    /// </summary>
    public class AddHttpHeaderEndpointBehavior : IEndpointBehavior
    {
        private readonly AddHttpHeaderMessageInspector inspector;

        /// <summary>
        /// Creates an AddHttpHeaderEndpointBehavior object.
        /// </summary>
        /// <param name="headerName">the name of the HTTP header to include on requests</param>
        /// <param name="headerValue">the value of the HTTP header to include on requests</param>
        public AddHttpHeaderEndpointBehavior(string headerName, string headerValue)
        {
            inspector = new AddHttpHeaderMessageInspector(headerName, headerValue);
        }

        public void Validate(ServiceEndpoint endpoint)
        {
            // do nothing
        }

        public void AddBindingParameters(ServiceEndpoint endpoint, BindingParameterCollection bindingParameters)
        {
            // do nothing
        }

        public void ApplyDispatchBehavior(ServiceEndpoint endpoint, EndpointDispatcher endpointDispatcher)
        {
            // do nothing
        }

        public void ApplyClientBehavior(ServiceEndpoint endpoint, ClientRuntime clientRuntime)
        {
            clientRuntime.MessageInspectors.Add(inspector);
        }
    }

    /// <summary>
    /// Modifies outgoing WCF service requests to include an additional
    /// HTTP header.
    /// </summary>
    public class AddHttpHeaderMessageInspector : IClientMessageInspector
    {
        private readonly string headerName;
        private readonly string headerValue;

        /// <summary>
        /// Creates an AddHttpHeaderMessageInspector object.
        /// </summary>
        /// <param name="headerName">the name of the HTTP header to include on requests</param>
        /// <param name="headerValue">the value of the HTTP header to include on requests</param>
        public AddHttpHeaderMessageInspector(string headerName, string headerValue)
        {
            this.headerName = headerName;
            this.headerValue = headerValue;
        }

        public object BeforeSendRequest(ref Message request, IClientChannel channel)
        {
            // See if there's already an HTTP request associated with this SOAP request
            if (request.Properties.TryGetValue(HttpRequestMessageProperty.Name, out object httpRequestObject))
            {
                // If so, set the header
                var httpRequest = (HttpRequestMessageProperty) httpRequestObject;
                httpRequest.Headers[headerName] = headerValue;
            }
            else
            {
                // Otherwise, make an HTTP request and set the header
                var httpRequest = new HttpRequestMessageProperty();
                httpRequest.Headers.Add(headerName, headerValue);
                request.Properties[HttpRequestMessageProperty.Name] = httpRequest;
            }

            return null;
        }

        public void AfterReceiveReply(ref Message reply, object correlationState)
        {
            // do nothing
        }
    }
}

using System;
using System.Data.Services;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;

namespace AdventureWorksSalesService
{
    /// <summary> Singleton access to authentication behaviors. </summary>
    internal static class Auth
    {
        /// <summary> The name of the request's HTTP header that holds the authentication token. </summary>
        public const string AuthTokenHeader = "X-AdventureWorks-Auth";

        /// <summary> Provides authentication behaviors. </summary>
        public static IAuthenticationProvider Provider { get; }

        static Auth()
        {
            Provider = new SampleAuthenticationProvider(); // TODO: Replace with real authentication in a real scenario
        }

        /// <summary> Determines if the current request is from an authenticated session. </summary>
        /// <returns>true if the request is from an authenticated session; false otherwise</returns>
        private static bool IsAuthenticated()
        {
            return Provider.IsAuthenticated(WebOperationContext.Current?.IncomingRequest.Headers?[AuthTokenHeader]);
        }

        /// <summary>
        /// Throws a <see cref="FaultException"/> with the reason "Not authenticated"
        /// if the current request is not from an authenticated session.
        /// </summary>
        /// <exception cref="FaultException">if the request is not from an authenticated session</exception>
        public static void ThrowFaultExceptionIfNotAuthenticated()
        {
            if (!IsAuthenticated())
            {
                ServerAppInsights.Client.TrackEvent("Unauthenticated request");
                throw new FaultException("Not authenticated");
            }
        }

        /// <summary>
        /// Throws a <see cref="DataServiceException"/> with code 403 and the 
        /// message "Not authenticated" if the current request is not from an authenticated session.
        /// </summary>
        /// <exception cref="DataServiceException">if the request is not from an authenticated session</exception>
        public static void ThrowDataServiceExceptionIfNotAuthenticated()
        {
            if (!IsAuthenticated())
            {
                ServerAppInsights.Client.TrackEvent("Unauthenticated request");
                throw new DataServiceException(403, "Not authenticated");
            }
        }
    }

    /// <summary> An implementation of a two-factor authentication scheme. </summary>
    internal interface IAuthenticationProvider
    {
        /// <summary>
        /// Determines if the given authentication token hash belongs to an
        /// authenticated session.
        /// </summary>
        /// <param name="tokenHash">the token hash</param>
        /// <returns>true if the hash is valid and current; false otherwise</returns>
        bool IsAuthenticated(string tokenHash);

        /// <summary>
        /// Begins the authentication process.
        /// Validates provided first-factor credentials and, if valid, provides
        /// a handshake token for use with <see cref="FinishLogin"/> and triggers
        /// second-factor confirmation code generation.
        /// </summary>
        /// <param name="credentials">login credentials</param>
        /// <returns>a token for use with <see cref="FinishLogin"/> or <see cref="CancelLogin"/></returns>
        /// <exception cref="FaultException">if the credentials are invalid</exception>
        HandshakeToken BeginLogin(LoginCredentials credentials);

        /// <summary> Aborts the authentication process. </summary>
        /// <param name="handshakeToken">the token provided by <see cref="BeginLogin"/></param>
        void CancelLogin(HandshakeToken handshakeToken);

        /// <summary>
        /// Completes the authentication process.
        /// Validates the provided handshake token (provided by <see cref="BeginLogin"/>)
        /// and second-factor confirmation code and, if valid, provides an authentication
        /// token for use with other services, for a limited session time.
        /// </summary>
        /// <param name="handshakeToken">the token provided by <see cref="BeginLogin"/></param>
        /// <param name="secondFactor">a confirmation code provided through two-factor authentication</param>
        /// <returns>an authentication token whose hash cam be used to access other services</returns>
        /// <exception cref="FaultException">if the credentials are invalid</exception>
        AuthToken FinishLogin(HandshakeToken handshakeToken, SecondFactor secondFactor);

        /// <summary>
        /// Invalidates a current session.
        /// The user will need to reauthenticate before using other services.
        /// </summary>
        /// <param name="token">the token provided by <see cref="FinishLogin"/></param>
        void Logout(AuthToken token);
    }

    /// <summary>
    /// Credentials for logging in to the services.
    /// The first factor (something the user knows) in the two-factor
    /// authentication scheme.
    /// </summary>
    [DataContract]
    public class LoginCredentials
    {
        /// <summary> The user's username. </summary>
        [DataMember(IsRequired = true)] public string Username { get; set; }

        /// <summary> The user's password. </summary>
        [DataMember(IsRequired = true)] public string Password { get; set; }
    }

    /// <summary>
    /// A token representing a started, but not completed, authentication process.
    /// </summary>
    [DataContract]
    public class HandshakeToken
    {
        /// <summary> Identifies the token. Provided by the server. </summary>
        [DataMember(IsRequired = true)] public readonly string Identifier;

        /// <summary> The time beyond which the server will no longer honor this token. </summary>
        [DataMember(IsRequired = true)] public readonly DateTime ExpiresUtc;

        public HandshakeToken(string identifier, DateTime expiresUtc)
        {
            Identifier = identifier;
            ExpiresUtc = expiresUtc;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as HandshakeToken);
        }

        public bool Equals(HandshakeToken other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Identifier.Equals(other.Identifier);
        }

        public override int GetHashCode()
        {
            return Identifier.GetHashCode();
        }
    }

    /// <summary>
    /// The representation of the second factor in the two-factor 
    /// authentication scheme. Typically this will be a one-time use
    /// code from something the user has, such as a mobile phone.
    /// </summary>
    [DataContract]
    public class SecondFactor
    {
        /// <summary> The one-time use code proving the user has the second factor. </summary>
        [DataMember(IsRequired = true)] public string OneTimeUseCode;
    }

    /// <summary> A token representing an active session for the Sales Service. </summary>
    [DataContract]
    public class AuthToken
    {
        /// <summary> 
        /// Provided by the server. The client must provide this value in the 
        /// X-AdventureWorks-Auth HTTP header in all requests to the other Sales Service
        /// endpoints.
        /// </summary>
        [DataMember(IsRequired = true)] public readonly string Hash;

        /// <summary> The time beyond which the server will no longer honor this token. </summary>
        [DataMember(IsRequired = true)] public readonly DateTime ExpiresUtc;

        public AuthToken(string hash, DateTime expiresUtc)
        {
            Hash = hash;
            ExpiresUtc = expiresUtc;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as AuthToken);
        }

        public bool Equals(AuthToken other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Hash.Equals(other.Hash);
        }

        public override int GetHashCode()
        {
            return Hash.GetHashCode();
        }
    }

    /// <summary> Interface for the Authentication service. </summary>
    [ServiceContract]
    public interface IAuthenticationService
    {
        /// <summary> Start the login process. </summary>
        /// <param name="credentials">user credentials, proving the end user knows something</param>
        /// <returns>a token for continuing the login through <see cref="FinishLogin"/> or <see cref="CancelLogin"/></returns>
        [OperationContract]
        HandshakeToken BeginLogin(LoginCredentials credentials);

        /// <summary> Aborts the login process after it has begun. </summary>
        /// <param name="handshakeToken">the token returned by <see cref="BeginLogin"/></param>
        [OperationContract]
        void CancelLogin(HandshakeToken handshakeToken);

        /// <summary> Finish the login process. </summary>
        /// <param name="handshakeToken">the token returned by <see cref="BeginLogin"/></param>
        /// <param name="secondFactor">the second factor of the authentication, proving the end user has something</param>
        /// <returns>the token required to access other services on this host for a limited time via the X-AdventureWorks-Auth header</returns>
        [OperationContract]
        AuthToken FinishLogin(HandshakeToken handshakeToken, SecondFactor secondFactor);

        /// <summary> Invalidate an authenticated user before the token expiry. </summary>
        /// <param name="token">the token returned by <see cref="FinishLogin"/></param>
        [OperationContract]
        void Logout(AuthToken token);
    }

    /// <summary> The Authentication service for the Adventure Works Sales host. </summary>
    [AiLogException]
    public class Authentication : IAuthenticationService
    {
        public HandshakeToken BeginLogin(LoginCredentials credentials)
        {
            return Auth.Provider.BeginLogin(credentials);
        }

        public void CancelLogin(HandshakeToken handshakeToken)
        {
            Auth.Provider.CancelLogin(handshakeToken);
        }

        public AuthToken FinishLogin(HandshakeToken handshakeToken, SecondFactor secondFactor)
        {
            return Auth.Provider.FinishLogin(handshakeToken, secondFactor);
        }

        public void Logout(AuthToken token)
        {
            Auth.Provider.Logout(token);
        }
    }
}

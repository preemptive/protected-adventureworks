using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;

namespace AdventureWorksSalesService
{
    /// <summary>
    /// DO NOT USE IN PRODUCTION. 
    /// Uses hardcoded credentials and second passwords instead of true 
    /// two-factor authentication. Inefficient. For sample purposes only.
    /// </summary>
    [Obsolete("SampleAuthenticationProvider is insecure and for sample purposes only.")]
    internal class SampleAuthenticationProvider : IAuthenticationProvider
    {
        private class Session
        {
            public User Owner { get; set; }

            public HandshakeToken HandshakeToken { get; set; }
            public AuthToken AuthToken { get; set; }

            public bool IsAuthenticated => AuthToken != null;
        }

        private class User
        {
            public readonly string Username;
            public readonly string Password;

            /// <summary>
            /// A real implementation would store an email or an SMS
            /// number, then send a unique second factor with each login.
            /// For simplicity, this sample just has a hardcoded second
            /// password.
            /// </summary>
            public readonly string HardcodedSecondFactorCode;

            public User(string username, string password, string secondFactorCode)
            {
                Username = username;
                Password = password;
                HardcodedSecondFactorCode = secondFactorCode;
            }

            public override bool Equals(object obj)
            {
                return Equals(obj as User);
            }

            public bool Equals(User other)
            {
                if (other == null)
                {
                    return false;
                }
                return this.Username.Equals(other.Username);
            }

            public override int GetHashCode()
            {
                return Username.GetHashCode();
            }
        }
        
        private List<User> users = new List<User>
        {
            new User("UserA", "PasswordA", "SecretA"),
            new User("UserB", "PasswordB", "SecretB"),
            new User("UserC", "PasswordC", "SecretC")
        };

        private readonly List<Session> sessions = new List<Session>();

        private readonly Random rng;
        private readonly double handshakeMinutes, sessionMinutes;

        public SampleAuthenticationProvider()
        {
            rng = new Random();
            handshakeMinutes = double.Parse(ConfigurationManager.AppSettings["HandshakeMinutes"]);
            sessionMinutes = double.Parse(ConfigurationManager.AppSettings["SessionMinutes"]);
        }

        private string GetNewHash()
        {
            var bytes = new byte[8];
            rng.NextBytes(bytes);
            return string.Join("", bytes.Select(b => b.ToString("X2")));
        }

        private DateTime GetHandshakeExpiryFromNowUtc()
        {
            return DateTime.UtcNow.AddMinutes(handshakeMinutes);
        }

        private DateTime GetSessionExpiryFromNowUtc()
        {
            return DateTime.UtcNow.AddMinutes(sessionMinutes);
        }

        private void RemoveExpiredSessions()
        {
            var now = DateTime.UtcNow;
            sessions.RemoveAll(s =>
            {
                if (!s.IsAuthenticated)
                {
                    return s.HandshakeToken.ExpiresUtc < now;
                }
                else
                {
                    return s.AuthToken.ExpiresUtc < now;
                }
            });
        }

        public HandshakeToken BeginLogin(LoginCredentials credentials)
        {
            lock (sessions)
            {
                RemoveExpiredSessions();

                var user = users.SingleOrDefault(u => u.Username == credentials.Username);
                if (user != null)
                {
                    if (user.Password == credentials.Password)
                    {
                        var toRet = new HandshakeToken(GetNewHash(), GetHandshakeExpiryFromNowUtc());
                        sessions.Add(new Session
                        {
                            Owner = user,
                            HandshakeToken = toRet
                        });
                        return toRet;
                    }
                }
                throw new FaultException("Bad username or password");
            }
        }

        public void CancelLogin(HandshakeToken handshakeToken)
        {
            lock (sessions)
            {
                RemoveExpiredSessions();

                sessions.RemoveAll(s => !s.IsAuthenticated && s.HandshakeToken.Equals(handshakeToken));
            }
        }

        public AuthToken FinishLogin(HandshakeToken handshakeToken, SecondFactor secondFactor)
        {
            lock (sessions)
            {
                RemoveExpiredSessions();

                var session =
                    sessions.SingleOrDefault(s => !s.IsAuthenticated && s.HandshakeToken.Equals(handshakeToken));

                if (session != null)
                {
                    if (session.Owner.HardcodedSecondFactorCode.Equals(secondFactor.OneTimeUseCode))
                    {
                        var toRet = new AuthToken(GetNewHash(), GetSessionExpiryFromNowUtc());

                        session.AuthToken = toRet;

                        return toRet;
                    }
                }
                throw new FaultException("Authentication error");
            }
        }

        public bool IsAuthenticated(string tokenHash)
        {
            if (string.IsNullOrWhiteSpace(tokenHash))
            {
                return false;
            }

            lock (sessions)
            {
                RemoveExpiredSessions();

                return sessions.Any(s => s.IsAuthenticated && s.AuthToken.Hash.Equals(tokenHash));
            }
        }

        public void Logout(AuthToken token)
        {
            lock (sessions)
            {
                RemoveExpiredSessions();

                sessions.RemoveAll(s => s.IsAuthenticated && s.AuthToken.Equals(token));
            }
        }
    }
}
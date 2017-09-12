using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Windows;
using AdventureWorksSalesClient.AuthenticationServiceReference;

namespace AdventureWorksSalesClient.Dialogs
{
    /// <summary> Interaction logic for the login dialog </summary>
    public partial class LoginDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool isDebugged; // to be set by the "Login" Debugging Check
        private readonly Clients clients;

        /// <summary> The handshake token for a login in progress. </summary>
        private HandshakeToken HandshakeToken
        {
            get { return handshake; }
            set
            {
                handshake = value;
                OnPropertyChanged(nameof(IsLoginStarted));
            }
        }
        private HandshakeToken handshake;

        /// <summary>
        /// true if the login process has started, or false otherwise
        /// </summary>
        public bool IsLoginStarted => handshake != null;

        /// <summary> Creates the login dialog. </summary>
        public LoginDialog()
        {
            InitializeComponent();
            clients = new Clients();

            HandshakeToken = null;
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            ClientAppInsights.Shutdown();
        }

        /// <summary> 
        /// Event handler when the user clicks the "OK" button. 
        /// Starts or confirms the login.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsLoginStarted)
            {
                ConfirmLogin();
            }
            else
            {
                StartLogin();
            }
        }

        /// <summary> 
        /// Event handler when the user clicks the "Cancel" button. 
        /// Cancels an in-progress login or exits the app if no login is in progress.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void CancelButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (IsLoginStarted)
            {
                CancelLogin();
            }
            else
            {
                this.Close(); // exits the app
            }
        }

        /// <summary> Begins the login process. </summary>
        private void StartLogin()
        {
            var username = TextBox.Text;
            var password = PasswordBox.Password;

            if (string.IsNullOrWhiteSpace(username))
            {
                Utilities.ShowValidationErrorDialog("You must enter a username.");
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                Utilities.ShowValidationErrorDialog("You must enter a password.");
                return;
            }

            PasswordBox.Clear();

            try
            {
                HandshakeToken = clients.Auth.BeginLogin(new LoginCredentials
                {
                    Username = username,
                    Password = password
                });
            }
            catch (FaultException fe)
            {
                Utilities.Log($"FaultException from BeginLogin: {fe}");
                Utilities.ShowErrorDialog($"Login failed because \"{fe.Message}\". Please check your credentials and try again.", "Login failed");
            }
        }

        /// <summary> Cancels an incomplete login process. </summary>
        private void CancelLogin()
        {
            clients.Auth.CancelLogin(handshake);
         
            // Cancelling the login process should be rare;
            // track it in case there's some usability problem.
            ClientAppInsights.TelemetryClient.TrackEvent("Login cancelled");

            HandshakeToken = null;

            PasswordBox.Clear();
        }

        /// <summary> Completes a login process. </summary>
        private void ConfirmLogin()
        {
            var oneTimeUseCode = PasswordBox.Password;
            if (string.IsNullOrWhiteSpace(oneTimeUseCode))
            {
                Utilities.ShowValidationErrorDialog("You must enter the authentication code.");
                return;
            }
            PasswordBox.Clear();

            AuthToken authToken;
            try
            {
                authToken = clients.Auth.FinishLogin(handshake, new SecondFactor
                {
                    OneTimeUseCode = oneTimeUseCode
                });
            }
            catch (FaultException fe)
            {
                Utilities.Log($"Error on ConfirmLogin: {fe}");
                Utilities.ShowErrorDialog($"Login failed because \"{fe.Message}\". Please check your credentials and try again.", "Login failed");
                return;
            }

            HandshakeToken = null;

            RunUserSession(authToken);

            PasswordBox.Focus(); // focus back on the password, because the username will remain populated
        }

        /// <summary> Hides the login window and runs an authenticated session. </summary>
        /// <param name="authToken">the authentication token for the session</param>
        private void RunUserSession(AuthToken authToken)
        {
            var startTime = DateTimeOffset.Now;

            clients.BeginSession(authToken);
            var customerWindow = new Windows.CustomerWindow(clients, isDebugged);

            Hide();

            try
            {
                customerWindow.ShowDialog();
            }
            catch
            {
                ClientAppInsights.TelemetryClient.TrackRequest(
                    "ClientSession", startTime, DateTimeOffset.Now - startTime, 
                    "500", false);
                throw;
            }

            ClientAppInsights.TelemetryClient.TrackRequest(
                "ClientSession", startTime, DateTimeOffset.Now - startTime, 
                "200", true);

            Show();

            clients.Auth.Logout(authToken);
        }
    }
}

using System;
using System.Windows;

namespace AdventureWorksSalesClient
{
    /// <summary>
    /// Interaction logic for the client app overall
    /// </summary>
    public partial class App : Application
    {
        private void OnStartup(object sender, StartupEventArgs eventArgs)
        {
            // Attach an unhandled exception handler that displays the error to the user
            AppDomain.CurrentDomain.UnhandledException += (o, args) =>
            {
                var ex = (args.ExceptionObject as Exception);

                ClientAppInsights.TelemetryClient.TrackException(ex);
                ClientAppInsights.Shutdown();

                Utilities.Log(ex);

                var body = "";
                while (ex != null)
                {
                    // Don't display the typical outer exception, just the inner exceptions.
                    const string unneededOuterExceptionMessage 
                        = "The invocation of the constructor on type 'AdventureWorksSalesClient.LoginDialog' that matches the specified binding constraints threw an exception.";
                    if (!ex.Message.Contains(unneededOuterExceptionMessage))
                    {
                        body += $"\n* {ex.Message}";
                    }
                    ex = ex.InnerException;
                }

                if (body == "")
                {
                    body = "Unknown error.";
                }
                Utilities.ShowErrorDialog($"An unexpected error occurred. " +
                                          $"Please contact Adventure Works technical support. " +
                                          $"The error was:\n{body}", "An error occurred");
            };
        }
    }
}

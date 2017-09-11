using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient
{
    /// <summary> Provide helper functions for the client. </summary>
    internal static class Utilities
    {
        /// <summary> Writes an informational message to debugging and trace listeners. </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            Debug.WriteLine(message);
            Trace.WriteLine(message);
        }

        /// <summary> Writes exception information to debugging and trace listeners. </summary>
        /// <param name="exception"></param>
        public static void Log(Exception exception)
        {
            Log($"Exception occurred: {exception}");
        }

        /// <summary> Describes a customer entry. </summary>
        /// <param name="person">the customer entry to describe</param>
        /// <returns>a human-readable description of the customer</returns>
        public static string GetDescription(this Person person)
        {
            return $"Customer #{person.BusinessEntityID} ({person.LastName}, {person.FirstName})";
        }

        private static void ShowAndHandleDialog(
            string message, 
            string title, 
            MessageBoxButton buttons, 
            MessageBoxImage image, 
            MessageBoxResult defaultResult, 
            Dictionary<MessageBoxResult, Action> callbacks)
        {
            var result = MessageBox.Show(message, title, buttons, image, defaultResult);
            if (callbacks == null)
            {
                return;
            }
            if (!callbacks.ContainsKey(result))
            {
                throw new NotImplementedException();
            }
            callbacks[result]();
        }

        /// <summary>
        /// Displays a modal error dialog.
        /// </summary>
        /// <param name="message">text for the dialog to display</param>
        /// <param name="title">title of the dialog</param>
        public static void ShowErrorDialog(string message, string title)
        {
            ShowAndHandleDialog(message, title, MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK, null);
        }

        /// <summary>
        /// Displays a modal error dialog for a validation error.
        /// </summary>
        /// <param name="requirement">the requirement that was not met</param>
        public static void ShowValidationErrorDialog(string requirement)
        {
            // Track validation errors - if we see a lot, then maybe
            // our UI or employee training should be examined.
            ClientAppInsights.TelemetryClient.TrackEvent("Validation error",
                new Dictionary<string, string>{{"Requirement", requirement}});

            ShowErrorDialog(requirement, "Validation error");
        }

        /// <summary>
        /// Displays a modal yes/no dialog.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title">title of the dialog</param>
        /// <param name="yesCallback">code to run if the user selects "Yes"</param>
        /// <param name="noCallback">code to run if the user selects "No" or closes the window</param>
        /// <param name="warn">should the dialog show a warning icon?</param>
        public static void ShowYesNoDialog(
            string message, string title, 
            Action yesCallback, Action noCallback, 
            bool warn = false)
        {
            ShowAndHandleDialog(
                message, 
                title, 
                MessageBoxButton.YesNo, 
                warn ? MessageBoxImage.Warning : MessageBoxImage.Question, 
                MessageBoxResult.No, 
                new Dictionary<MessageBoxResult, Action>
                {
                    { MessageBoxResult.Yes, yesCallback },
                    { MessageBoxResult.No, noCallback }
                });
        }

        /// <summary>
        /// Displays a modal yes/no/cancel dialog.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title">title of the dialog</param>
        /// <param name="yesCallback">code to run if the user selects "Yes"</param>
        /// <param name="noCallback">code to run if the user selects "No"</param>
        /// <param name="cancelCallback">code to run if the user selects "Cancel" or closes the window</param>
        /// <param name="warn">should the dialog show a warning icon?</param>
        public static void ShowYesNoCancelDialog(
            string message, string title, 
            Action yesCallback, Action noCallback, Action cancelCallback, 
            bool warn = false)
        {
            ShowAndHandleDialog(
                message,
                title,
                MessageBoxButton.YesNoCancel,
                warn ? MessageBoxImage.Warning : MessageBoxImage.Question,
                MessageBoxResult.Cancel,
                new Dictionary<MessageBoxResult, Action>
                {
                    { MessageBoxResult.Yes, yesCallback },
                    { MessageBoxResult.No, noCallback },
                    { MessageBoxResult.Cancel, cancelCallback },
                });
        }

        /// <summary>
        /// Displays a modal OK/cancel dialog.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title">title of the dialog</param>
        /// <param name="okCallback">code to run if the user selects "OK"</param>
        /// <param name="cancelCallback">code to run if the user selects "Cancel" or closes the window</param>
        /// <param name="warn">should the dialog show a warning icon?</param>
        public static void ShowOKCancelDialog(
            string message, string title, 
            Action okCallback, Action cancelCallback, 
            bool warn = false)
        {
            ShowAndHandleDialog(
                message,
                title,
                MessageBoxButton.OKCancel,
                warn ? MessageBoxImage.Warning : MessageBoxImage.Question,
                MessageBoxResult.Cancel,
                new Dictionary<MessageBoxResult, Action>
                {
                    { MessageBoxResult.OK, okCallback },
                    { MessageBoxResult.Cancel, cancelCallback },
                });
        }
    }
}

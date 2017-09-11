using System;
using System.Windows;
using AdventureWorksSalesClient.CustomerManagementServiceReference;

namespace AdventureWorksSalesClient.Dialogs
{
    /// <summary> Interaction logic for the Add Customer dialog </summary>
    public partial class AddCustomerDialog : Window
    {
        private readonly Clients clients;

        /// <summary> The customer's last name. </summary>
        public string LastName { get; set; }

        /// <summary> The customer's first name. </summary>
        public string FirstName { get; set; }

        /// <summary> The created record's ID, if the dialog was succesful . </summary>
        public long CreatedCustomerID { get; set; }

        /// <summary> Creates the Add Customer dialog. </summary>
        public AddCustomerDialog(Clients clients)
        {
            InitializeComponent();
            this.clients = clients;
        }

        /// <summary> 
        /// Event handler when the user clicks the "OK" button. 
        /// Validates the inputs and, if successful, creates the record
        /// and exits the dialog.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LastName))
            {
                Utilities.ShowValidationErrorDialog("A Last Name must be provided.");
                return;
            }
            if (string.IsNullOrWhiteSpace(FirstName))
            {
                Utilities.ShowValidationErrorDialog("A First Name must be provided.");
                return;
            }

            CreatedCustomerID = clients.Management.CreateCustomer(new CustomerInfo
            {
                LastName = LastName,
                FirstName = FirstName
            });

            DialogResult = true;
            Close();
        }
    }
}

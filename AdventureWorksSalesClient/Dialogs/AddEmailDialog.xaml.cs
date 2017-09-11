using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using AdventureWorksSalesClient.CustomerManagementServiceReference;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient.Dialogs
{
    /// <summary> Interaction logic for the add email window </summary>
    public partial class AddEmailWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Clients clients;

        /// <summary> The customer who owns this email. </summary>
        public Person Customer
        {
            get { return customer; }
            private set
            {
                customer = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CustomerDescription));
            }
        }
        private Person customer;

        /// <summary> A human-readable description of the customer. </summary>
        public string CustomerDescription => Customer?.GetDescription();

        /// <summary> The email address of the record to be added. </summary>
        public string Email { get; set; }

        /// <summary> Creates the Add Email dialog. </summary>
        public AddEmailWindow(Clients clients, Person customer)
        {
            InitializeComponent();

            this.clients = clients;
            Customer = customer;
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
            if (string.IsNullOrWhiteSpace(Email))
            {
                Utilities.ShowValidationErrorDialog("An email address must be provided.");
                return;
            }

            clients.Management.CreateEmailAndAssociateWithCustomer(
                Customer.BusinessEntityID,
                new EmailInfo { EmailAddress = Email });

            DialogResult = true;
            Close();
        }
    }
}

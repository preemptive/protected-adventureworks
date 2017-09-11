using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AdventureWorksSalesClient.CustomerManagementServiceReference;
using AdventureWorksSalesClient.SalesServiceReference;
using AdventureWorksSalesClient.Windows;

namespace AdventureWorksSalesClient.Dialogs
{
    /// <summary> Interaction logic for the add/edit phone window </summary>
    /// <remarks> 
    /// We need to use this to edit phone entries because we can't edit them
    /// through the data service, which is what's bound in <see cref="PhoneWindow"/>.
    /// </remarks>
    public partial class EditPhoneDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Clients clients;

        private PersonPhone ExistingRecord
        {
            get { return existingRecord; }
            set
            {
                existingRecord = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsExistingRecord));
            }
        }
        private PersonPhone existingRecord;

        /// <summary> Whether this record already exists (edit mode) or does not (add mode). </summary>
        public bool IsExistingRecord => existingRecord != null;

        /// <summary> The customer who owns this phone. </summary>
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
        
        /// <summary> The phone number of the record. </summary>
        public string PhoneNumber
        {
            get { return phoneNumber; }
            set { phoneNumber = value; OnPropertyChanged(); }
        }
        private string phoneNumber;
        
        /// <summary> The phone type of the record. </summary>
        public int PhoneNumberTypeId
        {
            get { return phoneNumberTypeId; }
            set { phoneNumberTypeId = value; OnPropertyChanged(); }
        }
        private int phoneNumberTypeId;

        /// <summary> Creates the Edit Phone dialog. </summary>
        public EditPhoneDialog(Clients clients, Person customer, PersonPhone existingRecord)
        {
            InitializeComponent();

            this.clients = clients;
            ExistingRecord = existingRecord;
            Customer = customer;
            
            TypeCombobox.ItemsSource = PhoneNumberTypeConverter.Values;

            if (IsExistingRecord)
            {
                PhoneNumber = existingRecord.PhoneNumber;
                PhoneNumberTypeId = existingRecord.PhoneNumberTypeID;
            }
            else
            {
                PhoneNumberTypeId = PhoneNumberTypeConverter.Values.First().PhoneNumberTypeID;
            }
        }

        /// <summary> 
        /// Event handler when the user clicks the "OK" button. 
        /// Validates the inputs and, if successful, creates 
        /// or replaces the record and exits the dialog.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                Utilities.ShowValidationErrorDialog("A phone number must be provided.");
                return;
            }
            if (PhoneNumberTypeConverter.Values.All(pnt => pnt.PhoneNumberTypeID != PhoneNumberTypeId))
            {
                Utilities.ShowValidationErrorDialog("A phone type must be provided.");
                return;
            }

            if (IsExistingRecord)
            {
                clients.Management.ReplacePhone(
                    Customer.BusinessEntityID,
                    new PhoneInfo
                    {
                        PhoneNumber = existingRecord.PhoneNumber,
                        PhoneNumberTypeId = existingRecord.PhoneNumberTypeID
                    },
                    new PhoneInfo
                    {
                        PhoneNumber = PhoneNumber,
                        PhoneNumberTypeId = PhoneNumberTypeId
                    });
            }
            else
            {
                clients.Management.CreatePhoneAndAssociateWithCustomer(
                    Customer.BusinessEntityID,
                    new PhoneInfo
                    {
                        PhoneNumber = PhoneNumber,
                        PhoneNumberTypeId = PhoneNumberTypeId
                    });
            }

            DialogResult = true;
            Close();
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using AdventureWorksSalesClient.CustomerManagementServiceReference;
using AdventureWorksSalesClient.Dialogs;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient.Windows
{
    /// <summary> Interaction logic for the phone information window </summary>
    public partial class PhoneWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Clients clients;
        private readonly CollectionViewSource collectionViewSource;

        /// <summary> The customer for whom this window is editing phones. </summary>
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

        private PersonPhone CurrentRow => (PersonPhone) collectionViewSource.View.CurrentItem;

        /// <summary> Creates the phone window. </summary>
        internal PhoneWindow(Clients clients, Person customer)
        {
            InitializeComponent();

            this.clients = clients;
            Customer = customer;
            collectionViewSource = (CollectionViewSource) FindResource("PhoneDataViewSource");
        }

        /// <summary> Event handler when the window has loaded. </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdatePhoneTypes();
            UpdatePhones();
        }

        /// <summary> 
        /// Event handler when the user clicks the "add phone" button. 
        /// Opens an <see cref="EditPhoneDialog"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            var newPhoneWindow = new EditPhoneDialog(clients, Customer, null);
            var dialogResult = newPhoneWindow.ShowDialog();
            if (dialogResult == true)
            {
                UpdatePhones();
            }
        }

        /// <summary> 
        /// Event handler when the user clicks a row's "edit" button. 
        /// Opens an <see cref="EditPhoneDialog"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void EditPhoneButton_OnClick(object sender, RoutedEventArgs e)
        {
            var editPhoneWindow = new EditPhoneDialog(clients, Customer, CurrentRow);
            var dialogResult = editPhoneWindow.ShowDialog();
            if (dialogResult == true)
            {
                UpdatePhones();
            }
        }

        /// <summary> 
        /// Event handler when the user clicks a row's "delete" button. 
        /// Deletes the entry and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            clients.Management.RemovePhone(Customer.BusinessEntityID, new PhoneInfo
            {
                PhoneNumber = CurrentRow.PhoneNumber,
                PhoneNumberTypeId = CurrentRow.PhoneNumberTypeID
            });
            UpdatePhones();
        }

        /// <summary> 
        /// Queries the server and updates the data displayed. 
        /// </summary>
        private void UpdatePhones()
        {
            // We can't use the Data client to edit existing phone entries,
            // because we would be editing their primary keys.
            // Just expand the existing Person for reading.

            clients.Data.LoadProperty(Customer, nameof(Customer.PersonPhones));
            
            var results = Customer.PersonPhones.OrderBy(p => p.PhoneNumber);

            collectionViewSource.Source = new ObservableCollection<PersonPhone>(results);
            collectionViewSource.View.MoveCurrentToFirst();
        }

        /// <summary> 
        /// Queries the server and updates the available phone types. 
        /// </summary>
        private void UpdatePhoneTypes()
        {
            PhoneNumberTypeConverter.PopulateValues(clients.Data);
        }
    }
}

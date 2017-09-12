using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AdventureWorksSalesClient.Dialogs;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient.Windows
{
    /// <summary> Interaction logic for the email information window </summary>
    public partial class EmailWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Clients clients;
        private readonly CollectionViewSource collectionViewSource;

        private bool isClosing = false;

        /// <summary> The customer for whom this window is editing emails. </summary>
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

        /// <summary> Whether changes are pending. </summary>
        public bool ChangesPending
        {
            get { return changesPending; }
            private set { changesPending = value; OnPropertyChanged(); }
        }
        private bool changesPending;

        private EmailAddress CurrentRow => (EmailAddress) collectionViewSource.View.CurrentItem;
        
        /// <summary> Creates the email window. </summary>
        internal EmailWindow(Clients clients, Person customer)
        {
            InitializeComponent();

            this.clients = clients;
            Customer = customer;
            collectionViewSource = (CollectionViewSource) FindResource("EmailDataViewSource");
        }

        /// <summary> Event handler when the window has loaded. </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            UpdateData();
        }

        /// <summary> 
        /// Event handler when the window has been requested to close.
        /// Depending on user response to this handler, the close may be cancelled.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            isClosing = true;
            if (ChangesPending)
            {
                Utilities.ShowYesNoCancelDialog(
                    $"Save changes to Email Addresses for {Customer.GetDescription()}?",
                    "Save Changes?",
                    yesCallback: () =>
                    {
                        SaveChanges();
                    },
                    noCallback: () =>
                    {
                        RevertChanges();
                    },
                    cancelCallback: () =>
                    {
                        e.Cancel = true;
                        isClosing = false;
                    });
            }
        }

        /// <summary> 
        /// Event handler when the user clicks the "add email" button. 
        /// Opens an <see cref="AddEmailWindow"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void AddButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            var newEmailWindow = new AddEmailWindow(clients, Customer);
            var dialogResult = newEmailWindow.ShowDialog();
            if (dialogResult == true)
            {
                UpdateData();
            }
        }

        /// <summary> 
        /// Event handler when the user clicks the "save changes" button. 
        /// Saves staged changes for the page.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void SaveButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();
        }

        /// <summary> 
        /// Event handler when the user clicks the "revert changes" button. 
        /// Reverts staged changes.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void RevertButton_OnClick(object sender, RoutedEventArgs e)
        {
            RevertChanges();
        }

        /// <summary> 
        /// Event handler when the user finishes editing a cell. 
        /// If the user did not cancel the edit, stages the changes to the current row.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Don't commit changes when we're already closing
            if (isClosing)
            {
                RevertChanges();
            }
            else if (e.EditAction != DataGridEditAction.Cancel)
            {
                PrepareRowForSave();
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
            clients.Management.RemoveEmail(CurrentRow.EmailAddressID);
            UpdateData();
        }

        /// <summary> 
        /// Saves pending changes, queries the server, and updates the data displayed. 
        /// </summary>
        private void UpdateData()
        {
            SaveChanges();
            
            var query = clients.Data.EmailAddresses
                .AddQueryOption("$orderby", "EmailAddressID")
                .AddQueryOption("$filter", $"BusinessEntityID eq {Customer.BusinessEntityID}");

            var results = (QueryOperationResponse<EmailAddress>) query.Execute();

            collectionViewSource.Source = new ObservableCollection<EmailAddress>(results);
            collectionViewSource.View.MoveCurrentToFirst();
        }

        /// <summary> 
        /// Stages the changes to the current row. 
        /// </summary>
        private void PrepareRowForSave()
        {
            clients.Data.UpdateObject(collectionViewSource.View.CurrentItem);
            ChangesPending = true;
        }

        /// <summary> 
        /// Saves all staged changes. 
        /// </summary>
        private void SaveChanges()
        {
            clients.Data.SaveChanges();
            ChangesPending = false;
        }

        /// <summary> 
        /// Reverts all staged changes and reloads the data. 
        /// </summary>
        private void RevertChanges()
        {
            foreach (LinkDescriptor link in clients.Data.Links)
            {
                if (link.State != EntityStates.Unchanged && link.State != EntityStates.Detached)
                {
                    clients.Data.DetachLink(link.Source, link.SourceProperty, link.Target);
                }
            }

            foreach (EntityDescriptor entity in clients.Data.Entities)
            {
                if (entity.State != EntityStates.Unchanged && entity.State != EntityStates.Detached)
                {
                    clients.Data.Detach(entity.Entity);
                }
            }

            UpdateData();
            ChangesPending = false;
        }

        // To be called by the "Sensitive Data" Debugging Check for UpdateData
        private void ReportDebugging(bool isDebugging)
        {
            if (isDebugging)
            {
                ClientAppInsights.TelemetryClient.TrackEvent(
                    "Debugger Detected when Querying Sensitive Data",
                    new Dictionary<string, string> { { "Query", "Email Addresses" } });
                ClientAppInsights.Shutdown();
            }
        }
    }
}

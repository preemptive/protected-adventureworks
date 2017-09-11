using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AdventureWorksSalesClient.Dialogs;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient.Windows
{
    /// <summary> Interaction logic for the credit card information window </summary>
    public partial class CreditCardWindow : Window, INotifyPropertyChanged
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

        private CreditCard CurrentRow => (CreditCard) collectionViewSource.View.CurrentItem;

        /// <summary> Creates the credit card window. </summary>
        internal CreditCardWindow(Clients clients, Person customer)
        {
            InitializeComponent();

            this.clients = clients;
            Customer = customer;
            collectionViewSource = (CollectionViewSource) FindResource("CreditCardDataViewSource");
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
                    $"Save changes to Credit Cards for {Customer.GetDescription()}?",
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
        /// Event handler when the user clicks the "associate credit card" button. 
        /// Opens an <see cref="AddCreditCardDialog"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void AssociateButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            var newCardWindow = new AddCreditCardDialog(clients, Customer);
            newCardWindow.ShowDialog();
            UpdateData();
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
        /// Event handler when the user clicks a row's "disassociate" button. 
        /// Asks the user for confirmation if the card is shared with other customers,
        /// or deletes immediately if not shared.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void DisassociateButton_OnClick(object sender, RoutedEventArgs e)
        {
            var toDelete = CurrentRow;

            // determine if this customer is the only one using this record
            var query = clients.Data.PersonCreditCards
                .AddQueryOption("$filter", $"(BusinessEntityID ne {Customer.BusinessEntityID}) and (CreditCardID eq {toDelete.CreditCardID})");

            if (query.Count() > 0) // Any() is not implemented for this IEnumerable<PersonCreditCard>
            {
                Utilities.ShowYesNoCancelDialog(
                    "This credit card is also associated with other customers.\nDelete this card for those customers as well?",
                    "Delete shared credit card?",
                    yesCallback: () =>
                    {
                        clients.Management.RemoveCreditCard(toDelete.CreditCardID);
                    },
                    noCallback: () =>
                    {
                        clients.Management.DisassociateCreditCardFromCustomer(Customer.BusinessEntityID, toDelete.CreditCardID);
                    },
                    cancelCallback: () =>
                    {
                        // do nothing
                    });
            }
            else
            {
                // Not shared with anyone else, just delete it
                clients.Management.RemoveCreditCard(toDelete.CreditCardID);
            }

            UpdateData();
        }

        /// <summary> 
        /// Saves pending changes, queries the server, and updates the data displayed. 
        /// </summary>
        private void UpdateData()
        {
            SaveChanges();

            // Get the relevant Credit Card IDs
            clients.Data.LoadProperty(Customer, nameof(Customer.PersonCreditCards));
            var ccIds = Customer.PersonCreditCards.Select(pcc => pcc.CreditCardID).ToList();
            
            // Only display those Credit Cards, or, if there are none, display nothing
            if (ccIds.Any())
            {
                var filter = string.Join(" or ", ccIds.Select(ccId => $"(CreditCardID eq {ccId})"));
                var query = clients.Data.CreditCards
                    .AddQueryOption("$orderby", "CreditCardID")
                    .AddQueryOption("$filter", filter);

                var results = (QueryOperationResponse<CreditCard>) query.Execute();

                collectionViewSource.Source = new ObservableCollection<CreditCard>(results);
                collectionViewSource.View.MoveCurrentToFirst();
            }
            else
            {
                collectionViewSource.Source = Enumerable.Empty<CreditCard>();
                collectionViewSource.View.MoveCurrentToFirst();
            }
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
                    new Dictionary<string, string>{{"Query", "Credit Cards"}});
            }
        }
    }
}

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Services.Client;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AdventureWorksSalesClient.CustomerManagementServiceReference;
using AdventureWorksSalesClient.Dialogs;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient.Windows
{
    /// <summary> Interaction logic for the customer information window </summary>
    public partial class CustomerWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private const int PageSize = 25;

        private readonly Clients clients;
        private readonly CollectionViewSource collectionViewSource;

        private readonly bool isDebugged;

        private bool isClosing = false;
        private Filter filter = new Filter();

        /// <summary>
        /// The current page number, with the value ranging
        /// from from 1 to <see cref="TotalPages"/> inclusive.
        /// </summary>
        public long CurrentPage
        {
            get { return currentPageZeroIndexed + 1; }
            set
            {
                if (1 <= value && value <= TotalPages)
                {
                    currentPageZeroIndexed = value - 1;
                    UpdateData();
                }
                OnPropertyChanged();
                PrevPageButtonActive = 1 < CurrentPage;
                NextPageButtonActive = CurrentPage < TotalPages;
            }
        }
        private long currentPageZeroIndexed = 0;

        /// <summary>
        /// The total number of pages.
        /// </summary>
        public long TotalPages
        {
            get { return totalPages; }
            private set
            {
                totalPages = value;
                OnPropertyChanged();
                NextPageButtonActive = CurrentPage < TotalPages;
            }
        }
        private long totalPages = 0;

        /// <summary>
        /// Whether the "previous page" button should be enabled.
        /// </summary>
        public bool PrevPageButtonActive
        {
            get { return prevPageButtonActive; }
            private set { prevPageButtonActive = value; OnPropertyChanged(); }
        }
        private bool prevPageButtonActive = false;

        /// <summary>
        /// Whether the "next page" button should be enabled.
        /// </summary>
        public bool NextPageButtonActive
        {
            get { return nextPageButtonActive; }
            private set { nextPageButtonActive = value; OnPropertyChanged(); }
        }
        private bool nextPageButtonActive = true;

        /// <summary>
        /// Whether the "clear filter" button should be enabled.
        /// </summary>
        public bool ClearFilterButtonActive
        {
            get { return clearFilterButtonActive; }
            private set { clearFilterButtonActive = value; OnPropertyChanged(); }
        }
        private bool clearFilterButtonActive = false;

        /// <summary>
        /// Whether changes to this page are pending.
        /// </summary>
        public bool ChangesPending
        {
            get { return changesPending; }
            private set { changesPending = value; OnPropertyChanged(); }
        }
        private bool changesPending;

        /// <summary>
        /// The total number of records available through the current filter.
        /// </summary>
        internal long ResultCount
        {
            get { return resultCount; }
            private set { resultCount = value; OnPropertyChanged(); }
        }
        private long resultCount;

        private Person CurrentRow => (Person) collectionViewSource.View.CurrentItem;
        
        /// <summary> Creates the customer window. </summary>
        public CustomerWindow(Clients clients, bool isDebugged)
        {
            InitializeComponent();

            this.clients = clients;
            this.isDebugged = isDebugged;
            collectionViewSource = (CollectionViewSource) FindResource("CustomerDataViewSource");
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
                    "Do you want to save your changes before logging out?",
                    "Save Changes and Log Out?",
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
            else
            {
                Utilities.ShowOKCancelDialog(
                    "Log out?",
                    "Log Out?",
                    okCallback: () =>
                    {
                        // continue closing
                    },
                    cancelCallback: () =>
                    {
                        e.Cancel = true;
                        isClosing = false;
                    });
            }
        }

        /// <summary> 
        /// Event handler when the user presses a key while focus is on the "current page" textbox. 
        /// If the key pressed was enter/return, updates the current page number.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void CurrentPageBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                CurrentPageBox?.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            }
        }

        /// <summary> 
        /// Event handler when the user clicks the "previous page" button. 
        /// Decrements the current page number.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void PrevPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentPage--;
        }

        /// <summary> 
        /// Event handler when the user clicks the "next page" button. 
        /// Increments the current page number.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void NextPageButton_OnClick(object sender, RoutedEventArgs e)
        {
            CurrentPage++;
        }

        /// <summary> 
        /// Event handler when the user clicks the "filter" button. 
        /// Opens a <see cref="Dialogs.FilterDialog"/> and applies filtering changes as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void FilterButton_OnClick(object sender, RoutedEventArgs e)
        {
            var filterDialog = new Dialogs.FilterDialog(filter) { Owner = this };
            var dialogResult = filterDialog.ShowDialog();
            if (dialogResult == true)
            {
                ClearFilterButtonActive = true;

                UpdateData();
            }
        }
        
        /// <summary> 
        /// Event handler when the user clicks the "clear filter" button. 
        /// Resets the filter.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void ClearFilterButton_OnClick(object sender, RoutedEventArgs e)
        {
            filter = new Filter();
            ClearFilterButtonActive = false;

            UpdateData();
        }

        /// <summary> 
        /// Event handler when the user clicks the "add customer" button.
        /// Opens an <see cref="Dialogs.AddCustomerDialog"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void AddCustomerButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();

            var newCustomerDialog = new AddCustomerDialog(clients);
            var dialogResult = newCustomerDialog.ShowDialog();
            if (dialogResult == true)
            {
                filter = new Filter() { IdValue = newCustomerDialog.CreatedCustomerID.ToString() };
                ClearFilterButtonActive = true;

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
        /// Reverts staged changes for the page.
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
            // Don't stage changes when we're already closing
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
        /// Event handler when the user edits a row's "email preference" dropdown. 
        /// Stages the changes to the current row.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void EmailPreference_OnDropDownClosed(object sender, EventArgs e)
        {
            CurrentRow.EmailPromotion = ((ComboBox) sender).SelectedIndex;
            PrepareRowForSave();
        }


        /// <summary> 
        /// Event handler when the user clicks a row's "email" button. 
        /// Opens an <see cref="EmailWindow"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void EmailButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            var emailDialog = new EmailWindow(clients, CurrentRow);
            emailDialog.ShowDialog();
        }

        /// <summary> 
        /// Event handler when the user clicks a row's "phone" button. 
        /// Opens an <see cref="PhoneWindow"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void PhoneButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            var phoneDialog = new PhoneWindow(clients, CurrentRow);
            phoneDialog.ShowDialog();
        }

        /// <summary> 
        /// Event handler when the user clicks a row's "credit card" button. 
        /// Opens an <see cref="CreditCardWindow"/> and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void CreditCardButton_OnClick(object sender, RoutedEventArgs e)
        {
            SaveChanges();
            var creditCardDialog = new CreditCardWindow(clients, CurrentRow);
            creditCardDialog.ShowDialog();
        }
        
        /// <summary> 
        /// Event handler when the user clicks a row's "delete" button. 
        /// Confirms the deletion with the user and saves/updates data as necessary.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void DeleteButton_OnClick(object sender, RoutedEventArgs e)
        {
            var toDelete = CurrentRow;
            Utilities.ShowYesNoDialog(
                $"Are you sure you want to delete record for {toDelete.GetDescription()}?\n\n" +
                "This will also delete any email addresses, phone numbers, and credit cards that are associated with only this customer.",
                "Delete customer record?",
                yesCallback: () =>
                {
                    SaveChanges();

                    clients.Management.RemoveCustomer(toDelete.BusinessEntityID);

                    // Track when customer records are deleted; if we see a sudden
                    // burst, then it might be due to a bad actor (or not, but we should be alerted).
                    ClientAppInsights.TelemetryClient.TrackEvent("Removed customer record");

                    UpdateData();
                },
                noCallback: () =>
                {
                    // do nothing
                },
                warn: true);
        }

        /// <summary> 
        /// Saves pending changes, queries the server, and updates the data displayed. 
        /// </summary>
        private void UpdateData()
        {
            SaveChanges();

            var query = clients.Data.People
                .AddQueryOption("$orderby", "LastName,FirstName")
                .AddQueryOption("$filter", filter.FilterString)
                .AddQueryOption("$skip", currentPageZeroIndexed * PageSize)
                .AddQueryOption("$top", PageSize)
                .IncludeTotalCount();
            var results = (QueryOperationResponse<Person>)query.Execute();

            collectionViewSource.Source = new ObservableCollection<Person>(results);
            collectionViewSource.View.MoveCurrentToFirst();

            ResultCount = results.TotalCount;

            TotalPages = 1 + ((ResultCount - 1) / PageSize);
            if (CurrentPage > TotalPages)
            {
                CurrentPage = TotalPages;
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
    }
}

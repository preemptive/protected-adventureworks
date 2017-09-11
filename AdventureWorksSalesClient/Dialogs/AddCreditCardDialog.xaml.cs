using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using AdventureWorksSalesClient.CustomerManagementServiceReference;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient.Dialogs
{
    /// <summary> Interaction logic for the add credit card window </summary>
    /// <remarks> 
    /// Customers can share a single credit card. If the user enters a card number
    /// that is already used, this dialog will prompt to this effect.
    /// </remarks>
    public partial class AddCreditCardDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly Clients clients;

        /// <summary> The customer who is associated with this credit card. </summary>
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

        /// <summary> The card number of the record to be added or associated. </summary>
        public string CardNumber { get; set; }

        /// <summary> The card type of the record to be added. </summary>
        public string CardType { get; set; }

        /// <summary> The expiration month number of the record to be added. </summary>
        public string ExpMonth { get; set; }

        /// <summary> The expiration year number of the record to be added. </summary>
        public string ExpYear { get; set; }

        /// <summary> Creates the Add Credit Card dialog. </summary>
        public AddCreditCardDialog(Clients clients, Person customer)
        {
            InitializeComponent();

            this.clients = clients;
            Customer = customer;
        }

        /// <summary> 
        /// Event handler when the user clicks the "OK" button. 
        /// Validates the inputs and, if successful, creates or updates
        /// the record and exits the dialog.
        /// </summary>
        /// <param name="sender">the sender of this event</param>
        /// <param name="e">the event arguments of this event</param>
        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CardNumber))
            {
                Utilities.ShowValidationErrorDialog("A credit card number must be provided.");
                return;
            }

            var existingCardEntry = clients.Data.CreditCards
                .AddQueryOption("$filter", $"CardNumber eq '{CardNumber}'")
                .Execute()
                .SingleOrDefault();

            if (existingCardEntry != null)
            {
                clients.Data.LoadProperty(existingCardEntry, nameof(CreditCard.PersonCreditCards));
                if (existingCardEntry.PersonCreditCards.Any(pcc => pcc.BusinessEntityID == Customer.BusinessEntityID))
                {
                    Utilities.ShowValidationErrorDialog("This customer is already associated with that card number.");
                    return;
                }
                
                Utilities.ShowYesNoDialog(
                    $"Credit card number {CardNumber} is already in use by another customer. Associate this customer with that same card?",
                    "Card already in use", yesCallback: () =>
                    {
                        clients.Management.AssociateCreditCardWithCustomer(Customer.BusinessEntityID, existingCardEntry.CreditCardID);

                        // We think this is a rarely-used feature.
                        // Track its usage; if we see it being used, we can implement
                        // a stronger UI for this case.
                        ClientAppInsights.TelemetryClient.TrackEvent("Joined Credit Card to Multiple Customers");

                        DialogResult = true;
                        Close();
                    }, noCallback: () =>
                    {
                        // do nothing, we will return from the handler without closing the dialog
                    });
                
                return;
            }

            if (string.IsNullOrWhiteSpace(CardType))
            {
                Utilities.ShowValidationErrorDialog("A credit card type must be provided for a new card.");
                return;
            }

            byte monthNum;
            if (!byte.TryParse(ExpMonth, out monthNum))
            {
                Utilities.ShowValidationErrorDialog("An expiration month must be provided for a new card.");
                return;
            }
            if (monthNum == 0 || monthNum > 12)
            {
                Utilities.ShowValidationErrorDialog("A valid expiration month number (1-12 inclusive) must be provided.");
                return;
            }

            ushort yearNum;
            if (!ushort.TryParse(ExpYear, out yearNum))
            {
                Utilities.ShowValidationErrorDialog("An expiration year must be provided for a new card.");
                return;
            }

            clients.Management.CreateCreditCardAndAssociateWithCustomer(Customer.BusinessEntityID, new CreditCardInfo
            {
                CardType = CardType,
                CardNumber = CardNumber,
                ExpMonth = monthNum,
                ExpYear = yearNum
            });

            DialogResult = true;
            Close();
        }
    }
}

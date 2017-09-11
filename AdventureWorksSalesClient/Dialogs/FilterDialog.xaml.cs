using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace AdventureWorksSalesClient.Dialogs
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Filter filter;

        public Filter Filter
        {
            get => filter;
            private set { filter = value; OnPropertyChanged(); }
        }

        public FilterDialog(Filter filter)
        {
            InitializeComponent();
            Filter = filter;
        }

        private void OKButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(IdValueTextbox.Text))
            {
                if (!int.TryParse(IdValueTextbox.Text, out _))
                {
                    Utilities.ShowValidationErrorDialog("ID must be blank or an integer.");
                    return;
                }
                else if (string.IsNullOrWhiteSpace(Filter.IdValue))
                {
                    // The user explicitly chose to filter by ID.
                    // We think this is a rarely-used feature, so track it.
                    ClientAppInsights.TelemetryClient.TrackEvent("Filter by Customer ID");
                }
            }

            IdValueTextbox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            LastNameValueTextbox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            FirstNameValueTextbox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            DialogResult = true;
            Close();
        }
    }
}

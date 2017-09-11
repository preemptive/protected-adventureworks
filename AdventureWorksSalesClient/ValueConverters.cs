using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using AdventureWorksSalesClient.SalesServiceReference;

namespace AdventureWorksSalesClient
{
    /// <summary>
    /// Converts PhoneNumberTypeID (int) to Name (string) and back.
    /// </summary>
    public class PhoneNumberTypeConverter : IValueConverter
    {
        /// <summary> The <see cref="PhoneNumberType"/> values retrieved from the data service. </summary>
        internal static List<PhoneNumberType> Values { get; private set; }

        /// <summary> Populates <see cref="Values"/> from the data service. </summary>
        /// <param name="dataClient"></param>
        internal static void PopulateValues(SalesEntities dataClient)
        {
            if (Values == null)
            {
                Values = dataClient.PhoneNumberTypes.ToList();
            }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // int -> String
            var input = (int?) value;
            if (input == null)
            {
                return null;
            }
            return Values.Single(pnt => pnt.PhoneNumberTypeID == input).Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // String -> int
            var input = (string) value;
            if (input == null)
            {
                return null;
            }
            return Values.Single(pnt => pnt.Name == input).PhoneNumberTypeID;
        }
    }
}

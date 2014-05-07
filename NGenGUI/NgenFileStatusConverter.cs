using System;
using System.Globalization;
using System.Windows.Data;

namespace Hiale.NgenGui
{
    [ValueConversion(typeof (NgenFileStatus), typeof (string))]
    public class NgenFileStatusConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var status = (NgenFileStatus) value;
            switch (status)
            {
                case NgenFileStatus.Unknown:
                    return "Unknown";
                case NgenFileStatus.Installed:
                    return "Native";
                case NgenFileStatus.Deinstalled:
                    return "Not Native";
                case NgenFileStatus.InProgress:
                    return "In Progress...";
                case NgenFileStatus.Pending:
                    return "Pending";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
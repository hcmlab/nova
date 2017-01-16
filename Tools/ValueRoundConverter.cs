using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ssi
{
    internal class ValueRoundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {           
            double Value = (double)value;
            return Math.Round(Value, 2);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double Value = 0;
            try
            {
                Value = System.Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture.NumberFormat);
            }
            catch (System.Exception e)
            {
                MessageBox.Show(e.Message);
            }
            return Math.Round(Value, 4);
        }
    }
}
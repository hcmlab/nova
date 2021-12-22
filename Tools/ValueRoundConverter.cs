using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

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
    internal class ValueRoundConverterminDur : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double Value = (double)value;

            return Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, Math.Round(Value, 2));
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

    

    internal class ValueRoundConverter01 : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double Value = (double)value;
       
            return Math.Min(1.0, (Math.Max(0.0, Math.Round(Value, 2))));
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
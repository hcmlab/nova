using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoTierNewPointSchemeWindow.xaml
    /// </summary>
    public partial class AnnoTierNewPointSchemeWindow : Window
    {
        private AnnoScheme scheme;

        public AnnoTierNewPointSchemeWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            this.scheme = scheme;

            nameTextBox.Text = scheme.Name;
            srTextBox.Text = scheme.SampleRate.ToString();
            numPointsTextBox.Text = scheme.NumberOfPoints.ToString();
            colorPicker.SelectedColor = scheme.MaxOrForeColor;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            scheme.Name = nameTextBox.Text == "" ? Defaults.Strings.Unknown : nameTextBox.Text;
            double value;
            if (double.TryParse(srTextBox.Text, out value))
            {
                scheme.SampleRate = value;
            }
            if (double.TryParse(numPointsTextBox.Text, out value))
            {
                if (value <= 0)
                {
                    value = 1;
                }
                scheme.NumberOfPoints = (int)value;
            }
            scheme.MinOrBackColor = colorPicker.SelectedColor.Value;
            scheme.MaxOrForeColor = colorPicker.SelectedColor.Value;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

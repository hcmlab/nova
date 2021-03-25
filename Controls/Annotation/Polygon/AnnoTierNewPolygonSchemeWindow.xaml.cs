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
    /// Interaction logic for AnnoTierNewPolygonSchemeWindow.xaml
    /// </summary>
    public partial class AnnoTierNewPolygonSchemeWindow : Window
    {
        private AnnoScheme scheme;

        public AnnoTierNewPolygonSchemeWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            this.scheme = scheme;

            nameTextBox.Text = scheme.Name;
            srTextBox.Text = scheme.SampleRate.ToString();
            dlTextBox.Text = scheme.DefaultLabel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            scheme.Name = nameTextBox.Text == "" ? Defaults.Strings.Unkown : nameTextBox.Text;
            scheme.DefaultLabel = dlTextBox.Text == "" ? Defaults.Strings.Unkown : nameTextBox.Text;
            
            if (double.TryParse(srTextBox.Text, out double value))
            {
                scheme.SampleRate = value;
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

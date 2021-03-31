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
    public partial class DefaultLabelWindow : Window
    {
        private AnnoScheme scheme;

        public DefaultLabelWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            this.scheme = scheme;
            dlTextBox.Text = scheme.DefaultLabel;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            scheme.DefaultLabel = dlTextBox.Text == "" ? Defaults.Strings.Unkown : dlTextBox.Text;  

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

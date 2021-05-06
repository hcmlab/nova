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
            dlColor.SelectedColor = scheme.DefaultColor;

            if (scheme.DefaultLabel != "")
                dlTextBox.Text = scheme.DefaultLabel;
            else
                dlTextBox.Text = "Unknown";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            scheme.DefaultLabel = dlTextBox.Text == "" ? Defaults.Strings.Unkown : dlTextBox.Text;

            Byte R = dlColor.SelectedColor.GetValueOrDefault().R;
            Byte G = dlColor.SelectedColor.GetValueOrDefault().G;
            Byte B = dlColor.SelectedColor.GetValueOrDefault().B;

            scheme.DefaultColor = Color.FromRgb(R, G, B);

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

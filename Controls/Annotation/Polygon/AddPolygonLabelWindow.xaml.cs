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
    public partial class AddPolygonLabelWindow : Window
    {
        private AnnoScheme scheme;
        private MainHandler mainHandler;

        public AddPolygonLabelWindow(ref AnnoScheme scheme, ref MainHandler mainHandler)
        {
            InitializeComponent();

            this.scheme = scheme;
            this.mainHandler = mainHandler;
            plTextBox.Text = scheme.DefaultLabel;
            plTextBox.SelectAll();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            PolygonLabel polygonLabel = new PolygonLabel(null, plTextBox.Text == "" ? Defaults.Strings.Unkown : plTextBox.Text);
            this.mainHandler.addPolygonLabelToDataGrid(polygonLabel);
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

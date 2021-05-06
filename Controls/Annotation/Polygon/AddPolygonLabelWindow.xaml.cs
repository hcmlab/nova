using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;


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
            dlColor.SelectedColor = scheme.DefaultColor;

            if (scheme.DefaultLabel != "")
                plTextBox.Text = scheme.DefaultLabel;
            else
                plTextBox.Text = "Unknown";
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Byte R = dlColor.SelectedColor.GetValueOrDefault().R;
            Byte G = dlColor.SelectedColor.GetValueOrDefault().G;
            Byte B = dlColor.SelectedColor.GetValueOrDefault().B;

            PolygonLabel polygonLabel = new PolygonLabel(null, plTextBox.Text == "" ? Defaults.Strings.Unkown : plTextBox.Text, Color.FromRgb(R, G, B));
            
            this.mainHandler.addPolygonLabelToPolygonList(polygonLabel);

            Close();
            MainHandler.IsCreateModeOn = true;
            this.mainHandler.drawPolygon(polygonLabel);
            
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

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
        AnnoScheme result;
        public AnnoScheme Result { get { return result; } }

        public class Input
        {
            public double SampleRate { get; set; }
            public int NumNodes { get; set; }
            public Color NodeColour { get; set; }
            public Color LineColour{ get; set; }
        }

        public AnnoTierNewPolygonSchemeWindow(Input defaultInput)
        {
            InitializeComponent();

            result = new AnnoScheme();
            result.Type = AnnoScheme.TYPE.POLYGON;
            result.SampleRate = defaultInput.SampleRate;
            result.NumberOfPoints = defaultInput.NumNodes;
            result.MaxOrForeColor = defaultInput.LineColour;
            result.MinOrBackColor = defaultInput.NodeColour;

            nameTextBox.Text = Defaults.Strings.Unkown;
            srTextBox.Text = defaultInput.SampleRate.ToString();
            numNodesTextBox.Text = defaultInput.NumNodes.ToString();
            colorPickerNodes.SelectedColor = defaultInput.NodeColour;
            colorPickerLines.SelectedColor = defaultInput.LineColour;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            result.Name = nameTextBox.Text == "" ? Defaults.Strings.Unkown : nameTextBox.Text;
            double value;
            if (double.TryParse(srTextBox.Text, out value))
            {
                result.SampleRate = value;
            }
            if (double.TryParse(numNodesTextBox.Text, out value))
            {
                result.NumberOfPoints = (int)value;
            }
            result.MinOrBackColor = colorPickerNodes.SelectedColor.Value;
            result.MaxOrForeColor = colorPickerLines.SelectedColor.Value;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

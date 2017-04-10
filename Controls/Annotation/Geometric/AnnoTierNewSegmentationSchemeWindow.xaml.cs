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
    /// Interaction logic for AnnoTierNewSegmentationSchemeWindow.xaml
    /// </summary>
    public partial class AnnoTierNewSegmentationSchemeWindow : Window
    {
        AnnoScheme result;
        public AnnoScheme Result { get { return result; } }

        public class Input
        {
            public double SampleRate { get; set; }
            public int Width { get; set; }
            public int Height { get; set; }
        }

        public AnnoTierNewSegmentationSchemeWindow(Input defaultInput)
        {
            InitializeComponent();

            result = new AnnoScheme();
            result.Type = AnnoScheme.TYPE.SEGMENTATION;
            result.SampleRate = defaultInput.SampleRate;
            result.WidthAndHeight = new int[]{ defaultInput.Width, defaultInput.Height};

            nameTextBox.Text = Defaults.Strings.Unkown;
            srTextBox.Text = defaultInput.SampleRate.ToString();

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

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

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
    /// Interaction logic for AnnoTierNewContinuousSchemeWindow.xaml
    /// </summary>
    public partial class AnnoTierNewContinuousSchemeWindow : Window
    {
        AnnoScheme result;
        public AnnoScheme Result { get { return result; } }

        public class Input
        {
            public double SampleRate { get; set; }
            public double MinScore { get; set; }
            public double MaxScore { get; set; }
            public Color MinColor { get; set; }
            public Color MaxColor { get; set; }
        }

        public AnnoTierNewContinuousSchemeWindow(Input defaultInput)
        {
            InitializeComponent();

            result = new AnnoScheme();
            result.Type = AnnoScheme.TYPE.CONTINUOUS;
            result.SampleRate = defaultInput.SampleRate;
            result.MaxScore = defaultInput.MaxScore;
            result.MinScore = defaultInput.MinScore;
            result.MaxOrForeColor = defaultInput.MaxColor;
            result.MinOrBackColor = defaultInput.MinColor;

            srTextBox.Text = defaultInput.SampleRate.ToString();
            minTextBox.Text = defaultInput.MinScore.ToString();
            maxTextBox.Text = defaultInput.MaxScore.ToString();
            colorPickerMin.SelectedColor = defaultInput.MinColor;
            colorPickerMax.SelectedColor = defaultInput.MaxColor;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            double value;
            if (double.TryParse(srTextBox.Text, out value))
            { 
                result.SampleRate = value;
            }            
            if (double.TryParse(minTextBox.Text, out value))
            {
                result.MinScore = value;
            }
            if (double.TryParse(maxTextBox.Text, out value))
            {
                result.MaxScore = value;
            }
            result.MinOrBackColor = colorPickerMin.SelectedColor.Value;
            result.MaxOrForeColor = colorPickerMax.SelectedColor.Value;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

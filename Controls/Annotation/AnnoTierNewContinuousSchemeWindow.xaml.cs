using ssi.Controls;
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
        private AnnoScheme scheme;

        public AnnoTierNewContinuousSchemeWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            this.scheme = scheme;
            
            nameTextBox.Text = scheme.Name;
            srTextBox.Text = scheme.SampleRate.ToString();
            minTextBox.Text = scheme.MinScore.ToString();
            maxTextBox.Text = scheme.MaxScore.ToString();
            colorPickerMin.SelectedColor = scheme.MinOrBackColor;
            colorPickerMax.SelectedColor = scheme.MaxOrForeColor;
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
            if (double.TryParse(minTextBox.Text, out value))
            {
                scheme.MinScore = value;
            }
            if (double.TryParse(maxTextBox.Text, out value))
            {
                scheme.MaxScore = value;
            }
            scheme.MinOrBackColor = colorPickerMin.SelectedColor.Value;
            scheme.MaxOrForeColor = colorPickerMax.SelectedColor.Value;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Description_Click(object sender, RoutedEventArgs e)
        {
            DescriptionWindow aaw = new DescriptionWindow(ref scheme);
            aaw.ShowDialog();
            if (aaw.DialogResult != true)
            {
                return;
            }

        }
    }
}

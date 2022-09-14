using System.Windows;
using System.Windows.Media;

namespace ssi
{
    public partial class AnnoTierNewFreeSchemeWindow : Window
    {
        private AnnoScheme scheme;

        public AnnoTierNewFreeSchemeWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            this.scheme = scheme;

            backgroundColorPicker.SelectedColor = scheme.MinOrBackColor;
            labelColorPicker.SelectedColor = scheme.MaxOrForeColor;
            schemeNameTextBox.Text = scheme.Name;                                   
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            scheme.MinOrBackColor = backgroundColorPicker.SelectedColor.Value;
            scheme.MaxOrForeColor = labelColorPicker.SelectedColor.Value;
            scheme.Name = schemeNameTextBox.Text == "" ? Defaults.Strings.Unkown : schemeNameTextBox.Text;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Attributes_Click(object sender, RoutedEventArgs e)
        {
            AnnoTierAttributesWindow aaw = new AnnoTierAttributesWindow(ref scheme);
            aaw.ShowDialog();
            if (aaw.DialogResult != true)
            {
                return;
            }

        }

    }
}
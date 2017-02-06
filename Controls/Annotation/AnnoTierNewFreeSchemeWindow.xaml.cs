using System.Windows;
using System.Windows.Media;

namespace ssi
{
    public partial class AnnoTierNewFreeSchemeWindow : Window
    {
        public AnnoScheme Result { get; }

        public AnnoTierNewFreeSchemeWindow()
        {
            InitializeComponent();

            Result = new AnnoScheme();

            backgroundColorPicker.SelectedColor = Colors.Black;
            labelColorPicker.SelectedColor = Colors.LightYellow;
            schemeNameTextBox.Text = "Noname";
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Result.MinOrBackColor = backgroundColorPicker.SelectedColor.Value;
            Result.MaxOrForeColor = labelColorPicker.SelectedColor.Value;
            Result.Name = schemeNameTextBox.Text == "" ? "Noname" : schemeNameTextBox.Text;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
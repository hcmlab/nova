using System.Windows;
using System.Windows.Media;

namespace ssi
{
    public partial class AnnoSchemeNewLabelWindow : Window
    {
        public AnnoListItem Result { get; set; }

        public AnnoSchemeNewLabelWindow()
        {
            InitializeComponent();
            Result = new AnnoListItem(0, 0, "");
            colorPicker.SelectedColor = Colors.Black;
            infoLabel.Text = "Add new label";
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Result.Color = colorPicker.SelectedColor.Value;
            Result.Label = labelTextBox.Text;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
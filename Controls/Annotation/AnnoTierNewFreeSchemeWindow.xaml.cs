using System.Windows;
using System.Windows.Media;

namespace ssi
{
    public partial class AnnoTierNewFreeSchemeWindow : Window
    {
        public AnnoScheme Result { get; }

        public AnnoTierNewFreeSchemeWindow(int annotierscount)
        {
            InitializeComponent();

            Result = new AnnoScheme();

            backgroundColorPicker.SelectedColor = selectColor(annotierscount);
            labelColorPicker.SelectedColor = Defaults.Colors.Foreground;
            schemeNameTextBox.Text = Defaults.Strings.Unkown;
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Result.MinOrBackColor = backgroundColorPicker.SelectedColor.Value;
            Result.MaxOrForeColor = labelColorPicker.SelectedColor.Value;
            Result.Name = schemeNameTextBox.Text == "" ? Defaults.Strings.Unkown : schemeNameTextBox.Text;

            Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }


        private Color selectColor(int index)
        {
            if (index % 8 == 0) return Colors.Khaki;
            else if (index % 8 == 1) return Colors.SkyBlue;
            else if (index % 8 == 2) return Colors.YellowGreen;
            else if (index % 8 == 3) return Colors.Tomato;
            else if (index % 8 == 4) return Colors.RosyBrown;
            else if (index % 8 == 5) return Colors.Goldenrod;
            else if (index % 8 == 6) return Colors.LightSeaGreen;
            else if (index % 8 == 7) return Colors.LightGray;
            else return Colors.White;
        }
    }
}
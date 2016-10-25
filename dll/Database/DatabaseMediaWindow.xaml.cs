using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseMediaWindow.xaml
    /// </summary>
    public partial class DatabaseMediaWindow : Window
    {
        public DatabaseMediaWindow()
        {
            InitializeComponent();

            ServerTextfield.Text = Properties.Settings.Default.DataServer;
            FolderTextfield.Text = Properties.Settings.Default.DataServerFolder;
            Connectiontype.SelectedIndex = 0;

            string[] split = Properties.Settings.Default.Filenames.Split(';');

            for (int i = 0; i < split.Length; i++)
            {
                Filenames.AppendText(split[i] + "\r\n");
            }

            //   Filenames.Text = Properties.Settings.Default.Filenames;
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Connectiontype.SelectedIndex == 2)
            {
                ServerLabel.Visibility = Visibility.Collapsed;
                ServerTextfield.Visibility = Visibility.Collapsed;
                FolderLabel.Visibility = Visibility.Collapsed;
                FolderTextfield.Visibility = Visibility.Collapsed;
                InputDescription.Content = "URLs, seperated by lines";
            }
            else if (Connectiontype.SelectedIndex == 0 || Connectiontype.SelectedIndex == 1)
            {
                ServerLabel.Visibility = Visibility.Visible;
                ServerTextfield.Visibility = Visibility.Visible;
                FolderLabel.Visibility = Visibility.Visible;
                FolderTextfield.Visibility = Visibility.Visible;
                InputDescription.Content = "Filenames, seperated by lines";
            }
        }

        public string Server()
        {
            return ServerTextfield.Text;
        }

        public string Folder()
        {
            return FolderTextfield.Text;
        }

        public string Files()
        {
            string[] split = Filenames.Text.Split('\n');
            string result = "";
            for (int i = 0; i < split.Length; i++)
            {
                result += split[i].Replace('\r', ';');
            }

            return result.TrimEnd(';');
        }

        public string Type()
        {
            return Connectiontype.SelectionBoxItem.ToString();
        }

        public bool Auth()
        {
            return (bool)requiresAuth.IsChecked;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
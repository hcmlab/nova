using System.Collections.Generic;
using System.Windows;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DataBaseResultsWindow.xaml
    /// </summary>
    public partial class DataBaseResultsWindow : Window
    {
        private bool shoulddelete = false;

        public DataBaseResultsWindow(List<string> sessions)
        {
            InitializeComponent();

            foreach (string session in sessions)
            {
                DataBaseResultsBox.Items.Add(session);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            shoulddelete = false;
            this.DialogResult = true;
            this.Close();
        }

        public string Result()
        {
            if (DataBaseResultsBox.SelectedItem != null)
                return DataBaseResultsBox.SelectedItem.ToString();
            else return null;
        }

        public bool ShouldDelete()
        {
            return shoulddelete;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            shoulddelete = false;
            this.DialogResult = false;
            this.Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            shoulddelete = true;
            this.DialogResult = true;
        }
    }
}
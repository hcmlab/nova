using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Drawing;
using System.Windows;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseUserTableWindow.xaml
    /// </summary>
    public partial class DatabaseSelectionWindow : Window
    {                
        public DatabaseSelectionWindow(List<string> strings, string title)
        {
            InitializeComponent();
            this.titlelabel.Content = title;          

            foreach (string s in strings)
            {
                DataBaseResultsBox.Items.Add(s);
            }

            if (strings.Count > 0)
            {
                DataBaseResultsBox.SelectedItem = strings[0];
            }
        }

        public string Result()
        {
            if (DataBaseResultsBox.SelectedItem == null) return null;
            return DataBaseResultsBox.SelectedItem.ToString();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }
        
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

    }
}
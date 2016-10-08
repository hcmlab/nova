using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DataBaseResultsWindow.xaml
    /// </summary>
    public partial class DataBaseResultsWindow : Window
    {
        private bool shoulddelete = false;
        private bool okayhit = false;

        public DataBaseResultsWindow(List<string> sessions, bool showdelete = true, string title = "Select Database")
        {

           
            InitializeComponent();
            this.Title = title;
            if(showdelete)
            this.Delete.Visibility = Visibility.Visible;
            else this.Delete.Visibility = Visibility.Hidden;

            foreach (string session in sessions)
            {
                DataBaseResultsBox.Items.Add(session);
            }
        }

        public void SetSelectMultiple(bool selectmultiple)
        {
            if (selectmultiple == false)
                DataBaseResultsBox.SelectionMode = SelectionMode.Single;
            else DataBaseResultsBox.SelectionMode = SelectionMode.Multiple;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            shoulddelete = false;
            okayhit = true;
            this.DialogResult = true;
            this.Close();
        }

        public System.Collections.IList Result()
        {
            if (DataBaseResultsBox.SelectedItems != null)
                return DataBaseResultsBox.SelectedItems;
            else return null;
        }


        public bool Isokayhit()
        {
            return okayhit;
        }
        public bool ShouldDelete()
        {
            if(shoulddelete)
            {

             MessageBoxResult mb =  MessageBox.Show("Are you sure you want to delete the selection? This can't be undone!","Question", MessageBoxButton.YesNo,MessageBoxImage.Question);
             if (mb == MessageBoxResult.Yes) return true;
               
            }

            return false;


  
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            shoulddelete = false;
            okayhit = false;
            this.DialogResult = false;
            this.Close();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            shoulddelete = true;
            okayhit = false;
            this.DialogResult = true;
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
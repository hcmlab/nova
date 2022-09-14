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
using MongoDB.Bson;
using MongoDB.Driver;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseAnnotatorWindow.xaml
    /// </summary>
    /// 

    public partial class DatabaseAdminUserWindow : Window
    {
        public DatabaseAdminUserWindow(string name = null, string fullname = null, string email = null, int expertise = 0, double xp = 0, double rating = 0)
        {
            InitializeComponent();
           
            if (name != null)
            {
                NameField.Text = name;
                NameField.IsEnabled = false;
                FullNameField.Text = fullname;
                Emailfield.Text = email;
                Expertisefield.SelectedIndex = expertise;
                UserAdminCheckBox.Visibility = Visibility.Hidden;
                ratingLabel.Content = "Rating: " + rating.ToString("F2");
                xpLabel.Content = "XP: " + xp;
            }               
        }

        public string GetName()
        {
            return NameField.Text;
        }

        public string GetPassword()
        {
            return PasswordField.Password;
        }


        public string GetFullName()
        {
            return FullNameField.Text;
        }

        public string Getemail()
        {
            return Emailfield.Text;
        }


        public int GetExpertise()
        {
            return Expertisefield.SelectedIndex;
        }

        public bool GetIsUserAdmin()
        {
            return UserAdminCheckBox.IsChecked.Value;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}


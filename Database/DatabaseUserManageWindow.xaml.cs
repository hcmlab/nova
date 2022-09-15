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

    public partial class DatabaseUserManageWindow : Window
    {
        public DatabaseUserManageWindow(string name = null, string fullname = null,  string email = null, int expertise = 0, double xp = 0, double rating = 0, double ratingContractor = 0)
        {
            InitializeComponent();
           
            if (name != null)
            {                
                NameField.Text = name;
                NameField.IsEnabled = false;
                FullNameField.Text = fullname;
                Emailfield.Text = email;
                int xplevel = 0;
                if (xp > 2100000) xplevel = 10;
                else if (xp > 1000000) xplevel = 9;
                else if (xp > 500000) xplevel = 8;
                else if (xp > 250000) xplevel = 7;
                else if (xp > 100000) xplevel = 6;
                else if (xp > 50000) xplevel = 5;
                else if (xp > 10000) xplevel = 4;
                else if (xp > 5000) xplevel = 3;
                else if (xp > 1000) xplevel = 2;
                else if (xp > 200) xplevel = 1;

                if(xplevel > expertise) expertise = xplevel;
                Expertisefield.SelectedIndex = expertise;
                Expertisefield.IsEnabled = false;
                ratingLabel.Content = "Rating as Annotator: " + rating.ToString("F2");
                ratingContractorLabel.Content = "Rating as Contractor: " + ratingContractor.ToString("F2");
                xpLabel.Content = "XP: " + xp;
            }
            if (Properties.Settings.Default.LoggedInWithLightning)
            {
                CurrentPasswordField.Visibility = Visibility.Hidden;
                PasswordField.Visibility = Visibility.Hidden;
                CurrentPasswordLabel.Visibility = Visibility.Hidden;
                NewPasswordLabel.Visibility = Visibility.Hidden;
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

        private void OkClick(object sender, RoutedEventArgs e)
        {
            if((MainHandler.Encode(CurrentPasswordField.Password) == Properties.Settings.Default.MongoDBPass && PasswordField.Password != "") || PasswordField.Password == "")
            {
                DialogResult = true;
                Close();
            }

            else
            {
                MessageBox.Show("Current Password is wrong. Please try again or contact an Administrator.");
            }



        
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}


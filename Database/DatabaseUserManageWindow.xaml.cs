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
        public DatabaseUserManageWindow(string name = null, string fullname = null,  string email = null, int expertise = 0)
        {
            InitializeComponent();
           
            if (name != null)
            {                
                NameField.Text = name;
                NameField.IsEnabled = false;
                FullNameField.Text = fullname;
                Emailfield.Text = email;
                Expertisefield.SelectedIndex = expertise;       
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


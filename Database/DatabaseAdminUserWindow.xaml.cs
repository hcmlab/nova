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
        public DatabaseAdminUserWindow(string name = null)
        {
            InitializeComponent();
           
            if (name != null)
            {                
                NameField.IsEnabled = false;
                NameField.Text = name;
                UserAdminCheckBox.Visibility = Visibility.Hidden;           
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


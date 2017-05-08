using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseDBMeta.xaml
    /// </summary>
    public partial class DatabaseAdminDBMeta : Window
    {
        private DatabaseDBMeta db;

        public DatabaseAdminDBMeta(ref DatabaseDBMeta db)
        {           
            InitializeComponent();

            this.db = db;

            NameField.Text = db.Name;
            DescriptionField.Text = db.Description;
            ServerField.Text = db.Server;
            AuthentificationBox.IsChecked = db.ServerAuth;
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            db.Name = NameField.Text == "" ? Defaults.Strings.Unkown : NameField.Text;
            db.Description = DescriptionField.Text;
            db.Server = ServerField.Text;
            db.ServerAuth = AuthentificationBox.IsChecked.Value;

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
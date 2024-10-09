using MongoDB.Bson;
using MongoDB.Driver;
using System;
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

            try
            {
                this.db = db;

                NameField.Text = db.Name;
                DescriptionField.Text = db.Description;
                ServerField.Text = db.Server;
                AuthentificationBox.IsChecked = db.ServerAuth;
                if (db.UrlFormat == UrlFormat.NEXTCLOUD)
                {
                    UrlFormatNextCloud.IsChecked = true;
                }

                else
                {
                    UrlFormatGeneral.IsChecked = true;
                }
            }

            catch(Exception ex) {
                Console.Write("Internal: " + ex.Message);
                    
                    }

          
            
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            db.Name = NameField.Text == "" ? Defaults.Strings.Unknown : NameField.Text;
            db.Description = DescriptionField.Text;
            db.Server = ServerField.Text;
            db.ServerAuth = AuthentificationBox.IsChecked.Value;
            db.UrlFormat = UrlFormatGeneral.IsChecked == true ? UrlFormat.GENERAL : UrlFormat.NEXTCLOUD;

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
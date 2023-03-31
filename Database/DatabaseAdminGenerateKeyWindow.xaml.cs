using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Windows;
using System.Windows.Forms;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DataBaseSessionWindow.xaml
    /// </summary>
    public partial class DatabaseAdminGenerateKeyWindow : Window
    {
        private DatabaseSession session;

        public DatabaseAdminGenerateKeyWindow()
        {
            InitializeComponent();

            DatabaseBox.Items.Clear();
            var db_databases = DatabaseHandler.GetDatabases(Properties.Settings.Default.MongoDBUser, DatabaseAuthentication.DBADMIN);

            foreach(var db in db_databases)
            {
                DatabaseBox.Items.Add(db);
            }

            DatabaseBox.SelectedItem = DatabaseHandler.DatabaseName;

            //TODO make expiry date flexible

            //DatePicker.SelectedDate = DateTime.Now;
        }       

        //public DateTime SessionDate()
        //{
        //    if (DatePicker.SelectedDate != null)
        //        return DatePicker.SelectedDate.Value;
        //    else return new DateTime();
        //}

        private async  void OkClick(object sender, RoutedEventArgs e)
        {
            //make this dynamically, maybe
            string validFor = "24h";

            ValidforLabel.Content = validFor;

           // session.Date = DatePicker.SelectedDate.Value;

            string databases = "";
            foreach(var db in DatabaseBox.SelectedItems)
            {
                databases += db + ",";
            }
            databases = databases.Remove(databases.Length - 1);
            dynamic requestform = await MainHandler.GenerateToken(databases, validFor);

            KeyLabel.Content = requestform["accessToken"];
            System.Windows.Clipboard.SetText(KeyLabel.Content.ToString());
            KeyInfo.Content = "Copied to clipboard";

            //DialogResult = false;
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

    }
}
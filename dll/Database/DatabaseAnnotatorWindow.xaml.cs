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


    public partial class DatabaseAnnotatorWindow : Window
    {

        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";

        public DatabaseAnnotatorWindow(string name = null, string fullname = null, string email = null, string expertise = null)
        {
            InitializeComponent();
     
            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase("admin");
            var annotators = database.GetCollection<BsonDocument>("system.users");

         
                List<BsonDocument> documents;
                var builder = Builders<BsonDocument>.Filter;
                documents = annotators.Find(_ => true).ToList();

                NameCombo.Items.Add("");

                foreach (BsonDocument b in documents)
                {
                    NameCombo.Items.Add(b["user"].ToString());
                }
            

         
           



            if (name != null)
            {
                NameCombo.SelectedItem = name;
                NameCombo.IsEnabled = false;
                Namefield.IsEnabled = false;
            }
               
            if (fullname != null) FullNameField.Text = fullname;
            if (email != null) Emailfield.Text = email;

            foreach (var item in ExpertiseBox.Items)
            {
                if (expertise != null && item.ToString().Contains(expertise))
                    ExpertiseBox.SelectedItem = item;
            }

        
          
        }

        public string Name()
        {
            return Namefield.Text;
        }

        public string Password()
        {
            return PasswordField.Password;
        }

        public string Fullname()
        {
            return FullNameField.Text;
        }

        public string Email()
        {
            return Emailfield.Text;
        }

        public string Expertise()
        {
            return ExpertiseBox.SelectionBoxItem.ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
 

        private void NameCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Namefield.Clear();
            PasswordField.Clear();
        }

        private void Namefield_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            NameCombo.SelectedIndex = 0;
        }
    }
}


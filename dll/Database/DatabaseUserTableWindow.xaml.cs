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
    /// Interaction logic for DatabaseUserTableWindow.xaml
    /// </summary>
    public partial class DatabaseUserTableWindow : Window
    {

        string collection;
        MongoClient mongo;
        public DatabaseUserTableWindow(List<string> sessions, bool showadminbuttons, string title = "Select Database", string Collection = "none")
        {
            InitializeComponent();
            this.Title = title;
            this.collection = Collection;
            if (showadminbuttons)
            {
                this.Add.Visibility = Visibility.Visible;
                this.Delete.Visibility = Visibility.Visible;
            }

            else
            {

                this.Add.Visibility = Visibility.Hidden;
                this.Delete.Visibility = Visibility.Hidden;
            }

            foreach (string session in sessions)
            {
                DataBaseResultsBox.Items.Add(session);
            }

           string connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;
           mongo = new MongoClient(connectionstring);



            DataBaseResultsBox.SelectedItem = sessions[0];
        }



        public string Result()
        {
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

        private void Delete_Click(object sender, RoutedEventArgs e)
        {


       
                var database = mongo.GetDatabase(Properties.Settings.Default.Database);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", DataBaseResultsBox.SelectedItem.ToString());
                var update = Builders<BsonDocument>.Update.Set("isValid", false);
                var result = database.GetCollection<BsonDocument>(collection).UpdateOne(filter, update);
                DataBaseResultsBox.Items.Remove(DataBaseResultsBox.SelectedItem);
               
            
           

            //todo care for deleting everything correctly.
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("New Entry", "Enter Name", "");
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {

                DataBaseResultsBox.Items.Add(l.Result());
                DataBaseResultsBox.SelectedItem = l.Result();
            }
        }

    }
}

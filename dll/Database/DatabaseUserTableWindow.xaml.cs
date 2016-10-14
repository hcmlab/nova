using MongoDB.Bson;
using MongoDB.Driver;
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

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseUserTableWindow.xaml
    /// </summary>
    public partial class DatabaseUserTableWindow : Window
    {
        private string collection;
        private bool isscheme = false;
        private MongoClient mongo;

        public DatabaseUserTableWindow(List<string> sessions, bool showadminbuttons, string title = "Select Database", string Collection = "none", bool scheme = false)
        {
            InitializeComponent();
            this.Title = title;
            this.collection = Collection;
            this.isscheme = scheme;
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
            if(sessions.Count > 0)
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
            if (isscheme)
            {
                

                DatabaseAnnoScheme dbas = new DatabaseAnnoScheme();
                dbas.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dbas.ShowDialog();

                if (dbas.DialogResult == true)
                {
                    DataBaseResultsBox.Items.Add(dbas.GetName());
                    DataBaseResultsBox.SelectedItem = dbas.GetName();

             
                    var database = mongo.GetDatabase(Properties.Settings.Default.Database);
                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("name", DataBaseResultsBox.SelectedItem.ToString());

                    BsonDocument d = new BsonDocument();
                    BsonElement a = new BsonElement("name", dbas.GetName());
                    BsonElement b = new BsonElement("type", dbas.GetType().ToUpper());
                    BsonElement c = new BsonElement("isValid", true);
                    BsonElement f = new BsonElement("sr", dbas.GetFps());
                    BsonElement g = new BsonElement("min", dbas.GetMin());
                    BsonElement h = new BsonElement("max", dbas.GetMax());
                    BsonElement i = new BsonElement("min_color", dbas.GetColorMin());
                    BsonElement i2 = new BsonElement("color", dbas.GetColorMin());
                    BsonElement j = new BsonElement("max_color", dbas.GetColorMax());

                    int index = 0;
                    List<LabelColorPair> lcp = dbas.GetLabelColorPairs();

                    BsonArray labels = new BsonArray();

                    foreach (LabelColorPair l in lcp)
                    {
                        labels.Add(new BsonDocument() { { "id", index++ }, { "name", l.label }, { "color", l.color }, { "isValid", true } });
                    }

                    d.Add(a);
                    d.Add(b);
                    d.Add(c);
                    d.Add(i2);
                    if (dbas.GetType() == "Discrete")
                    {
                        d.Add("labels", labels);
                    }
                    else
                    {
                        d.Add(f);
                        d.Add(g);
                        d.Add(h);
                        d.Add(i);
                        d.Add(j);
                    }

                    var coll = database.GetCollection<BsonDocument>("AnnotationSchemes");
                    coll.InsertOne(d);

                   
                }
               
            }

            else
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
}
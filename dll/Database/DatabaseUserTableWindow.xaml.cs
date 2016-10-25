using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

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
        private string connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;
        private AnnoTrack a = null;
        private HashSet<LabelColorPair> usedlabels = null;
        private bool isdiscrete = true;

        public DatabaseUserTableWindow(List<string> sessions, bool showadminbuttons, string title = "Select Database", string Collection = "none", bool isDiscrete = true, bool scheme = false, AnnoTrack _a = null)
        {
            InitializeComponent();
            this.titlelabel.Content = title;
            this.collection = Collection;
            this.isscheme = scheme;
            this.a = _a;
            this.isdiscrete = isDiscrete;
            if (showadminbuttons)
            {
                this.Add.Visibility = Visibility.Visible;
                this.Delete.Visibility = Visibility.Visible;
                if (this.isscheme == true) this.Edit.Visibility = Visibility.Visible;
            }
            else
            {
                this.Add.Visibility = Visibility.Hidden;
                this.Delete.Visibility = Visibility.Hidden;
                this.Edit.Visibility = Visibility.Hidden;
            }

            foreach (string session in sessions)
            {
                DataBaseResultsBox.Items.Add(session);
            }

            mongo = new MongoClient(connectionstring);
            if (sessions.Count > 0)
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
            if (DataBaseResultsBox.SelectedItem != null)
            {
                var database = mongo.GetDatabase(Properties.Settings.Default.Database);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", DataBaseResultsBox.SelectedItem.ToString());
                var update = Builders<BsonDocument>.Update.Set("isValid", false);
                var result = database.GetCollection<BsonDocument>(collection).UpdateOne(filter, update);
                DataBaseResultsBox.Items.Remove(DataBaseResultsBox.SelectedItem);
            }
            //todo care for deleting everything correctly.
        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            if (isscheme)
            {
                string name = null;
                Brush col1 = null;
                Brush col2 = null;
                string sr = null;
                string min = null;
                string max = null;

                if (a != null)
                {
                    name = a.AnnoList.Name;
                    isdiscrete = a.AnnoList.isDiscrete;

                    if (a.isDiscrete)
                    {
                        usedlabels = new HashSet<LabelColorPair>();

                        foreach (AnnoListItem item in a.AnnoList)
                        {
                            LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                            bool detected = false;
                            foreach (LabelColorPair p in usedlabels)
                            {
                                if (p.Label == l.Label)
                                {
                                    detected = true;
                                }
                            }

                            if (detected == false) usedlabels.Add(l);
                        }
                        col1 = a.BackgroundColor;
                    }
                    else
                    {
                        col1 = new SolidColorBrush(((LinearGradientBrush)a.ContiniousBrush).GradientStops[0].Color);
                        col2 = new SolidColorBrush(((LinearGradientBrush)a.ContiniousBrush).GradientStops[1].Color);
                        sr = (1000.0 / (a.samplerate * 1000.0)).ToString();
                        min = a.AnnoList.Lowborder.ToString();
                        max = a.AnnoList.Highborder.ToString();
                    }
                }

                storeAnnotationSchemetoDatabase(name, usedlabels, isdiscrete, col1, col2, sr, min, max);
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

        private void storeAnnotationSchemetoDatabase(string name = null, HashSet<LabelColorPair> _usedlabels = null, bool isDiscrete = true, Brush col1 = null, Brush col2 = null, string sr = null, string min = null, string max = null)
        {
            DatabaseAnnoScheme dbas = new DatabaseAnnoScheme(name, usedlabels, isdiscrete, col1, col2, sr, min, max);
            dbas.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dbas.ShowDialog();

            if (dbas.DialogResult == true)
            {
                DataBaseResultsBox.Items.Remove(name);
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
                    labels.Add(new BsonDocument() { { "id", index++ }, { "name", l.Label }, { "color", l.Color }, { "isValid", true } });
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
                var filterup = builder.Eq("name", name);
                UpdateOptions uo = new UpdateOptions();
                uo.IsUpsert = true;
                var result = coll.ReplaceOne(filterup, d, uo);

                // coll.InsertOne(d);
            }
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            Brush col1 = null;
            Brush col2 = null;
            string sr = null;
            string min = null;
            string max = null;

            DatabaseHandler dh = new DatabaseHandler(connectionstring);
            AnnotationScheme a = dh.GetAnnotationScheme(DataBaseResultsBox.SelectedItem.ToString(), isdiscrete);
            bool isDiscete = true;
            if (a.type == "CONTINUOUS") isDiscete = false;

            col1 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.mincolor));

            if (isDiscete)
            {
                usedlabels = new HashSet<LabelColorPair>();

                foreach (LabelColorPair item in a.LabelsAndColors)
                {
                    bool detected = false;
                    foreach (LabelColorPair p in usedlabels)
                    {
                        if (p.Label == item.Label)
                        {
                            detected = true;
                        }
                    }

                    if (detected == false) usedlabels.Add(item);
                }
            }
            else
            {
                col2 = new SolidColorBrush((Color)ColorConverter.ConvertFromString(a.maxcolor));
                sr = a.sr.ToString();
                min = a.minborder.ToString();
                max = a.maxborder.ToString();
            }

            storeAnnotationSchemetoDatabase(a.name, usedlabels, isDiscete, col1, col2, sr, min, max);
        }
    }
}
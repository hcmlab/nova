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
    public partial class DatabaseSelectionWindow : Window
    {
        private string collection;
        private bool allowEdit = false;
        private MongoClient mongo;
        private string connectionString = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.DatabaseAddress;
        private AnnoList annoList = null;
        private HashSet<AnnoScheme.Label> usedlabels = null;

        public DatabaseSelectionWindow(List<string> strings, bool showadminbuttons, string title = "Select", string Collection = "none", bool allowEdit = false, AnnoList annoList = null)
        {
            InitializeComponent();
            this.titlelabel.Content = title;
            this.collection = Collection;
            this.allowEdit = allowEdit;
            this.annoList = annoList;
            if (showadminbuttons)
            {
                this.Add.Visibility = Visibility.Visible;
                this.Delete.Visibility = Visibility.Visible;
                if (this.allowEdit == true) this.Edit.Visibility = Visibility.Visible;
            }
            else
            {
                this.Add.Visibility = Visibility.Hidden;
                this.Delete.Visibility = Visibility.Hidden;
                this.Edit.Visibility = Visibility.Hidden;
            }

            foreach (string session in strings)
            {
                DataBaseResultsBox.Items.Add(session);
            }

            mongo = new MongoClient(connectionString);
            if (strings.Count > 0)
                DataBaseResultsBox.SelectedItem = strings[0];
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
                var database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);
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
            if (allowEdit)
            {
                string name = null;
                Brush col1 = null;
                Brush col2 = null;
                string sr = null;
                string min = null;
                string max = null;

                if (annoList != null)
                {
                    name = annoList.Scheme.Name;

                    if (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE ||
                        annoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                    {
                        usedlabels = new HashSet<AnnoScheme.Label>();

                        foreach (AnnoListItem item in annoList)
                        {
                            AnnoScheme.Label l = new AnnoScheme.Label(item.Label, item.Color);
                            bool detected = false;
                            foreach (AnnoScheme.Label p in usedlabels)
                            {
                                if (p.Name == l.Name)
                                {
                                    detected = true;
                                }
                            }

                            if (detected == false) usedlabels.Add(l);
                        }
                        col1 = new SolidColorBrush(annoList.Scheme.MinOrBackColor);
                    }
                    else
                    {
                        col1 = new SolidColorBrush(annoList.Scheme.MinOrBackColor);
                        col2 = new SolidColorBrush(annoList.Scheme.MaxOrForeColor);
                        sr = (1000.0 / (annoList.Scheme.SampleRate * 1000.0)).ToString();
                        min = annoList.Scheme.MinScore.ToString();
                        max = annoList.Scheme.MaxScore.ToString();
                    }
                }

                storeAnnotationSchemetoDatabase(name, usedlabels, annoList.Scheme.Type, col1, col2, sr, min, max);
            }
            else
            {
                Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = "" };
                UserInputWindow dialog = new UserInputWindow("Add new name", input);
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    DataBaseResultsBox.Items.Add(dialog.Result("name"));
                    DataBaseResultsBox.SelectedItem = dialog.Result("name");
                }
            }
        }

        private void storeAnnotationSchemetoDatabase(string name = null, HashSet<AnnoScheme.Label> _usedlabels = null, AnnoScheme.TYPE isDiscrete = AnnoScheme.TYPE.DISCRETE, Brush col1 = null, Brush col2 = null, string sr = null, string min = null, string max = null)
        {
            DatabaseAnnoSchemeWindow dbas = new DatabaseAnnoSchemeWindow(name, usedlabels, annoList.Scheme.Type, col1, col2, sr, min, max);
            dbas.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dbas.ShowDialog();

            if (dbas.DialogResult == true)
            {
                DataBaseResultsBox.Items.Remove(name);
                DataBaseResultsBox.Items.Add(dbas.GetName());
                DataBaseResultsBox.SelectedItem = dbas.GetName();

                var database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", DataBaseResultsBox.SelectedItem.ToString());

                BsonDocument document = new BsonDocument();
                BsonElement documentName = new BsonElement("name", dbas.GetName());
                BsonElement documentType = new BsonElement("type", dbas.GetType().ToUpper());
                BsonElement documentIsValid = new BsonElement("isValid", true);
                BsonElement documentSr = new BsonElement("sr", dbas.GetFps());
                BsonElement documentMin = new BsonElement("min", dbas.GetMin());
                BsonElement documentMax = new BsonElement("max", dbas.GetMax());
                BsonElement documentMinColor = new BsonElement("min_color", dbas.GetColorMin());
                BsonElement documentColor = new BsonElement("color", dbas.GetColorMin());
                BsonElement documentMaxColor = new BsonElement("max_color", dbas.GetColorMax());

                int index = 0;
                List<AnnoScheme.Label> labelList = dbas.GetLabelColorPairs();

                BsonArray labels = new BsonArray();

                foreach (AnnoScheme.Label label in labelList)
                {
                    labels.Add(new BsonDocument() { { "id", index++ }, { "name", label.Name }, { "color", label.Color.ToString() }, { "isValid", true } });
                }

                document.Add(documentName);
                document.Add(documentType);

                if (dbas.GetType().ToUpper() == "DISCRETE")
                {
                    document.Add(documentColor);
                    document.Add("labels", labels);
                }
                else if (dbas.GetType().ToUpper() == "FREE")
                {
                    document.Add(documentColor);
                }
                else
                {
                    document.Add(documentSr);
                    document.Add(documentMin);
                    document.Add(documentMax);
                    document.Add(documentMinColor);
                    document.Add(documentMaxColor);
                }
                document.Add(documentIsValid);

                var coll = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                var filterup = builder.Eq("name", name);
                UpdateOptions uo = new UpdateOptions();
                uo.IsUpsert = true;
                var result = coll.ReplaceOne(filterup, document, uo);

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

            AnnoScheme a = DatabaseHandler.GetAnnotationScheme(DataBaseResultsBox.SelectedItem.ToString(), annoList.Scheme.Type);
            AnnoScheme.TYPE annoType = a.Type;            

            col1 = new SolidColorBrush(a.MinOrBackColor);

            if (annoType == AnnoScheme.TYPE.DISCRETE)
            {
                usedlabels = new HashSet<AnnoScheme.Label>();

                foreach (AnnoScheme.Label item in a.Labels)
                {
                    bool detected = false;
                    foreach (AnnoScheme.Label p in usedlabels)
                    {
                        if (p.Name == item.Name)
                        {
                            detected = true;
                        }
                    }

                    if (detected == false) usedlabels.Add(item);
                }
            }
            else if (annoType == AnnoScheme.TYPE.FREE)
            {
                col1 = new SolidColorBrush(a.MinOrBackColor);
            }
            else if (annoType == AnnoScheme.TYPE.CONTINUOUS)
            {
                col2 = new SolidColorBrush(a.MaxOrForeColor);
                sr = a.SampleRate.ToString();
                min = a.MinScore.ToString();
                max = a.MaxScore.ToString();
            }

            storeAnnotationSchemetoDatabase(a.Name, usedlabels, annoType, col1, col2, sr, min, max);
        }
    }
}
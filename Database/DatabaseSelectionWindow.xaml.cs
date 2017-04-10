using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Drawing;
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
            if (DataBaseResultsBox.SelectedItem == null) return null;
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
                AnnoList annolist = new AnnoList();
                annoList.Scheme.SampleRate = 25; //todo: get this from video
                storeAnnotationSchemetoDatabase(annoList);
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

        private void storeAnnotationSchemetoDatabase(AnnoList annolist)
        {
            if (annolist.Scheme.Type == AnnoScheme.TYPE.FREE)
            {
                AnnoTierNewFreeSchemeWindow dialog2 = new AnnoTierNewFreeSchemeWindow(0);
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();

                if (dialog2.DialogResult == true)
                {
                    AnnoScheme annoScheme = dialog2.Result;
                    annoList = new AnnoList() { Scheme = annoScheme };
                }
            }
            else if (annolist.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                AnnoTierNewDiscreteSchemeWindow dialog2 = new AnnoTierNewDiscreteSchemeWindow();
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();

                if (dialog2.DialogResult == true)
                {
                    annoList = dialog2.GetAnnoList();
                }
            }
            else if (annolist.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                AnnoTierNewContinuousSchemeWindow.Input input = new AnnoTierNewContinuousSchemeWindow.Input() { SampleRate = annolist.Scheme.SampleRate, MinScore = 0.0, MaxScore = 1.0, MinColor = Defaults.Colors.GradientMin, MaxColor = Defaults.Colors.GradientMax };
                AnnoTierNewContinuousSchemeWindow dialog2 = new AnnoTierNewContinuousSchemeWindow(input);
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();
                if (dialog2.DialogResult == true)
                {
                    AnnoScheme annoScheme = dialog2.Result;
                    annoList = new AnnoList() { Scheme = annoScheme };
                }
            }
            else if (annolist.Scheme.Type == AnnoScheme.TYPE.POINT)
            {
                AnnoTierNewPointSchemeWindow.Input input = new AnnoTierNewPointSchemeWindow.Input() { SampleRate = annolist.Scheme.SampleRate, NumPoints = 1, Color = Colors.Green };
                AnnoTierNewPointSchemeWindow dialog2 = new AnnoTierNewPointSchemeWindow(input);
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();
                if (dialog2.DialogResult == true)
                {
                    AnnoScheme annoScheme = dialog2.Result;
                    annoList = new AnnoList() { Scheme = annoScheme };
                }
            }
            else if (annolist.Scheme.Type == AnnoScheme.TYPE.POLYGON)
            {
                AnnoTierNewPolygonSchemeWindow.Input input = new AnnoTierNewPolygonSchemeWindow.Input() { SampleRate = annolist.Scheme.SampleRate, NumNodes = 1, NodeColour = Colors.Green, LineColour = Colors.Red };
                AnnoTierNewPolygonSchemeWindow dialog2 = new AnnoTierNewPolygonSchemeWindow(input);
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();
                if (dialog2.DialogResult == true)
                {
                    AnnoScheme annoScheme = dialog2.Result;
                    annoList = new AnnoList() { Scheme = annoScheme };
                }
            }
            else if (annolist.Scheme.Type == AnnoScheme.TYPE.GRAPH)
            {
                AnnoTierNewGraphSchemeWindow.Input input = new AnnoTierNewGraphSchemeWindow.Input() { SampleRate = annolist.Scheme.SampleRate, NumNodes = 1, NodeColour = Colors.Green, LineColour = Colors.Red };
                AnnoTierNewGraphSchemeWindow dialog2 = new AnnoTierNewGraphSchemeWindow(input);
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();
                if (dialog2.DialogResult == true)
                {
                    AnnoScheme annoScheme = dialog2.Result;
                    annoList = new AnnoList() { Scheme = annoScheme };
                }
            }
            else if (annolist.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
            {
                AnnoTierNewSegmentationSchemeWindow.Input input = new AnnoTierNewSegmentationSchemeWindow.Input() { SampleRate = annolist.Scheme.SampleRate, Width=1, Height=1};
                AnnoTierNewSegmentationSchemeWindow dialog2 = new AnnoTierNewSegmentationSchemeWindow(input);
                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog2.ShowDialog();
                if (dialog2.DialogResult == true)
                {
                    AnnoScheme annoScheme = dialog2.Result;
                    annoList = new AnnoList() { Scheme = annoScheme };
                }
            }



            //DatabaseAnnoSchemeWindow dbas = new DatabaseAnnoSchemeWindow(name, usedlabels, annoList.Scheme.Type, col1, col2, sr, min, max);
            //dbas.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //dbas.ShowDialog();

          
                DataBaseResultsBox.Items.Remove(annoList.Scheme.Name);
                DataBaseResultsBox.Items.Add(annoList.Scheme.Name);
                DataBaseResultsBox.SelectedItem = annoList.Scheme.Name;

                var database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", DataBaseResultsBox.SelectedItem.ToString());

                BsonDocument document = new BsonDocument();
                BsonElement documentName = new BsonElement("name", annoList.Scheme.Name);
                BsonElement documentType = new BsonElement("type", annoList.Scheme.Type.ToString());
                BsonElement documentIsValid = new BsonElement("isValid", true);
                BsonElement documentSr = new BsonElement("sr", annoList.Scheme.SampleRate);
                BsonElement documentMin = new BsonElement("min", annoList.Scheme.MinScore);
                BsonElement documentMax = new BsonElement("max", annoList.Scheme.MaxScore);
                BsonElement documentMinColor = new BsonElement("min_color", new SolidColorBrush(annoList.Scheme.MinOrBackColor).Color.ToString() );
                BsonElement documentColor = new BsonElement("color", new SolidColorBrush(annoList.Scheme.MinOrBackColor).Color.ToString());
                BsonElement documentMaxColor = new BsonElement("max_color", new SolidColorBrush(annoList.Scheme.MaxOrForeColor).Color.ToString());
                BsonElement documentPointsNum = new BsonElement("num", annoList.Scheme.NumberOfPoints);
            


                int index = 0;
                BsonArray labels = new BsonArray();

                foreach (AnnoScheme.Label label in annoList.Scheme.Labels)
                {
                    labels.Add(new BsonDocument() { { "id", index++ }, { "name", label.Name }, { "color", label.Color.ToString() }, { "isValid", true } });
                }

                document.Add(documentName);
                document.Add(documentType);

                if (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    document.Add(documentColor);
                    document.Add("labels", labels);
                }

                else if (annoList.Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    document.Add(documentPointsNum);
                    document.Add(documentSr);
                    document.Add(documentColor);
               
                }

               else if (annoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    document.Add(documentColor);
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    document.Add(documentSr);
                    document.Add(documentMin);
                    document.Add(documentMax);
                    document.Add(documentMinColor);
                    document.Add(documentMaxColor);
                }
                document.Add(documentIsValid);

                var coll = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                var filterup = builder.Eq("name", annoList.Scheme.Name);
                UpdateOptions uo = new UpdateOptions();
                uo.IsUpsert = true;
                var result = coll.ReplaceOne(filterup, document, uo);

                // coll.InsertOne(d);
            
        }

        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Media.Brush col1 = null;
            System.Windows.Media.Brush col2 = null;
            string sr = null;
            string min = null;
            string max = null;

            AnnoList annolist = new AnnoList();
            annolist.Scheme = DatabaseHandler.GetAnnotationScheme(DataBaseResultsBox.SelectedItem.ToString(), annoList.Scheme.Type);
            AnnoScheme.TYPE annoType = annolist.Scheme.Type;            

            col1 = new SolidColorBrush(annolist.Scheme.MinOrBackColor);

            if (annoType == AnnoScheme.TYPE.DISCRETE)
            {
                usedlabels = new HashSet<AnnoScheme.Label>();

                foreach (AnnoScheme.Label item in annolist.Scheme.Labels)
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
                col1 = new SolidColorBrush(annolist.Scheme.MinOrBackColor);
            }
            else if (annoType == AnnoScheme.TYPE.CONTINUOUS)
            {
                col2 = new SolidColorBrush(annolist.Scheme.MaxOrForeColor);
                sr = annolist.Scheme.SampleRate.ToString();
                min = annolist.Scheme.MinScore.ToString();
                max = annolist.Scheme.MaxScore.ToString();
            }

            storeAnnotationSchemetoDatabase(annolist);
        }
    }
}
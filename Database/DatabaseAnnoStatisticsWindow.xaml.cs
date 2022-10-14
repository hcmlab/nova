using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Web.UI.DataVisualization.Charting;
using System.IO;
using MongoDB.Driver.Linq;
using System.Windows.Data;
using Octokit;
using Dicom.Imaging.LUT;
using System.Printing;
using NDtw;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoStatisticsWindow : System.Windows.Window
    {
        private bool selectedisContinuous = false;
        private readonly object syncLock = new object();
        private string defaultlabeltext = "Hover here to calculate correlations";


        private CultureInfo culture = CultureInfo.InvariantCulture;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;
        public DatabaseAnnoStatisticsWindow()
        {
            InitializeComponent();

          
            GetDatabases(DatabaseHandler.DatabaseName);
            GetSessions();
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(SessionsResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();
                GetAnnotations();
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public void GetSessions(string selectedItem = null)

        {
            if (SessionsResultsBox.HasItems)
            {
                SessionsResultsBox.ItemsSource = null;
            }

            List<DatabaseSession> items = DatabaseHandler.Sessions;
            SessionsResultsBox.ItemsSource = items;

            if (SessionsResultsBox.HasItems)
            {
                SessionsResultsBox.SelectedIndex = 0;
                if (selectedItem != null)
                {
                    SessionsResultsBox.SelectedItem = items.Find(item => item.Name == selectedItem);
                }
            }
        }

        public ObjectId GetObjectID(IMongoDatabase database, string collection, string value, string attribute)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq(value, attribute);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0) id = result[0].GetValue(0).AsObjectId;

            return id;
        }


        public void GetAnnotations(bool onlyme = false, string sessionname = null)

        {
           // GetAnnotationSchemes();
            AnnotationResultBox.ItemsSource = null;
            //AnnoSchemesBox.ItemsSource = null;
            //AnnoSchemesBox.Items.Clear();
            //  AnnotationResultBox.Items.Clear();
            List<DatabaseAnnotation> items = new List<DatabaseAnnotation>();
            List<string> Collections = new List<string>();

            var sessions = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationschemes = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var roles = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);

            var builder = Builders<BsonDocument>.Filter;

            ObjectId schemeid = new ObjectId();
            BsonDocument result = new BsonDocument();
            if (AnnoSchemesBox.SelectedValue != null)
            {
                var filteras = builder.Eq("name", AnnoSchemesBox.SelectedValue.ToString());
                try
                {
                    result = annotationschemes.Find(filteras).Single();
                    if (result.ElementCount > 0) schemeid = result.GetValue(0).AsObjectId;
                    string type = result.GetValue(2).ToString();
                    if (type == "CONTINUOUS")
                    {
                        selectedisContinuous = true;
                    }
                    else
                    {
                        selectedisContinuous = false;
                    }
                }
                catch
                {
                    ;
                }
               
            }

            ObjectId roleid = new ObjectId();
            BsonDocument result2 = new BsonDocument();
            if (RolesBox.SelectedValue != null)
            {
                var filterat = builder.AnyEq("name", RolesBox.Items.ToString());

                try
                {
                    result2 = roles.Find(filterat).Single();
                    if (result2.ElementCount > 0) roleid = result2.GetValue(0).AsObjectId;
                }
                catch
                {
                    ;
                }
             
            }

            if (SessionsResultsBox.SelectedItem == null) SessionsResultsBox.SelectedIndex = 0;

            ObjectId sessionid = ObjectId.Empty;
            if (sessionname == null && DatabaseHandler.Sessions.Count > 1)
            {
                DatabaseSession session = SessionsResultsBox.SelectedItems.Count > 0 ? (DatabaseSession)SessionsResultsBox.SelectedItem : DatabaseHandler.Sessions[0];
                sessionid = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Sessions, "name", session.Name);
                sessionname = session.Name;
            }

            else
            {
                sessionid = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Sessions, "name", sessionname);
            }
           


            var filter = builder.Eq("session_id", sessionid);
            var annos = annotations.Find(filter).ToList();

            foreach (var anno in annos)
            {
                var filtera = builder.Eq("_id", anno["role_id"]);
                var roledb = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles).Find(filtera).Single();
                string rolename = roledb.GetValue(1).ToString();

                var filterb = builder.Eq("_id", anno["scheme_id"]);
                var annotdb = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes).Find(filterb).Single();
                string annoschemename = annotdb.GetValue(1).ToString();
                string type = annotdb.GetValue(2).ToString();

                var filterc = builder.Eq("_id", anno["annotator_id"]);
                var annotatdb = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filterc).ToList();
                string annotatorname = "Deleted User";
                string annotatornamefull = annotatorname;
                if (annotatdb.Count > 0)
                {
                    annotatorname = annotatdb[0].GetValue(1).ToString();

                    annotatornamefull = DatabaseHandler.Annotators.Find(a => a.Name == annotatorname).FullName;
      

                if (result.ElementCount > 0 && result2.ElementCount > 0 && anno["scheme_id"].AsObjectId == schemeid && anno["role_id"].AsObjectId == roleid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = sessionname });
                }
                else if (result.ElementCount > 0 && result2.ElementCount == 0 && anno["scheme_id"].AsObjectId == schemeid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = sessionname });
                }
                }
            }

            AnnotationResultBox.ItemsSource = items;
        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var annosch = annoschemes.Find(_ => true).SortBy(bson => bson["name"]).ToList();

            if (annosch.Count > 0)
            {
                if (AnnoSchemesBox.Items != null) AnnoSchemesBox.Items.Clear();

                foreach (var c in annosch)
                {
                    if (c["isValid"].AsBoolean == true) AnnoSchemesBox.Items.Add(c["name"]);
                }
                AnnoSchemesBox.SelectedItem = AnnoSchemesBox.Items.GetItemAt(0);
                
            }
        }

        public void GetRoles()
        {
            if (DatabaseHandler.Roles.Count > 0)
            {
                if (RolesBox.Items != null) RolesBox.Items.Clear();

                foreach (var r in DatabaseHandler.Roles)
                {
                    RolesBox.Items.Add(r.Name);
                }
                if (RolesBox.SelectedIndex == -1) RolesBox.SelectedIndex = 0;
            }
        }


        private string CalculateOverlapingLabels(List<AnnoList> al)
        {
            string restclass = "REST";
            AnnoList cont = new AnnoList();
            cont.Scheme = al[0].Scheme;
            List<AnnoScheme.Label> schemelabels = al[0].Scheme.Labels;

            int[] overlaps = new int[schemelabels.Count+1];

            for (int i = 0; i < al[0].Count; i++)
            {
                string[] vec = new string[al.Count];

                string maxRepeated = restclass;
                bool issamelabel = false;
                for (int j = 0; j < al.Count; j++)
                {
                    vec[j] = al[j][i].Label;
                    if (j > 0)
                    {
                        if (vec[j] == vec[j - 1]) issamelabel = true;
                        else {
                            issamelabel = false;
                            break;
                              }
                    }
                 
                }

                if (issamelabel)
                {
                    overlaps[al[0].Scheme.Labels.FindIndex(s => s.Name == al[0][i].Label)] += 1;

                }
                else
                {
                    overlaps[al[0].Scheme.Labels.Count] += 1;
                }
            }

            string result = "\n\nOverlapping Windows: ("+ Properties.Settings.Default.DefaultMinSegmentSize + "s)\n";
            double overall = 0;
            double overallpercentage = 0;
            foreach (var label in schemelabels)
            {
                result += label.Name + ": " + (((float)overlaps[schemelabels.IndexOf(label)] / ((float)al[0].Count)) * 100).ToString("F3") + "% ("+ overlaps[schemelabels.IndexOf(label)] + ")\n";
                overallpercentage += (float)(overlaps[schemelabels.IndexOf(label)] / ((float)al[0].Count) * 100);
                overall += overlaps[schemelabels.IndexOf(label)];
            }
            result += "Overall: " + overallpercentage.ToString("F3") + "% ("+ overall + "/"+ al[0].Count+  ")\n";

            return result;
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            if (annolists.Count == 0) return;

            if (annolists[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                DtwButton.Visibility = Visibility.Collapsed;
                string restclass = "REST";
                List<AnnoList> convertedlists = Statistics.convertAnnoListsToMatrix(annolists, restclass);
                StatisticsLabel.Content = CalculateClassDistribution(convertedlists);
               
              

                if(annolists.Count > 1)
                {
                    double cohenkappa = 0;
                    double fleisskappa = 0;
                    double kappa = 0;
                    string interpretation = "";
                    string kappatype = "";

                    if (annolists.Count == 2)
                    {
                        cohenkappa = Statistics.CohensKappa(convertedlists);
                        kappa = cohenkappa;
                        kappatype = "Cohen's κ: ";
                    }
                    else if (annolists.Count > 2)
                    {
                        fleisskappa = Statistics.FleissKappa(convertedlists);
                        kappa = fleisskappa;
                        kappatype = "Fleiss' κ: ";
                    }
                    StatisticsLabel.Content += "\n\nInterrater reliability:\n" + kappatype + kappa;



                    
                    StatisticsLabel.Content +=  CalculateOverlapingLabels(convertedlists);
                }
              
               
                
            }

            else if (annolists[0].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                DtwButton.Visibility = Visibility.Visible;
                StatisticsLabel.Content = "Correlation meassures:\n\n";
                double cronbachalpha = 0;
                string interpretation = "";
                if (annolists.Count > 1)
                {
              

                            cronbachalpha = Statistics.Cronbachsalpha(annolists);
                            

                        if (cronbachalpha < 0) cronbachalpha = 0.0; //can happen that it gets a little below 0, this is to avoid confusion.

                        interpretation = Statistics.Cronbachinterpretation(cronbachalpha);
                  

                    StatisticsLabel.Content += "Samples: " + annolists[0].Count;
                    StatisticsLabel.ToolTip = "Samples: " + annolists[0].Count;
                    StatisticsLabel.Content = StatisticsLabel.Content + " \nCronbach's α: " + cronbachalpha.ToString("F3");
                    StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Cronbach's α: " + interpretation;

                        double spearmancorrelation = double.MaxValue;
                        if (annolists.Count == 2)
                        {
                            spearmancorrelation = Statistics.SpearmanCorrelationMathNet(annolists[0], annolists[1]);

                            if (spearmancorrelation != double.MaxValue)
                            {
                                interpretation = Statistics.Spearmaninterpretation(spearmancorrelation);

                            StatisticsLabel.Content = StatisticsLabel.Content + " \nSpearman Correlation: " + spearmancorrelation.ToString("F3");
                            StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Spearman Correlation: " + interpretation;
                            }

                            double concordancecorrelation = double.MaxValue;
                            concordancecorrelation = Statistics.ConcordanceCorrelationCoefficient(annolists[0], annolists[1]);

                            if (concordancecorrelation != double.MaxValue)
                            {

                                interpretation = Statistics.CCCinterpretation(concordancecorrelation);

                            StatisticsLabel.Content = StatisticsLabel.Content + " \nConcordance Correlation: " + concordancecorrelation.ToString("F3");
                            StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Concordance Correlation: " + interpretation;
                            }

                            double pearsoncorrelation = double.MaxValue;

                            pearsoncorrelation = Statistics.PearsonCorrelationMathNet(annolists[0], annolists[1]);

                            int N = annolists[0].Count;
                            if (annolists[1].Count < annolists[0].Count) N = annolists[1].Count;

                            interpretation = Statistics.Pearsoninterpretation(pearsoncorrelation, N);
                            var fisherz = Statistics.transform_r_to_z(pearsoncorrelation, N);
                            var lpvalue = Statistics.transform_z_to_p(fisherz);
                            var rpvalue = 1 - lpvalue;
                            var twopvalue = 2 * rpvalue;
                            var confpvalue = 1 - twopvalue;

                            if (pearsoncorrelation != double.MaxValue)
                            {

                                double ra = 0.794;
                                double rb = 0.8;
                                var testz = Statistics.transform_2r_to_z(ra, N, rb, N);
                                var testp = Statistics.transform_z_to_p(testz);

                                if (ra > rb)
                                {
                                    testp = 1 - testp;
                                }

                                var testtwopvalue = 2 * testp;



                            StatisticsLabel.Content = StatisticsLabel.Content + " \nPearson Correlation r: " + pearsoncorrelation.ToString("F3") + " \nFisher z-score: " + fisherz.ToString("F3") + " \nTwo-tailed p value: " + twopvalue.ToString("F3"); ;
                            StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Pearson Correlation r: " + interpretation + " | Fisher z-score: " + fisherz.ToString("F3");
                            }

                            double nmse = double.MaxValue;
                            double mse = double.MaxValue;

                            nmse = Statistics.MSE(annolists, true);
                            mse = Statistics.MSE(annolists, false);

                            if (nmse != double.MaxValue)
                            {
                             StatisticsLabel.Content = StatisticsLabel.Content + " \nMSE: " + mse.ToString("F6") + " \nNMSE: " + nmse.ToString("F6"); ;


                        }
                    }



                }
                    }
            else
            {
                DtwButton.Visibility = Visibility.Collapsed;
                StatisticsLabel.Content = "Only discrete/non empty Schemes are supported for now";
            }

        }


        private string CalculateClassDistribution(List<AnnoList> convertedlists)
        {
            string restclass = "REST";
            //List<AnnoList> convertedlists = Statistics.convertAnnoListsToMatrix(al, restclass);
            List<AnnoScheme.Label> schemelabels = convertedlists[0].Scheme.Labels;
            schemelabels.Remove(schemelabels.ElementAt(schemelabels.Count - 1));
            int[] counter = new int[schemelabels.Count + 1];

            for (int i = 0; i < convertedlists[0].Count; i++)
            {
                foreach (AnnoList list in convertedlists)
                {
                    AnnoScheme.Label label = schemelabels.Find(s => s.Name == list.ElementAt(i).Label);
                    if (label == null) { counter[schemelabels.Count]++; }
                    else counter[schemelabels.IndexOf(label)]++;
                }
            }

            string result = "Class Distribution in %: \n";
            schemelabels.Add(new AnnoScheme.Label(restclass, Colors.Black));
            foreach (var label in schemelabels)
            {
                result += label.Name + ": " + (((float)counter[schemelabels.IndexOf(label)] / ((float)convertedlists[0].Count * convertedlists.Count)) * 100).ToString("F3") + "%" + "\n";
            }
            return result;
        }

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            GetAnnotations();
        }

   
        private void RolesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        public void GetDatabases(string selectedItem = null)
        {
            DatabasesBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases(DatabaseAuthentication.READWRITE);

            foreach (string db in databases)
            {
                DatabasesBox.Items.Add(db);
            }

            Select(DatabasesBox, selectedItem);
        }

        private void Select(ListBox list, string select)
        {
            if (select != null)
            {
                foreach (string item in list.Items)
                {
                    if (item == select)
                    {
                        list.SelectedItem = item;
                    }
                }
            }
        }

        private void DatabasesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
            }
           
            GetRoles();
            GetAnnotationSchemes();
            GetSessions();
            GetAnnotations();
        }


        private void SortListView(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader headerClicked =
             e.OriginalSource as GridViewColumnHeader;
            ListSortDirection direction;

            if (headerClicked != null)
            {
                if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                {
                    if (headerClicked != _lastHeaderClicked)
                    {
                        direction = ListSortDirection.Ascending;
                    }
                    else
                    {
                        if (_lastDirection == ListSortDirection.Ascending)
                        {
                            direction = ListSortDirection.Descending;
                        }
                        else
                        {
                            direction = ListSortDirection.Ascending;
                        }
                    }

                    string header = headerClicked.Column.Header as string;
                    ICollectionView dataView = CollectionViewSource.GetDefaultView(((ListView)sender).ItemsSource);

                    dataView.SortDescriptions.Clear();
                    SortDescription sd = new SortDescription(header, direction);
                    dataView.SortDescriptions.Add(sd);
                    dataView.Refresh();


                    if (direction == ListSortDirection.Ascending)
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowUp"] as DataTemplate;
                    }
                    else
                    {
                        headerClicked.Column.HeaderTemplate =
                          Resources["HeaderTemplateArrowDown"] as DataTemplate;
                    }

                    // Remove arrow from previously sorted header  
                    if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                    {
                        _lastHeaderClicked.Column.HeaderTemplate = null;
                    }

                    _lastHeaderClicked = headerClicked;
                    _lastDirection = direction;
                }
            }
        }

        private void DtwButton_Click(object sender, RoutedEventArgs e)
            {
            List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            if (annolists.Count == 2)
            {

                double[] seriesA = new double[annolists[0].Count];
                double[] seriesB = new double[annolists[1].Count];

                for (int i = 0; i < annolists[0].Count; i++)
                {
                    seriesA[i] = annolists[0].ElementAt(i).Score;
                }
                for (int i = 0; i < annolists[1].Count; i++)
                {
                    seriesB[i] = annolists[1].ElementAt(i).Score;
                }

                Dtw dtw = new Dtw(seriesA, seriesB);
                var cost = dtw.GetCost();
                StatisticsLabel.Content += "\n\nDynamic Time Wrapping\nCost: " + cost.ToString();
                GC.Collect();
            }
            else StatisticsLabel.Content = "For Dynamic Time Wrapping Cost calculation, select 2 Continuous Annotations";
        }
    }
}
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
using SharpDX;
using MathNet.Numerics;
using SharpCompress.Common;

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

                bool isFinished = false;
                DateTime date = DateTime.Now;
                BsonElement value;

                if (anno.TryGetElement("isFinished", out value))
                {
                    isFinished = anno["isFinished"].AsBoolean;
                }

                if (anno.TryGetElement("date", out value))
                {
                    
                    date = anno["date"].ToUniversalTime();
                }
               

                if (annotatdb.Count > 0)
                {
                    annotatorname = annotatdb[0].GetValue(1).ToString();

                    annotatornamefull = DatabaseHandler.Annotators.Find(a => a.Name == annotatorname).FullName;
      

                if (result.ElementCount > 0 && result2.ElementCount > 0 && anno["scheme_id"].AsObjectId == schemeid && anno["role_id"].AsObjectId == roleid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = sessionname, IsFinished = isFinished, Date = date });
                }
                else if (result.ElementCount > 0 && result2.ElementCount == 0 && anno["scheme_id"].AsObjectId == schemeid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = sessionname, IsFinished = isFinished, Date = date });
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


        private string CalculateOverlapingLabels(List<AnnoList> al, bool singleLine = false, bool GetHeaderOnly = false)
        {
            string restclass = "REST";
            AnnoList cont = new AnnoList();
            cont.Scheme = al[0].Scheme;
            List<AnnoScheme.Label> schemelabels = al[0].Scheme.Labels;

            if (!GetHeaderOnly) { 

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

                string result = "";
                if (!singleLine)
                {

                
             result = "\n\nOverlapping Windows: ("+ Properties.Settings.Default.DefaultMinSegmentSize + "s)\n";
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
                else
                {
                    double overall = 0;
                    double overallpercentage = 0;
                    foreach (var label in schemelabels)
                    {
                        result += (((float)overlaps[schemelabels.IndexOf(label)] / ((float)al[0].Count)) * 100).ToString("F3") + ";" + overlaps[schemelabels.IndexOf(label)]+";";
                        overallpercentage += (float)(overlaps[schemelabels.IndexOf(label)] / ((float)al[0].Count) * 100);
                        overall += overlaps[schemelabels.IndexOf(label)];
                    }
                    result += overallpercentage.ToString("F3") + ";" + overall + ";";

                    return result;

                }
            }

                else
            {
                string result = "";
                foreach (var label in schemelabels)
                {
                    result += "Overlaps in % " + label.Name + ";Overlaps Num " + label.Name + ";";
                }
                result += " Overlaps in % Overall;Overlaps Num Overall;";
                return result;
            }
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

                StatisticsLabel.Content = "";
                StatisticsLabel.Content = buildDiscreteStats(convertedlists);

                
            }

            else if (annolists[0].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                DtwButton.Visibility = Visibility.Visible;
                StatisticsLabel.Content = "Correlation meassures:\n\n";
                StatisticsLabel.Content +=  buildContinuousStats(annolists);


            }
            else
            {
                DtwButton.Visibility = Visibility.Collapsed;
                StatisticsLabel.Content = "Only discrete/non empty Schemes are supported for now";
            }

        }


        private string CalculateClassDistribution(List<AnnoList> convertedlists, bool singleline = false, bool getHeaderonly = false)
        {
            
            string restclass = "REST";
            List<AnnoScheme.Label> schemelabels = convertedlists[0].Scheme.Labels;
            schemelabels.Remove(schemelabels.ElementAt(schemelabels.Count - 1));


            if (!getHeaderonly)
            {

          
                //List<AnnoList> convertedlists = Statistics.convertAnnoListsToMatrix(al, restclass);
           
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

            string result = "";
                if (!singleline)
                    {
                      result = "Class Distribution in %: \n";
              
                        schemelabels.Add(new AnnoScheme.Label(restclass, Colors.Black));
                        foreach (var label in schemelabels)
                        {
                            result += label.Name + ": " + (((float)counter[schemelabels.IndexOf(label)] / ((float)convertedlists[0].Count * convertedlists.Count)) * 100).ToString("F3") + "%" + "\n";
                        }
                        return result;

                }
                else
                {
                    schemelabels.Add(new AnnoScheme.Label(restclass, Colors.Black));
                    foreach (var label in schemelabels)
                    {
                        result += (((float)counter[schemelabels.IndexOf(label)] / ((float)convertedlists[0].Count * convertedlists.Count)) * 100).ToString("F3") + ";";
                    }
                    return result;

                }
            }

            else
            {
                string result = "";
                schemelabels.Add(new AnnoScheme.Label(restclass, Colors.Black));
                foreach (var label in schemelabels)
                {
                    result += "Distribution " + label.Name + ";";
                }
                return result;

            }
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

            List<string> databases = DatabaseHandler.GetDatabases("", DatabaseAuthentication.READWRITE);

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
                StatisticsLabel.Content += "\n\nDynamic Time Warping\nCost: " + cost.ToString();
                GC.Collect();
            }
            else StatisticsLabel.Content = "For Dynamic Time Warping Cost calculation, select 2 Continuous Annotations";
        }

        private void Stats_Click(object sender, RoutedEventArgs e)
        {


            string filepath = FileTools.SaveFileDialog(AnnoSchemesBox.SelectedItem.ToString(), "csv", "CSV (*.csv)|*.csv", Defaults.LocalDataLocations()[0] + "\\" + DatabasesBox.SelectedItem.ToString());

            string header = "";
            string data = "";

            string headercont = "Session;Scheme;Samples;Cronbachs α;Spearman Correlation;Concordance Correlation;Pearson Correlation r;MSE;NMSE\n";

            string headerdisc = buildDiscreteStats(DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems), false, "", true);

            foreach (var session in SessionsResultsBox.SelectedItems)
            {
                List<DatabaseAnnotation> newList = new List<DatabaseAnnotation>();

                foreach(DatabaseAnnotation anno in AnnotationResultBox.SelectedItems)
                {
                    DatabaseAnnotation newAnno = anno;
                    newAnno.Session = ((DatabaseSession)(session)).Name;
                    newList.Add(newAnno);
                }


                List<AnnoList> annolists = DatabaseHandler.LoadSession(newList);
                if (annolists.Count == 0) return;

                if (annolists[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    header = headerdisc;
                    string restclass = "REST";
                    List<AnnoList> convertedLists = Statistics.convertAnnoListsToMatrix(annolists, restclass);
                    data +=  buildDiscreteStats(convertedLists,true, ((DatabaseSession)session).Name);


                }
                else if (annolists[0].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    header = headercont;
                    data += (buildContinuousStats(annolists, true, ((DatabaseSession)session).Name));
                }
            }

            try
            {
                File.WriteAllText(filepath, header + data);
                MessageBox.Show("File created!");
            }
            catch
            {
                MessageBox.Show("Error writing file!");
            }

          

        }

        private string buildDiscreteStats(List<AnnoList> convertedlists, bool singleLine = false, string sessionname = "", bool GetHeaderOnly = false )
        {
            string returnstring = "";
            string delimiter = ";";

            if (singleLine) returnstring = sessionname + delimiter + convertedlists[0].Scheme.Name + delimiter;
            else if(GetHeaderOnly) returnstring += "Session;Scheme;";
            returnstring += CalculateClassDistribution(convertedlists, singleLine, GetHeaderOnly);

            returnstring += CalculateOverlapingLabels(convertedlists, singleLine, GetHeaderOnly);

            if (convertedlists.Count > 1)
            {
                double kappa = 0;
                string kappatype = "";

                if (convertedlists.Count == 2)
                {
                    if (!GetHeaderOnly)
                    {
                        double cohenkappa = Statistics.CohensKappa(convertedlists);
                        kappa = cohenkappa;
                    }
                  
                    kappatype = "Cohen's κ: ";
                }
                else if (convertedlists.Count > 2)
                {
                    if (!GetHeaderOnly)
                    {
                    double fleisskappa = Statistics.FleissKappa(convertedlists);
                    kappa = fleisskappa;
                    }
                    kappatype = "Fleiss' κ: ";
                }
                if (GetHeaderOnly) returnstring += kappatype + delimiter;
                else if (singleLine) returnstring += kappa.ToString("F3");
                else returnstring += "\n\nInterrater reliability:\n" + kappatype + kappa.ToString("F3");




                

            }
            if (GetHeaderOnly || singleLine) returnstring += "\n";
            return returnstring;

        }

        private string buildContinuousStats(List<AnnoList> annolists, bool singleline = false, string sessionname = "")
        {
            string returnstring = "";
            string delimiter = ";";

            if (singleline) returnstring = sessionname + delimiter + annolists[0].Scheme.Name + delimiter;
          
            
            double cronbachalpha = 0;
            string interpretation = "";
            if (annolists.Count > 1)
            {
                if (singleline) returnstring += annolists[0].Count + delimiter;
                else returnstring += "Samples: " + annolists[0].Count;

                cronbachalpha = Statistics.Cronbachsalpha(annolists);
                if (cronbachalpha < 0) cronbachalpha = 0.0; //can happen that it gets a little below 0, this is to avoid confusion.

                //interpretation = Statistics.Cronbachinterpretation(cronbachalpha);


                // StatisticsLabel.ToolTip = "Samples: " + annolists[0].Count;
                if (singleline) returnstring += cronbachalpha.ToString("F3") + delimiter;
                else returnstring += " \nCronbach's α: " + cronbachalpha.ToString("F3");
                // StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Cronbach's α: " + interpretation;

                double spearmancorrelation = double.MaxValue;
                if (annolists.Count == 2)
                {
                    spearmancorrelation = Statistics.SpearmanCorrelationMathNet(annolists[0], annolists[1]);

                    if (spearmancorrelation != double.MaxValue)
                    {
                        //interpretation = Statistics.Spearmaninterpretation(spearmancorrelation);
                        if (singleline) returnstring += spearmancorrelation.ToString("F3") + delimiter;
                        else returnstring += " \nSpearman Correlation: " + spearmancorrelation.ToString("F3");
                        //StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Spearman Correlation: " + interpretation;
                    }

                    double concordancecorrelation = double.MaxValue;
                    concordancecorrelation = Statistics.ConcordanceCorrelationCoefficient(annolists[0], annolists[1]);

                    if (concordancecorrelation != double.MaxValue)
                    {

                        //interpretation = Statistics.CCCinterpretation(concordancecorrelation);
                        if (singleline) returnstring += spearmancorrelation.ToString("F3") + delimiter;
                        else returnstring += " \nConcordance Correlation: " + concordancecorrelation.ToString("F3");
                        //StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Concordance Correlation: " + interpretation;
                    }

                    double pearsoncorrelation = double.MaxValue;

                    pearsoncorrelation = Statistics.PearsonCorrelationMathNet(annolists[0], annolists[1]);

                    int N = annolists[0].Count;
                    if (annolists[1].Count < annolists[0].Count) N = annolists[1].Count;

                    //interpretation = Statistics.Pearsoninterpretation(pearsoncorrelation, N);
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


                        if (singleline) returnstring += pearsoncorrelation.ToString("F3") + delimiter;
                        else returnstring += " \nPearson Correlation r: " + pearsoncorrelation.ToString("F3") + " \nFisher z-score: " + fisherz.ToString("F3") + " \nTwo-tailed p value: " + twopvalue.ToString("F3"); ;
                        //StatisticsLabel.ToolTip = StatisticsLabel.ToolTip + " | Pearson Correlation r: " + interpretation + " | Fisher z-score: " + fisherz.ToString("F3");
                    }

                    double nmse = double.MaxValue;
                    double mse = double.MaxValue;

                    nmse = Statistics.MSE(annolists, true);
                    mse = Statistics.MSE(annolists, false);

                    if (nmse != double.MaxValue)
                    {
                        if (singleline) returnstring += mse.ToString("F6") + delimiter + nmse.ToString("F6");
                        else returnstring += " \nMSE: " + mse.ToString("F6") + " \nNMSE: " + nmse.ToString("F6"); ;


                    }
                }


                return returnstring + "\n";
            }

            else return "";

        }
    }
}
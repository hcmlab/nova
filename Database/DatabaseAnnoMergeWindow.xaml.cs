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

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoMergeWindow : System.Windows.Window
    {
        private bool selectedisContinuous = false;
        private readonly object syncLock = new object();
        private string defaultlabeltext = "Hover here to calculate correlations";


        private CultureInfo culture = CultureInfo.InvariantCulture;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        public DatabaseAnnoMergeWindow()
        {
            InitializeComponent();

            if ((DatabaseHandler.CheckAuthentication() < DatabaseAuthentication.DBADMIN)) Warning.Visibility = Visibility.Visible;
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
            AnnotationResultBox.ItemsSource = null;
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
                var filterat = builder.Eq("name", RolesBox.SelectedValue.ToString());
                result2 = roles.Find(filterat).Single();
                if (result2.ElementCount > 0) roleid = result2.GetValue(0).AsObjectId;
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

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((DatabaseHandler.CheckAuthentication() > DatabaseAuthentication.READWRITE))
            {
                if (AnnotationResultBox.SelectedItems.Count == 1)
                {
                    Copy.IsEnabled = true;
                    CalculateMedian.IsEnabled = false;
                    CalculateRMS.IsEnabled = false;
                    CalculateMergeDiscrete.IsEnabled = false;
                    WeightExpertise.IsEnabled = false;
                    WeightNone.IsEnabled = false;
                }
                else
                {
                    handleButtons(!selectedisContinuous);
                    Copy.IsEnabled = false;
                }
            }
            else
            {
                Copy.IsEnabled = false;
                CalculateMedian.IsEnabled = false;
                CalculateRMS.IsEnabled = false;
                CalculateMergeDiscrete.IsEnabled = false;
                WeightExpertise.IsEnabled = false;
                WeightNone.IsEnabled = false;
            }

            Stats.Content = defaultlabeltext;
        }

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        private void copyAnnotation(List<AnnoList> al)
        {
            if (al.Count > 0)
            {
                bool isSaved = false;
                string originalScheme = al[0].Scheme.Name;
                string originalFullname = al[0].Meta.AnnotatorFullName;

                AnnoList newList = new AnnoList();

                foreach (AnnoListItem ali in al[0])
                {
                    newList.AddSorted(ali);
                }

                newList.Scheme = al[0].Scheme;
                newList.Meta = al[0].Meta;
                newList.Meta.AnnotatorFullName = (string)AnnotatorsBox.SelectedItem;
                newList.Meta.Annotator = DatabaseHandler.Annotators.Find(a => a.FullName == newList.Meta.AnnotatorFullName).Name;
                newList.Source.StoreToDatabase = true;
                newList.Source.Database.Session = al[0].Source.Database.Session;
                newList.Meta.isFinished = true;
                newList.HasChanged = true;

                if (newList != null)
                {
                    isSaved = newList.Save(null, false, true);
                }

                Ok.IsEnabled = true;
                if (isSaved)
                    MessageBox.Show("Annotation: " + originalScheme + " from Annotator: " + originalFullname + " has been copied to Annotator: " + newList.Meta.AnnotatorFullName);

                GetAnnotations();
            }
        }

        private void rootMeanSquare(List<AnnoList> al)
        {
            bool isSaved = false;
            int numberoftracks = al.Count;

            AnnoList newList = new AnnoList();
            foreach (AnnoListItem ali in al[0])
            {
                newList.AddSorted(ali);
            }

            newList.Scheme = al[0].Scheme;
            newList.Meta = al[0].Meta;
            newList.Meta.AnnotatorFullName = (string)AnnotatorsBox.SelectedItem;
            newList.Meta.Annotator = DatabaseHandler.Annotators.Find(a => a.FullName == newList.Meta.AnnotatorFullName).Name;
            newList.Source.StoreToDatabase = true;
            newList.Source.Database.Session = al[0].Source.Database.Session;
            newList.Meta.isFinished = true;
            newList.HasChanged = true;

            int minSize = int.MaxValue;

            foreach (AnnoList a in al)
            {
                if (a.Count < minSize) minSize = a.Count;
            }

            double[] array = new double[minSize];

            foreach (AnnoList a in al)
            {
                for (int i = 0; i < minSize; i++)
                {
                    double label = a[i].Score;
                    array[i] = array[i] + label * label;
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                newList[i].Score = System.Math.Sqrt(array[i] / numberoftracks);
            }
            newList.Scheme.SampleRate = 1 / (newList[0].Stop - newList[0].Start);

            if (newList != null)
            {
                isSaved = newList.Save(null, false, true);
            }
            if (isSaved) MessageBox.Show("The annotations have been merged");
            Ok.IsEnabled = true;
            GetAnnotations();
        }

        private void calculateMean(List<AnnoList> al)
        {
            bool isSaved = false;
            int numberoftracks = al.Count;

            AnnoList newList = new AnnoList();
            foreach (AnnoListItem ali in al[0])
            {
                newList.AddSorted(ali);
            }

            newList.Scheme = al[0].Scheme;
            newList.Meta = al[0].Meta;
            newList.Meta.AnnotatorFullName = (string)AnnotatorsBox.SelectedItem;
            newList.Meta.Annotator = DatabaseHandler.Annotators.Find(a => a.FullName == newList.Meta.AnnotatorFullName).Name;
            newList.Source.StoreToDatabase = true;
            newList.Source.Database.Session = al[0].Source.Database.Session;
            newList.Meta.isFinished = true;
            newList.HasChanged = true;

            int minSize = int.MaxValue;

            foreach (AnnoList a in al)
            {
                if (a.Count < minSize) minSize = a.Count;
            }

            double[] array = new double[minSize];

            for (int i = 0; i < minSize; i++)
            {

                foreach (AnnoList a in al)
                {
                    array[i] = array[i] + a[i].Score;
                }

                newList[i].Score = array[i] / numberoftracks;
            }

            newList.Scheme.SampleRate = 1 / (newList[0].Stop - newList[0].Start);

            Ok.IsEnabled = true;
            if (newList != null)
            {
                isSaved = newList.Save(null, false, true);
            }

            if (isSaved) MessageBox.Show("The annotations have been merged");
            Ok.IsEnabled = true;
            GetAnnotations();
        }

        private void handleButtons(bool discrete)
        {
            if (discrete)
            {
                CalculateMedian.IsEnabled = false;
                CalculateRMS.IsEnabled = false;
                CalculateMergeDiscrete.IsEnabled = true;
                WeightExpertise.IsEnabled = true;
                WeightNone.IsEnabled = true;
            }
            else
            {
                CalculateMedian.IsEnabled = true;
                CalculateRMS.IsEnabled = true;
                CalculateMergeDiscrete.IsEnabled = false;
                WeightExpertise.IsEnabled = true;
                WeightNone.IsEnabled = true;
            }
        }

  

     
        private void MergeDiscreteLists(List<AnnoList> al, string restclass = "Rest")
        {
            AnnoList cont = new AnnoList();
            cont.Scheme = al[0].Scheme;
            bool isSaved = false;

            for (int i = 0; i < al[0].Count; i++)
            {
                string[] vec = new string[al.Count];

                string maxRepeated = restclass;
                double conf = 1.0;
                for (int j = 0; j < al.Count; j++)
                {
                    vec[j] = al[j][i].Label;

                    //todo.. some more advanced logic

                    maxRepeated = vec.GroupBy(s => s).OrderByDescending(s => s.Count()).First().Key;

                    var groups = vec.GroupBy(v => v);
                    foreach (var group in groups)
                    {
                        if (group.Key == maxRepeated)
                        {
                            conf = (double)group.Count() / (double)al.Count;
                        }
                    }
                }

                AnnoListItem ali = new AnnoListItem(al[0][i].Start, al[0][i].Duration, maxRepeated, "", Colors.Black, conf);
                cont.Add(ali);
            }

            AnnoList newList = new AnnoList();
            newList.Scheme = al[0].Scheme;
            newList.Meta = al[0].Meta;
            newList.Meta.AnnotatorFullName = (string)AnnotatorsBox.SelectedItem;
            newList.Meta.Annotator = DatabaseHandler.Annotators.Find(a => a.FullName == newList.Meta.AnnotatorFullName).Name;
            newList.Source.StoreToDatabase = true;
            newList.Source.Database.Session = al[0].Source.Database.Session;
            newList.Meta.isFinished = true;
            newList.HasChanged = true;

            for (int i = 0; i < cont.Count - 1; i++)
            {
                double start = cont[i].Start;
                double conf = 1.0;

                while (cont[i].Label == cont[i + 1].Label)
                {
                    if (cont[i].Confidence < conf) conf = cont[i].Confidence;

                    i++;
                    if (i == cont.Count - 1) break;
                }
                double dur = cont[i].Start + cont[i].Duration - start; ;

                AnnoListItem ali = new AnnoListItem(start, dur, cont[i].Label, "", Colors.Black, conf);

                if (ali.Label != restclass) newList.Add(ali);
            }

            if (newList != null)
            {
                isSaved = newList.Save(null, false, true);
            }

            if (isSaved) MessageBox.Show("Annotations have been merged");

            Ok.IsEnabled = true;

            GetAnnotations();
        }

     

        private void CalculateMedian_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;

            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);

            if (WeightExpertise.IsChecked == true) //some option
            {
                List<AnnoList> multial = multiplyAnnoListsbyExpertise(al);
                calculateMean(multial);
            }
            else
            {
                calculateMean(al);
            }
        }

        private void RolesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        private List<AnnoList> multiplyAnnoListsbyExpertise(List<AnnoList> lists)
        {
            List<AnnoList> multipliedal = new List<AnnoList>();

            foreach (AnnoList a in lists)
            {
                int expertise = DatabaseHandler.Annotators.Find(at => at.Name == a.Meta.Annotator).Expertise;
                if (expertise == 0) expertise = 1;
                for (int i = 0; i < expertise; i++)
                {
                    multipliedal.Add(a);
                }
            }
            return multipliedal;
        }

        private void RMS_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;

            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);

            if (WeightExpertise.IsChecked == true) //some option
            {
                List<AnnoList> multial = multiplyAnnoListsbyExpertise(al);
                rootMeanSquare(multial);
            }
            else
            {
                rootMeanSquare(al);
            }
        }

        private async Task CalculateKappaWrapper(List<AnnoList> annolists)
        {
            if (annolists.Count > 1)
            {
                double cohenkappa = 0;
                double fleisskappa = 0;
                double kappa = 0;
                string interpretation = "";
                string kappatype = "";

                CancellationToken token = new CancellationToken();
                await Task.Run(() =>
                {
                    lock (syncLock)
                    {
                        string restclass = "Rest";

                        List<AnnoList> convertedlists = Statistics.convertAnnoListsToMatrix(annolists, restclass);

                        if (annolists.Count == 2)
                        {
                            cohenkappa = Statistics.CohensKappa(convertedlists, restclass);
                            kappa = cohenkappa;
                            kappatype = "Cohen's κ: ";
                        }
                        else if (annolists.Count > 2)
                        {
                            fleisskappa = Statistics.FleissKappa(convertedlists, restclass);
                            kappa = fleisskappa;
                            kappatype = "Fleiss' κ: ";
                        }
                    }

                    //Landis and Koch (1977)
                    if (kappa <= 0) interpretation = "Poor agreement";
                    else if (kappa >= 0.01 && kappa < 0.21) interpretation = "Slight agreement";
                    else if (kappa >= 0.21 && kappa < 0.41) interpretation = "Fair agreement";
                    else if (kappa >= 0.41 && kappa < 0.61) interpretation = "Moderate agreement";
                    else if (kappa >= 0.61 && kappa < 0.81) interpretation = "Substantial agreement";
                    else if (kappa >= 0.81 && kappa < 1.00) interpretation = "Almost perfect agreement";
                    else if (kappa >= 1.0) interpretation = "Perfect agreement";
                }, token);

                Action EmptyDelegate = delegate () { };

                Stats.Content = kappatype + kappa.ToString("F3") + ": " + interpretation;

                this.UpdateLayout();
                this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            }
            else
            {
                Stats.Content = defaultlabeltext;
            }
        }

        private async Task CalculateContinuousWrapper(List<AnnoList> annolists)
        {
            double cronbachalpha = 0;
            string interpretation = "";
            if (annolists.Count > 1)
            {
                CancellationToken token = new CancellationToken();

                await Task.Run(() =>
                {
                    lock (syncLock)
                    {
                        cronbachalpha = Statistics.Cronbachsalpha(annolists);
                    }

                    if (cronbachalpha < 0) cronbachalpha = 0.0; //can happen that it gets a little below 0, this is to avoid confusion.

                    interpretation = Statistics.Cronbachinterpretation(cronbachalpha);
                }, token);

                Stats.Content = "Samples: " + annolists[0].Count;
                Stats.ToolTip = "Samples: " + annolists[0].Count;

                Action EmptyDelegate = delegate () { };
                Stats.Content = Stats.Content + " | Cronbach's α: " + cronbachalpha.ToString("F3");
                Stats.ToolTip = Stats.ToolTip + " | Cronbach's α: " + interpretation;

                double spearmancorrelation = double.MaxValue;
                if (annolists.Count == 2)
                {
                    spearmancorrelation = Statistics.SpearmanCorrelationMathNet(annolists[0], annolists[1]);

                    if (spearmancorrelation != double.MaxValue)
                    {
                        interpretation = Statistics.Spearmaninterpretation(spearmancorrelation);

                        Stats.Content = Stats.Content + " | Spearman Correlation: " + spearmancorrelation.ToString("F3");
                        Stats.ToolTip = Stats.ToolTip + " | Spearman Correlation: " + interpretation;
                    }

                    double concordancecorrelation = double.MaxValue;
                    concordancecorrelation = Statistics.ConcordanceCorrelationCoefficient(annolists[0], annolists[1]);

                    if (concordancecorrelation != double.MaxValue){

                        interpretation = Statistics.CCCinterpretation(concordancecorrelation);

                        Stats.Content = Stats.Content + " | Concordance Correlation: " + concordancecorrelation.ToString("F3");
                        Stats.ToolTip = Stats.ToolTip + " | Concordance Correlation: " + interpretation;
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

                        if(ra > rb)
                        {
                            testp = 1 - testp;
                        }

                        var testtwopvalue = 2 * testp;



                        Stats.Content = Stats.Content + " | Pearson Correlation r: " + pearsoncorrelation.ToString("F3") + " | Fisher z-score: " + fisherz.ToString("F3") +  " | Two-tailed p value: " + twopvalue.ToString("F3");;
                        Stats.ToolTip = Stats.ToolTip + " | Pearson Correlation r: " + interpretation + " | Fisher z-score: " + fisherz.ToString("F3");
                    }

                    double nmse = double.MaxValue;
                    double mse = double.MaxValue;

                    nmse = Statistics.MSE(annolists, true);
                    mse = Statistics.MSE(annolists, false);

                    if (nmse != double.MaxValue)
                    {
                        Stats.Content = Stats.Content + " | MSE: " + mse.ToString("F6") + " | NMSE: " + nmse.ToString("F6"); ;
                    }


                }

                this.UpdateLayout();
                this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            }
            else
            {
                Stats.Content = defaultlabeltext;
            }
        }

 

        private async Task CalculateRMSEWrapper(List<AnnoList> annolists)
        {
            if (annolists.Count == 2)
            {
                CancellationToken token = new CancellationToken();
                double mse = 0.0;
                double rmsd = 0.0;
                double sum_sq = 0;
                double minerr = double.MaxValue;
                double maxerr = 0.0;

                await Task.Run(() =>
                {
                    lock (syncLock)
                    {
                        if (annolists.Count == 2)
                        {
                            int N = int.MaxValue;
                            foreach (AnnoList a in annolists)
                            {
                                if (a.Count < N) N = a.Count;
                            }

                            for (int i = 0; i < N; i++)
                            {
                                double err = annolists[0][i].Score - annolists[1][i].Score;
                                if (err > maxerr) maxerr = err;
                                if (err < minerr) minerr = err;
                                sum_sq += (err * err);
                            }

                            mse = sum_sq / N;
                            rmsd = Math.Sqrt(mse);
                        }
                    }
                }, token);

                double nrmsd = rmsd / (maxerr - minerr);
                Action EmptyDelegate = delegate () { };
                Stats.Content += "  RMSE:  " + rmsd.ToString("F4") + " (NRMSE : " + nrmsd.ToString("F4") + ")";
                this.UpdateLayout();
                this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            }
        }

        private void CalculateMergeDiscrete_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationResultBox.SelectedItems.Count > 1)
            {
                string restclass = "Rest";
                List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
                List<AnnoList> convertedlists = Statistics.convertAnnoListsToMatrix(annolists, restclass);

                if (WeightExpertise.IsChecked == true) //some option
                {
                    List<AnnoList> multial = multiplyAnnoListsbyExpertise(convertedlists);
                    MergeDiscreteLists(multial, restclass);
                }
                else
                {
                    MergeDiscreteLists(convertedlists, restclass);
                }
            }
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
            GetAnnotators();
            GetSessions();
            GetAnnotations();
        }

        public void GetAnnotators()
        {
            AnnotatorsBox.Items.Clear();

            foreach (DatabaseAnnotator annotator in DatabaseHandler.Annotators)
            {
                AnnotatorsBox.Items.Add(annotator.FullName);
            }

            if (AnnotatorsBox.Items.Count > 0)
            {
                if (AnnotatorsBox.SelectedItem == null)
                {
                    AnnotatorsBox.SelectedIndex = 0;
                }

                AnnotatorsBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotator;
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;
            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            copyAnnotation(al);
        }

        private void calculateStatistics()
        {
            List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            if (selectedisContinuous)
            {
                CalculateContinuousWrapper(annolists);
            }
            else
            {
                CalculateKappaWrapper(annolists);
            };
        }

        private void Stats_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            calculateStatistics();
        }

 
    }
}
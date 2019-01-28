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
using Python.Runtime;
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

        public void GetAnnotations(bool onlyme = false)

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

            ObjectId roleid = new ObjectId();
            BsonDocument result2 = new BsonDocument();
            if (RolesBox.SelectedValue != null)
            {
                var filterat = builder.Eq("name", RolesBox.SelectedValue.ToString());
                result2 = roles.Find(filterat).Single();
                if (result2.ElementCount > 0) roleid = result2.GetValue(0).AsObjectId;
            }

            if (SessionsResultsBox.SelectedItem == null) SessionsResultsBox.SelectedIndex = 0;

            DatabaseSession session = SessionsResultsBox.SelectedItems.Count > 0 ? (DatabaseSession)SessionsResultsBox.SelectedItem : DatabaseHandler.Sessions[0];
            ;

            ObjectId sessionid = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Sessions, "name", session.Name);
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
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = session.Name });
                }
                else if (result.ElementCount > 0 && result2.ElementCount == 0 && anno["scheme_id"].AsObjectId == schemeid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = session.Name });
                }
                }
            }

            AnnotationResultBox.ItemsSource = items;
        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var annosch = annoschemes.Find(_ => true).ToList();

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

        private double normalizermvalue(double value, AnnoScheme scheme)
        {
            if (scheme.MinScore >= 0)
            {
                double norm = (value - scheme.MinScore) / (scheme.MaxScore - scheme.MinScore);
                value = norm * 2 - 1;
            }

            return value;
        }

        private double denormalize(double value, AnnoScheme scheme)
        {
            double norm = value / 2 + 1;

            double result = norm * (scheme.MaxScore - scheme.MinScore) + scheme.MinScore;

            return value;
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
            foreach (AnnoList a in al)
            {
                for (int i = 0; i < minSize; i++)
                {
                    array[i] = array[i] + a[i].Score;
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
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

        public double MSE(List<AnnoList> al, bool normalize)
        {
            double mse = 0;
            double nsm = 0;
            if (AnnotationResultBox.SelectedItems.Count == 2)
            {
                double sum_sq = 0;
                double minerr = double.MaxValue;
                double maxerr = 0.0;

                double[] array = new double[al[0].Count];
                double length = GetAnnoListMinLength(al);
                // if (al[0].Meta.Annotator != null && al[0].Meta.Annotator.Contains("RootMeanSquare"))
                {
                    for (int i = 0; i < length; i++)
                    {
                        double err = al[0][i].Score - al[1][i].Score;
                        if (err > maxerr) maxerr = err;
                        if (err < minerr) minerr = err;
                        sum_sq += err * err;
                    }
                    mse = (double)sum_sq / (al[0].Count);
                    nsm = mse / (maxerr - minerr);
                    //  MessageBox.Show("The Mean Square Error for Annotation " + al[1].Scheme.Name + " is " + mse + " (Normalized: " + mse / (maxerr - minerr) + "). This is for your information only, no new tier has been created!");
                }
            }

            if (normalize) return nsm;
            else return mse;
        }


        public Dictionary<string, double> PythonInterface(List<AnnoList> al)
        {

            Dictionary<string, double> measures = InvokePython.correlate(al[0], al[1]);

            return measures;

        }


        private List<AnnoList> convertAnnoListsToMatrix(List<AnnoList> annolists, string restclass)
        {
            List<AnnoList> convertedlists = new List<AnnoList>();

            double maxlength = GetAnnoListMinLength(annolists);
            double chunksize = Properties.Settings.Default.DefaultMinSegmentSize; //Todo make option

            foreach (AnnoList al in annolists)
            {
                AnnoList list = ConvertDiscreteAnnoListToContinuousList(al, chunksize, maxlength, restclass);
                convertedlists.Add(list);
            }

            return convertedlists;
        }

        private double GetAnnoListMinLength(List<AnnoList> annolists)
        {
            double length = 0;
            foreach (AnnoList al in annolists)
            {
                if (al.Count > 0 && al.ElementAt(al.Count - 1).Stop > length) length = al.ElementAt(al.Count - 1).Stop;
            }

            return length;
        }

        private AnnoList ConvertDiscreteAnnoListToContinuousList(AnnoList annolist, double chunksize, double end, string restclass = "Rest")
        {
            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;
            result.Source.StoreToDatabase = true;
            result.Source.Database.Session = annolist.Source.Database.Session;
            double currentpos = 0;

            bool foundlabel = false;

            while (currentpos < end)
            {
                foundlabel = false;
                foreach (AnnoListItem orgitem in annolist)
                {
                    if (orgitem.Start < currentpos && orgitem.Stop > currentpos)
                    {
                        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                        result.Add(ali);
                        foundlabel = true;
                        break;
                    }
                }

                if (foundlabel == false)
                {
                    AnnoListItem ali = new AnnoListItem(currentpos, chunksize, restclass);
                    result.Add(ali);
                }

                currentpos = currentpos + chunksize;
            }

            return result;
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

        private double FleissKappa(List<AnnoList> annolists, string restclass)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists

            List<AnnoScheme.Label> classes = annolists[0].Scheme.Labels;
            AnnoScheme.Label rest = new AnnoScheme.Label(restclass, Colors.Black);
            classes.Add(rest);

            int k = 0;  //k = number of classes

            //For Discrete Annotations find number of classes, todo, find number of classes on free annotations.
            if (annolists[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                k = classes.Count;
            }

            int N = annolists[0].Count;

            double[] pj = new double[k];
            double[] Pi = new double[N];

            int dim = n * N;

            //add  and initalize matrix
            int[,] matrix = new int[N, k];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    matrix[i, j] = 0;
                }
            }

            //fill the matrix

            foreach (AnnoList al in annolists)
            {
                int count = 0;
                foreach (AnnoListItem ali in al)
                {
                    for (int i = 0; i < classes.Count; i++)
                    {
                        if (ali.Label == classes[i].Name)
                        {
                            matrix[count, i] = matrix[count, i] + 1;
                        }
                    }

                    count++;
                }
            }

            //calculate pj
            for (int j = 0; j < k; j++)
            {
                for (int i = 0; i < N; i++)
                {
                    pj[j] = pj[j] + matrix[i, j];
                }

                pj[j] = pj[j] / dim;
            }

            //Calculate Pi

            for (int i = 0; i < N; i++)
            {
                double sum = 0;
                for (int j = 0; j < k; j++)
                {
                    sum = sum + (Math.Pow(matrix[i, j], 2.0) - matrix[i, j]);
                }

                Pi[i] = (1.0 / (n * (n - 1.0))) * (sum);
            }

            //calculate Pd
            double Pd = 0;

            for (int i = 0; i < N; i++)
            {
                Pd = Pd + Pi[i];
            }

            Pd = (1.0 / (((double)N) * (n * n - 1.0))) * (Pd * (n * n - 1.0));

            double Pe = 0;

            for (int i = 0; i < k; i++)
            {
                Pe = Pe + Math.Pow(pj[i], 2.0);
            }

            double fleiss_kappa = 0.0;

            fleiss_kappa = (Pd - Pe) / (1.0 - Pe);

            return fleiss_kappa;

            //todo recheck the formula.
        }

        public double CohensKappa(List<AnnoList> annolists, string restclass)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists

            List<AnnoScheme.Label> classes = annolists[0].Scheme.Labels;
            //add the restclass we introduced in last step.
            AnnoScheme.Label rest = new AnnoScheme.Label(restclass, System.Windows.Media.Colors.Black);
            classes.Add(rest);

            int k = 0;  //k = number of classes
            //For Discrete Annotations find number of classes, todo, find number of classes on free annotations.
            if (annolists[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                k = classes.Count;
            }

            int N = int.MaxValue;

            foreach (AnnoList a in annolists)
            {
                if (a.Count < N) N = a.Count;
            }

            double[] pj = new double[k];
            double[] Pi = new double[N];

            int dim = n * N;

            //add  and initalize matrix
            int[,] matrix = new int[N, k];

            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < k; j++)
                {
                    matrix[i, j] = 0;
                }
            }

            //fill the matrix

            foreach (AnnoList al in annolists)
            {
                int count = 0;
                foreach (AnnoListItem ali in al)
                {
                    for (int i = 0; i < classes.Count; i++)
                    {
                        if (ali.Label == classes[i].Name)
                        {
                            matrix[count, i] = matrix[count, i] + 1;
                        }
                    }

                    count++;
                }
            }

            //calculate pj
            for (int j = 0; j < k; j++)
            {
                for (int i = 0; i < N; i++)
                {
                    pj[j] = pj[j] + matrix[i, j];
                }

                pj[j] = pj[j] / dim;
            }

            //here it differs from fleiss' kappa

            //Calculate Pi

            for (int i = 0; i < N; i++)
            {
                double sum = 0;
                for (int j = 0; j < k; j++)
                {
                    sum = sum + (Math.Pow(matrix[i, j], 2.0) - matrix[i, j]);
                }

                Pi[i] = (sum / (n * (n - 1.0)));
            }

            //calculate Pd
            double Pd = 0;

            for (int i = 0; i < N; i++)
            {
                Pd = Pd + Pi[i];
            }

            Pd = (1.0 / (N * n * (n - 1.0))) * Pd * n * (n - 1);

            double Pc = 0;

            for (int i = 0; i < k; i++)
            {
                Pc = Pc + Math.Pow(pj[i], 2.0);
            }

            double cohens_kappa = 0.0;

            cohens_kappa = (Pd - Pc) / (1.0 - Pc);

            return cohens_kappa;
        }

        private double Cronbachsalpha(List<AnnoList> annolists, int decimals)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists

            int N = int.MaxValue;

            foreach (AnnoList a in annolists)
            {
                if (a.Count < N) N = a.Count;
            }

            double[] varj = new double[n];
            double[] vari = new double[N];

            double[][] data = new double[n][];

            for (int i = 0; i < n; i++)
            {
                double[] row = new double[N];
                for (int j = 0; j < N; j++)
                {
                    double inputValue = annolists[i][j].Score;
                    row[j] = Math.Round(inputValue, decimals);
                }

                data[i] = row;
            }

            Matrix<double> CorrelationMatrix = SpearmanCorrelationMatrix(data);

            Matrix<double> uppertriangle = CorrelationMatrix.UpperTriangle();

            double rvec = 0.0;

            for (int i = 0; i < CorrelationMatrix.ColumnCount; i++)
            {
                for (int j = 0; j < CorrelationMatrix.RowCount; j++)
                {
                    rvec += uppertriangle[i, j];
                }
            }

            double factor = (n * (n - 1)) / 2.0;

            rvec = (rvec - (double)n) / factor;

            double alpha = (n * rvec) / (1 + (n - 1) * rvec);

            return alpha;
        }

        #region Helper Functions

        private MathNet.Numerics.LinearAlgebra.Matrix<double> SpearmanCorrelationMatrix(double[][] data)
        {
            var m = MathNet.Numerics.LinearAlgebra.Matrix<double>.Build.DenseIdentity(data.Length);
            for (int i = 0; i < data.Length; i++)
                for (int j = i + 1; j < data.Length; j++)
                {
                    var c = Correlation.Spearman(data[i], data[j]);
                    m.At(i, j, c);
                    m.At(j, i, c);
                }
            return m;
        }

        private double PearsonCorrelation(AnnoList xs, AnnoList ys)
        {
            // sums of x, y, x squared etc.
            double sx = 0.0;
            double sy = 0.0;
            double sxx = 0.0;
            double syy = 0.0;
            double sxy = 0.0;

            int n = 0;

            using (var enX = xs.GetEnumerator())
            {
                using (var enY = ys.GetEnumerator())
                {
                    while (enX.MoveNext() && enY.MoveNext())
                    {
                        if (!double.IsNaN(enX.Current.Score) && !double.IsNaN(enX.Current.Score))
                        {
                            double x = enX.Current.Score;
                            double y = enY.Current.Score;

                            n += 1;
                            sx += x;
                            sy += y;
                            sxx += x * x;
                            syy += y * y;
                            sxy += x * y;
                        }
                    }
                }
            }

            // covariation
            double cov = sxy / n - sx * sy / n / n;
            // standard error of x
            double sigmaX = Math.Sqrt(sxx / n - sx * sx / n / n);
            // standard error of y
            double sigmaY = Math.Sqrt(syy / n - sy * sy / n / n);

            // correlation is just a normalized covariation
            return cov / sigmaX / sigmaY;
        }

        private double PearsonCorrelationMathNet(AnnoList xs, AnnoList ys)
        {
            int N = ys.Count;
            if (xs.Count < ys.Count) N = xs.Count;

            double[] list1 = new double[N];
            double[] list2 = new double[N];

 
            for (int i = 0; i < N; i++)
            {
                if(double.IsNaN(xs[i].Score))
                {
                    if (i > 0) xs[i].Score = xs[i - 1].Score;
                    else xs[i].Score = ((xs.Scheme.MaxScore - xs.Scheme.MinScore) / 2.0);
                }
                if (double.IsNaN(ys[i].Score))
                {
                    if (i > 0) ys[i].Score = ys[i - 1].Score;
                    else ys[i].Score = ((ys.Scheme.MaxScore - ys.Scheme.MinScore) / 2.0);
                }

                list1[i] = xs[i].Score;
                list2[i] = ys[i].Score;
            }

            double mean = Math.Abs(list1.Mean() - list2.Mean());

             double p = MathNet.Numerics.ExcelFunctions.TDist(mean, list1.Count(), 2);





            double r = Correlation.Pearson(list1, list2);
            return r;
        }

        private double SpearmanCorrelationMathNet(AnnoList xs, AnnoList ys)
        {
            int N = ys.Count;
            if (xs.Count < ys.Count) N = xs.Count;

            double[] list1 = new double[N];
            double[] list2 = new double[N];

            for (int i = 0; i < N; i++)
            {
                list1[i] = xs[i].Score;
                list2[i] = ys[i].Score;
            }

            double r = Correlation.Spearman(list1, list2);
            return r;
        }

        private double Variance(double[] nums)
        {
            if (nums.Length > 1)
            {
                double avg = Average(nums);

                double sumofSquares = 0.0;

                foreach (double num in nums)
                {
                    sumofSquares += Math.Pow(num - avg, 2.0);
                }

                return sumofSquares / (double)(nums.Length - 1);
            }
            else return 0.0;
        }

        private double Sum(double[] nums)
        {
            double sum = 0;
            foreach (double num in nums)
            {
                sum += num;
            }
            return sum;
        }

        private double Average(double[] nums)
        {
            double sum = 0;

            if (nums.Length > 1)
            {
                foreach (double num in nums)
                {
                    sum += num;
                }
                return sum / (double)nums.Length;
            }
            else return (double)nums[0];
        }

        private double StandardDeviation(double variance)
        {
            return Math.Sqrt(variance);
        }

        #endregion Helper Functions

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

                        List<AnnoList> convertedlists = convertAnnoListsToMatrix(annolists, restclass);

                        if (annolists.Count == 2)
                        {
                            cohenkappa = CohensKappa(convertedlists, restclass);
                            kappa = cohenkappa;
                            kappatype = "Cohen's κ: ";
                        }
                        else if (annolists.Count > 2)
                        {
                            fleisskappa = FleissKappa(convertedlists, restclass);
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


        private double transform_r_to_z(double r, int N)
        {

            double z = 0.5 * (Math.Log(1 + r) - Math.Log(1-r));
            return z;
        }


        private double transform_2r_to_z (double r1, double N1, double r2, double N2)
        {

            double leftlog = (1.0 + r1) / (1.0 - r1);
            double rightlog = (1.0 + r2) / (1.0 - r2);
            double upperleftpart = 0.5 *  Math.Log(leftlog);
            double upperrightpart = 0.5 * Math.Log(rightlog);
            double upperpart = upperleftpart - upperrightpart;

            double lowerinnerleftpart = 1.0 / ((double)N1 - 3.0);
            double lowerinnerrightpart =  1.0 / ((double)N2 - 3.0);

            double lowerinnerpart = lowerinnerleftpart + lowerinnerrightpart;
            double lowerpart = Math.Sqrt(lowerinnerpart);
            return upperpart / lowerpart;
        }



        private double transform_z_to_p(double z)
        {
            double Z_MAX = 6;
            double y, x, w;
            if (z == 0.0)
            {
                x = 0.0;
            }
            else
            {
                y = 0.5 * Math.Abs(z);
                if (y > (Z_MAX * 0.5))
                {
                    x = 1.0;
                }
                else if (y < 1.0)
                {
                    w = y * y;
                    x = ((((((((0.000124818987 * w
                        - 0.001075204047) * w + 0.005198775019) * w
                        - 0.019198292004) * w + 0.059054035642) * w
                        - 0.151968751364) * w + 0.319152932694) * w
                        - 0.531923007300) * w + 0.797884560593) * y * 2.0;
                }
                else
                {
                    y -= 2.0;
                    x = (((((((((((((-0.000045255659 * y
                        + 0.000152529290) * y - 0.000019538132) * y
                        - 0.000676904986) * y + 0.001390604284) * y
                        - 0.000794620820) * y - 0.002034254874) * y
                        + 0.006549791214) * y - 0.010557625006) * y
                        + 0.011630447319) * y - 0.009279453341) * y
                        + 0.005353579108) * y - 0.002141268741) * y
                        + 0.000535310849) * y + 0.999936657524;
                }
            }
            return z > 0.0 ? ((x + 1.0) * 0.5) : ((1.0 - x) * 0.5);
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
                        cronbachalpha = Cronbachsalpha(annolists, 3);
                    }

                    if (cronbachalpha < 0) cronbachalpha = 0.0; //can happen that it gets a little below 0, this is to avoid confusion.

                    interpretation = Cronbachinterpretation(cronbachalpha);
                }, token);

                Stats.Content = "Samples: " + annolists[0].Count;
                Stats.ToolTip = "Samples: " + annolists[0].Count;

                Action EmptyDelegate = delegate () { };
                Stats.Content = Stats.Content + " | Cronbach's α: " + cronbachalpha.ToString("F3");
                Stats.ToolTip = Stats.ToolTip + " | Cronbach's α: " + interpretation;

                double spearmancorrelation = double.MaxValue;
                if (annolists.Count == 2)
                {
                    spearmancorrelation = SpearmanCorrelationMathNet(annolists[0], annolists[1]);

                    if (spearmancorrelation != double.MaxValue)
                    {
                        interpretation = Spearmaninterpretation(spearmancorrelation);

                        Stats.Content = Stats.Content + " | Spearman Correlation: " + spearmancorrelation.ToString("F3");
                        Stats.ToolTip = Stats.ToolTip + " | Spearman Correlation: " + interpretation;
                    }

                    double pearsoncorrelation = double.MaxValue;

                    pearsoncorrelation = PearsonCorrelationMathNet(annolists[0], annolists[1]);

                    int N = annolists[0].Count;
                    if (annolists[1].Count < annolists[0].Count) N = annolists[1].Count;

                    interpretation = Pearsoninterpretation(pearsoncorrelation, N);
                    var fisherz = transform_r_to_z(pearsoncorrelation, N);
                    var lpvalue = transform_z_to_p(fisherz);
                    var rpvalue = 1 - lpvalue;
                    var twopvalue = 2 * rpvalue;
                    var confpvalue = 1 - twopvalue;

                    if (pearsoncorrelation != double.MaxValue)
                    {

                        double ra = 0.794;
                        double rb = 0.8;
                        var testz = transform_2r_to_z(ra, N, rb, N);
                        var testp = transform_z_to_p(testz);

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

                    nmse = MSE(annolists, true);
                    mse = MSE(annolists, false);

                    if (nmse != double.MaxValue)
                    {
                        Stats.Content = Stats.Content + " | MSE: " + mse.ToString("F6") + " | NMSE: " + nmse.ToString("F6"); ;
                    }

                    //PythonInterface(annolists);

                }

                this.UpdateLayout();
                this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            }
            else
            {
                Stats.Content = defaultlabeltext;
            }
        }

        private string Pearsoninterpretation(double r, int N)
        {
            string interpretation = "";
            if (r <= -1) interpretation = "perfect downhill (negative) linear relationship";
            else if (r > -1 && r <= -0.7) interpretation = "strong downhill (negative) linear relationship";
            else if (r > -0.7 && r <= -0.5) interpretation = "moderate downhill (negative) relationship";
            else if (r > -0.5 && r <= -0.3) interpretation = "weak downhill (negative) linear relationship";
            else if (r > -0.3 && r <= 0.3) interpretation = "no linear relationship";
            else if (r > 0.3 && r <= 0.5) interpretation = "weak uphill (positive) linear relationship";
            else if (r > 0.5 && r <= 0.7) interpretation = "moderate uphill (positive) relationship";
            else if (r > 0.7 && r < 1) interpretation = "strong uphill (positive) linear relationship";
            else if (r >= 1.0) interpretation = "perfect uphill (positive) linear relationship";

            //double t = r / (Math.Sqrt((1 - (r * r)) / (N - 2)));





            //double t = r * Math.Sqrt(N-2) / (Math.Sqrt((1 - (r * r))));

            //Chart Chart1 = new Chart();
            //double p = Chart1.DataManipulator.Statistics.TDistribution(t, N - 2, false);
            //string significance = "";


            //if (p < 0.05) significance = p.ToString("F6") + "< 0.05" ;
            //else  significance = p.ToString("F6") + ">= 0.05";

            return interpretation; // + " " + significance;
        }

        private string Spearmaninterpretation(double spearmancorrelation)
        {
            string interpretation = "";
            if (spearmancorrelation <= 0.19) interpretation = "Very week";
            else if (spearmancorrelation >= 0.20 && spearmancorrelation < 0.39) interpretation = "Weak";
            else if (spearmancorrelation >= 0.40 && spearmancorrelation < 0.59) interpretation = "Moderate";
            else if (spearmancorrelation >= 0.60 && spearmancorrelation < 0.79) interpretation = "Strong";
            else if (spearmancorrelation >= 0.8) interpretation = "Very strong";

            return interpretation;
        }

        private string Cronbachinterpretation(double cronbachalpha)
        {
            string interpretation = "";
            if (cronbachalpha <= 0.5) interpretation = "Unacceptable agreement";
            else if (cronbachalpha >= 0.51 && cronbachalpha < 0.61) interpretation = "Poor agreement";
            else if (cronbachalpha >= 0.61 && cronbachalpha < 0.71) interpretation = "Questionable agreement";
            else if (cronbachalpha >= 0.71 && cronbachalpha < 0.81) interpretation = "Acceptable agreement";
            else if (cronbachalpha >= 0.81 && cronbachalpha < 0.90) interpretation = "Good agreement";
            else if (cronbachalpha >= 0.9) interpretation = "Excellent agreement";

            return interpretation;
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
                List<AnnoList> convertedlists = convertAnnoListsToMatrix(annolists, restclass);

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
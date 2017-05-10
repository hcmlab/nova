using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoMergeWindow : System.Windows.Window
    {
        private MongoClient mongo;
        private IMongoDatabase database;
        private int authlevel = 0;
        private AnnoList mean = null;
        private AnnoList rms = null;
        private AnnoList merge = null;

        public DatabaseAnnoMergeWindow()
        {
            InitializeComponent();
            ConnecttoDB();
        }

        private void ConnecttoDB()
        {
            try
            {
                mongo = DatabaseHandler.Client;
                int count = 0;
                while (mongo.Cluster.Description.State.ToString() == "Disconnected")
                {
                    Thread.Sleep(100);
                    if (count++ >= 25) throw new MongoException("Unable to connect to the database. Please make sure that " + mongo.Settings.Server.Host + " is online and you entered your credentials correctly!");
                }

                authlevel = DatabaseHandler.CheckAuthentication();

                if (authlevel > 2)
                {
                    GetSessions();
                }
                else
                {
                    MessageBox.Show("Sorry, you are not authorized on the database to perform this step!");
                    this.Close();
                }
            }
            catch (MongoException e)

            {
                MessageBox.Show(e.Message);
                mongo.Cluster.Dispose();
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnecttoDB();
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(SessionsResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();

                GetAnnotationSchemes();
                // GetRoles();

                //     GetAnnotations();
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

        public System.Collections.IList Annotations()
        {
            if (AnnotationResultBox.SelectedItems != null)
                return AnnotationResultBox.SelectedItems;
            else return null;
        }

        public void GetSessions()

        {
            database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);

            var sessioncollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var sessions = sessioncollection.Find(_ => true).ToList();

            if (sessions.Count > 0)
            {
                if (SessionsResultsBox.Items != null) SessionsResultsBox.Items.Clear();
                List<DatabaseSession> items = new List<DatabaseSession>();
                foreach (var c in sessions)
                {
                    //CollectionResultsBox.Items.Add(c.GetElement(1).Value.ToString());
                    items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].ToUniversalTime(), OID = c["_id"].AsObjectId });
                }

                SessionsResultsBox.ItemsSource = items;
            }
            else SessionsResultsBox.ItemsSource = null;
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

            database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);
            var sessions = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationschemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var roles = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);

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
                    handleButtons(false);
                }
                else
                {
                    handleButtons(true);
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

            DatabaseSession session = (DatabaseSession) SessionsResultsBox.SelectedItem;
            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), DatabaseDefinitionCollections.Sessions, "name", session.Name);
            var filter = builder.Eq("session_id", sessionid);
            var annos = annotations.Find(filter).ToList();

            foreach (var anno in annos)
            {
                var filtera = builder.Eq("_id", anno["role_id"]);
                var roledb = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles).Find(filtera).Single();
                string rolename = roledb.GetValue(1).ToString();

                var filterb = builder.Eq("_id", anno["scheme_id"]);
                var annotdb = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes).Find(filterb).Single();
                string annoschemename = annotdb.GetValue(1).ToString();
                string type = annotdb.GetValue(2).ToString();

                var filterc = builder.Eq("_id", anno["annotator_id"]);
                var annotatdb = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filterc).Single();
                string annotatorname = annotatdb.GetValue(1).ToString();
                string annotatornamefull = annotatdb.GetValue(2).ToString();

                if (result.ElementCount > 0 && result2.ElementCount > 0 && anno["scheme_id"].AsObjectId == schemeid && anno["role_id"].AsObjectId == roleid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session=  session.Name});
                }
                //else if (result.ElementCount == 0 && result2.ElementCount > 0 && annos["role_id"].AsObjectId == roleid)
                //{
                //    items.Add(new DatabaseAnno() { Role = rolename, AnnoScheme.AnnoType = annoschemename, Annotator = annotatorname });
                //}
                else if (result.ElementCount > 0 && result2.ElementCount == 0 && anno["scheme_id"].AsObjectId == schemeid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = session.Name });
                }
            }

            AnnotationResultBox.ItemsSource = items;
        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
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
            var rolesdb = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
            var roles = rolesdb.Find(_ => true).ToList();

            if (roles.Count > 0)
            {
                if (RolesBox.Items != null) RolesBox.Items.Clear();

                foreach (var c in roles)
                {
                    RolesBox.Items.Add(c["name"]);
                }
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        private AnnoList rootMeanSquare(List<AnnoList> al)
        {
            int numberoftracks = AnnotationResultBox.SelectedItems.Count;

            AnnoList merge = al[0];
            merge.Meta.Annotator = "RootMeanSquare";
            merge.Meta.AnnotatorFullName = "RootMeanSquare";

            double[] array = new double[al[0].Count];

            foreach (AnnoList a in al)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    array[i] = array[i] + double.Parse(a[i].Label) * double.Parse(a[i].Label);
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                merge[i].Label = System.Math.Sqrt(array[i] / numberoftracks).ToString();
            }
            merge.Scheme.SampleRate = 1 / (merge[0].Stop - merge[0].Start);
            MessageBox.Show("Median of all Annotations has been calculated");
            Ok.IsEnabled = true;

            return merge;
        }

        private AnnoList calculateMean(List<AnnoList> al)
        {
            int numberoftracks = AnnotationResultBox.SelectedItems.Count;

            AnnoList merge = al[0];
            merge.Meta.Annotator = "Mean";
            merge.Meta.AnnotatorFullName = "Mean";

            double[] array = new double[al[0].Count];
            foreach (AnnoList a in al)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    array[i] = array[i] + double.Parse(a[i].Label);
                }
            }

            for (int i = 0; i < array.Length; i++)
            {
                merge[i].Label = (array[i] / numberoftracks).ToString();
            }
            merge.Scheme.SampleRate = 1 / (merge[0].Stop - merge[0].Start);
            MessageBox.Show("Mean values of all Annotations have been calculated");
            Ok.IsEnabled = true;
            return merge;
        }

        public AnnoList Mean()
        {
            return mean;
        }

        public AnnoList RMS()
        {
            return rms;
        }

        public AnnoList Merge()
        {
            return merge;
        }

        private void handleButtons(bool discrete)
        {
            if (discrete)
            {
                CalculateMedian.IsEnabled = false;
                CalculateRMS.IsEnabled = false;
                CalculateRMSE.IsEnabled = false;
                CalculateFleissKappa.IsEnabled = true;
                CalculateCohenKappa.IsEnabled = true;
                CalculateCronbach.IsEnabled = false;
                CalculateMergeDiscrete.IsEnabled = true;
            }
            else
            {
                CalculateMedian.IsEnabled = true;
                CalculateRMS.IsEnabled = true;
                CalculateRMSE.IsEnabled = true;
                CalculateFleissKappa.IsEnabled = false;
                CalculateCohenKappa.IsEnabled = false;
                CalculateCronbach.IsEnabled = true;
                CalculateMergeDiscrete.IsEnabled = false;
            }
        }

        private void RMSE(List<AnnoList> al)
        {
            if (AnnotationResultBox.SelectedItems.Count == 2)
            {
                double sum_sq = 0;
                double mse;
                double minerr = double.MaxValue;
                double maxerr = 0.0;

                double[] array = new double[al[0].Count];

                // if (al[0].Meta.Annotator != null && al[0].Meta.Annotator.Contains("RootMeanSquare"))
                {
                    for (int i = 0; i < al[0].Count; i++)
                    {
                        double err = double.Parse(al[0][i].Label) - double.Parse(al[1][i].Label);
                        if (err > maxerr) maxerr = err;
                        if (err < minerr) minerr = err;
                        sum_sq += err * err;
                    }
                    mse = (double)sum_sq / (al[0].Count);
                    MessageBox.Show("The Mean Square Error for Annotation " + al[1].Scheme.Name + " is " + mse + " (Normalized: " + mse / (maxerr - minerr) + "). This is for your information only, no new tier has been created!");
                }
                //else if (al[1].Meta.Annotator != null && al[1].Meta.Annotator.Contains("RootMeanSquare"))
                //{
                //    for (int i = 0; i < al[0].Count; i++)
                //    {
                //        double err = double.Parse(al[1][i].Label) - double.Parse(al[0][i].Label);
                //        if (err > maxerr) maxerr = err;
                //        if (err < minerr) minerr = err;
                //        sum_sq += err * err;
                //    }
                //    mse = (double)sum_sq / (al[1].Count);
                //    MessageBox.Show("The Mean Square Error for Annotation " + al[0].Scheme.Name + " is " + mse + " (Normalized: " + mse / (maxerr - minerr) + "). This is for your information only, no new tier has been created!");
                //}
                //else MessageBox.Show("Select RMS Annotation and Reference Annotation. If RMS Annotation is not present, please create it first.");
            }
            else MessageBox.Show("Select RMS Annotation and ONE Reference Annotation. If RMS Annotation is not present, please create it first.");
        }

        private List<AnnoList> convertAnnoListsToMatrix(string restclass)
        {
            List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);

            List<AnnoList> convertedlists = new List<AnnoList>();

            double maxlength = GetAnnoListLength(annolists);
            double chunksize = Properties.Settings.Default.DefaultMinSegmentSize; //Todo make option

            foreach (AnnoList al in annolists)
            {
                AnnoList list = ConvertDiscreteAnnoListToContinuousList(al, chunksize, maxlength, restclass);
                convertedlists.Add(list);
            }

            return convertedlists;
        }

        private double GetAnnoListLength(List<AnnoList> annolists)
        {
            double length = 0;
            foreach (AnnoList al in annolists)
            {
                if (al.ElementAt(al.Count - 1).Stop > length) length = al.ElementAt(al.Count - 1).Stop;
            }

            return length;
        }

        private AnnoList ConvertDiscreteAnnoListToContinuousList(AnnoList annolist, double chunksize, double end, string restclass = "Rest")
        {
            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;
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

        private AnnoList MergeDiscreteLists(List<AnnoList> al, string restclass = "Rest")
        {
            AnnoList cont = new AnnoList();
            cont.Scheme = al[0].Scheme;

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

            AnnoList result = new AnnoList();
            result.Scheme = al[0].Scheme;
            result.Meta = al[0].Meta;
            result.Source = al[0].Source;
            result.Meta.Annotator = "Merge";
            result.Meta.AnnotatorFullName = "Merge";

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

                if (ali.Label != restclass) result.Add(ali);
            }
            MessageBox.Show("Annotations have been merged");
            Ok.IsEnabled = true;

            return result;
        }

        private double FleissKappa(List<AnnoList> annolists, string restclass)
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

            int N = annolists[0].Count; //Number of Subjects, here Number of Labels.

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
                    sum = sum + Math.Pow(matrix[i, j], 2.0);
                }

                sum = sum - n;

                Pi[i] = (1.0 / (n * (n - 1.0))) * (sum);
            }

            //calculate Pd
            double Pd = 0;

            for (int i = 0; i < N; i++)
            {
                Pd = Pd + Pi[i];
            }

            Pd = Pd / N;

            double Pe = 0;

            for (int i = 0; i < k; i++)
            {
                Pe = Pe + Math.Pow(pj[i], 2.0);
            }

            double fleiss_kappa = 0.0;

            fleiss_kappa = (Pd - Pe) / (1.0 - Pe);

            return fleiss_kappa;
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

            int N = annolists[0].Count; //Number of Subjects, here Number of Labels.

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

        public double Krippendorffsalpha(List<AnnoList> annolists, string restclass)
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

            int N = annolists[0].Count; //Number of Subjects, here Number of Labels.

            double[] pj = new double[k];
            double[] Pi = new double[N];

            int dim = n * N;

            double[,] matrix = new double[n, N];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    double inputValue = double.Parse(annolists[i][j].Label);
                    matrix[i, j] = inputValue;
                }
            }

            double[,] coincidencematrix = new double[N, N];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < N; j++)
                {
                }
            }

            ////add  and initalize matrix
            //int[,] matrix = new int[N, k];

            //for (int i = 0; i < N; i++)
            //{
            //    for (int j = 0; j < k; j++)
            //    {
            //        matrix[i, j] = 0;
            //    }
            //}

            ////fill the matrix

            //foreach (AnnoList al in annolists)
            //{
            //    int count = 0;
            //    foreach (AnnoListItem ali in al)
            //    {
            //        for (int i = 0; i < classes.Count; i++)
            //        {
            //            if (ali.Label == classes[i].Name)
            //            {
            //                matrix[count, i] = matrix[count, i] + 1;
            //            }
            //        }

            //        count++;
            //    }
            //}

            return 0;
        }

        private double Cronbachsalpha(List<AnnoList> annolists, int decimals)
        {
            int n = annolists.Count;   // n = number of raters, here number of annolists
            int N = annolists[0].Count; //  Number of Values.

            double[] varj = new double[n];
            double[] vari = new double[N];

            double[][] data = new double[n][];

            for (int i = 0; i < n; i++)
            {
                double[] row = new double[N];
                for (int j = 0; j < N; j++)
                {
                    double inputValue = double.Parse(annolists[i][j].Label);
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
            //return PearsonCorrelation(annolists[0], annolists[1]);
            return alpha;
        }

        private Matrix<double> SpearmanCorrelationMatrix(double[][] data)
        {
            var m = Matrix<double>.Build.DenseIdentity(data.Length);
            for (int i = 0; i < data.Length; i++)
                for (int j = i + 1; j < data.Length; j++)
                {
                    var c = Correlation.Spearman(data[i], data[j]);
                    m.At(i, j, c);
                    m.At(j, i, c);
                }
            return m;
        }

        private double PearsonCorrelation(IEnumerable<Double> xs, IEnumerable<Double> ys)
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
                        double x = enX.Current;
                        double y = enY.Current;

                        n += 1;
                        sx += x;
                        sy += y;
                        sxx += x * x;
                        syy += y * y;
                        sxy += x * y;
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

        private void CalculateMedian_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;

            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            mean = calculateMean(al);

            //todo do something
        }

        private void RolesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        private void RMS_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;

            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            rms = rootMeanSquare(al);
        }

        private void CalculateRMSE_Click(object sender, RoutedEventArgs e)
        {
            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            RMSE(al);
        }

        private void CalculateFleissKappa_Click(object sender, RoutedEventArgs e)
        {
            string restclass = "Rest";
            List<AnnoList> convertedlists = convertAnnoListsToMatrix(restclass);
            double fleisskappa = FleissKappa(convertedlists, restclass);

            //Landis and Koch (1977)
            string interpretation = "";
            if (fleisskappa < 0) interpretation = "Poor agreement";
            else if (fleisskappa >= 0.01 && fleisskappa < 0.21) interpretation = "Slight agreement";
            else if (fleisskappa >= 0.21 && fleisskappa < 0.41) interpretation = "Fair agreement";
            else if (fleisskappa >= 0.41 && fleisskappa < 0.61) interpretation = "Moderate agreement";
            else if (fleisskappa >= 0.61 && fleisskappa < 0.81) interpretation = "Substantial agreement";
            else if (fleisskappa >= 0.81 && fleisskappa < 1.00) interpretation = "Almost perfect agreement";
            else if (fleisskappa == 1.0) interpretation = "Perfect agreement";

            MessageBox.Show("Fleiss Kappa: " + fleisskappa.ToString("F3") + ": " + interpretation);
        }

        private void CalculateCohenKappa_Click(object sender, RoutedEventArgs e)
        {
            string restclass = "Rest";
            List<AnnoList> convertedlists = convertAnnoListsToMatrix(restclass);
            double cohenkappa = CohensKappa(convertedlists, restclass);

            //Landis and Koch (1977)
            string interpretation = "";
            if (cohenkappa < 0) interpretation = "Poor agreement";
            else if (cohenkappa >= 0.01 && cohenkappa < 0.21) interpretation = "Slight agreement";
            else if (cohenkappa >= 0.21 && cohenkappa < 0.41) interpretation = "Fair agreement";
            else if (cohenkappa >= 0.41 && cohenkappa < 0.61) interpretation = "Moderate agreement";
            else if (cohenkappa >= 0.61 && cohenkappa < 0.81) interpretation = "Substantial agreement";
            else if (cohenkappa >= 0.81 && cohenkappa < 1.00) interpretation = "Almost perfect agreement";
            else if (cohenkappa == 1.0) interpretation = "Perfect agreement";

            MessageBox.Show("Cohen's Kappa: " + cohenkappa.ToString("F3") + ": " + interpretation);
        }

        private void CalculateCronbach_Click(object sender, RoutedEventArgs e)
        {
            List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);

            double cronbachalpha = Cronbachsalpha(annolists, 10);

            if (cronbachalpha < 0) cronbachalpha = 0.0; //can happen that it gets a little below 0, this is to avoid confusion.

            string interpretation = "";
            if (cronbachalpha <= 0.5) interpretation = "Unacceptable agreement";
            else if (cronbachalpha >= 0.51 && cronbachalpha < 0.61) interpretation = "Poor agreement";
            else if (cronbachalpha >= 0.61 && cronbachalpha < 0.71) interpretation = "Questionable agreement";
            else if (cronbachalpha >= 0.71 && cronbachalpha < 0.81) interpretation = "Acceptable agreement";
            else if (cronbachalpha >= 0.81 && cronbachalpha < 0.90) interpretation = "Good agreement";
            else if (cronbachalpha >= 0.9) interpretation = "Excellent agreement";

            MessageBox.Show("Cronbach's alpha: " + cronbachalpha.ToString("F3") + ": " + interpretation + "\nThis feature is in beta, no warranty!");
        }

        private void CalculateMergeDiscrete_Click(object sender, RoutedEventArgs e)
        {
            string restclass = "Rest";
            List<AnnoList> convertedlists = convertAnnoListsToMatrix(restclass);

            merge = MergeDiscreteLists(convertedlists, restclass);
        }
    }
}
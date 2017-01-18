using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoMergeWindow : Window
    {
        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        private int authlevel = 0;
        private List<DatabaseMediaInfo> files = new List<DatabaseMediaInfo>();
        private List<DatabaseMediaInfo> allfiles = new List<DatabaseMediaInfo>();
        private AnnoList median;
        private AnnoList rms;

        public DatabaseAnnoMergeWindow()
        {
            InitializeComponent();

            this.db_server.Text = Properties.Settings.Default.DatabaseAddress;
            this.db_login.Text = Properties.Settings.Default.MongoDBUser;
            this.db_pass.Password = Properties.Settings.Default.MongoDBPass;
            Autologin.IsEnabled = false;

            if (Properties.Settings.Default.DatabaseAutoLogin == true)
            {
                Autologin.IsChecked = true;
            }
            else Autologin.IsChecked = false;

            if (Autologin.IsChecked == true)
            {
                ConnecttoDB();
            }
        }

        private void ConnecttoDB()
        {
            Properties.Settings.Default.DatabaseAddress = this.db_server.Text;
            Properties.Settings.Default.MongoDBUser = this.db_login.Text;
            Properties.Settings.Default.MongoDBPass = this.db_pass.Password;
            Properties.Settings.Default.Save();

            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.DatabaseAddress;

            try
            {
                mongo = new MongoClient(connectionstring);
                int count = 0;
                while (mongo.Cluster.Description.State.ToString() == "Disconnected")
                {
                    Thread.Sleep(100);
                    if (count++ >= 25) throw new MongoException("Unable to connect to the database. Please make sure that " + mongo.Settings.Server.Host + " is online and you entered your credentials correctly!");
                }

                authlevel = checkAuth(this.db_login.Text, "admin");

                if (authlevel > 0)
                {
                    SelectDatabase();
                    Autologin.IsEnabled = true;
                }
                else MessageBox.Show("You have no rights to access the database list");
                authlevel = checkAuth(this.db_login.Text, Properties.Settings.Default.DatabaseName);
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

        private void DataBasResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataBasResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.DatabaseName = DataBasResultsBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                GetSessions();
            }
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();

                GetAnnotationSchemes();
                GetRoles();

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

        private int checkAuth(string dbuser, string db = "admin")
        {
            //4 = root
            //3 = admin
            //2 = write
            //1 = read
            //0 = notauthorized

            int auth = 0;
            try
            {
                var adminDB = mongo.GetDatabase(db);
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && auth < 4) auth = 4;
                    else if (roles[i]["role"].ToString() == "dbAdminAnyDatabase" || roles[i]["role"].ToString() == "dbAdmin" && auth < 3) auth = 3;
                    else if (roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && auth < 2) auth = 2;
                    else if (roles[i]["role"].ToString() == "readAnyDatabase" || roles[i]["role"].ToString() == "read" && auth < 1) auth = 1;
                    else auth = 0;

                    //edit/add more roles if you want to change security levels
                }
            }
            catch
            {
                var adminDB = mongo.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && auth < 4) auth = 4;
                    else if (roles[i]["role"].ToString() == "dbAdminAnyDatabase" && auth < 3) auth = 3;
                    else if (roles[i]["role"].ToString() == "readWriteAnyDatabase" && auth < 2) auth = 2;
                    else if (roles[i]["role"].ToString() == "readAnyDatabase" && auth < 1) auth = 1;
                    else auth = 0;

                    //edit/add more roles if you want to change security levels
                }
            }

            return auth;
        }

        public void SelectDatabase()
        {
            DataBasResultsBox.Items.Clear();

            var databases = mongo.ListDatabasesAsync().Result.ToListAsync().Result;
            foreach (var c in databases)
            {
                if (c.GetElement(0).Value.ToString() != "admin" && c.GetElement(0).Value.ToString() != "local")
                    DataBasResultsBox.Items.Add(c.GetElement(0).Value.ToString());
            }
        }

        public void GetSessions()

        {
            database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);

            var sessioncollection = database.GetCollection<BsonDocument>("Sessions");
            var sessions = sessioncollection.Find(_ => true).ToList();

            if (sessions.Count > 0)
            {
                if (CollectionResultsBox.Items != null) CollectionResultsBox.Items.Clear();
                List<DatabaseSession> items = new List<DatabaseSession>();
                foreach (var c in sessions)
                {
                    //CollectionResultsBox.Items.Add(c.GetElement(1).Value.ToString());
                    items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].AsDateTime.ToShortDateString(), OID = c["_id"].AsObjectId });
                }

                CollectionResultsBox.ItemsSource = items;
            }
            else CollectionResultsBox.ItemsSource = null;
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
            List<DatabaseAnno> items = new List<DatabaseAnno>();
            List<string> Collections = new List<string>();

            database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var annotations = database.GetCollection<BsonDocument>("Annotations");
            var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
            var roles = database.GetCollection<BsonDocument>("Roles");

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
            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), "Sessions", "name", Properties.Settings.Default.LastSessionId);
            var filter = builder.Eq("session_id", sessionid);
            var annos = annotations.Find(filter).ToList();

            foreach (var anno in annos)
            {
                var filtera = builder.Eq("_id", anno["role_id"]);
                var roledb = database.GetCollection<BsonDocument>("Roles").Find(filtera).Single();
                string rolename = roledb.GetValue(1).ToString();

                var filterb = builder.Eq("_id", anno["scheme_id"]);
                var annotdb = database.GetCollection<BsonDocument>("AnnotationSchemes").Find(filterb).Single();
                string annoschemename = annotdb.GetValue(1).ToString();
                string type = annotdb.GetValue(2).ToString();

                var filterc = builder.Eq("_id", anno["annotator_id"]);
                var annotatdb = database.GetCollection<BsonDocument>("Annotators").Find(filterc).Single();
                string annotatorname = annotatdb.GetValue(1).ToString();
                string annotatornamefull = annotatdb.GetValue(2).ToString();

                if (result.ElementCount > 0 && result2.ElementCount > 0 && anno["scheme_id"].AsObjectId == schemeid && anno["role_id"].AsObjectId == roleid)
                {
                    items.Add(new DatabaseAnno() { Role = rolename, AnnoScheme = annoschemename, AnnotatorFullname = annotatornamefull, Annotator = annotatorname, OID = anno["_id"].AsObjectId });
                }
                //else if (result.ElementCount == 0 && result2.ElementCount > 0 && annos["role_id"].AsObjectId == roleid)
                //{
                //    items.Add(new DatabaseAnno() { Role = rolename, AnnoScheme.AnnoType = annoschemename, Annotator = annotatorname });
                //}
                else if (result.ElementCount > 0 && result2.ElementCount == 0 && anno["scheme_id"].AsObjectId == schemeid)
                {
                    items.Add(new DatabaseAnno() { Role = rolename, AnnoScheme = annoschemename, AnnotatorFullname = annotatornamefull, Annotator = annotatorname, OID = anno["_id"].AsObjectId });
                }
            }

            AnnotationResultBox.ItemsSource = items;
        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
            var annosch = annoschemes.Find(_ => true).ToList();

            if (annosch.Count > 0)
            {
                if (AnnoSchemesBox.Items != null) AnnoSchemesBox.Items.Clear();

                foreach (var c in annosch)
                {
                    AnnoSchemesBox.Items.Add(c["name"]);
                }
            }
        }

        public void GetRoles()
        {
            var rolesdb = database.GetCollection<BsonDocument>("Roles");
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

        private void Autologin_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseAutoLogin = false;
            Properties.Settings.Default.Save();
        }

        private void Autologin_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseAutoLogin = true;
            Properties.Settings.Default.Save();
        }

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        private AnnoList rootMeanSquare()
        {
            int numberoftracks = AnnotationResultBox.SelectedItems.Count;

            DatabaseHandler db = new DatabaseHandler(connectionstring);
            List<AnnoList> al = db.LoadFromDatabase(AnnotationResultBox.SelectedItems, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
            AnnoList merge = new AnnoList();
            double[] array = new double[al[0].Count];
            foreach (AnnoList a in al)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    array[i] = array[i] + double.Parse(a[i].Label) * double.Parse(a[i].Label);
                }
            }

            merge = al[0];
            for (int i = 0; i < array.Length; i++)
            {
                merge[i].Label = System.Math.Sqrt(array[i] / numberoftracks).ToString();
            }
            merge.Scheme.SampleRate = 1000.0 / ((merge[0].Stop - merge[0].Start) * 1000.0);
            MessageBox.Show("Median of all Annotations has been calculated");
            Ok.IsEnabled = true;
            return merge;
        }

        private AnnoList calculateMedian()
        {
            int numberoftracks = AnnotationResultBox.SelectedItems.Count;

            DatabaseHandler db = new DatabaseHandler(connectionstring);
            List<AnnoList> al = db.LoadFromDatabase(AnnotationResultBox.SelectedItems, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);

            AnnoList merge = new AnnoList();
            double[] array = new double[al[0].Count];
            foreach (AnnoList a in al)
            {
                for (int i = 0; i < a.Count; i++)
                {
                    array[i] = array[i] + double.Parse(a[i].Label);
                }
            }

            merge = al[0];
            for (int i = 0; i < array.Length; i++)
            {
                merge[i].Label = (array[i] / numberoftracks).ToString();
            }
            merge.Scheme.SampleRate = 1000.0 / ((merge[0].Stop - merge[0].Start) * 1000.0);
            MessageBox.Show("Median of all Annotations has been calculated");
            Ok.IsEnabled = true;
            return merge;
        }

        public AnnoList Median()
        {
            return median;
        }

        public AnnoList RMS()
        {
            return rms;
        }

        private void handleButtons(bool discrete)
        {
            if (discrete)
            {
                CalculateMedian.IsEnabled = false;
                CalculateRMS.IsEnabled = false;
                CalculateRMSE.IsEnabled = false;
            }
            else
            {
                CalculateMedian.IsEnabled = true;
                CalculateRMS.IsEnabled = true;
                CalculateRMSE.IsEnabled = true;
            }
        }

        private void RMSE()
        {
            DatabaseHandler db = new DatabaseHandler(connectionstring);

            if (AnnotationResultBox.SelectedItems.Count == 2)
            {
                List<AnnoList> al = db.LoadFromDatabase(AnnotationResultBox.SelectedItems, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);

                double sum_sq = 0;
                double mse;
                double minerr = double.MaxValue;
                double maxerr = 0.0;

                double[] array = new double[al[0].Count];

                if (al[0].Annotator != null && al[0].Annotator.Contains("RMS"))
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
                else if (al[1].Annotator != null && al[1].Annotator.Contains("RMS"))
                {
                    for (int i = 0; i < al[0].Count; i++)
                    {
                        double err = double.Parse(al[1][i].Label) - double.Parse(al[0][i].Label);
                        if (err > maxerr) maxerr = err;
                        if (err < minerr) minerr = err;
                        sum_sq += err * err;
                    }
                    mse = (double)sum_sq / (al[1].Count);
                    MessageBox.Show("The Mean Square Error for Annotation " + al[0].Scheme.Name + " is " + mse + " (Normalized: " + mse / (maxerr - minerr) + "). This is for your information only, no new tier has been created!");
                }
                else MessageBox.Show("Select RMS Annotation and Reference Annotation. If RMS Annotation is not present, please create it first.");
            }
            else MessageBox.Show("Select RMS Annotation and ONE Reference Annotation. If RMS Annotation is not present, please create it first.");
        }

        private void CalculateMedian_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;
            median = calculateMedian();

            //todo do something
        }

        private void db_login_TextChanged(object sender, TextChangedEventArgs e)
        {
            Autologin.IsChecked = false;
            Autologin.IsEnabled = false;
        }

        private void db_pass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Autologin.IsChecked = false;
            Autologin.IsEnabled = false;
        }

        private void RolesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        private void RMS_Click(object sender, RoutedEventArgs e)
        {
            Ok.IsEnabled = false;
            rms = rootMeanSquare();
        }

        private void CalculateRMSE_Click(object sender, RoutedEventArgs e)
        {
            RMSE();
        }
    }
}
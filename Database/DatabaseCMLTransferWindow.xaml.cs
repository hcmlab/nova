using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseCMLCompleteTier.xaml
    /// </summary>
    public partial class DatabaseCMLTransferWindow : Window
    {
        private MainHandler handler;
        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        private DatabaseHandler dbh;
        private int authlevel = 0;

        public DatabaseCMLTransferWindow(MainHandler handler)
        {
            InitializeComponent();

            this.handler = handler;
            //TODO Hard coded mfccs for now, this should be more dynamic in the future.
            StreamListBox.Items.Add("close.mfccdd[-f 0.04 -d 0]");

            try
            {
                mongo = DatabaseHandler.Client;
                int count = 0;
                while (mongo.Cluster.Description.State.ToString() == "Disconnected")
                {
                    Thread.Sleep(100);
                    if (count++ >= 25) throw new MongoException("Unable to connect to the database. Please make sure that " + mongo.Settings.Server.Host + ":" + mongo.Settings.Server.Port + " is online and you entered your credentials correctly!");
                }

                authlevel = DatabaseHandler.CheckAuthentication(Properties.Settings.Default.MongoDBUser, "admin");
                database = mongo.GetDatabase(Properties.Settings.Default.DatabaseName);
                if (authlevel > 0)
                {
                    GetAnnotationSchemes();
                    GetRoles();
                    GetAnnotators();
                    GetSessionsTraining();
                    GetSessionsForward();

                    ContextTextBox.Text = Properties.Settings.Default.CMLContext.ToString();
                    AnnotatorListBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotator;
                    TierListBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;

                    ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
                    FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
                    RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;

                    ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
                    FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
                    RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();

                    string[] roles = Properties.Settings.Default.CMLDefaultRole.Split(';');
                    for (int i = 0; i < roles.Length; i++)
                    {
                        RoleListBox.SelectedItems.Add(roles[i]);
                    }

                    StreamListBox.SelectedIndex = 0;
                }
                else
                {
                    MessageBox.Show("You have no rights to access the database list");
                    authlevel = DatabaseHandler.CheckAuthentication(Properties.Settings.Default.MongoDBUser, Properties.Settings.Default.DatabaseName);
                }
            }
            catch { };
        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var annosch = annoschemes.Find(_ => true).ToList();

            if (annosch.Count > 0)
            {
                List<string> items = new List<string>();

                if (TierListBox.Items != null)
                {
                    TierListBox.Items.Clear();
                }

                foreach (var c in annosch)
                {
                    if (c["type"].ToString() == "DISCRETE") items.Add(c["name"].ToString());
                }
                items.Sort();
                TierListBox.ItemsSource = items;
            }
            else
            {
                TierListBox.ItemsSource = null;
            }
        }

        public void GetRoles()
        {
            var rolesdb = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
            var roles = rolesdb.Find(_ => true).ToList();

            if (roles.Count > 0)
            {
                if (RoleListBox.Items != null)
                {
                    RoleListBox.Items.Clear();
                }

                List<string> items = new List<string>();
                foreach (var c in roles)
                {
                    items.Add(c["name"].ToString());
                }
                items.Sort();
                RoleListBox.ItemsSource = items;
            }
        }

        public void GetAnnotators(string selecteditem = null)

        {
            AnnotatorListBox.Items.Clear();

            List<string> Collections = new List<string>();
            var annotators = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators);

            var documents = annotators.Find(_ => true).ToList();

            List<string> items = new List<string>();
            foreach (BsonDocument b in documents)
            {
                items.Add(b["fullname"].ToString());
            }
            items.Sort();
            AnnotatorListBox.ItemsSource = items;
        }

        public void GetSessionsTraining()
        {
            List<BsonDocument> presentannotations = new List<BsonDocument>();
            var sessioncollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var annotationscollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationschemescollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var rolescollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
            var annotatorscollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators);
            if (TierListBox.SelectedItem != null && RoleListBox.SelectedItem != null && AnnotatorListBox.SelectedItem != null)
            {
                string schemename = TierListBox.SelectedItem.ToString();
                ObjectId annoschemeid = GetIdFromName(annotationschemescollection, schemename);

                List<ObjectId> roleids = new List<ObjectId>();
                foreach (var item in RoleListBox.SelectedItems)
                {
                    string rolename = RoleListBox.SelectedItem.ToString();
                    ObjectId roleid = GetIdFromName(rolescollection, rolename);
                    roleids.Add(roleid);
                }

                string annotatorfullname = AnnotatorListBox.SelectedItem.ToString();
                ObjectId annotatorid = GetIdFromName(annotatorscollection, annotatorfullname, "fullname");

                foreach (var roleid in roleids)
                {
                    List<BsonDocument> presentannotationstemp = new List<BsonDocument>();
                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("scheme_id", annoschemeid) & builder.Eq("role_id", roleid) & builder.Eq("annotator_id", annotatorid);
                    presentannotationstemp = annotationscollection.Find(filter).ToList();
                    presentannotations = presentannotations.Union(presentannotationstemp).ToList();
                }

                List<string> items = new List<string>();

                foreach (var annotation in presentannotations)
                {
                    string sessionname = FetchDBRef(database, DatabaseDefinitionCollections.Sessions, "name", annotation["session_id"].AsObjectId);

                    //TODO make this more flexible in the future to work with "not noxi" data.
                    bool wavfileloaded = File.Exists(Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\\" + sessionname + "\\Expert_close.wav")
                    && File.Exists(Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\\" + sessionname + "\\Novice_close.wav");

                    if (!items.Contains(sessionname) && wavfileloaded) items.Add(sessionname);
                }
                var newList = items.OrderBy(x => x).ToList();

                newList.Sort();
                TrainSessionsListBox.ItemsSource = newList;
            }
        }

        private ObjectId GetIdFromName(IMongoCollection<BsonDocument> collection, string name, string Name = "name")
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(Name, name);
            var database = collection.Find(filter).ToList();
            if (database.Count > 0) id = database[0].GetValue(0).AsObjectId;

            return id;
        }

        public void GetSessionsForward()

        {
            var sessioncollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var sessions = sessioncollection.Find(_ => true).ToList();

            if (sessions.Count > 0)
            {
                if (ForwardSessionsListBox.Items != null) ForwardSessionsListBox.ItemsSource = null;
                List<string> items = new List<string>();
                foreach (var c in sessions)
                {
                    //NOXI Database only, will be more general in the future.

                    bool wavfileloaded = File.Exists(Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\\" + c["name"].ToString() + "\\Expert_close.wav")
                   && File.Exists(Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\\" + c["name"].ToString() + "\\Novice_close.wav");

                    if (wavfileloaded) items.Add(c["name"].ToString());
                }
                items.Sort();
                ForwardSessionsListBox.ItemsSource = items;
            }
            else
            {
                ForwardSessionsListBox.ItemsSource = null;
            }
        }

        private void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            if (TierListBox.SelectedItem != null && StreamListBox.SelectedItem != null)
            {
                process(true, false, false);
            }
        }

        private void TrainButton_Click(object sender, RoutedEventArgs e)
        {
            if (TierListBox.SelectedItem != null && StreamListBox.SelectedItem != null)
            {
                process(false, true, false);
            }
        }

        private void ForwardButton_Click(object sender, RoutedEventArgs e)
        {
            if (TierListBox.SelectedItem != null && StreamListBox.SelectedItem != null)
            {
                process(false, false, true);
            }
        }

        private void FinishedButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void process(bool extract, bool train, bool forward)
        {
            int result = 5;
            if (int.TryParse(ContextTextBox.Text, out result))
            {
                Properties.Settings.Default.CMLContext = result;
                Properties.Settings.Default.Save();
            }

            Directory.CreateDirectory(Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\\models");

            string stream = (string)StreamListBox.SelectedItem;
            int context = 0;
            int.TryParse(ContextTextBox.Text, out context);

            string username = Properties.Settings.Default.MongoDBUser;
            string password = Properties.Settings.Default.MongoDBPass;
            string sessions = "";
            string datapath = Properties.Settings.Default.DatabaseDirectory;
            string ipport = Properties.Settings.Default.DatabaseAddress;
            string[] split = ipport.Split(':');
            string ip = split[0];
            string port = split[1];
            string db = Properties.Settings.Default.DatabaseName;

            string roles = "";
            foreach (var item in RoleListBox.SelectedItems)
            {
                roles = roles + item + ";";
            }

            roles = roles.Remove(roles.Length - 1);

            string scheme = TierListBox.SelectedItem.ToString();
            string annotatorfullname = AnnotatorListBox.SelectedItem.ToString();
            var annotatorscollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators);
            ObjectId annotatorid = GetIdFromName(annotatorscollection, annotatorfullname, "fullname");
            string annotator = FetchDBRef(database, DatabaseDefinitionCollections.Annotators, "name", annotatorid);

            double confidence = -1.0;
            if (ConfidenceTextBox.IsEnabled)
            {
                double.TryParse(ConfidenceTextBox.Text, out confidence);
            }
            double minGap = 0.0;
            if (FillGapTextBox.IsEnabled)
            {
                double.TryParse(FillGapTextBox.Text, out minGap);
            }
            double minDur = 0.0;
            if (RemoveLabelTextBox.IsEnabled)
            {
                double.TryParse(RemoveLabelTextBox.Text, out minDur);
            }
            Properties.Settings.Default.CMLDefaultGap = minGap;
            Properties.Settings.Default.CMLDefaultConf = confidence;
            Properties.Settings.Default.CMLDefaultMinDur = minDur;
            Properties.Settings.Default.Save();

            logTextBox.Text = "";
            try
            {
                if (extract)
                //EXTRACT MISSING FEATURES
                {
                    sessions = "";

                    foreach (var item in TrainSessionsListBox.SelectedItems)
                    {
                        sessions = sessions + item + ";";
                    }
                    foreach (var item in ForwardSessionsListBox.SelectedItems)
                    {
                        sessions = sessions + item + ";";
                    }
                    sessions = sessions.Remove(sessions.Length - 1);

                    string arguments = " -list " + sessions + " -log cml.log " + "\"" + Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\" " + " expert;novice close";

                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmltrain.exe";
                    startInfo.Arguments = "--extract" + arguments;
                    logTextBox.AppendText(startInfo.Arguments + "-------------------------------------------\r\n");
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return;
                    }
                    logTextBox.AppendText(File.ReadAllText("cml.log"));
                }

                //TRAIN MODEL
                if (train)
                {
                    sessions = "";
                    foreach (var item in TrainSessionsListBox.SelectedItems)
                    {
                        sessions = sessions + item + ";";
                    }
                    sessions = sessions.Remove(sessions.Length - 1);

                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmltrain.exe";
                    startInfo.Arguments = "--train "
                        + "-context " + context +
                        " -username " + username +
                        " -password " + password +
                        " -list " + sessions +
                        " -log cml.log " +
                        "\"" + datapath + "\\" + db + "\" " +
                        ip + " " +
                        port + " " +
                        db + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\""; ;

                    logTextBox.AppendText("-------------------------------------------\r\n" + startInfo.Arguments + "\r\n\r\n");

                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return;
                    }
                    logTextBox.AppendText(File.ReadAllText("cml.log"));
                }

                //CREATE ANNOTATIONS
                if (forward)
                {
                    sessions = "";
                    foreach (var item in ForwardSessionsListBox.SelectedItems)
                    {
                        sessions = sessions + item + ";";
                    }

                    sessions = sessions.Remove(sessions.Length - 1);

                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = "cmltrain.exe";
                    startInfo.Arguments = "--forward "
                        + "-context " + context +
                        " -username " + username +
                        " -password " + password +
                        " -list " + sessions +
                        " -assign " + username +
                        " -confidence " + confidence +
                        " -mingap " + minGap +
                        " -mindur " + minDur +
                        " -log cml.log " +
                        "\"" + datapath + "\\" + db + "\" " +
                        ip + " " +
                        port + " " +
                        db + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\"";

                    logTextBox.AppendText("-------------------------------------------\r\n" + startInfo.Arguments + "\r\n\r\n");

                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    if (process.ExitCode != 0)
                    {
                        return;
                    }
                    logTextBox.AppendText(File.ReadAllText("cml.log"));
                }
            }
            catch
            {
                MessageBox.Show("Cooperative Machine Learning System not found. This will be integrated in the public release soon.");
            }
        }

        private void TierListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.CMLDefaultScheme = TierListBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();
            GetSessionsTraining();
        }

        public string FetchDBRef(IMongoDatabase database, string collection, string attribute, ObjectId reference)
        {
            string output = "";
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq("_id", reference);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0)
            {
                output = result[0][attribute].ToString();
            }

            return output;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(logTextBox.Text);
        }

        private void ConfidenceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLSetConf = true;
            Properties.Settings.Default.Save();
            ConfidenceTextBox.IsEnabled = true;
        }

        private void ConfidenceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLSetConf = false;
            Properties.Settings.Default.Save();
            ConfidenceTextBox.IsEnabled = false;
        }

        private void FillGapCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLFill = true;
            Properties.Settings.Default.Save();
            FillGapTextBox.IsEnabled = true;
        }

        private void FillGapCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLFill = false;
            Properties.Settings.Default.Save();
            FillGapTextBox.IsEnabled = false;
        }

        private void RemoveLabelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLRemove = true;
            Properties.Settings.Default.Save();
            RemoveLabelTextBox.IsEnabled = true;
        }

        private void RemoveLabelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLRemove = false;
            Properties.Settings.Default.Save();
            RemoveLabelTextBox.IsEnabled = false;
        }

        private void AnnotatorListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.CMLDefaultAnnotator = AnnotatorListBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();
            GetSessionsTraining();
        }

        private void RoleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string roles = "";
            foreach (var item in RoleListBox.SelectedItems)
            {
                roles = roles + item.ToString() + ";";
            }
            Properties.Settings.Default.CMLDefaultRole = roles;
            Properties.Settings.Default.Save();

            GetSessionsTraining();
        }
    }
}
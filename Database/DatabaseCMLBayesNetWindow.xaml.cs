using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseCMLBayesNetWindow : Window
    {
        private MainHandler handler;
        private Mode mode;

        private string tempTrainerPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        public enum Mode
        {
            TRAIN,
            PREDICT,
            COMPLETE
        }

        private List<SchemeRoleAnnotator> selectedAnnotations = new List<SchemeRoleAnnotator>();

        public DatabaseCMLBayesNetWindow(MainHandler handler, Mode mode)
        {
            InitializeComponent();

            this.handler = handler;
            this.mode = mode;

            Loaded += DatabaseCMLTrainAndPredictWindow_Loaded;

            HelpTrainLabel.Content = "Annotations are chunked in frames of this size (in ms).\r\n\r\nContinous Annotations are discretized in these classes (seperate by ;)\r\n\r Timesteps (0 for static)";
            HelpPredictLabel.Content = "Apply thresholds to fill up gaps between segments of the same class\r\nand remove small segments (in seconds).\r\n\r\nSet confidence to a fixed value.";

            switch (mode)
            {
                case Mode.COMPLETE:

                    Title = "Complete Annotation";
                    ApplyButton.Content = "Complete";

                    ShowAllSessionsCheckBox.Visibility = Visibility.Collapsed;
                    PredictOptionsPanel.Visibility = Visibility.Visible;
                    TrainOptionsPanel.Visibility = Visibility.Visible;
                    ForceCheckBox.Visibility = Visibility.Collapsed;
                    //TrainerNameTextBox.IsEnabled = false;

                    ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
                    FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
                    RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;

                    ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
                    FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
                    RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();

                    break;

                case Mode.TRAIN:

                    Title = "Train Bayesian Network (beta)";
                    ApplyButton.Content = "Train";
                    ShowAllSessionsCheckBox.Visibility = Visibility.Collapsed;
                    PredictOptionsPanel.Visibility = Visibility.Collapsed;
                    TrainOptionsPanel.Visibility = Visibility.Visible;
                    ForceCheckBox.Visibility = Visibility.Visible;

                    break;

                case Mode.PREDICT:

                    Title = "Predict Annotations";
                    ApplyButton.Content = "Predict";

                    ShowAllSessionsCheckBox.Visibility = Visibility.Collapsed;
                    PredictOptionsPanel.Visibility = Visibility.Visible;
                    TrainOptionsPanel.Visibility = Visibility.Collapsed;
                    ForceCheckBox.Visibility = Visibility.Collapsed;

                    ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
                    FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
                    RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;

                    ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
                    FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
                    RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();

                    break;
            }
        }

        private void DatabaseCMLTrainAndPredictWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetDatabases(DatabaseHandler.DatabaseName);
            GetAnnotators();
            GetRoles();
            GetSchemes();

            if (mode == Mode.COMPLETE)
            {
                AnnoList annoList = AnnoTierStatic.Selected.AnnoList;
                DatabasesBox.IsEnabled = false;
                SchemesBox.SelectedItem = annoList.Scheme.Name;
                SchemesBox.IsEnabled = false;
                RolesBox.SelectedItem = annoList.Meta.Role;
                RolesBox.IsEnabled = false;
            }

            if (mode == Mode.COMPLETE ||
                mode == Mode.PREDICT)
            {
                string annotatorName = Properties.Settings.Default.MongoDBUser;
                if (DatabaseHandler.Annotators.Find(a => a.Name == annotatorName) != null)
                {
                    string annotatorFullName = DatabaseHandler.Annotators.Find(a => a.Name == annotatorName).FullName;
                    AnnotatorsBox.SelectedItem = annotatorFullName;
                }

                AnnotatorsBox.IsEnabled = DatabaseHandler.CheckAuthentication() > DatabaseAuthentication.READWRITE;
            }
            else
            {
                AnnotatorsBox.SelectedItem = Defaults.CML.GoldStandardFullName;
            }

            //GetSessions();

            if (mode == Mode.COMPLETE)
            {
                SessionsBox.UnselectAll();
                SessionsBox.SelectedItem = DatabaseHandler.Sessions.Find(s => s.Name == DatabaseHandler.SessionName);
                SessionsBox.IsEnabled = false;
            }

            ApplyButton.Focus();
            Update();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLDefaultTrainer = TrainerPathComboBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            bool force = mode == Mode.COMPLETE || ForceCheckBox.IsChecked.Value;

            string database = DatabaseHandler.DatabaseName;

            var sessions = SessionsBox.SelectedItems;

            logTextBox.Text = "";

            string networkrDir = Properties.Settings.Default.CMLDirectory + "\\" +
                Defaults.CML.FusionFolderName + "\\" +
                Defaults.CML.FusionBayesianNetworkFolderName + "\\" + TrainerPathComboBox.SelectedItem + ".xdsl";

            string datasetDir = Properties.Settings.Default.CMLDirectory + "\\" +
               Defaults.CML.FusionFolderName + "\\" +
               Defaults.CML.FusionBayesianNetworkFolderName + "\\" + TrainerPathComboBox.SelectedItem + ".txt";

            double chunksizeinMS;
            double.TryParse(chunksizebox.Text, out chunksizeinMS);

            int tempsteps;
            int.TryParse(timestepsbox.Text, out tempsteps);

            string[] discretelabels = classesbox.Text.Split(';');
            bool isdynamic = tempsteps > 0 ? true : false;


            if (File.Exists(datasetDir) && ForceCheckBox.IsChecked == false)
            {
                logTextBox.Text = "dataset exists, skip.\n";
                logTextBox.Text += handler.CMLTrainBayesianNetwork(networkrDir, datasetDir, isdynamic);
                return;
            }

            File.Delete(datasetDir);



            bool ishead = true;
            foreach (DatabaseSession session in SessionsBox.SelectedItems)
            {
                List<AnnoList> annoLists = new List<AnnoList>();
                foreach (SchemeRoleAnnotator item in AnnotationSelectionBox.SelectedItems)
                {
                    DatabaseRole role = DatabaseHandler.Roles.Find(r => r.Name == item.Role);
                    DatabaseScheme scheme = DatabaseHandler.Schemes.Find(s => s.Name == item.Name);
                    ObjectId annotatorID = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator).Id;

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("scheme_id", scheme.Id) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", role.Id) & builder.Eq("session_id", session.Id);
                    List<DatabaseAnnotation> list = DatabaseHandler.GetAnnotations(filter);
                    foreach (DatabaseAnnotation anno in list)
                    {
                        AnnoList annolist = DatabaseHandler.LoadAnnoList(anno.Id);
                        annoLists.Add(annolist);
                        logTextBox.Text = logTextBox.Text + "Session: " + session.Name + " Role: " + annolist.Meta.Role + " Scheme: " + annolist.Scheme.Name + "\n";

                      
                        logTextBox.Focus();
                        logTextBox.CaretIndex = logTextBox.Text.Length;
                        logTextBox.ScrollToEnd();
                    }
                }
                logTextBox.Text = logTextBox.Text + "----------------------------------\n";
                ExportFrameWiseAnnotations(chunksizeinMS, discretelabels, ";", "REST", datasetDir, annoLists, ishead, session.Name, tempsteps);

                if (ishead) ishead = false;
            }

           
            logTextBox.Text += handler.CMLTrainBayesianNetwork(networkrDir, datasetDir, isdynamic);

            //List<AnnoList> annoLists = new List<AnnoList>();
            //    foreach (string role in RolesBox.SelectedItems)
            //    {
            //        DatabaseRole ro = DatabaseHandler.Roles.Find(r => r.Name == role);

            //        foreach (string dbscheme in SchemesBox.SelectedItems)
            //        {
            //            DatabaseScheme scheme = DatabaseHandler.Schemes.Find(s => s.Name == dbscheme);
            //            var builder = Builders<BsonDocument>.Filter;
            //            var filter = builder.Eq("scheme_id", scheme.Id) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", ro.Id) & builder.Eq("session_id", session.Id);
            //            List<DatabaseAnnotation> list = DatabaseHandler.GetAnnotations(filter);
            //            logTextBox.Text = logTextBox.Text + "Exporting Annotations:\n";
            //            foreach (DatabaseAnnotation anno in list)
            //            {
            //                AnnoList annolist = DatabaseHandler.LoadAnnoList(anno.Id);
            //                annoLists.Add(annolist);
            //                logTextBox.Text = logTextBox.Text + "Session: " + session.Name + " Role: " + annolist.Meta.Role + " Scheme: " + annolist.Scheme.Name + "\n";
            //            }
            //        }
            //    }

            //    ExportFrameWiseAnnotations(chunksizeinMS, discretelabels, ";", "REST", datasetDir, annoLists, ishead, tempsteps);
            //    logTextBox.Text = logTextBox.Text + "This might take up to a few minutes...\n";
            //    if (ishead) ishead = false;
            //}

            //bool isdynamic = tempsteps > 0 ? true : false;
            //logTextBox.Text += handler.CMLTrainBayesianNetwork(networkrDir, datasetDir, isdynamic);

            // MessageBox.Show("Trained Bayesian Network", "Sucess", MessageBoxButton.OK, MessageBoxImage.Information);

            //todo call bayesfusion --train
        }

        private void ExportFrameWiseAnnotations(double chunksize, string[] discretizeclasses, string seperator, string restclass, string filepath, List<AnnoList> annoLists, bool ishead, string sessionname, int tempsteps = 0)
        {
            bool found = false;

            double currenttime = 0;
            string headline = "";
            double maxdur = double.MaxValue;

            string[][] history = new string[annoLists.Count][];

            for (int a = 0; a < annoLists.Count; a++)
            {
                history[a] = new string[tempsteps];

                if (ishead)
                {
                    headline += annoLists[a].Meta.Role + "_" + annoLists[a].Scheme.Name + seperator;

                    for (int i = 0; i < tempsteps; i++)
                    {
                        history[a][i] = restclass;
                        headline += annoLists[a].Meta.Role + "_" + annoLists[a].Scheme.Name + "_" + (i) + seperator;
                    }
                }
                double localdur = double.MaxValue;

                if(annoLists[a].Count > 0)
                {
                    localdur = annoLists[a][annoLists[a].Count - 1].Stop * 1000;
                }

                maxdur = Math.Min(maxdur, localdur);
            }

            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, true))
                {
                    if (ishead)
                    {
                        headline = headline.Remove(headline.Length - 1);
                        file.WriteLine(headline);
                    }
                    headline = "";

                    while (currenttime < maxdur)
                    {
                        for (int a = 0; a < annoLists.Count; a++)
                        {
                            if (annoLists[a].Count > 0)
                            {
                                for (int i = 0; i < annoLists[a].Count; i++)
                                {
                                    if ((annoLists[a][i].Start * 1000) - (annoLists[a][i].Duration * 1000) < currenttime && annoLists[a][i].Stop * 1000 > currenttime)
                                    {
                                        found = true;

                                        if (annoLists[a].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                                        {
                                            double value;
                                            double.TryParse(annoLists[a][i].Label, out value);

                                            string discretelabel = discretize(value, annoLists[a].Scheme.MinScore, annoLists[a].Scheme.MaxScore, discretizeclasses);
                                            headline += discretelabel + seperator;

                                            if (tempsteps > 0)
                                            {
                                                for (int k = 0; k < tempsteps; k++)
                                                {
                                                    headline += history[a][k] + seperator;
                                                }

                                                for (int k = tempsteps - 1; k > 0; k--)
                                                {
                                                    history[a][k] = history[a][k - 1];
                                                }

                                                history[a][0] = discretelabel;
                                            }
                                        }
                                        else
                                        {
                                            headline += annoLists[a][i].Label + seperator;

                                            if (tempsteps > 0)
                                            {
                                                for (int k = 0; k < tempsteps; k++)
                                                {
                                                    headline += history[a][k] + seperator;
                                                }

                                                for (int k = tempsteps - 1; k > 0; k--)
                                                {
                                                    history[a][k] = history[a][k - 1];
                                                }

                                                history[a][0] = annoLists[a][i].Label;
                                            }
                                        }

                                        break;
                                    }
                                    else found = false;
                                }
                            }
                            else
                            {
                                found = false;
                            }
                            if (!found)

                            {
                                headline += restclass + seperator;
                                if (tempsteps > 0)
                                {
                                    for (int k = 0; k < tempsteps; k++)
                                    {
                                        headline += history[a][k] + seperator;
                                    }

                                    for (int k = tempsteps - 1; k > 0; k--)
                                    {
                                        history[a][k] = history[a][k - 1];
                                    }

                                    history[a][0] = restclass;
                                }
                            }
                        }

                        headline = headline.Remove(headline.Length - 1);
                        file.WriteLine(headline);
                        headline = "";
                        currenttime += chunksize;
                        Action EmptyDelegate = delegate () { };

                        //  progressBar.Value = progressBar.Value + chunksize;
                        this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                        // Dispatcher.BeginInvoke(DispatcherPriority.Normal, new UpdateProgressDelegate(UpdateProgress), chunksize);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not create Sampled Annotations Data File! " + ex, "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string discretize(double value, double MinScore, double MaxScore, string[] classes)
        {
            int numclasses = classes.Length;

            double range = MaxScore - MinScore;

            double chunk = range / numclasses;

            for (int i = 0; i < numclasses; i++)
            {
                if (value >= (MinScore + i * chunk) && value < (MinScore + (i + 1) * chunk))
                {
                    return classes[i];
                }
            }

            //else

            return classes[classes.Length - 1];
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

        public void GetDatabases(string selectedItem = null)
        {
            DatabasesBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases();

            foreach (string db in databases)
            {
                DatabasesBox.Items.Add(db);
            }

            Select(DatabasesBox, selectedItem);
        }

        private void DatabasesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                GetAnnotators();
                GetRoles();
                GetSchemes();
            }

            Update();
        }

        public void GetSchemes()
        {
            SchemesBox.Items.Clear();

            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseScheme> schemesValid = new List<DatabaseScheme>();
            List<DatabaseScheme> schemes = DatabaseHandler.Schemes;

            foreach (DatabaseScheme scheme in schemes)
            {
                schemesValid.Add(scheme);
            }

            foreach (DatabaseScheme item in schemesValid)
            {
                SchemesBox.Items.Add(item.Name);
            }

            if (SchemesBox.Items.Count > 0)
            {
                SchemesBox.SelectedIndex = 0;
                SchemesBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;
            }
        }

        public void GetRoles()
        {
            RolesBox.Items.Clear();

            foreach (DatabaseRole item in DatabaseHandler.Roles)
            {
               
                    RolesBox.Items.Add(item.Name);
                
            }

            if (RolesBox.Items.Count > 0)
            {
                RolesBox.SelectedIndex = 0;
                RolesBox.SelectedItem = Properties.Settings.Default.CMLDefaultRole;
            }
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
                AnnotatorsBox.SelectedIndex = 0;
                AnnotatorsBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotator;
            }
        }

        public void GetSessions()
        {
            if (AnnotationSelectionBox.SelectedItem == null)
            {
                return;
            }

            Properties.Settings.Default.CMLDefaultAnnotator = AnnotatorsBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultRole = RolesBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultScheme = SchemesBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            if (AnnotationSelectionBox.HasItems)
            {
                AnnotationSelectionBox.ItemsSource = null;
            }

            if (mode != Mode.COMPLETE
                && (mode == Mode.TRAIN))
            {
                // show sessions for which an annotation exists or is missing

                List<BsonDocument> annotations = new List<BsonDocument>();

                List<string> sessionNames = new List<string>();

                {
                    var item = (SchemeRoleAnnotator)AnnotationSelectionBox.SelectedItems[0];
                    string schemeName = item.Name;
                    ObjectId schemeID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref schemeID, DatabaseDefinitionCollections.Schemes, schemeName);
                    string roleName = item.Role;
                    ObjectId roleID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref roleID, DatabaseDefinitionCollections.Roles, roleName);

                    string annotatorName = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator).Name;
                    ObjectId annotatorID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, annotatorName);

                    var builder = Builders<BsonDocument>.Filter;

                    var filter = builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", roleID);

                    annotations.AddRange(DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotations, true, filter));

                    foreach (BsonDocument annotation in annotations)
                    {
                        string sessionName = "";
                        DatabaseHandler.GetObjectName(ref sessionName, DatabaseDefinitionCollections.Sessions, annotation["session_id"].AsObjectId);
                        if (sessionName != "" && !sessionNames.Contains(sessionName))
                        {
                            sessionNames.Add(sessionName);
                        }
                    }
                }

                for (int i = 1; i < AnnotationSelectionBox.SelectedItems.Count; i++)
                {
                    List<BsonDocument> annotationstemp = new List<BsonDocument>();
                    List<string> sessionNamestemp = new List<string>();
                    var item = (SchemeRoleAnnotator)AnnotationSelectionBox.SelectedItems[i];
                    string schemeName = item.Name;
                    ObjectId schemeID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref schemeID, DatabaseDefinitionCollections.Schemes, schemeName);
                    string roleName = item.Role;
                    ObjectId roleID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref roleID, DatabaseDefinitionCollections.Roles, roleName);

                    string annotatorName = DatabaseHandler.Annotators.Find(a => a.FullName == item.Annotator).Name;
                    ObjectId annotatorID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, annotatorName);

                    var builder = Builders<BsonDocument>.Filter;

                    var filter = builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", roleID);

                    //for(int j=0; j < sessionNames.Count; j++)
                    //{
                    //    ObjectId sessionID = new ObjectId();
                    //    DatabaseHandler.GetObjectID(ref sessionID, DatabaseDefinitionCollections.Sessions, sessionNames[j]);
                    //    filter = filter | builder.Eq("session_id", sessionID);
                    //}

                    annotationstemp.AddRange(DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotations, true, filter));

                    foreach (BsonDocument annotation in annotationstemp)
                    {
                        string sessionName = "";
                        DatabaseHandler.GetObjectName(ref sessionName, DatabaseDefinitionCollections.Sessions, annotation["session_id"].AsObjectId);
                        if (sessionName != "" && !sessionNamestemp.Contains(sessionName))
                        {
                            sessionNamestemp.Add(sessionName);
                        }
                    }

                    sessionNames.RemoveAll(thing => !sessionNamestemp.Contains(thing));
                }

                List<DatabaseSession> sessions = new List<DatabaseSession>();
                foreach (string sessionName in sessionNames)
                {
                    DatabaseSession session = new DatabaseSession() { Name = sessionName };
                    if (DatabaseHandler.GetSession(ref session))
                    {
                        sessions.Add(session);
                    }
                }

                SessionsBox.ItemsSource = sessions.OrderBy(s => s.Name).ToList();
            }
            else
            {
                // show all sessions

                SessionsBox.ItemsSource = DatabaseHandler.Sessions;
            }

            if (SessionsBox.HasItems)
            {
                if(SessionsBox.SelectedItem == null)
                {
                    SessionsBox.SelectedIndex = 0;
                }
                
            }
        }

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrainerPathComboBox.Items.Clear();
            List<string> nets = getBayesianNetworks();
            foreach (string net in nets)
                if (SessionsBox.HasItems)
                {
                    TrainerPathComboBox.Items.Add(Path.GetFileNameWithoutExtension(net));
                }


            if(TrainerPathComboBox.SelectedItem == null)
            {
                TrainerPathComboBox.SelectedIndex = 0;
            }
        

            Update();
        }

        private List<string> getBayesianNetworks()
        {
            List<string> networks = new List<string>();

            string networkrDir = Properties.Settings.Default.CMLDirectory + "\\" +
                 Defaults.CML.FusionFolderName + "\\" +
                 Defaults.CML.FusionBayesianNetworkFolderName + "\\";

            if (Directory.Exists(networkrDir))
            {
                //string[] searchDirs = Directory.GetDirectories(networkrDir);
                //foreach (string searchDir in searchDirs)
                //{
                string[] networkFiles = Directory.GetFiles(networkrDir, "*." + Defaults.CML.BayesianNetworkextension);
                foreach (string network in networkFiles)
                {
                    networks.Add(network);
                }
                // }
            }

            return networks;
        }

        private void Annotations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetSessions();

            Update();
        }

        private void ShowAllSessionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // GetSessions();
        }

        private void ShowAllSessionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // GetSessions();
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

        private void TrainerPathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TrainerPathComboBox.SelectedItem != null)
            {
                string trainer = (string)TrainerPathComboBox.SelectedItem;

                string database = "";
                if (DatabasesBox.SelectedItem != null)
                {
                    database = DatabasesBox.SelectedItem.ToString();
                }
                // TrainerNameTextBox.Text = mode == Mode.COMPLETE ? Path.GetFileName(tempTrainerPath) : database;
            }
        }

        private void Update()
        {
            bool enable = false;

            if (TrainerPathComboBox.Items.Count > 0
                && DatabasesBox.SelectedItem != null
                && SessionsBox.SelectedItem != null
                && RolesBox.SelectedItem != null
                && AnnotatorsBox.SelectedItem != null
                && SchemesBox.SelectedItem != null)
            {
                enable = true;
            }

            ApplyButton.IsEnabled = enable;
            TrainOptionsPanel.IsEnabled = enable;
            PredictOptionsPanel.IsEnabled = enable;
            ForceCheckBox.IsEnabled = enable;
            TrainerPathComboBox.IsEnabled = enable;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void AnnotationSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            removePair.IsEnabled = false;
            if (AnnotationSelectionBox.SelectedItems != null)
            {
                removePair.IsEnabled = true;
            }

            GetSessions();
        }

        private void removePair_Click(object sender, RoutedEventArgs e)
        {
         
                selectedAnnotations.Remove((SchemeRoleAnnotator)AnnotationSelectionBox.SelectedItem);
                AnnotationSelectionBox.Items.Remove(AnnotationSelectionBox.SelectedItem);
            
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var scheme in SchemesBox.SelectedItems)
            {
                foreach (var role in RolesBox.SelectedItems)
                {
                    SchemeRoleAnnotator stp = new SchemeRoleAnnotator() { Name = scheme.ToString(), Role = role.ToString(), Annotator = AnnotatorsBox.SelectedItem.ToString() };

                    if (selectedAnnotations.Find(item => item.Role == stp.Role && item.Name == stp.Name) == null)
                    {
                        selectedAnnotations.Add(stp);
                        AnnotationSelectionBox.Items.Add(stp);

                        AnnotationSelectionBox.SelectAll();
                    }
                }
            }

            GetSessions();
        }
    }

    public class SchemeRoleAnnotator
    {
        public string Name { get; set; }
        public string Role { get; set; }
        public string Annotator { get; set; }
    }
}
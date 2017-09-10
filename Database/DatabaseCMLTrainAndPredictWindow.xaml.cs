using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseCMLTrainAndPredictWindow : Window
    {
        private MainHandler handler;
        private Mode mode;

        public class Trainer
        {
            public string Path { get; set; }
            public string Name { get; set; }
            public string LeftContext { get; set; }
            public string RightContext { get; set; }
            public string Balance { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private string tempTrainerPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        public enum Mode
        {
            TRAIN,
            PREDICT,
            COMPLETE
        }

        public DatabaseCMLTrainAndPredictWindow(MainHandler handler, Mode mode)
        {
            InitializeComponent();

            this.handler = handler;
            this.mode = mode;

            Loaded += DatabaseCMLTrainAndPredictWindow_Loaded;

            HelpTrainLabel.Content = "To balance the number of samples per class samples can be removed ('under') or duplicated ('over').\r\n\r\nDuring training the current feature frame can be extended by adding left and / or right frames.\r\n\r\nThe default output name may be altered.";
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
                    TrainerNameTextBox.IsEnabled = false;

                    ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
                    FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
                    RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;

                    ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
                    FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
                    RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();

                    break;

                case Mode.TRAIN:

                    Title = "Train Models";
                    ApplyButton.Content = "Train";
                    ShowAllSessionsCheckBox.Visibility = Visibility.Collapsed;
                    PredictOptionsPanel.Visibility = Visibility.Collapsed;
                    TrainOptionsPanel.Visibility = Visibility.Visible;
                    ForceCheckBox.Visibility = Visibility.Visible;  
                                      
                    break;

                case Mode.PREDICT:

                    Title = "Predict Annotations";
                    ApplyButton.Content = "Predict";

                    ShowAllSessionsCheckBox.Visibility = Visibility.Visible;
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

            GetSessions();

            if (mode == Mode.COMPLETE)
            {
                SessionsBox.UnselectAll();
                SessionsBox.SelectedItem = DatabaseHandler.Sessions.Find(s => s.Name == DatabaseHandler.SessionName);
                SessionsBox.IsEnabled = false;
            }

            GetStreams();

            ApplyButton.Focus();
            Update();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLDefaultTrainer = TrainerPathComboBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            Trainer trainer = (Trainer) TrainerPathComboBox.SelectedItem;
            bool force = mode == Mode.COMPLETE || ForceCheckBox.IsChecked.Value;

            if (!File.Exists(trainer.Path))
            {
                MessageTools.Warning("file does not exist '" + trainer.Path + "'");
                return;
            }
            
            string database = DatabaseHandler.DatabaseName;

            DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;                        

            string sessionList = "";
            var sessions = SessionsBox.SelectedItems;
            foreach (DatabaseSession session in sessions)
            {
                if (sessionList == "")
                {
                    sessionList += session.Name;
                }
                else
                {
                    sessionList += ";" + session.Name;
                }
            }

            DatabaseScheme scheme = DatabaseHandler.Schemes.Find(s => s.Name == SchemesBox.SelectedItem.ToString());

            string rolesList = "";
            var roles = RolesBox.SelectedItems;
            foreach (string role in roles)
            {
                if (rolesList == "")
                {
                    rolesList += role;
                }
                else
                {
                    rolesList += ";" + role;
                }
            }

            string annotatorFullName = (string)AnnotatorsBox.SelectedItem;
            string annotator = DatabaseHandler.Annotators.Find(a => a.FullName == annotatorFullName).Name;

            string trainerLeftContext = LeftContextTextBox.Text;
            string trainerRightContext = RightContextTextBox.Text;
            string trainerBalance = ((ComboBoxItem) BalanceComboBox.SelectedItem).Content.ToString();

            logTextBox.Text = "";

            if (mode == Mode.TRAIN 
                || mode == Mode.COMPLETE)
            {
                string[] streamParts = stream.Name.Split('.');
                if (streamParts.Length <= 1)
                {
                    return;
                }
                string streamName = streamParts[1];
                for (int i = 2; i < streamParts.Length; i++)
                {
                    streamName += "." + streamParts[i];
                }

                string trainerDir = Properties.Settings.Default.CMLDirectory + "\\" +
                                Defaults.CML.ModelsFolderName + "\\" +
                                Defaults.CML.ModelsTrainerFolderName + "\\" +
                                scheme.Type.ToString().ToLower() + "\\" +
                                scheme.Name + "\\" +
                                stream.Type + "{" +
                                streamName + "}\\" +
                                trainer.Name + "\\";
                                
                Directory.CreateDirectory(trainerDir);
                
                string trainerName = TrainerNameTextBox.Text == "" ? trainer.Name : TrainerNameTextBox.Text;
                string trainerOutPath = mode == Mode.COMPLETE ? tempTrainerPath : trainerDir + trainerName;

                if (force || !File.Exists(trainerOutPath + ".trainer"))
                {
                    logTextBox.Text += handler.CMLTrainModel(trainer.Path,
                        trainerOutPath,
                        Properties.Settings.Default.DatabaseDirectory,
                        Properties.Settings.Default.DatabaseAddress,
                        Properties.Settings.Default.MongoDBUser,
                        MainHandler.Decode(Properties.Settings.Default.MongoDBPass),
                        database,
                        sessionList,
                        scheme.Name,
                        rolesList,
                        annotator,
                        stream.Name,
                        trainerLeftContext,
                        trainerRightContext,
                        trainerBalance,
                        mode == Mode.COMPLETE);
                }
                else
                {
                    logTextBox.Text += "skip " + trainerOutPath + "\n";
                }                
            }

            if (mode == Mode.PREDICT
                || mode == Mode.COMPLETE)
            {
                if (true || force)
                {
                    double confidence = -1.0;
                    if (ConfidenceCheckBox.IsChecked == true && ConfidenceTextBox.IsEnabled)
                    {
                        double.TryParse(ConfidenceTextBox.Text, out confidence);
                        Properties.Settings.Default.CMLDefaultConf = confidence;
                    }
                    double minGap = 0.0;
                    if (FillGapCheckBox.IsChecked == true && FillGapTextBox.IsEnabled)
                    {
                        double.TryParse(FillGapTextBox.Text, out minGap);
                        Properties.Settings.Default.CMLDefaultGap = minGap;
                    }
                    double minDur = 0.0;
                    if (RemoveLabelCheckBox.IsChecked == true && RemoveLabelTextBox.IsEnabled)
                    {
                        double.TryParse(RemoveLabelTextBox.Text, out minDur);
                        Properties.Settings.Default.CMLDefaultMinDur = minDur;
                    }                                                          
                    Properties.Settings.Default.Save();
                    
                    logTextBox.Text += handler.CMLPredictAnnos(mode == Mode.COMPLETE ? tempTrainerPath : trainer.Path,
                        Properties.Settings.Default.DatabaseDirectory,
                        Properties.Settings.Default.DatabaseAddress,
                        Properties.Settings.Default.MongoDBUser,
                        MainHandler.Decode(Properties.Settings.Default.MongoDBPass),
                        database,
                        sessionList,
                        scheme.Name,
                        rolesList,
                        annotator,
                        stream.Name,
                        trainerLeftContext,
                        trainerRightContext,
                        confidence,
                        minGap,
                        minDur,                        
                        mode == Mode.COMPLETE);
                }
                
            }

            if (mode == Mode.COMPLETE)
            {
                handler.ReloadAnnoTierFromDatabase(AnnoTierStatic.Selected, false);

                var dir = new DirectoryInfo(Path.GetDirectoryName(tempTrainerPath));
                foreach (var file in dir.EnumerateFiles(Path.GetFileName(tempTrainerPath) + "*"))
                {
                    file.Delete();
                }

                Close();
            }
           
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
                GetStreams();                
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
                foreach (DatabaseStream stream in streams)
                {

                    bool template = mode == Mode.TRAIN || mode == Mode.COMPLETE;
                    if (getTrainer(stream, scheme, template).Count > 0)
                    {
                        schemesValid.Add(scheme);
                        break;
                    }
                }
            }

            foreach (DatabaseScheme item in schemesValid)
            {
                SchemesBox.Items.Add(item.Name);
            }

            if (SchemesBox.Items.Count > 0)
            {
                if (SchemesBox.SelectedItem == null)
                {
                    SchemesBox.SelectedIndex = 0;
                }
                SchemesBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;
            }
        }

        public void GetRoles()
        {
            RolesBox.Items.Clear();
            
            foreach (DatabaseRole item in DatabaseHandler.Roles)
            {
                if (item.HasStreams)
                {
                    RolesBox.Items.Add(item.Name);
                }
            }

            if (RolesBox.Items.Count > 0)
            {

                if (RolesBox.SelectedItem == null)
                {
                    RolesBox.SelectedIndex = 0;
                }
                RolesBox.SelectedItem = Properties.Settings.Default.CMLDefaultRole;
            }
        }

        private void GetStreams()
        {
            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseStream> streamsValid = new List<DatabaseStream>();
            List<DatabaseScheme> schemes = DatabaseHandler.Schemes;

            foreach (DatabaseStream stream in streams)
            {
                foreach (DatabaseScheme scheme in schemes)
                {
                    bool template = mode == Mode.TRAIN || mode == Mode.COMPLETE;
                    if (getTrainer(stream, scheme, template).Count > 0)
                    {
                        streamsValid.Add(stream);
                        break;
                    }
                }
            }

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }

            StreamsBox.ItemsSource = streamsValid;

            if (StreamsBox.Items.Count > 0)
            {
                if(StreamsBox.SelectedItem == null)
                {
                    StreamsBox.SelectedIndex = 0;
                }
              
                StreamsBox.SelectedItem = streamsValid.Find(item => item.Name == Properties.Settings.Default.CMLDefaultStream);
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
                if(AnnotatorsBox.SelectedItem == null)
                {
                    AnnotatorsBox.SelectedIndex = 0;
                }
                
                AnnotatorsBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotator;
            }
        }

        public void GetSessions()
        {
            if (SchemesBox.SelectedItem == null || RolesBox.SelectedItem == null || AnnotatorsBox.SelectedItem == null)
            {
                return;
            }

            Properties.Settings.Default.CMLDefaultAnnotator = AnnotatorsBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultRole = RolesBox.SelectedItem.ToString();
            Properties.Settings.Default.CMLDefaultScheme = SchemesBox.SelectedItem.ToString();
            Properties.Settings.Default.Save();

            if (SessionsBox.HasItems)
            {
                SessionsBox.ItemsSource = null;
            }
            
            if (mode != Mode.COMPLETE 
                && (mode == Mode.TRAIN || !ShowAllSessionsCheckBox.IsChecked.Value))
            {
                // show sessions for which an annotation exists or is missing

                string schemeName = SchemesBox.SelectedItem.ToString();
                ObjectId schemeID = new ObjectId();
                DatabaseHandler.GetObjectID(ref schemeID, DatabaseDefinitionCollections.Schemes, schemeName);

                List<ObjectId> roleIDs = new List<ObjectId>();
                foreach (var item in RolesBox.SelectedItems)
                {
                    string roleName = item.ToString();
                    ObjectId roleID = new ObjectId();
                    DatabaseHandler.GetObjectID(ref roleID, DatabaseDefinitionCollections.Roles, roleName);
                    roleIDs.Add(roleID);
                }

                string annotatorFullName = (string)AnnotatorsBox.SelectedItem;
                string annotatorName = DatabaseHandler.Annotators.Find(a => a.FullName == annotatorFullName).Name;
                ObjectId annotatorID = new ObjectId();
                DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, annotatorName);

                var builder = Builders<BsonDocument>.Filter;
                List<BsonDocument> annotations = new List<BsonDocument>();
                foreach (ObjectId roleID in roleIDs)
                {
                    var filter = builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", roleID) & builder.Eq("isFinished", true);                    
                    annotations.AddRange(DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotations, true, filter));
                }

                List<string> sessionNames = new List<string>();
                if (mode == Mode.TRAIN)
                {
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
                else
                {
                    List<DatabaseSession> allSessions = DatabaseHandler.Sessions;
                    foreach(DatabaseSession s in allSessions)
                    {
                        sessionNames.Add(s.Name);
                    }
                    foreach (BsonDocument annotation in annotations)
                    {
                        string sessionName = "";
                        DatabaseHandler.GetObjectName(ref sessionName, DatabaseDefinitionCollections.Sessions, annotation["session_id"].AsObjectId);
                        sessionNames.Remove(sessionName);                        
                    }
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
            Update();
        }    

        private bool parseTrainerFile(ref Trainer trainer, bool isTemplate)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(trainer.Path);

                string[] tokens = trainer.Path.Split('\\');

                trainer.Name = isTemplate ? Path.GetFileNameWithoutExtension(trainer.Path) : tokens[tokens.Length - 2] + " > " + Path.GetFileNameWithoutExtension(trainer.Path);
                trainer.LeftContext = "0";
                trainer.RightContext = "0";
                trainer.Balance = "none";

                foreach (XmlNode node in doc.SelectNodes("//meta"))
                {
                    var leftContext = node.Attributes["leftContext"];
                    if (leftContext != null)
                    {
                        trainer.LeftContext = leftContext.Value;
                    }
                    var rightContext = node.Attributes["rightContext"];
                    if (rightContext != null)
                    {
                        trainer.RightContext = rightContext.Value;
                    }
                    var balance = node.Attributes["balance"];
                    if (balance != null)
                    {
                        trainer.Balance = balance.Value;
                    }
                }
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
                return false;
            }

            return true;
        }

        private List<Trainer> getTrainer(DatabaseStream stream, DatabaseScheme scheme, bool isTemplate)
        {
            List<Trainer> trainers = new List<Trainer>();

            if (scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                if (stream.SampleRate != scheme.SampleRate)
                {
                    return trainers;
                }
            }

            string[] streamParts = stream.Name.Split('.');
            if (streamParts.Length <= 1)
            {
                return trainers;
            }
            string streamName = streamParts[1];
            for (int i = 2; i < streamParts.Length; i++)
            {
                streamName += "." + streamParts[i];
            }

            string trainerDir = null;

            if (isTemplate)
            {
                trainerDir = Properties.Settings.Default.CMLDirectory + "\\" +
                    Defaults.CML.ModelsFolderName + "\\" +
                    Defaults.CML.ModelsTemplatesFolderName + "\\" +
                    scheme.Type.ToString().ToLower() + "\\" +
                    stream.Type;
            }
            else
            {
                trainerDir = Properties.Settings.Default.CMLDirectory + "\\" +
                    Defaults.CML.ModelsFolderName + "\\" +
                    Defaults.CML.ModelsTrainerFolderName + "\\" +
                    scheme.Type.ToString().ToLower() + "\\" +
                    scheme.Name + "\\" +
                    stream.Type + "{" +
                    streamName + "}\\";                                
            }

            if (Directory.Exists(trainerDir))
            {
                string[] searchDirs = Directory.GetDirectories(trainerDir);
                foreach (string searchDir in searchDirs)
                {
                    string[] trainerFiles = Directory.GetFiles(searchDir, "*." + Defaults.CML.TrainerFileExtension);
                    foreach (string trainerFile in trainerFiles)
                    {
                        Trainer trainer = new Trainer() { Path = trainerFile };
                        if (parseTrainerFile(ref trainer, isTemplate))
                        {
                            trainers.Add(trainer);
                        }
                    }
                }
            }

            return trainers;
        }


        private void StreamsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RolesBox.SelectedItem == null || AnnotatorsBox.SelectedItem == null || SchemesBox.SelectedItem == null)
            {
                return;
            }

            TrainerPathComboBox.Items.Clear();

            if (StreamsBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultStream = StreamsBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                string schemeName = (string)SchemesBox.SelectedItem;
                DatabaseScheme scheme = DatabaseHandler.Schemes.Find(s => s.Name == schemeName);

                if(scheme != null)
                {
                    bool isDiscrete = scheme.Type == AnnoScheme.TYPE.DISCRETE;

                    FillGapCheckBox.IsEnabled = isDiscrete;
                    FillGapTextBox.IsEnabled = isDiscrete;
                    RemoveLabelCheckBox.IsEnabled = isDiscrete;
                    RemoveLabelTextBox.IsEnabled = isDiscrete;

                    bool template = mode == Mode.TRAIN || mode == Mode.COMPLETE;

                    List<Trainer> trainers = getTrainer(stream, scheme, template);
                    foreach (Trainer trainer in trainers)
                    {
                        TrainerPathComboBox.Items.Add(trainer);
                    }
                }
            }
            
            if (TrainerPathComboBox.Items.Count > 0)
            {
                if (TrainerPathComboBox.SelectedItem == null)
                {
                 
                        TrainerPathComboBox.SelectedIndex = 0;

                }
                TrainerPathComboBox.SelectedItem = Properties.Settings.Default.CMLDefaultTrainer;
            }

            Update();
        }

        private void Annotations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetSessions();
            GetStreams();

            Update();
        }

        private void ShowAllSessionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GetSessions();
        }

        private void ShowAllSessionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GetSessions();
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
                Trainer trainer = (Trainer)TrainerPathComboBox.SelectedItem;
                LeftContextTextBox.Text = trainer.LeftContext;
                RightContextTextBox.Text = trainer.RightContext;


                BalanceComboBox.SelectedIndex = 0;
                if (trainer.Balance.ToLower() == "under")
                {
                    BalanceComboBox.SelectedIndex = 1;
                } else if (trainer.Balance.ToLower() == "over")
                {
                    BalanceComboBox.SelectedIndex = 2;
                }
                string database = "";
                if (DatabasesBox.SelectedItem != null)
                {
                    database = DatabasesBox.SelectedItem.ToString();
                }
                TrainerNameTextBox.Text = mode == Mode.COMPLETE ? Path.GetFileName(tempTrainerPath) : database;
            }
        }

        private void Update()
        {
            bool enable = false;

            if (TrainerPathComboBox.Items.Count > 0
                && DatabasesBox.SelectedItem != null 
                && SessionsBox.SelectedItem != null 
                && StreamsBox.SelectedItem != null
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
    }
}
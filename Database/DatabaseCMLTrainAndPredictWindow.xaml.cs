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
            public string Left { get; set; }
            public string Right { get; set; }
            public override string ToString()
            {
                return Name + " [ left=" + Left + " right=" + Right + "]";
            }
        }

        public enum Mode
        {
            TRAIN,
            PREDICT,
        }

        public DatabaseCMLTrainAndPredictWindow(MainHandler handler, Mode mode)
        {
            InitializeComponent();

            this.handler = handler;
            this.mode = mode;

            switch(mode)
            {
                case Mode.TRAIN:
                    Title = "Train Models";
                    ApplyButton.Content = "Train";
                    ShowAllSessionsCheckBox.Visibility = Visibility.Collapsed;
                    ForceCheckBox.Visibility = Visibility.Visible;                    
                    break;
                case Mode.PREDICT:
                    Title = "Predict Annotations";
                    ApplyButton.Content = "Predict";
                    ShowAllSessionsCheckBox.Visibility = Visibility.Visible;
                    ForceCheckBox.Visibility = Visibility.Collapsed;
                    break;
            }

            GetDatabases(DatabaseHandler.DatabaseName);
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (TrainerPathComboBox.SelectedItem == null)
            {
                MessageTools.Warning("select a trainer first");
                return;
            }

            Trainer trainerPath = (Trainer) TrainerPathComboBox.SelectedItem;
            bool force = ForceCheckBox.IsChecked.Value;

            if (!File.Exists(trainerPath.Path))
            {
                MessageTools.Warning("file does not exist '" + trainerPath.Path + "'");
                return;
            }

            if (DatabaseBox.SelectedItem == null || SessionsBox.SelectedItem == null || StreamsBox.SelectedItem == null)
            {
                MessageTools.Warning("select database, annotation, session(s) and stream first");
                return;
            }

            string database = DatabaseHandler.DatabaseName;

            DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;            
            string fileName = Path.GetFileNameWithoutExtension(stream.Name) + "." + trainerPath.Name;            

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

            string scheme = SchemesBox.SelectedItem.ToString();

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

            string annotator = ((DatabaseAnnotator)AnnotatorsBox.SelectedItem).Name;

            logTextBox.Text = "";

            switch (mode)
            {
                case Mode.TRAIN:
                {
                    string trainerDir = Properties.Settings.Default.DatabaseDirectory + "\\" +
                                    Defaults.CML.FolderName + "\\" +
                                    Defaults.CML.ModelsFolderName + "\\" +
                                    Defaults.CML.ModelsTrainerFolderName + "\\" +
                                    stream.StreamType + "\\" +
                                    stream.StreamName + "\\" +
                                    scheme + "\\";
                    Directory.CreateDirectory(trainerDir);
                    string trainerOutPath = trainerDir + fileName;

                    if (force || !File.Exists(trainerOutPath + ".trainer"))
                    {
                        logTextBox.Text += handler.CMLTrainModel(trainerPath.Path,
                            trainerOutPath,
                            Properties.Settings.Default.DatabaseDirectory,
                            Properties.Settings.Default.DatabaseAddress,
                            Properties.Settings.Default.MongoDBUser,
                            Properties.Settings.Default.MongoDBPass,
                            database,
                            sessionList,
                            scheme,
                            rolesList,
                            annotator,
                            stream.Name,
                            trainerPath.Left,
                            trainerPath.Right);
                    }
                    else
                    {
                        logTextBox.Text += "skip " + trainerOutPath + "\n";
                    }

                    break;
                }
                case Mode.PREDICT:
                {
                        if (true || force)
                        {
                            logTextBox.Text += handler.CMLPredictAnnos(trainerPath.Path,
                                Properties.Settings.Default.DatabaseDirectory,
                                Properties.Settings.Default.DatabaseAddress,
                                Properties.Settings.Default.MongoDBUser,
                                Properties.Settings.Default.MongoDBPass,
                                database,
                                sessionList,
                                scheme,
                                rolesList,
                                annotator,
                                stream.Name,
                                trainerPath.Left,
                                trainerPath.Right);
                        }

                        break;
                }
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
            DatabaseBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases();

            foreach (string db in databases)
            {
                DatabaseBox.Items.Add(db);
            }

            Select(DatabaseBox, selectedItem);
        }

        private void DataBaseResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                string name = DatabaseBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                GetAnnotators();
                GetRoles();
                GetSchemes();
            }
        }

        public void GetSchemes()
        {
            SchemesBox.Items.Clear();

            List<DatabaseScheme> items = DatabaseHandler.GetSchemes();
            foreach (DatabaseScheme item in items)
            {
                SchemesBox.Items.Add(item.Name);
            }

        }

        public void GetRoles()
        {
            RolesBox.Items.Clear();

            List<DatabaseRole> items = DatabaseHandler.GetRoles();
            foreach (DatabaseRole item in items)
            {
                RolesBox.Items.Add(item.Name);
            }

        }

        public void GetAnnotators()
        {
            AnnotatorsBox.ItemsSource = null;

            List<BsonDocument> annotators = DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotators, true);

            List<DatabaseAnnotator> items = new List<DatabaseAnnotator>();
            foreach (BsonDocument annotator in annotators)
            {
                items.Add(new DatabaseAnnotator() { Name = annotator["name"].AsString, FullName = annotator["fullname"].AsString });
            }

            AnnotatorsBox.ItemsSource = items;
        }

        

        public void GetSessions()
        {
            if (SchemesBox.SelectedItem == null || RolesBox.SelectedItem == null || AnnotatorsBox.SelectedItem == null)
            {
                return;
            }

            if (SessionsBox.HasItems)
            {
                SessionsBox.ItemsSource = null;
            }

            if (mode == Mode.TRAIN || !ShowAllSessionsCheckBox.IsChecked.Value)
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

                DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
                ObjectId annotatorID = new ObjectId();
                DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, annotator.Name);

                var builder = Builders<BsonDocument>.Filter;
                List<BsonDocument> annotations = new List<BsonDocument>();
                foreach (ObjectId roleID in roleIDs)
                {
                    var filter = builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("role_id", roleID);                    
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
                    List<DatabaseSession> allSessions = DatabaseHandler.GetSessions();
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

                SessionsBox.ItemsSource = sessions;
                    
            }
            else
            {
                // show all sessions

                List<BsonDocument> sessions = DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Sessions, true);

                List<DatabaseSession> items = new List<DatabaseSession>();
                foreach (var c in sessions)
                {
                    items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].ToUniversalTime(), Id = c["_id"].AsObjectId });
                }
                SessionsBox.ItemsSource = items;
            }

            
            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }         
        } 

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetStreams(session);
            }
        }    

        private void GetStreams(DatabaseSession session, string selectedItem = null)
        {
            List<DatabaseStream> streams = DatabaseHandler.GetSessionStreams(session);
            
            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }

            StreamsBox.ItemsSource = streams;

            if (selectedItem != null)
            {
                StreamsBox.SelectedItem = streams.Find(item => item.Name == selectedItem);
            }
        }

        private bool parseTrainerFile(ref Trainer chain)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(chain.Path);

                chain.Name = Path.GetFileNameWithoutExtension(chain.Path);

                foreach (XmlNode node in doc.SelectNodes("//meta"))
                {
                    var leftContext = node.Attributes["leftContext"];
                    if (leftContext != null)
                    {
                        chain.Left = leftContext.Value;
                    }
                    else
                    {
                        chain.Left = "0";
                    }
                    var rightContext = node.Attributes["rightContext"];
                    if (rightContext != null)
                    {
                        chain.Right = rightContext.Value;
                    }
                    else
                    {
                        chain.Right = "0";
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

        private void StreamsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TrainerPathComboBox.Items.Clear();

            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                string scheme = (string)SchemesBox.SelectedItem;

                string trainerDir = "";

                switch (mode)
                {
                    case Mode.TRAIN:
                    {                        
                        trainerDir = Properties.Settings.Default.DatabaseDirectory + "\\" +
                                Defaults.CML.FolderName + "\\" +
                                Defaults.CML.ModelsFolderName + "\\" +
                                Defaults.CML.ModelsTemplatesFolderName + "\\" + 
                                stream.StreamType;
                        break;
                    }
                    case Mode.PREDICT:
                    {
                            trainerDir = Properties.Settings.Default.DatabaseDirectory + "\\" +
                                    Defaults.CML.FolderName + "\\" +
                                    Defaults.CML.ModelsFolderName + "\\" +
                                    Defaults.CML.ModelsTrainerFolderName + "\\" +
                                    stream.StreamType + "\\" +
                                    stream.StreamName + "\\" +
                                    scheme;

                        break;
                    }
                }

                if (Directory.Exists(trainerDir))
                {
                    string[] trainerFiles = Directory.GetFiles(trainerDir, "*." + Defaults.CML.TrainerFileExtension);
                    foreach (string trainerFile in trainerFiles)
                    {
                        Trainer trainer = new Trainer() { Path = trainerFile };
                        if (parseTrainerFile(ref trainer))
                        {
                            TrainerPathComboBox.Items.Add(trainer);
                        }
                    }
                }
            }
            
            if (TrainerPathComboBox.Items.Count > 0)
            {
                TrainerPathComboBox.SelectedIndex = 0;
                ApplyButton.IsEnabled = true;
            }          
            else
            {
                ApplyButton.IsEnabled = false;
            }
        }

        private void Annotations_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetSessions();
        }

        private void ShowAllSessionsCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GetSessions();
        }

        private void ShowAllSessionsCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GetSessions();
        }
    }
}
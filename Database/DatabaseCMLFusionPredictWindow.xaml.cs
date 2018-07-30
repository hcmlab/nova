using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseCMLFusionPredictWindow : Window
    {
        private MainHandler handler;

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


        private List<SchemeAnnotatorPair> fusionstreams = new List<SchemeAnnotatorPair>();

        public DatabaseCMLFusionPredictWindow(MainHandler handler)
        {
            InitializeComponent();

            this.handler = handler;

            Loaded += DatabaseCMLTrainAndPredictWindow_Loaded;

            Title = "Predict Bayesian Network";
            ApplyButton.Content = "Predict";
            TrainOptionsPanel.Visibility = Visibility.Visible;
            ForceCheckBox.Visibility = Visibility.Visible;
        }

        private void DatabaseCMLTrainAndPredictWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GetDatabases(DatabaseHandler.DatabaseName);
            GetAnnotators();
            GetRoles();
            GetSchemes();

           
            GetSessions();
            parseFiles();

            ApplyButton.Focus();
           // Update();
        }

        private void parseFiles()
        {
            try
            {
                string cmlfolderpath = Properties.Settings.Default.CMLDirectory + "\\" +
                  Defaults.CML.FusionFolderName + "\\" +
                  Defaults.CML.FusionBayesianNetworkFolderName + "\\";

                string schemespath = cmlfolderpath + "schemes.set";
                string sessionsspath = cmlfolderpath + "sessions.set";
                StreamReader reader = File.OpenText(schemespath);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    string[] items = line.Split(':');
                    SchemeAnnotatorPair sap = new SchemeAnnotatorPair { Name = items[0], Annotator = items[1] };
                    SchemeandAnnotatorBox.Items.Add(sap);
                }

                SchemeandAnnotatorBox.SelectAll();

                StreamReader reader2 = File.OpenText(sessionsspath);

                while ((line = reader2.ReadLine()) != null)
                {
                    DatabaseSession session = ((List<DatabaseSession>)SessionsBox.ItemsSource).Find(a => a.Name == line);
                    SessionsBox.SelectedItems.Add(session);
                }
                SessionsBox.ScrollIntoView(SessionsBox.SelectedItem);

            }
            catch { }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            bool force = ForceCheckBox.IsChecked.Value;

            string database = DatabaseHandler.DatabaseName;

            string[] sessions = new string[SessionsBox.SelectedItems.Count];
            int i = 0;
            foreach (DatabaseSession item in SessionsBox.SelectedItems)
            {
                sessions[i] = item.Name;
                i++;
            }

            string[] schemes = new string[SchemeandAnnotatorBox.Items.Count];
            int s = 0;
            foreach (SchemeAnnotatorPair item in SchemeandAnnotatorBox.Items)
            {
                schemes[s] = item.Name + ":" + item.Annotator;
                s++;
            }

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

            Properties.Settings.Default.SettingCMLDefaultBN = NetworkBox.SelectedItem.ToString();
           

            if (RolesBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultRole = RolesBox.SelectedItem.ToString();
            }

            if (AnnotatorInputBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultAnnotator = AnnotatorInputBox.SelectedItem.ToString();
            }


            if (SchemeOutputBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultScheme = SchemeOutputBox.SelectedItem.ToString();
            }

            
            Properties.Settings.Default.CMLDefaultAnnotatorPrediction = AnnotatorsBox.SelectedItem.ToString();


            Properties.Settings.Default.Save();

            logTextBox.Text = "";

            string outputscheme = SchemeOutputBox.SelectedItem.ToString();

            string annotator = DatabaseHandler.Annotators.Find(n => n.FullName == Properties.Settings.Default.CMLDefaultAnnotatorPrediction).Name;

            string roleout = Outrole.SelectedItem.ToString();

            bool tocontinuous = false;
            DatabaseScheme outscheme = DatabaseHandler.Schemes.Find(n => n.Name == outputscheme);
            if (outscheme.Type == AnnoScheme.TYPE.CONTINUOUS) tocontinuous = true;

            string cmlfolderpath = Properties.Settings.Default.CMLDirectory + "\\" +
                    Defaults.CML.FusionFolderName + "\\" +
                    Defaults.CML.FusionBayesianNetworkFolderName + "\\";

            string netpath = cmlfolderpath + NetworkBox.SelectedItem.ToString();
            string schemespath = cmlfolderpath + "schemes.set";
            string sessionsspath = cmlfolderpath + "sessions.set";

            System.IO.File.WriteAllLines(schemespath, schemes);
            System.IO.File.WriteAllLines(sessionsspath, sessions);
            float filter = -1.0f;

            if (smoothcheckbox.IsChecked == true) float.TryParse(WindowSmoothBox.Text, out filter);



            logTextBox.Text += handler.CMLPredictBayesFusion(roleout, sessionsspath, schemespath, Properties.Settings.Default.DatabaseAddress, Properties.Settings.Default.MongoDBUser, MainHandler.Decode(Properties.Settings.Default.MongoDBPass), Properties.Settings.Default.DatabaseDirectory, database, outputscheme, rolesList, annotator, tocontinuous, netpath, filter);
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
                getBayesianNetworks();
            }

   
        }

        public void GetSchemes()
        {
            SchemesBox.Items.Clear();

            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseScheme> schemesValid = new List<DatabaseScheme>();
            List<DatabaseScheme> schemes = DatabaseHandler.Schemes;

            foreach (DatabaseScheme scheme in schemes)
            {
                SchemesBox.Items.Add(scheme.Name);
                SchemeOutputBox.Items.Add(scheme.Name);
            }

            if (SchemesBox.Items.Count > 0)
            {
                SchemesBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;
                SchemesBox.ScrollIntoView(SchemesBox.SelectedItem);
                SchemeOutputBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;
                SchemeOutputBox.ScrollIntoView(SchemeOutputBox.SelectedItem);

            }
        }

        public void GetRoles()
        {
            RolesBox.Items.Clear();
            Outrole.Items.Clear();

            foreach (DatabaseRole item in DatabaseHandler.Roles)
            {
               
                    RolesBox.Items.Add(item.Name);
                    Outrole.Items.Add(item.Name);
                
            }

            if (RolesBox.Items.Count > 0)
            {
                RolesBox.SelectedIndex = 0;
                RolesBox.SelectedItem = Properties.Settings.Default.CMLDefaultRole;
                Outrole.SelectedIndex = 0;
            }


           
        }

        public void GetAnnotators()
        {
            AnnotatorsBox.Items.Clear();
            AnnotatorInputBox.Items.Clear();

            foreach (DatabaseAnnotator annotator in DatabaseHandler.Annotators)
            {
                AnnotatorsBox.Items.Add(annotator.FullName);
                AnnotatorInputBox.Items.Add(annotator.FullName);
            }

    
                AnnotatorInputBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotator;
                AnnotatorInputBox.ScrollIntoView(AnnotatorInputBox.SelectedItem);


                AnnotatorsBox.SelectedItem = Properties.Settings.Default.CMLDefaultAnnotatorPrediction;
                AnnotatorsBox.ScrollIntoView(AnnotatorsBox.SelectedItem);

        }

        public void GetSessions()
        {
           

            if (SessionsBox.HasItems)
            {
                SessionsBox.ItemsSource = null;
            }


            List<string> sessionNames = new List<string>();

            List<DatabaseSession> allSessions = DatabaseHandler.Sessions;
            foreach (DatabaseSession s in allSessions)
            {
                sessionNames.Add(s.Name);
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



        private void getBayesianNetworks()
        {
            string networkrDir = Properties.Settings.Default.CMLDirectory + "\\" +
                 Defaults.CML.FusionFolderName + "\\" +
                 Defaults.CML.FusionBayesianNetworkFolderName + "\\";

            if (Directory.Exists(networkrDir))
            {
                string[] networkFiles = Directory.GetFiles(networkrDir, "*." + Defaults.CML.BayesianNetworkextension);
                foreach (string network in networkFiles)
                {
                    NetworkBox.Items.Add(Path.GetFileName(network));
                }
                NetworkBox.SelectedItem = Properties.Settings.Default.SettingCMLDefaultBN;
            }
        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (object scheme in SchemesBox.SelectedItems)
            {
                string annotator = DatabaseHandler.Annotators.Find(a => a.FullName == AnnotatorInputBox.SelectedItem.ToString()).Name;
                SchemeAnnotatorPair stp = new SchemeAnnotatorPair() { Name = scheme.ToString(), Annotator = annotator };

                if (fusionstreams.Find(item => item.Name == stp.Name) == null)
                {
                    fusionstreams.Add(stp);
                    SchemeandAnnotatorBox.Items.Add(stp);

                    SchemeandAnnotatorBox.SelectAll();
                }
            }
        }

        public class SchemeAnnotatorPair
        {
            public string Name { get; set; }

            public string Annotator { get; set; }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            fusionstreams.Remove((SchemeAnnotatorPair)SchemeandAnnotatorBox.SelectedItem);
            SchemeandAnnotatorBox.Items.Remove(SchemeandAnnotatorBox.SelectedItem);
        }

        private void SchemeandClassifierBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            removePair.IsEnabled = false;
            if (SchemeandAnnotatorBox.SelectedItems != null)
            {
                removePair.IsEnabled = true;
            }
        }

    }
}
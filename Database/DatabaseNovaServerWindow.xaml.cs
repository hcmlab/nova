using MathNet.Numerics.Distributions;
using Microsoft.Toolkit.HighPerformance;
using MongoDB.Bson;
using MongoDB.Driver;
using NAudio.CoreAudioApi;
using Newtonsoft.Json;
using Octokit;
using Org.Mentalis.Security.Certificates;
using SharpDX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Printing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using static ssi.AnnoScheme;
using static ssi.AnnoTierAttributesWindow;
using static ssi.DatabaseCMLExtractFeaturesWindow;
using Path = System.IO.Path;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseNovaServerWindow : Window
    {
        private MainHandler handler;
        private Mode mode;
        private Status status = 0;
        private List<SelectedDatabaseAndSessions> selectedDatabaseAndSessions = new List<SelectedDatabaseAndSessions>();
        private List<string> databases = new List<string>();
        static MultipartFormDataContent CMLpredictionContent;
        List<AnnoScheme.Attribute> ModelSpecificAttributes = new List<AnnoScheme.Attribute>();
        string lockedScheme = null;
        List<string> AllUsedRoles = new List<string>();
        List<string> AllUsedStreams = new List<string>();
        List<string> AllUsedSchemes = new List<string>();

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private Thread logThread = null;
        private Thread statusThread = null;
        private Thread predictAndReloadThread = null;

        Dictionary<string, List<UIElement>> SpecificModelattributesresult;
        private static IList sessions = null;
        private static string sessionList = "";
        static bool CML_TrainingStarted = false;
        static bool CML_PredictionStarted = false;
        bool ShowAnnotatorBox = false;

        ChainCategories chainCategories = new ChainCategories();
        public class Trainer
        {
            public string Path { get; set; }
            public string Name { get; set; }
            public string LeftContext { get; set; }
            public string RightContext { get; set; }
            public string Balance { get; set; }
            public string Backend { get; set; }
            public string Script { get; set; }
            public string Weight { get; set; }

            public string OptStr { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        public class Input
        {
            public Input()
            {
                Label = "";
                DefaultValue = "";
                Attributes = new List<string>();
                AttributeType = AnnoScheme.AttributeTypes.STRING;
                ExtraAttributes = new List<string>();
                ExtraAttributeType = AnnoScheme.AttributeTypes.LIST;
                Origin = "";
            }
            public string Label { get; set; }
            public List<string> Attributes { get; set; }
            public List<string> ExtraAttributes { get; set; }
            public string DefaultValue { get; set; }
            public AnnoScheme.AttributeTypes AttributeType { get; set; }
            public AnnoScheme.AttributeTypes ExtraAttributeType { get; set; }
            public string Origin { get; set; }
        }


        public class SessionSet
        {
            public enum Type
            {
                ALL,
                FINISHED,
                MISSING,
                FILE,
            }

            public string Path { get; set; }
            public string Name { get; set; }
            public Type Set { get; set; }

            public override string ToString()
            {
                switch (Set)
                {
                    case Type.ALL:
                        return "<All sessions>";
                    case Type.FINISHED:
                        return "<Sessions with annotations marked as finished>";
                    case Type.MISSING:
                        return "<Sessions that do not have an annotation yet>";

                }
                return Name;
            }
        }

        private string tempTrainerPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        public enum Mode
        {
            EXTRACT
        }

        public enum Status
        {
            WAITING = 0,
            RUNNING = 1,
            FINISHED = 2,
            ERROR = 3

        }


        private class statusObject
        {
            private int key;
            private string text;
            private SolidColorBrush color;
            public statusObject(int key, string text, SolidColorBrush color)
            {
                this.key = key;
                this.text = text;
                this.color = color;
            }

            public string getText()
            {
                return text;
            }

            public SolidColorBrush getColor()
            {
                return color;
            }
        }

        private statusObject[] states = new statusObject[4];

        private static bool pythonCaseOn = true;

        private bool handleSelectionChanged = false;

        public DatabaseNovaServerWindow(MainHandler handler)
        {
            InitializeComponent();
            createStatusObjects();

            GetChains("All");
            GetAnnotators();

            this.handler = handler;
            this.mode = Mode.EXTRACT;


            ModeTabControl.SelectedIndex = (int)mode;
            LeftContextTextBox.IsEnabled = true;
            FrameSizeTextBox.IsEnabled = true;
            RightContextTextBox.IsEnabled = true;
            FeatureNameTextBox.IsEnabled = true;
            

            int endtime = -1;

            if (AnnoTier.Selected != null)
            {
                if (AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    double endtimeInSec = MainHandler.Time.TotalDuration;
                    double resample = endtimeInSec * Properties.Settings.Default.DefaultDiscreteSampleRate;
                    endtime = (int)((Math.Round(resample)) / Properties.Settings.Default.DefaultDiscreteSampleRate * 1000);
                }
                else
                {
                    double endtimeInSec = ((AnnoListItem)handler.control.annoListControl.annoDataGrid.Items[handler.control.annoListControl.annoDataGrid.Items.Count - 1]).Stop;
                    endtime = (int)(Math.Round(value: endtimeInSec, digits: 2) * 1000);
                }

                int startTime = -1;

                if (AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    double startTimeInSec = MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition);
                    double resample = startTimeInSec * Properties.Settings.Default.DefaultDiscreteSampleRate;
                    startTime = (int)((Math.Round(resample)) / Properties.Settings.Default.DefaultDiscreteSampleRate * 1000);
                }
                else
                {
                    double startTimeInSec = MainHandler.Time.CurrentPlayPosition;
                    startTime = (int)(Math.Round(value: startTimeInSec, digits: 2) * 1000);
                }

   
            }


           Chain trainer = (Chain)ChainsBox.SelectedItem;

            if (trainer != null && (trainer.Backend.ToUpper() == "PYTHON" || trainer.Backend.ToUpper() == "NOVA-SERVER"))
            {
                logThread = new Thread(new ThreadStart(tryToGetLog));
                statusThread = new Thread(new ThreadStart(tryToGetStatus));
               // predictAndReloadThread = new Thread(new ThreadStart(predictAndReloadInCompleteCase));
                logThread.Start();
                statusThread.Start();
                //predictAndReloadThread.Start();
            }

            changeFrontendInPythonBackEndCase();

            Loaded += Window_Loaded;
        }



        private void createStatusObjects()
        {
            string[] stateStrings = { "Connected to Server!", "Process running...", "Process finished!", "An error occurred!" };
            SolidColorBrush[] stateColors = { new SolidColorBrush(Colors.LightGray), new SolidColorBrush(Colors.Orange),
                                              new SolidColorBrush(Colors.Green), new SolidColorBrush(Colors.Red) };

            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new statusObject(i, stateStrings[i], stateColors[i]);
            }
        }


        static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        private string getIdHash()
        {
            DatabaseAnnotator annotator = null;
            Chain chain = null;
            string ModelSpecificOptString = "";


            this.Dispatcher.Invoke(() =>
            {
    
                annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
                chain = (Chain)ChainsBox.SelectedItem;
                chain.Name = chain.Name.Split(' ')[0];
                setSessionList();
                ModelSpecificOptString = AttributesResult();

            });

            string database = DatabaseHandler.DatabaseName;
            

            int result = GetDeterministicHashCode("database" + database + "annotator" + annotator.Name + "sessions" + sessionList + "username" + Properties.Settings.Default.MongoDBUser + "chain" + chain.Name + "opts" + ModelSpecificOptString);

            var jobIDhash = (Math.Abs(result)).ToString();
            int MaxLength = 8;
            if (jobIDhash.Length > MaxLength)
                jobIDhash = jobIDhash.ToString().Substring(0, MaxLength);

            return jobIDhash;
        }



        private void tryToGetLog()
        {
            Dictionary<string, string> response = null;
            while (pythonCaseOn)
            {
                // MultipartFormDataContent content = getContent();

                var jobIDhash = getIdHash();

                var content = new MultipartFormDataContent
                {
                    { new StringContent(jobIDhash), "jobID"  }
                };
                // else case is handled in status-thread (see tryToGetStatus method)
                if (content != null)
                {
                    try
                    {
                        response = handler.getLogFromServer(content);

                        this.Dispatcher.Invoke(() =>
                        {
                            logTextBox.Text = response["message"];
                        });
                    }
                    catch (Exception ex)
                    {
                        handleError(ex);
                    }
                }

                Thread.Sleep(1500);
            }
        }

        private void tryToGetStatus()
        {
            Dictionary<string, string> response = null;
            bool updatedb = false;
            while (pythonCaseOn)
            {
                var jobIDhash = getIdHash();

                var content = new MultipartFormDataContent
                {
                    { new StringContent(jobIDhash), "jobID"  }
                };
                if (content != null)
                {
                    try
                    {
                        response = handler.getStatusFromServer(content);
                        this.status = (Status)Int16.Parse(response["status"]);

                        if (!(CML_PredictionStarted))
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                statusLabel.Content = states[(int)this.status].getText();
                                statusLabel.Background = states[(int)this.status].getColor();
                            });
                        }

                        if (this.status == Status.RUNNING)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                this.ApplyButton.IsEnabled = false;
                                this.Cancel_Button.IsEnabled = true;
                                updatedb = true;
                            });
                        }
                        else if (this.status == Status.FINISHED)
                        {
                            if (!(CML_PredictionStarted))
                            {
                                this.Dispatcher.Invoke(() =>
                                {
                                   
                                    this.ApplyButton.IsEnabled = true;
                                    this.Cancel_Button.IsEnabled = false;
                                });

                                if(this.status == Status.FINISHED && updatedb) {
                                    DatabaseHandler.UpdateDatabaseLocalLists();
                                    updatedb = false;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        handleError(ex);
                    }
                }
                else
                {
                    this.status = Status.ERROR;

                    this.Dispatcher.Invoke(() =>
                    {
                        statusLabel.Content = states[(int)this.status].getText();
                        statusLabel.Background = states[(int)this.status].getColor();
                        ApplyButton.IsEnabled = false;
                        logTextBox.Text = "Please select database, schema, stream, session, annotator and the trainer script!";
                    });
                }
                Thread.Sleep(1500);
            }

            this.Dispatcher.Invoke(() =>
            {
               // this.ApplyButton.IsEnabled = true;
               // logTextBox.Text = "";
            });
        }

    

        private void handleError(Exception ex)
        {
            this.status = Status.ERROR;

            this.Dispatcher.Invoke(() =>
            {
                statusLabel.Content = states[(int)this.status].getText();
                statusLabel.Background = states[(int)this.status].getColor();
                ApplyButton.IsEnabled = false;
                Cancel_Button.IsEnabled = false;
                logTextBox.Text = "No connection to server!";
            });
        }

        private void setSessionList()
        {
            sessions = SessionsBox.SelectedItems;
            sessionList = "";
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
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
      
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            CML_TrainingStarted = false;
            CML_PredictionStarted = false;
            pythonCaseOn = false;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you really want to cancel the current action?", "Warning", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var jobIDhash = getIdHash();

                var content = new MultipartFormDataContent
                {
                    { new StringContent(jobIDhash), "jobID"  }
                };
 
                handler.cancleCurrentAction(content);
                this.Cancel_Button.IsEnabled = false;
                CML_TrainingStarted = false;
                CML_PredictionStarted = false;
            }
        }


        #region Window

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switchMode();

            GetDatabases(DatabaseHandler.DatabaseName);

     
            ApplyButton.Focus();

            handleSelectionChanged = true;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        #endregion

        #region Switch

        private void switchMode()
        {
            switch (mode)
            {
     
                case Mode.EXTRACT:

                    Title = "Extract";
                    ApplyButton.Content = "Extract";
                    //TrainerLabel.Content = "Chain";

                    ExtractPanel.Visibility = System.Windows.Visibility.Visible;
                    ForceCheckBox.Visibility = System.Windows.Visibility.Visible;
                    LosoCheckBox.Visibility = System.Windows.Visibility.Collapsed;

                    //AnnotationSelectionBox.Visibility = System.Windows.Visibility.Visible;
                    //removePair.Visibility = System.Windows.Visibility.Visible;
                    //multidatabaseadd.Visibility = System.Windows.Visibility.Visible;
                    //multidatabaselabel.Visibility = System.Windows.Visibility.Visible;


                    break;


        
            }
        }

        #endregion

        #region Apply

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Chain chain = (Chain)ChainsBox.SelectedItem;
            bool force =  ForceCheckBox.IsChecked.Value;

            if (!File.Exists(chain.Path))
            {
                MessageTools.Warning("file does not exist '" + chain.Path + "'");
                return;
            }

            string database = DatabaseHandler.DatabaseName;

           // DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
            //setSessionList();
           // DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;

            string rolesList = "";
            var roles = RolesBox.SelectedItems;
            foreach (DatabaseRole role in roles)
            {
                if (rolesList == "")
                {
                    rolesList += role.Name;
                }
                else
                {
                    rolesList += ";" + role.Name;
                }

            }

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

            DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
        

            string trainerLeftContext = LeftContextTextBox.Text;
            string trainerRightContext = RightContextTextBox.Text;

            logTextBox.Text = "";
   

            if (chain.Backend.ToUpper() == "NOVA-SERVER" || chain.Backend.ToUpper() == "PYTHON") 
            {
              handlePythonBackend(chain, annotator, database, trainerLeftContext, trainerRightContext, rolesList, sessionList);
            }
        }

 
        private void handlePythonBackend(Chain chain, DatabaseAnnotator annotator, string database, string chainLeftContext, string chainRightContext, string rolesList, string sessionsList)
        {
            // this.ApplyButton.IsEnabled = false;

            // string streamName = getStreamName(stream);
            // string trainerDir = getTrainerDir(trainer, streamName, scheme, stream);
            // string trainerOutPath = getTrainerOutPath(trainer, trainerDir);

            string frameSize = FrameSizeTextBox.Text;
            string suffix = FeatureNameTextBox.Text;


            //if (scheme.Type == AnnoScheme.TYPE.CONTINUOUS || scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON
            //    || scheme.Type == AnnoScheme.TYPE.POINT || scheme.Type == AnnoScheme.TYPE.POLYGON)
            //{
            //    frameSize = 1000.0 / scheme.SampleRate;
            //}
            //else if (scheme.Type == AnnoScheme.TYPE.FREE || scheme.Type == AnnoScheme.TYPE.DISCRETE)
            //{
            //    string frameSizestring = FrameSizeTextBox.Text;
            //    if (frameSizestring.EndsWith("ms"))
            //    {
            //        frameSizestring = frameSizestring.Remove(frameSizestring.Length - 2);
            //        frameSize = double.Parse(frameSizestring);
            //    }
            //    else if (frameSizestring.EndsWith("s"))
            //    {
            //        frameSizestring = frameSizestring.Remove(frameSizestring.Length - 1);
            //        frameSize = double.Parse(frameSizestring) * 1000;
            //    }
            //    else
            //    {
            //        MessageBox.Show("Please use Seconds or Milliseconds for the Framesize");
            //        return;
            //    }
            //}

            int endTime = 0;
            int startTime = 0;
            //var trainerScriptPath = Directory.GetParent(trainer.Path) + "\\" + trainer.Script;
            //string relativeScriptPath = trainerScriptPath.Replace(Properties.Settings.Default.CMLDirectory, "");
            string relativeChainPath = chain.Path.Replace(Properties.Settings.Default.CMLDirectory, "").Remove(0, 1);

            //TODO traineroutpath on predict

            //FileInfo file_info = new FileInfo(trainerOutPath);
           // string traineroutfolder = file_info.DirectoryName;
           // string trainer_name = file_info.Name;

            bool deleteFiles = false;


            //string relativeTrainerOutputDirectory = traineroutfolder.Replace(Properties.Settings.Default.CMLDirectory, "").Remove(0, 1);
            bool flattenSamples = false;


            setSessionList();
            string ModelSpecificOptString = AttributesResult();

            string streams = "";
            foreach(var v in AllUsedStreams)
            {
                streams += v + ";";
            }
            if (streams.Length > 1)  streams = streams.Remove(streams.Length - 1);

            string schemes = "";
            foreach (var v in AllUsedSchemes)
            {
                schemes += v + ";";
            }
            if(schemes.Length>1) schemes = schemes.Remove(schemes.Length - 1);


            string roles = "";
            foreach (var v in AllUsedRoles)
            {
                roles += v + ";";
            }
            if (roles.Length > 1) roles = roles.Remove(roles.Length - 1);

            bool multi_role_input = true;


            //If one part of the chain doesnt allow multi user input, dont use it.
            foreach (Transformer t in chain.GetTransformers()){
                if (t.Multi_role_input == false)
                {
                    multi_role_input = false;
                    break;
                }
            }
            if (!multi_role_input)
            {
                roles = rolesList;
            }




            string filenameSuffix = "";

            var jobIDhash = getIdHash();

            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(flattenSamples.ToString()), "flattenSamples" },
                { new StringContent(relativeChainPath), "chainFilePath" },
                { new StringContent(Properties.Settings.Default.DatabaseAddress), "dbServer" },
                { new StringContent(Properties.Settings.Default.MongoDBUser), "dbUser" },
                { new StringContent(MainHandler.Decode(Properties.Settings.Default.MongoDBPass)), "dbPassword" },
                { new StringContent(database), "database" },
                { new StringContent(sessionsList), "sessions" },
                { new StringContent(schemes), "scheme" },
                { new StringContent(roles), "roles" },
                { new StringContent(annotator.Name), "annotator" },
                { new StringContent(streams), "streamName" },
                { new StringContent(chainLeftContext), "leftContext" },
                { new StringContent(chainRightContext), "rightContext" },
                { new StringContent(frameSize), "frameSize" },
                { new StringContent(filenameSuffix), "fileNameSuffix" },
                { new StringContent(ModelSpecificOptString), "optStr" },
                { new StringContent(suffix), "suffix"  },
                { new StringContent(jobIDhash), "jobID"  }
            };

            if (this.mode == Mode.EXTRACT)
            {
                _ = handler.pythonBackEndExtraction(content);
            }
 

        }

        private void changeFrontendInPythonBackEndCase()
        {
            Chain chain = (Chain)ChainsBox.SelectedItem;

            if (chain != null && (chain.Backend.ToUpper() == "PYTHON" || chain.Backend.ToUpper() == "NOVA-SERVER"))
            {
                pythonCaseOn = true;
                logThread = new Thread(new ThreadStart(tryToGetLog));
                statusThread = new Thread(new ThreadStart(tryToGetStatus));
               // predictAndReloadThread = new Thread(new ThreadStart(predictAndReloadInCompleteCase));
                logThread.Start();
                statusThread.Start();
                //predictAndReloadThread.Start();
                bool ClearUI = true;
                foreach(Transformer t in chain.GetTransformers())
                {
                    if (t.OptStr != "")
                        AddTrainerSpecificOptionsUIElements(t.OptStr, t.Multi_role_input, ClearUI);
                        ClearUI = false;
                }
              

                this.statusLabel.Visibility = Visibility.Visible;
                this.Cancel_Button.Visibility = Visibility.Visible;
                this.ModelSpecificOptions.Visibility = Visibility.Visible;

        
                    ModeTabControl.SelectedIndex = (int)mode;
      
                
            }
            else
            {
           

                //this.ApplyButton.IsEnabled = true;
                pythonCaseOn = false;
                this.statusLabel.Visibility = Visibility.Collapsed;

                this.Cancel_Button.Visibility = Visibility.Collapsed;
                this.ModelSpecificOptions.Visibility = Visibility.Collapsed;
                this.logTextBox.Text = "";
            }


        }

        #endregion

        #region Getter

        public void GetDatabases(string select = null)
        {
            DatabasesBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases();

            foreach (string db in databases)
            {
                DatabasesBox.Items.Add(db);
            }

            if (select != null)
            {
                foreach (string item in DatabasesBox.Items)
                {
                    if (item == select)
                    {
                        DatabasesBox.SelectedItem = item;
                    }
                }
            }
        }

        public void GetSchemes()
        {
            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseScheme> schemesValid = new List<DatabaseScheme>();
            List<DatabaseScheme> schemes = DatabaseHandler.Schemes;

            foreach (DatabaseScheme scheme in schemes)
            {
                foreach (DatabaseStream stream in streams)
                {
                    bool template = mode == Mode.EXTRACT;
                    //if (getTrainer(stream, scheme, template).Count > 0)
                    //{

                    //    if (lockedScheme == null) schemesValid.Add(scheme);
                    //    else if (scheme.Name == lockedScheme) schemesValid.Add(scheme);
                    //    break;
                    //}
                }
            }

            SchemesBox.ItemsSource = schemesValid;

            if (SchemesBox.Items.Count > 0)
            {
                DatabaseScheme scheme = ((List<DatabaseScheme>)SchemesBox.ItemsSource).Find(s => s.Name == Properties.Settings.Default.CMLDefaultScheme);
                if (scheme != null)
                {
                    SchemesBox.SelectedItem = scheme;
                }
                if (SchemesBox.SelectedItem == null)
                {
                    SchemesBox.SelectedIndex = 0;
                }
                SchemesBox.ScrollIntoView(SchemesBox.SelectedItem);
            }
        }

        public void GetRoles()
        {
            RolesBox.ItemsSource = DatabaseHandler.Roles;

            if (RolesBox.Items.Count > 0)
            {

                    string[] items = Properties.Settings.Default.CMLDefaultRole.Split(';');
                    foreach (string item in items)
                    {
                        DatabaseRole role = ((List<DatabaseRole>)RolesBox.ItemsSource).Find(r => r.Name == item);
                        if (role != null)
                        {
                            RolesBox.SelectedItems.Add(role);
                        }
                    }
                    if (RolesBox.SelectedItem == null)
                    {
                        RolesBox.SelectAll();
                    }
                




                RolesBox.ScrollIntoView(RolesBox.SelectedItem);
            }
        }

        private void GetStreams()
        {
            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseStream> streamsValid = new List<DatabaseStream>();
            List<DatabaseScheme> schemes = DatabaseHandler.Schemes;

            foreach (DatabaseStream stream in streams)
            {

                //foreach (DatabaseScheme scheme in schemes)
                //{
                DatabaseScheme scheme = ((DatabaseScheme)SchemesBox.SelectedItem);
                bool template = mode == Mode.EXTRACT;
                //if (getTrainer(stream, scheme, template).Count > 0)
                //{
                //    streamsValid.Add(stream);
                //    // break;

                //}
                // }



            }

            StreamsBox.ItemsSource = streamsValid;

            if (StreamsBox.Items.Count > 0)
            {
                DatabaseStream stream = ((List<DatabaseStream>)StreamsBox.ItemsSource).Find(s => s.Name == Properties.Settings.Default.CMLDefaultStream);
                if (stream != null)
                {
                    StreamsBox.SelectedItem = stream;
                }
                if (StreamsBox.SelectedItem == null)
                {
                    StreamsBox.SelectedIndex = 0;
                }

                double sr = ((DatabaseStream)StreamsBox.SelectedItem).SampleRate;
                FrameSizeTextBox.Text = (1000.0 / sr).ToString() + "ms";

                StreamsBox.ScrollIntoView(StreamsBox.SelectedItem);
            }
        }

        public void GetAnnotators()
        {
            AnnotatorsBox.ItemsSource = DatabaseHandler.Annotators;

            if (AnnotatorsBox.Items.Count > 0)
            {
                string annotatorName = Properties.Settings.Default.CMLDefaultAnnotator;
                AnnotatorsBox.IsEnabled = true;

                if (DatabaseHandler.CheckAuthentication() > DatabaseAuthentication.READWRITE)
                {
                    annotatorName = Properties.Settings.Default.CMLDefaultAnnotator;
                }

                // check for last user
                DatabaseAnnotator annotator = ((List<DatabaseAnnotator>)AnnotatorsBox.ItemsSource).Find(a => a.Name == annotatorName);
                if (annotator != null)
                {
                    AnnotatorsBox.SelectedItem = annotator;
                }

                // check for gold
                if (AnnotatorsBox.SelectedItem == null)
                {
                    annotator = ((List<DatabaseAnnotator>)AnnotatorsBox.ItemsSource).Find(a => a.Name == Defaults.CML.GoldStandardName);
                    if (annotator != null)
                    {
                        AnnotatorsBox.SelectedItem = annotator;
                    }
                }

                // select first
                if (AnnotatorsBox.SelectedItem == null)
                {
                    AnnotatorsBox.SelectedIndex = 0;
                }

                AnnotatorsBox.ScrollIntoView(AnnotatorsBox.SelectedItem);
            }
        }

        public void GetSessions()
        {
       

            //if (mode == Mode.TRAIN || mode == Mode.EVALUATE)
            //{
            //    // show user sessions only

            //    List<DatabaseSession> sessions = new List<DatabaseSession>();
            //    DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
            //    DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;
            //    foreach (DatabaseRole role in RolesBox.SelectedItems)
            //    {
            //        List<DatabaseAnnotation> annotations = DatabaseHandler.GetAnnotations(scheme, role, annotator);
            //        foreach (DatabaseAnnotation annotation in annotations)
            //        {
            //            DatabaseSession session = DatabaseHandler.Sessions.Find(s => s.Name == annotation.Session);
            //            if (session != null)
            //            {
            //                if (!sessions.Contains(session))
            //                {
            //                    sessions.Add(session);
            //                }
            //            }
            //        }
            //    }
            //    SessionsBox.ItemsSource = sessions.OrderBy(s => s.Name).ToList();
            //}
            //else
            {
                SessionsBox.ItemsSource = DatabaseHandler.Sessions;
                SessionsBox.SelectedIndex = 0;  
            }
        }

        #endregion

        #region Chain 

        public void GetChains(string Category)
        {
            ChainsBox.Items.Clear();

                List<Chain> chains = getChains();
                foreach (Chain chain in chains)
                {
                        
                    if(Category == "ALL")
                    {
                       ChainsBox.Items.Add(chain);
                    }
                    else if (Category == chain.Category)
                    {
                        ChainsBox.Items.Add(chain);
                    }
                   
                }
            

            if (ChainsBox.Items.Count > 0)
            {
                Chain chain = null;

                foreach (Chain c in ChainsBox.Items)
                {
                    if (c.Name == Properties.Settings.Default.CMLDefaultChain)
                    {
                        chain = c;
                        break;
                    }
                }

                if (chain != null)
                {
                    ChainsBox.SelectedItem = chain;
                }
                if (ChainsBox.SelectedItem == null)
                {
                    ChainsBox.SelectedIndex = 0;
                }
            }

            if (ChainsBox.SelectedItem != null)
            {
                Chain chain = (Chain)ChainsBox.SelectedItem;
                ChainPathLabel.Content = chain.Path;
                ChainDescription.Content = chain.Description;
                LeftContextTextBox.Text = chain.LeftContext;
                FrameSizeTextBox.Text = chain.FrameStep;
                RightContextTextBox.Text = chain.RightContext;

            }
        }

        private bool parseChainFile(ref Chain chain)
        {
            XmlDocument doc = new XmlDocument();
            try
            {
      
                doc.Load(chain.Path);
       


                chain.Name = Path.GetFileNameWithoutExtension(chain.Path);

                foreach (XmlNode node in doc.SelectNodes("//meta"))
                {
                    chain.FrameStep = node.Attributes["frameStep"].Value;
                    var leftContext = node.Attributes["leftContext"];
                    if (leftContext != null)
                    {
                        chain.LeftContext = leftContext.Value;
                    }
                    else
                    {
                        chain.LeftContext = "0";
                    }
                    var rightContext = node.Attributes["rightContext"];
                    if (rightContext != null)
                    {
                        chain.RightContext = rightContext.Value;
                    }
                    else
                    {
                        chain.RightContext = "0";
                    }

                    var Backend = node.Attributes["backend"];
                    if (Backend != null)
                    {
                        chain.Backend = Backend.Value;
                    }
                    else
                    {
                        chain.Backend = "SSI";
                    }

                    var Description = node.Attributes["description"];
                    if (Description != null)
                    {
                        chain.Description = Description.Value;
                    }
                    else
                    {
                        chain.Description = "";
                    }

                    var Category = node.Attributes["category"];
                    if (Category != null)
                    {
                        chain.Category = Category.Value;
                        chainCategories.AddCategory(chain.Category);
                       
                    }
                    else
                    {
                        chain.Category = "";
                    }



                }
                foreach (XmlNode node in doc.SelectNodes("//filter"))
                {
                    foreach (XmlNode itemnode in node.SelectNodes("//item"))
                    {
                        Transformer t = new Transformer();
                        var Name = itemnode.Attributes["create"];

                        if (Name != null)
                        {
                            t.Name = Name.Value;
                        }
                        else continue;
                        var Script = itemnode.Attributes["script"];
                        if (Script != null)
                        {
                            t.Script = Script.Value;
                        }
                        var Syspath = itemnode.Attributes["syspath"];
                        if (Syspath != null)
                        {
                            t.Syspath = Syspath.Value;
                        }
                        var OptStr = itemnode.Attributes["optsstr"];
                        if (OptStr != null)
                        {
                            t.OptStr = OptStr.Value;
                        }
                        var Multi_role_input = itemnode.Attributes["multi_role_input"];
                        if (Multi_role_input != null)
                        {
                            t.Multi_role_input = bool.Parse(itemnode.Attributes["multi_role_input"].Value);
                        }
                        else t.Multi_role_input = true;

                        t.Type = "filter";
                        chain.AddTransformer(t);
                    }
                }
                foreach (XmlNode node in doc.SelectNodes("//feature"))
                {
                    foreach (XmlNode itemnode in node.SelectNodes("//item"))
                    {
                        Transformer t = new Transformer();
                        var Name = itemnode.Attributes["create"];
                        if (Name != null)
                        {
                            t.Name = Name.Value;
                        }
                        else continue;
                        var Script = itemnode.Attributes["script"];
                        if (Script != null)
                        {
                            t.Script = Script.Value;
                        }
                        var Syspath = itemnode.Attributes["syspath"];
                        if (Syspath != null)
                        {
                            t.Syspath = Syspath.Value;
                        }
                        var OptStr = itemnode.Attributes["optsstr"];
                        if (OptStr != null)
                        {
                            t.OptStr = OptStr.Value;
                        }
                        var Multi_role_input = itemnode.Attributes["multi_role_input"];
                        if (Multi_role_input != null)
                        {
                            t.Multi_role_input = bool.Parse(itemnode.Attributes["multi_role_input"].Value);
                        }
                        else t.Multi_role_input = true;

                        t.Type = "feature";
                        chain.AddTransformer(t);
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

        private List<Chain> getChains()
        {
            List<Chain> chains = new List<Chain>();
            string[] types = {"nova-server"};


            foreach (string type in types)
            {
                string chainDir = Properties.Settings.Default.CMLDirectory +
                        "\\" + Defaults.CML.ChainFolderName +
                        "\\" + type;
                if (Directory.Exists(chainDir))
                {
                    string[] chainFiles = Directory.GetFiles(chainDir, "*." + Defaults.CML.ChainFileExtension, SearchOption.AllDirectories);

                  
                    if (chainCategories.GetCategories().Count == 0)
                    {
                        chainCategories.AddCategory("ALL");
                        if (ChainCategoryBox.ItemsSource == null)
                        {
                            ChainCategoryBox.ItemsSource = chainCategories.GetCategories();
                            ChainCategoryBox.SelectedIndex = 0;
                        }
                    }


                    foreach (string chainFile in chainFiles)
                    {
                        Chain chain = new Chain() { Path = chainFile };
                        if (parseChainFile(ref chain))
                        {
                            chains.Add(chain);
                        }
                    }
                }
            }

            return chains;
        }


        #endregion

        #region Sets

        private void LoadSessionSets()
        {
            SelectSessionSetComboBox.ItemsSource = null;

            List<SessionSet> sets = new List<SessionSet>();


            if (mode == Mode.EXTRACT)
            {
                sets.Add(new SessionSet()
                {
                    Set = SessionSet.Type.ALL,
                });
                sets.Add(new SessionSet()
                {
                    Set = SessionSet.Type.FINISHED,
                });
            }


            foreach (var location in Defaults.LocalDataLocations())
            {
                string root = location + '\\' + DatabaseHandler.DatabaseName;
                if (Directory.Exists(root))
                {

                    string[] paths = Directory.GetFiles(root, "*." + Defaults.CML.SessionSetExtension);

                    if (paths.Length > 0)
                    {
                        foreach (string path in paths)
                        {
                            sets.Add(new SessionSet()
                            {
                                Set = SessionSet.Type.FILE,
                                Path = path,
                                Name = Path.GetFileNameWithoutExtension(path)
                            });
                        }
                    }

                }
            }
            SelectSessionSetComboBox.ItemsSource = sets;

        }

        private void SelectSessionsFromFile(SessionSet set)
        {
            if (File.Exists(set.Path))
            {
                string[] items = File.ReadAllLines(set.Path);
                foreach (string item in items)
                {
                    List<DatabaseSession> sessions = (List<DatabaseSession>)SessionsBox.ItemsSource;
                    DatabaseSession session = sessions.Find(s => s.Name == item);
                    if (session != null)
                    {
                        SessionsBox.SelectedItems.Add(session);
                    }
                }
            }
        }

        private void SelectFinishedSessions()
        {
            List<DatabaseSession> sessions = (List<DatabaseSession>)SessionsBox.ItemsSource;
            if (sessions == null)
            {
                return;
            }

            DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;
            DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
            foreach (DatabaseRole role in RolesBox.SelectedItems)
            {
                List<DatabaseAnnotation> annotations = DatabaseHandler.GetAnnotations(scheme, role, annotator);
                foreach (DatabaseAnnotation annotation in annotations)
                {
                    if (annotation.IsFinished)
                    {
                        DatabaseSession session = sessions.Find(s => s.Name == annotation.Session);
                        if (session != null)
                        {
                            SessionsBox.SelectedItems.Add(session);
                        }
                    }
                }
            }
        }

        private void SelectMissingSessions()
        {
            List<DatabaseSession> sessions = (List<DatabaseSession>)SessionsBox.ItemsSource;
            if (sessions == null)
            {
                return;
            }

            SessionsBox.SelectAll();

            DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;
            DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
            foreach (DatabaseRole role in RolesBox.SelectedItems)
            {
                List<DatabaseAnnotation> annotations = DatabaseHandler.GetAnnotations(scheme, role, annotator);
                foreach (DatabaseAnnotation annotation in annotations)
                {
                    DatabaseSession session = sessions.Find(s => s.Name == annotation.Session);
                    if (session != null)
                    {
                        SessionsBox.SelectedItems.Remove(session);
                    }
                }
            }
        }

        private void ApplySessionSet()
        {
    

            handleSelectionChanged = false;

            SessionSet set = (SessionSet)SelectSessionSetComboBox.SelectedItem;
            if (set != null)
            {
                SessionsBox.SelectedItems.Clear();

                switch (set.Set)
                {
                    case SessionSet.Type.ALL:

                        SessionsBox.SelectAll();
                        break;

                    case SessionSet.Type.MISSING:

                        SelectMissingSessions();
                        break;

                    case SessionSet.Type.FINISHED:

                        SelectFinishedSessions();
                        break;

                    case SessionSet.Type.FILE:

                        SelectSessionsFromFile(set);
                        break;
                }
            }

            if (SessionsBox.SelectedItems != null)
            {
                SessionsBox.ScrollIntoView(SessionsBox.SelectedItem);
            }

            handleSelectionChanged = true;
        }

        #endregion

        #region Update

        private void Update(Mode oldmode)
        {
            SaveDefaults(oldmode);

            GetRoles();
            GetSchemes();
           

            GetStreams();
            GetSessions();
            //GetTrainers();
            //GetChains();
            ApplySessionSet();
            UpdateGUI();
        }

        private void SaveDefaults(Mode oldmode)
        {
            if (ChainsBox.SelectedItem != null)
            {
               // Properties.Settings.Default.CMLD = ((Chain)ChainsBox.SelectedItem).Name;
            }

            if(AnnotatorsBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultAnnotator = ((DatabaseAnnotator)AnnotatorsBox.SelectedItem).Name;
            }
         

            if (RolesBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultRole = "";
                foreach (DatabaseRole item in RolesBox.SelectedItems)
                {
                    Properties.Settings.Default.CMLDefaultRole += item.Name + ";";
                }
            }

            if (SchemesBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultScheme = ((DatabaseScheme)SchemesBox.SelectedItem).Name;
            }

            if (StreamsBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultStream = ((DatabaseStream)StreamsBox.SelectedItem).Name;
            }

            Properties.Settings.Default.Save();
        }

        private void UpdateGUI()
        {
            bool enable = false;

            if (ChainsBox.Items.Count > 0
                && DatabasesBox.SelectedItem != null
                && SessionsBox.SelectedItem != null
               )
       
            {
                enable = true;
            }

           // ApplyButton.IsEnabled = enable;
            ExtractPanel.IsEnabled = enable;
            ForceCheckBox.IsEnabled = enable;
            multidatabaseadd.IsEnabled = enable;

            //if (ChainsBox.Items.Count > 0)
            //{
            //    ApplyButton.IsEnabled = true;
            //    ExtractPanel.IsEnabled = true;
            //    ForceCheckBox.IsEnabled = true;
            //    //TrainerPathComboBox.IsEnabled = true;
            //}

            if (ChainsBox.SelectedItem != null)
            {
                Chain chain = (Chain)ChainsBox.SelectedItem;
                ChainPathLabel.Content = chain.Path;
                ChainDescription.Content = chain.Description;
                LeftContextTextBox.Text = chain.LeftContext;
                FrameSizeTextBox.Text = chain.FrameStep;
                RightContextTextBox.Text = chain.RightContext;

            }
        }

        #endregion


     

        private void GeneralBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            changeFrontendInPythonBackEndCase();

            if (handleSelectionChanged)
            {
                handleSelectionChanged = false;
                Update(mode);
                handleSelectionChanged = true;
               // GetStreams();
               
                if ((DatabaseStream)StreamsBox.SelectedItem != null)
                {
                    double sr = ((DatabaseStream)StreamsBox.SelectedItem).SampleRate;
                    FrameSizeTextBox.Text = (1000.0 / sr).ToString() + "ms";
                }

                

            }
        }


        private void DatabasesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                LoadSessionSets();
                GetAnnotators();
            }

            Update(mode);
        }

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (handleSelectionChanged)
            {
                SelectSessionSetComboBox.SelectedItem = null;
            }

            UpdateGUI();
        }

        private void SelectSessionSetComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SelectSessionSetComboBox.SelectedItem != null)
            {
                ApplySessionSet();
            }

            UpdateGUI();
        }

        private void ModeTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (handleSelectionChanged)
            {

                Mode oldmode = mode;
                mode = (Mode)ModeTabControl.SelectedIndex;
                switchMode();
                LoadSessionSets();
                Update(oldmode);
            }
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

  
        private void AnnotationSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void GenericTextChanged(object sender, TextChangedEventArgs e)
        {
           // UpdateFeatureName();
        }

        private void RemovePair_Click(object sender, RoutedEventArgs e)
        {


            selectedDatabaseAndSessions.Remove((SelectedDatabaseAndSessions)AnnotationSelectionBox.SelectedItem);
            AnnotationSelectionBox.Items.Remove(AnnotationSelectionBox.SelectedItem);



            var selecteddatabase = DatabasesBox.SelectedItem;
            if (AnnotationSelectionBox.Items.Count == 0)
            {
                string tempcontent = multidatabaselabel.Content.ToString();
                Action EmptyDelegate = delegate () { };
                multidatabaselabel.Content = "Please Wait...";
                this.UpdateLayout();
                this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                lockedScheme = null;
                GetDatabases();
                DatabasesBox.SelectedItem = selecteddatabase;
                removePair.IsEnabled = false;
                multidatabaselabel.Content = tempcontent;
            }


        }

        private void Add_Click(object sender, RoutedEventArgs e)
        {
            string sessions = "";
            foreach (var session in SessionsBox.SelectedItems)
            {
                sessions += session + ";";
            }

            string roles = "";
            foreach (var role in RolesBox.SelectedItems)
            {
                roles += role + ";";
            }


            if (selectedDatabaseAndSessions.Count == 0)
            {



               // checkSchemeexistsinotherDatabases();


            }

            string stream = ((DatabaseStream)StreamsBox.SelectedItem).Name + "." + ((DatabaseStream)StreamsBox.SelectedItem).FileExt;



            SelectedDatabaseAndSessions stp = new SelectedDatabaseAndSessions() { Database = DatabasesBox.SelectedItem.ToString(), Sessions = sessions, Roles = roles, Annotator = AnnotatorsBox.SelectedItem.ToString(), Stream = stream };


            //For now we allow to add data multiple times. This comes with the advantage that we also allow sessions from different annotators.
            //if (selectedDatabaseAndSessions.Find(item => item.Database == stp.Database) == null)
            //{
            selectedDatabaseAndSessions.Add(stp);
            AnnotationSelectionBox.Items.Add(stp);
            removePair.IsEnabled = true;
            // }




        }


        private void checkSchemeexistsinotherDatabases()
        {
            string tempcontent = multidatabaselabel.Content.ToString();
            Action EmptyDelegate = delegate () { };
            multidatabaselabel.Content = "Please Wait...";
            this.UpdateLayout();
            this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);


            var selecteddatabase = DatabasesBox.SelectedItem;
            lockedScheme = SchemesBox.SelectedItem.ToString();
            databases.Clear();
            foreach (var database in DatabasesBox.Items)
            {
                databases.Add(database.ToString());
            }

            GetSchemes();
            GetStreams();
            AnnoScheme lockedschemeinfo = DatabaseHandler.GetAnnotationScheme(lockedScheme);
            foreach (var database in databases)
            {
                //Dispatcher.Invoke(DispatcherPriority.Normal, new Action() => 

                //)
                //HERE we need to consider how restrictive we are. however if we dont rely on scheme name, it needs to be adjusted in CMLtrain
                DatabaseHandler.ChangeDatabase(database);
                AnnoScheme temp = DatabaseHandler.GetAnnotationScheme(lockedScheme);
                if (temp == null)
                {
                    DatabasesBox.Items.Remove(database);
                }
                else if (temp.SampleRate != lockedschemeinfo.SampleRate || temp.MaxScore != lockedschemeinfo.MaxScore || temp.MinScore != lockedschemeinfo.MinScore || temp.Labels.Count != lockedschemeinfo.Labels.Count)
                {
                    DatabasesBox.Items.Remove(database);
                }

            }

            multidatabaselabel.Content = tempcontent;



        }

        private List<AnnoScheme.Attribute> ParseAttributes(string optstr, bool multiroleinput)
        {
            ShowAnnotatorBox = false;
            List<AnnoScheme.Attribute> values = new List<AnnoScheme.Attribute>();
            if (optstr == null)
            {
                return null;
            }
            else
            {

                string[] split = optstr.Split(';');
                foreach (string s in split)
                {
                    string option;
                    string origin=null;
                    option = s.Replace("{", "");
                    option = option.Replace("}", "");
                    string[] attributes = option.Split(':');

                    AnnoScheme.AttributeTypes type = AnnoScheme.AttributeTypes.STRING;
                    List<string> content = new List<string>();

                    AnnoScheme.AttributeTypes xtype = AnnoScheme.AttributeTypes.LIST;
                    List<string> xcontent = new List<string>();

                    string name = attributes[0];


                    if (attributes[1].Contains("BOOL"))
                    {
                        type = AnnoScheme.AttributeTypes.BOOLEAN;
                        content.Add(attributes[2]);
                    }

                    else if (attributes[1].Contains("STRING"))
                    {
                        type = AnnoScheme.AttributeTypes.STRING;
                        if (attributes[2].StartsWith("$("))
                        {
                            attributes[2] = checkTag(attributes[2]);         
                        }
                        content.Add(attributes[2]);
                    }

                    else if (attributes[1].Contains("LIST"))
                    {
                        type = AnnoScheme.AttributeTypes.LIST;

                        if (attributes[2].StartsWith("$("))
                        {
                            if (attributes.Length > 3) {
                                attributes[3] = attributes[3].Replace(")", "");
                                content = checkTagList(attributes[2], attributes[3]);
                            }
                            else content = checkTagList(attributes[2], "");


                            if (multiroleinput)
                            {
                                foreach (var item in (DatabaseHandler.Roles))
                                {
                                    xcontent.Add(item.ToString());
                                }
                                RolesBox.Visibility = Visibility.Collapsed;
                                RolesLabel.Visibility = Visibility.Collapsed;
                            }
                            else
                            {
                                RolesBox.Visibility = Visibility.Visible;
                                RolesLabel.Visibility= Visibility.Visible;
                            }

                            if (content.Count > 0) ApplyButton.IsEnabled = true; else ApplyButton.IsEnabled = false;
                          

                            if (attributes[2].StartsWith("$(stream_name")){
                                origin = "stream";
                            }
                            else if (attributes[2].StartsWith("$(annotation_name") || attributes[2].StartsWith("$(anno_name"))
                            {
                                origin = "anno";
                                ShowAnnotatorBox = true;
                            }

                        }

                        else
                        {
                            string[] elements = attributes[2].Split(',');
                            foreach (string e in elements)
                            {
                                content.Add(e);
                            }
                        }

                      

                    }

                    AnnoScheme.Attribute attribute = new AnnoScheme.Attribute(name, content, type, xcontent, xtype, origin);
                    values.Add(attribute);
                }




            }

            if (ShowAnnotatorBox)
            {
                AnnotatorsBox.Visibility = Visibility.Visible;
                AnnotatorsLabel.Visibility = Visibility.Visible;
            }
            else 
            {
                AnnotatorsBox.Visibility = Visibility.Hidden;
                AnnotatorsLabel.Visibility = Visibility.Hidden;
            }

            return values;
        }


        public string checkTag (string Tag)
        {
            string result = "";
            if (Tag == "$(role)")
            {
                foreach (var role in DatabaseHandler.Roles)
                {
                    result += role.Name + ",";
                }
                result = result.Remove(result.Length - 1);
            }
            return result;
        }

        public List<string> checkTagList(string Tag, string Type)
        {
            List<string> result = new List<string>();
            string[] typesplitted = Type.Split(',');
            

            if (Tag == "$(role)")
            {
                foreach (var role in DatabaseHandler.Roles)
                {
                    result.Add(role.Name);
                }
            }

            else if (Tag == "$(annotator)")
            {
                foreach (var annotator in DatabaseHandler.Annotators)
                {
                    result.Add(annotator.Name);
                }
            }
            else if (Tag.StartsWith("$(stream_name"))
            {
     
                foreach (var stream in DatabaseHandler.Streams)
                {
                    string[] streamtype = stream.Type.Split(';');
                    for(int i = 0; i < streamtype.Length; i++)
                    {
                        streamtype[i] = streamtype[i].ToUpper();
                    }
                    if (streamtype.Any(typesplitted.Contains) || Type == "")

                      //  if (typesplitted.Contains(streamtype.find .ToUpper()) || Type == "")
                        result.Add(stream.Name);
                }
            }
            else if (Tag.StartsWith("$(annotation_name") || Tag.StartsWith("$(anno_name"))
            {
               

                foreach (var scheme in DatabaseHandler.Schemes)
                {
                    if (typesplitted.Contains(scheme.Type.ToString()) || Type == "")
                        foreach(DatabaseSession session in SessionsBox.SelectedItems)
                        {
                                if (AnnotatorsBox.SelectedItem != null && DatabaseHandler.AnnotationExists(((DatabaseAnnotator)(AnnotatorsBox.SelectedItem)).Name, session.Name, ((DatabaseRole)DatabaseHandler.Roles[0]).Name, scheme.Name))
                                {
                                    result.Add(scheme.Name);
                                  
                                break;
                                }
                           
                        }
                }
            }
          
            return result;
        }

        public string AttributesResult()
        {

            AllUsedSchemes.Clear();
            AllUsedStreams.Clear();
            AllUsedRoles.Clear();

            if (SpecificModelattributesresult == null)
            {
                return "";
            }

            string resultOptstring = "";
            foreach (var element in SpecificModelattributesresult)
            {

               

                //  if(element.Value.GetType() GetType().ToString() == "System.Windows.Controls.TextBox")
                {
                    if (element.Value.ElementAt(0).GetType().Name == "CheckBox")
                    {
                        resultOptstring = resultOptstring + element.Key.Split('.')[0] + "=" + ((CheckBox)element.Value.ElementAt(0)).IsChecked + ";";
                    }
                    else if (element.Value.ElementAt(0).GetType().Name == "ComboBox")
                    {

                        if (element.Key.Split('.')[1] != "")
                        {
                       
                                if (element.Key.Split('.')[1] == "anno" && !AllUsedSchemes.Contains(((ComboBox)element.Value.ElementAt(0)).SelectedItem))
                                {
                                    AllUsedSchemes.Add(((ComboBox)element.Value.ElementAt(0)).SelectedItem.ToString());
                                }
                                else if (element.Key.Split('.')[1] == "stream" && !AllUsedStreams.Contains(((ComboBox)element.Value.ElementAt(0)).SelectedItem))
                                {
                                    AllUsedStreams.Add(((ComboBox)element.Value.ElementAt(0)).SelectedItem.ToString());
                                 }
                            
                        }



                        if (element.Value.Count == 1)
                        {
                            resultOptstring = resultOptstring + element.Key.Split('.')[0] + "=" + ((ComboBox)element.Value.ElementAt(0)).SelectedItem + ";";
                        }
                        else if (element.Value.Count == 2)
                        {
                             resultOptstring = resultOptstring + element.Key.Split('.')[0] + "=" + ((ComboBox)element.Value.ElementAt(1)).SelectedItem + "." + ((ComboBox)element.Value.ElementAt(0)).SelectedItem + ";";
                            if (!AllUsedRoles.Contains(((ComboBox)element.Value.ElementAt(1)).SelectedItem)){
                                AllUsedRoles.Add(((ComboBox)element.Value.ElementAt(1)).SelectedItem.ToString());
                            }
                            
                        }



                    }
                    else if (element.Value.ElementAt(0).GetType().Name == "TextBox")
                    {
                        resultOptstring = resultOptstring + element.Key.Split('.')[0] + "=" + ((TextBox)element.Value.ElementAt(0)).Text + ";";
                    }
                    //var test = element.Value.ToString() ;
                }


            }

            resultOptstring = resultOptstring.Remove(resultOptstring.Length - 1, 1);
            return resultOptstring;
        }



        private void AddTrainerSpecificOptionsUIElements(string optstr, bool multiroleInput, bool Clear=false)
        {
            if (Clear) {
                ModelSpecificAttributes = null;
                ModelSpecificAttributes = new List<AnnoScheme.Attribute>();
                inputGrid.Children.Clear();
            }
          
            ModelSpecificAttributes.AddRange(ParseAttributes(optstr, multiroleInput));
           

            if (ModelSpecificAttributes != null && ModelSpecificAttributes.Count > 0)
            {

                Dictionary<string, Input> input = new Dictionary<string, Input>();

                foreach (var attribute in ModelSpecificAttributes)
                {
                    if (attribute.Values.Count == 0) attribute.Values.Add("");
                    input[attribute.Name] = new Input() { Label = attribute.Name, DefaultValue = attribute.Values[0], Attributes = attribute.Values, AttributeType = attribute.AttributeType, ExtraAttributes = attribute.ExtraValues, ExtraAttributeType = attribute.ExtraAttributeType, Origin = attribute.Origin };
                    


                }
                SpecificModelattributesresult = new Dictionary<string, List<UIElement>>();
                TextBox firstTextBox = null;
                foreach (KeyValuePair<string, Input> element in input)
                {
                    System.Windows.Controls.Label label = new System.Windows.Controls.Label() { Content = element.Value.Label };

                    Thickness tk = label.Margin; tk.Left = 5; tk.Right = 0; tk.Bottom = 0; label.Margin = tk;

                    inputGrid.Children.Add(label);

                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                    inputGrid.RowDefinitions.Add(rowDefinition);

                    Grid.SetColumn(label, 0);
                    Grid.SetRow(label, inputGrid.RowDefinitions.Count - 1);


                    if (element.Value.AttributeType == AnnoScheme.AttributeTypes.STRING)
                    {
                        TextBox textBox = new TextBox() { Text = element.Value.DefaultValue };
                        //textBox.GotFocus += TextBox_GotFocus;
                        if (firstTextBox == null)
                        {
                            firstTextBox = textBox;
                        }
                        Thickness margin = textBox.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; textBox.Margin = margin;
                        List<UIElement> list = new List<UIElement>
                            {
                                textBox,

                            };
                        SpecificModelattributesresult.Add(element.Key + "." + element.Value.Origin, list);
                        inputGrid.Children.Add(textBox);
                        if (firstTextBox != null)
                        {
                            firstTextBox.Focus();
                        }

                        Grid.SetColumn(textBox, 1);
                        Grid.SetRow(textBox, inputGrid.RowDefinitions.Count - 1);
                    }
                    else if (element.Value.AttributeType == AnnoScheme.AttributeTypes.BOOLEAN)
                    {
                        CheckBox cb = new CheckBox()
                        {
                            IsChecked = (element.Value.DefaultValue.ToLower() == "false") ? false : true
                        };

                        Thickness margin = cb.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; cb.Margin = margin;
                        List<UIElement> list = new List<UIElement>
                            {
                                cb,
                           
                            };
                        SpecificModelattributesresult.Add(element.Key + "." + element.Value.Origin, list);
                        inputGrid.Children.Add(cb);


                        Grid.SetColumn(cb, 1);
                        Grid.SetRow(cb, inputGrid.RowDefinitions.Count - 1);
                    }
                    else if (element.Value.AttributeType == AnnoScheme.AttributeTypes.LIST)
                    {


                     

                        ComboBox cb = new ComboBox()
                        {
                            ItemsSource = element.Value.Attributes
                           
                        };
                        if (element.Value.Attributes[0] == "") cb.IsEnabled = false;
                        cb.SelectedItem = element.Value.DefaultValue;
                        Thickness margin = cb.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; cb.Margin = margin;
                       
                        inputGrid.Children.Add(cb);

                        Grid.SetColumn(cb, 1);
                        Grid.SetRow(cb, inputGrid.RowDefinitions.Count - 1);

                        if (element.Value.ExtraAttributes != null && element.Value.ExtraAttributes.Count > 0)
                        {
                            ComboBox cb2 = new ComboBox()
                            {
                                ItemsSource = element.Value.ExtraAttributes
                            };
                            cb2.SelectedIndex = 0;
                            if (element.Value.Attributes[0] == "") cb2.IsEnabled = false;

                            Thickness margin2 = cb2.Margin; margin2.Top = 5; margin2.Right = 5; margin2.Bottom = 5; cb2.Margin = margin2;
                            inputGrid.Children.Add(cb2);

                            Grid.SetColumn(cb2, 2);
                            Grid.SetRow(cb2, inputGrid.RowDefinitions.Count - 1);

                            List<UIElement> list = new List<UIElement>
                            {
                                cb,
                                cb2
                            };
                            SpecificModelattributesresult.Add(element.Key + "." + element.Value.Origin, list);


                        }
                        else
                        {
                            List<UIElement> list = new List<UIElement>
                            {
                                cb
                     
                            };
                            SpecificModelattributesresult.Add(element.Key + "." + element.Value.Origin, list);
                        }

                           

                    }
                }
            }

            else this.ModelSpecificOptions.Visibility = Visibility.Collapsed;
        }



        private void AnnotationSelectionBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                try
                {


                    string[] lines = System.IO.File.ReadAllLines(files[0]);

                    foreach (var line in lines)
                    {
                        string[] entries = line.Split(':');
                        SelectedDatabaseAndSessions stp = new SelectedDatabaseAndSessions() { Database = entries[0], Sessions = entries[4], Roles = entries[2], Annotator = entries[1], Stream = entries[3] };

                        if (selectedDatabaseAndSessions.Find(item => item.Database == stp.Database) == null)
                        {
                            selectedDatabaseAndSessions.Add(stp);
                            AnnotationSelectionBox.Items.Add(stp);
                            removePair.IsEnabled = true;
                        }

                    }

                    checkSchemeexistsinotherDatabases();
                }
                catch
                {
                    MessageBox.Show("Can't read file format.");
                }
            }
        }

        private void ChainCategoryBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetChains((String)ChainCategoryBox.SelectedItem);

        }
    }
}
﻿using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using Path = System.IO.Path;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseCMLTrainAndPredictWindow : Window
    {
        private MainHandler handler;
        private Mode mode;
        private Status status = 0;
        private List<SelectedDatabaseAndSessions> selectedDatabaseAndSessions = new List<SelectedDatabaseAndSessions>();
        private List<string> databases = new List<string>();
        string lockedScheme = null;

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        private Thread logThread = null;
        private Thread statusThread = null;

        private static IList sessions = null;
        private static string sessionList = "";

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

            public override string ToString()
            {
                return Name;
            }
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
            TRAIN,
            EVALUATE,
            PREDICT,
            COMPLETE
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

        private static bool polygonCaseOn = true;

        private bool handleSelectionChanged = false;

        public DatabaseCMLTrainAndPredictWindow(MainHandler handler, Mode mode)
        {   
            InitializeComponent();
            createStatusObjects();

            this.handler = handler;
            this.mode = mode;


     

            if (mode == Mode.COMPLETE)
            {
                ModeTabControl.Visibility = System.Windows.Visibility.Collapsed;
                //CMLBeginFrameLabel.Visibility = System.Windows.Visibility.Visible;
                //CMLBeginFrameTextBox.Visibility = System.Windows.Visibility.Visible;
                //CMLEndFrameLabel.Visibility = System.Windows.Visibility.Visible;
                //CMLEndFrameTextBox.Visibility = System.Windows.Visibility.Visible;
                TrainerNameTextBox.IsEnabled = false;
            }
            else
            {
                ModeTabControl.SelectedIndex = (int)mode;
                //CMLBeginFrameLabel.Visibility = System.Windows.Visibility.Collapsed;
                //CMLBeginFrameTextBox.Visibility = System.Windows.Visibility.Collapsed;
                //CMLEndFrameLabel.Visibility = System.Windows.Visibility.Collapsed;
                //CMLEndFrameTextBox.Visibility = System.Windows.Visibility.Collapsed;
                TrainerNameTextBox.IsEnabled = true;
            }
       

            int endframe = -1;

            if (AnnoTier.Selected != null)
            {
                if (AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    double endtime = AnnoTier.Selected.AnnoList.ElementAt(AnnoTier.Selected.AnnoList.Count - 1).Stop;
                    double frame = (1000.0 / Properties.Settings.Default.DefaultDiscreteSampleRate) / 1000.0;
                    endframe = (int)(endtime / frame);

                }

                else endframe = AnnoTier.Selected.AnnoList.Count;
            
           

            



            this.CMLEndFrameTextBox.Text =endframe.ToString();
           
            
            double cmlbegintime = (AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition :
            MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition);

            double framef = (AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE) ? (1000.0 / Properties.Settings.Default.DefaultDiscreteSampleRate) / 1000.0 : (1000.0 / AnnoTier.Selected.AnnoList.Scheme.SampleRate) / 1000.0;
            int beginframe = (int)(cmlbegintime / framef);
            this.CMLBeginFrameTextBox.Text = beginframe.ToString();

            }

            //handler.control.annoListControl.annoDataGrid.SelectedIndex.ToString();

            Trainer trainer = (Trainer)TrainerPathComboBox.SelectedItem;

            if (trainer != null && trainer.Backend.ToUpper() == "PYTHON")
            {
                Thread logThread = new Thread(new ThreadStart(tryToGetLog));
                Thread statusThread = new Thread(new ThreadStart(tryToGetStatus));
                logThread.Start();
                statusThread.Start();
            }
            changeFrontendInPythonBackEndCase();

            Loaded += Window_Loaded;

            HelpTrainLabel.Content = "To balance the number of samples per class samples can be removed ('under') or duplicated ('over').\r\n\r\nDuring training the current feature frame can be extended by adding left and / or right frames.\r\n\r\nThe default output name may be altered.";
            HelpPredictLabel.Content = "Apply thresholds to fill up gaps between segments of the same class\r\nand remove small segments (in seconds).\r\n\r\nSet confidence to a fixed value.";
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

        private void tryToGetLog()
        {
            Trainer trainer = null;
            this.Dispatcher.Invoke(() =>
            {
                trainer = (Trainer)TrainerPathComboBox.SelectedItem;
            });

            if(trainer == null)
            {
                return;
            }

            if (trainer.Backend.ToUpper() != "PYTHON")
                return;

            Dictionary<string, string> response = null;
            while (polygonCaseOn)
            {
                MultipartFormDataContent content = getContent();
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

                Thread.Sleep(2000);
            }
        }

        private void tryToGetStatus()
        {
            //Trainer trainer = null;
            //this.Dispatcher.Invoke(() =>
            //{
            //    trainer = (Trainer)TrainerPathComboBox.SelectedItem;
            //});

            //if (trainer.Backend.ToUpper() != "PYTHON")
            //{
              
            //    return;

            //}

            Dictionary<string, string> response = null;
            while (polygonCaseOn)
            {
                MultipartFormDataContent content = getContent();
                if (content != null)
                {
                    try
                    {
                        response = handler.getStatusFromServer(content);
                        this.status = (Status)Int16.Parse(response["status"]);

                        this.Dispatcher.Invoke(() =>
                        {
                            statusLabel.Content = states[(int)this.status].getText();
                            statusLabel.Background = states[(int)this.status].getColor();
                        });

                        if (this.status != Status.RUNNING)
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                this.ApplyButton.IsEnabled = true;
                                this.Cancel_Button.IsEnabled = false;
                            });
                        }
                        else
                        {
                            this.Dispatcher.Invoke(() =>
                            {
                                this.ApplyButton.IsEnabled = false;
                                this.Cancel_Button.IsEnabled = true;
                            });
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

                Thread.Sleep(2000);
            }

            this.Dispatcher.Invoke(() =>
            {
                this.ApplyButton.IsEnabled = true;
                logTextBox.Text = "";
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
                logTextBox.Text = "No connection to server!";
            });
        }

        private MultipartFormDataContent getContent()
        {
            DatabaseScheme scheme = null;
            DatabaseStream stream = null;
            DatabaseAnnotator annotator = null;
            Trainer trainer = null;


            this.Dispatcher.Invoke(() =>
            {
                scheme = (DatabaseScheme)SchemesBox.SelectedItem;
                stream = (DatabaseStream)StreamsBox.SelectedItem;
                annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
                trainer = (Trainer)TrainerPathComboBox.SelectedItem;
                setSessionList();
            });

            string database = DatabaseHandler.DatabaseName;
            if (database == null || scheme == null || stream == null || trainer == null || annotator == null || sessionList == "")
                return null;

            return new MultipartFormDataContent
            {
                { new StringContent(database), "database" },
                { new StringContent(scheme.Name), "scheme" },
                { new StringContent(stream.Name), "streamName" },   
                { new StringContent(annotator.Name), "annotator" },
                { new StringContent(sessionList), "sessions" },
                { new StringContent(trainer.Name), "trainerScriptName" },
                { new StringContent(this.mode.ToString()), "mode" }
            };
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
            if (this.mode == Mode.COMPLETE && this.status == Status.RUNNING)
            {
                var result = MessageBox.Show("There is still a training going on at the moment. If you close this window, it will be canceled. Do you want that?", "Warning", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
                else
                {
                    handler.cancleCurrentAction(getContent());
                }
            }
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            polygonCaseOn = false;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Do you really want to cancel the current action?", "Warning", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                handler.cancleCurrentAction(getContent());
                this.Cancel_Button.IsEnabled = false;
            }
        }


        #region Window

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            switchMode();

            GetDatabases(DatabaseHandler.DatabaseName);

            if (mode == Mode.COMPLETE)
            {

                AnnoList annoList = AnnoTierStatic.Selected.AnnoList;

                DatabaseScheme scheme = ((List<DatabaseScheme>)SchemesBox.ItemsSource).Find(s => s.Name == annoList.Scheme.Name);
                if (scheme != null)
                {
                    SchemesBox.SelectedItem = scheme;
                    SchemesBox.ScrollIntoView(scheme);
                }
                DatabaseRole role = ((List<DatabaseRole>)RolesBox.ItemsSource).Find(r => r.Name == annoList.Meta.Role);
                if (role != null)
                {
                    RolesBox.SelectedItem = role;
                    RolesBox.ScrollIntoView(role);
                }
                DatabaseAnnotator annotator = ((List<DatabaseAnnotator>)AnnotatorsBox.ItemsSource).Find(a => a.Name == Properties.Settings.Default.MongoDBUser);
                if (annotator != null)
                {
                    AnnotatorsBox.SelectedItem = annotator;
                    AnnotatorsBox.ScrollIntoView(annotator);
                }
                DatabaseSession session = ((List<DatabaseSession>)SessionsBox.ItemsSource).Find(s => s.Name == DatabaseHandler.SessionName);
                if (session != null)
                {
                    SessionsBox.SelectedItem = session;
                    SessionsBox.ScrollIntoView(session);
                }           

                Update(mode);
            }

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
                case Mode.COMPLETE:

                    Title = "Complete Annotation";
                    ApplyButton.Content = "Complete";
                    TrainerLabel.Content = "Template";

                    PredictOptionsPanel.Visibility = System.Windows.Visibility.Visible;
                    TrainOptionsPanel.Visibility = System.Windows.Visibility.Visible;
                    ForceCheckBox.Visibility = System.Windows.Visibility.Collapsed;
                    SelectSessionSetComboBoxPanel.Visibility = System.Windows.Visibility.Collapsed;

                    TrainerNameTextBox.IsEnabled = false;
                    DatabasesBox.IsEnabled = false;
                    SessionsBox.IsEnabled = false;
                    RolesBox.IsEnabled = false;
                    AnnotatorsBox.IsEnabled = false;
                    SchemesBox.IsEnabled = false;

                    ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
                    FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
                    RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;

                    ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
                    FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
                    RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();
                    LosoCheckBox.Visibility = System.Windows.Visibility.Collapsed;

                    AnnotationSelectionBox.Visibility = System.Windows.Visibility.Collapsed;
                    removePair.Visibility = System.Windows.Visibility.Collapsed;
                    multidatabaseadd.Visibility = System.Windows.Visibility.Collapsed;
                    multidatabaselabel.Visibility = System.Windows.Visibility.Collapsed;

                    break;

                case Mode.TRAIN:

                    Title = "Train Models";
                    ApplyButton.Content = "Train";
                    TrainerLabel.Content = "Template";

                    PredictOptionsPanel.Visibility = System.Windows.Visibility.Collapsed;
                    TrainOptionsPanel.Visibility = System.Windows.Visibility.Visible;
                    ForceCheckBox.Visibility = System.Windows.Visibility.Visible;
                    LosoCheckBox.Visibility = System.Windows.Visibility.Collapsed;

                    AnnotationSelectionBox.Visibility = System.Windows.Visibility.Visible;
                    removePair.Visibility = System.Windows.Visibility.Visible;
                    multidatabaseadd.Visibility = System.Windows.Visibility.Visible;
                    multidatabaselabel.Visibility = System.Windows.Visibility.Visible;


                    break;

                case Mode.EVALUATE:

                    Title = "Evaluate Models";
                    ApplyButton.Content = "Evaluate";
                    TrainerLabel.Content = "Trainer";

                    PredictOptionsPanel.Visibility = System.Windows.Visibility.Collapsed;
                    TrainOptionsPanel.Visibility = System.Windows.Visibility.Collapsed;
                    ForceCheckBox.Visibility = System.Windows.Visibility.Collapsed;
                    LosoCheckBox.Visibility = System.Windows.Visibility.Visible;

                    AnnotationSelectionBox.Visibility = System.Windows.Visibility.Collapsed;
                    removePair.Visibility = System.Windows.Visibility.Collapsed;
                    multidatabaseadd.Visibility = System.Windows.Visibility.Collapsed;
                    multidatabaselabel.Visibility = System.Windows.Visibility.Collapsed;

                    break;

                case Mode.PREDICT:

                    Title = "Predict Annotations";
                    ApplyButton.Content = "Predict";
                    TrainerLabel.Content = "Trainer";

                    PredictOptionsPanel.Visibility = System.Windows.Visibility.Visible;
                    TrainOptionsPanel.Visibility = System.Windows.Visibility.Collapsed;
                    ForceCheckBox.Visibility = System.Windows.Visibility.Collapsed;
                    LosoCheckBox.Visibility = System.Windows.Visibility.Collapsed;

                    ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
                    FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
                    RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;

                    ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
                    FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
                    RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();

                    AnnotationSelectionBox.Visibility = System.Windows.Visibility.Collapsed;
                    removePair.Visibility = System.Windows.Visibility.Collapsed;
                    multidatabaseadd.Visibility = System.Windows.Visibility.Collapsed;
                    multidatabaselabel.Visibility = System.Windows.Visibility.Collapsed;

                    break;
            }
        }

        #endregion

        #region Apply

        private string createMetaFiles(string database, DatabaseAnnotator annotator, string rolesList, DatabaseStream stream, string sessionList, DatabaseScheme scheme, Trainer trainer)
        {


            string[] combinations;
            if (AnnotationSelectionBox.Items.Count > 0)

            {
                List<string> lines = new List<string>();
                
               // combinations = new string[selectedDatabaseAndSessions.Count]
                foreach (SelectedDatabaseAndSessions item in AnnotationSelectionBox.Items)
                {
                    //regular case where we only select one corpus. We need to split the string length in case it outruns SSI's max length
                    int partLength = 512;
                    int size = (item.Sessions.Length / partLength) + 2;
                    combinations = new string[size];


                    string[] words = item.Sessions.TrimEnd(';').Split(';');
                    var parts = new Dictionary<int, string>();
                    string part = string.Empty;
                    int partCounter = 0;
                    foreach (var word in words)
                    {
                        if (part.Length + word.Length < partLength)
                        {
                            part += string.IsNullOrEmpty(part) ? word : ";" + word;
                        }
                        else
                        {
                            parts.Add(partCounter, part);
                            part = word;
                            partCounter++;
                        }
                    }
                    parts.Add(partCounter, part);
                    foreach (var sessionlistpart in parts)
                    {
                        lines.Add(item.Database.TrimEnd(';') + ":" + item.Annotator.TrimEnd(';') + ":" + item.Roles.TrimEnd(';') + ":" + item.Stream.TrimEnd(';') + ":" + sessionlistpart.Value);
                        //combinations[sessionlistpart.Key] = database + ":" + annotator.Name + ":" + rolesList + ":" + stream.Name + "." + stream.FileExt + ":" + sessionlistpart.Value;
                        Console.WriteLine(item.Database.TrimEnd(';') + ":" + item.Annotator.TrimEnd(';') + ":" + item.Roles.TrimEnd(';') + ":" + item.Stream.TrimEnd(';') + ":" + sessionlistpart.Value);
                    }




                   
                }


                combinations = new string[lines.Count];
                int i = 0;
                foreach (string line in lines)
                {
                    combinations[i] = line;
                    i++;
                }

            }
            else
            {
                //regular case where we only select one corpus. We need to split the string length in case it outruns SSI's max length
                int partLength = 512;
                int size = (sessionList.Length / partLength) + 2;
                combinations = new string[size];

               
                string[] words = sessionList.Split(';');
                var parts = new Dictionary<int, string>();
                string part = string.Empty;
                int partCounter = 0;
                foreach (var word in words)
                {
                    if (part.Length + word.Length < partLength)
                    {
                        part += string.IsNullOrEmpty(part) ? word : ";" + word;
                    }
                    else
                    {
                        parts.Add(partCounter, part);
                        part = word;
                        partCounter++;
                    }
                }
                parts.Add(partCounter, part);
                foreach (var sessionlistpart in parts)
                {
                    combinations[sessionlistpart.Key] = database + ":" + annotator.Name + ":" + rolesList + ":" + stream.Name + "." + stream.FileExt + ":" + sessionlistpart.Value;
                    Console.WriteLine(combinations[sessionlistpart.Key]);
                }
               
            }

            string infofile = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetRandomFileName();

            TextWriter tw = new StreamWriter(infofile);

            foreach (String s in combinations)
                if(s != null) tw.WriteLine(s);

            tw.Close();



            //foreach (var item in combinations)
            //{
            //    if (item != null)
            //    {
            //        System.IO.File.WriteAllLines(infofile, combinations);
            //    }
            //}

          



            //For image/video training tasks we additionally provide the interface with information. make sure the interface deletes these files after reading.
            if (stream.FileExt == "mp4" || stream.FileExt == "avi" || stream.FileExt == "mov")
            {
                string trainertemplatesessioninfo = Path.GetDirectoryName(trainer.Path) + "\\nova_sessions";
                System.IO.File.WriteAllLines(trainertemplatesessioninfo, combinations);

                double cmlbegintime = (scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition :
                MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition);

                if (!polygonCaseOn)
                {
                    string[] dbinfo = {"ip="+Properties.Settings.Default.DatabaseAddress.Split(':')[0] +";port="+ Properties.Settings.Default.DatabaseAddress.Split(':')[1]+ ";user=" + Properties.Settings.Default.MongoDBUser +
                                    ";pw="+ MainHandler.Decode(Properties.Settings.Default.MongoDBPass) + ";scheme=" +  scheme.Name + ";root=" + Properties.Settings.Default.DatabaseDirectory + ";cooperative=" + (mode == Mode.COMPLETE)  + ";cmlbegintime=" + cmlbegintime};

                    string trainertemplatedbinfo = Path.GetDirectoryName(trainer.Path) + "\\nova_db_info";
                    System.IO.File.WriteAllLines(trainertemplatedbinfo, dbinfo);
                }
            }

            return infofile;
        }



        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            Trainer trainer = (Trainer)TrainerPathComboBox.SelectedItem;
            bool force = mode == Mode.COMPLETE || ForceCheckBox.IsChecked.Value;

            if (!File.Exists(trainer.Path))
            {
                MessageTools.Warning("file does not exist '" + trainer.Path + "'");
                return;
            }

            string database = DatabaseHandler.DatabaseName;

            DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
            setSessionList();
            DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;

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

            DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;

            string trainerLeftContext = LeftContextTextBox.Text;
            string trainerRightContext = RightContextTextBox.Text;
            string trainerBalance = ((ComboBoxItem)BalanceComboBox.SelectedItem).Content.ToString();

            logTextBox.Text = "";
            string root = "";
            foreach (var location in Defaults.LocalDataLocations())
            {
                if (File.Exists(location + "\\"
                  + database + "\\"
                  + sessionList.Split(';')[0] + "\\"
                  + rolesList.Split(';')[0] + "." + stream.Name + "." + stream.FileExt))
                {

                    root = location;
                    break;
                }
            }

            if (trainer.Backend.ToUpper() == "PYTHON")
            {
                handlePythonBackend(trainer, scheme, stream, annotator, database, trainerLeftContext, trainerRightContext, trainerBalance, rolesList);
            }
            else
            {
                if (mode == Mode.TRAIN || mode == Mode.COMPLETE)
                {
                    String streamName = getStreamName(stream);
                    String trainerDir = getTrainerDir(trainer, streamName, scheme, stream);
                    String trainerOutPath = getTrainerOutPath(trainer, trainerDir);

                    try
                    {
                        Directory.CreateDirectory(trainerDir);
                    }
                    catch
                    {
                        MessageBox.Show("Could not create folder " + trainerDir + " Make sure NOVA has writing access to the directory");
                        return;
                    }

                    if (force || !File.Exists(trainerOutPath + ".trainer"))
                    {
                        try
                        {
                            string infofile = createMetaFiles(database, annotator, rolesList, stream, sessionList, scheme, trainer);

                            if (trainer.Backend.ToUpper() == "SSI")
                            {



                                logTextBox.Text += handler.CMLTrainModel(trainer.Path,
                                trainerOutPath,
                                root,
                                Properties.Settings.Default.DatabaseAddress,
                                Properties.Settings.Default.MongoDBUser,
                                MainHandler.Decode(Properties.Settings.Default.MongoDBPass),
                                database,
                                sessionList,
                                scheme.Name,
                                rolesList,
                                annotator.Name,
                                stream.Name + "." + stream.FileExt,
                                trainerLeftContext,
                                trainerRightContext,
                                trainerBalance,
                                mode == Mode.COMPLETE,
                                (scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition :
                                MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition),
                                infofile);
                            }
                            //oldstyle cml call.
                            else
                            {

                                logTextBox.Text += handler.CMLTrainModel(trainer.Path,
                                trainerOutPath,
                                root + "\\" + database,
                                Properties.Settings.Default.DatabaseAddress,
                                Properties.Settings.Default.MongoDBUser,
                                MainHandler.Decode(Properties.Settings.Default.MongoDBPass),
                                database,
                                sessionList,
                                scheme.Name,
                                rolesList,
                                annotator.Name,
                                stream.Name + "." + stream.FileExt,
                                trainerLeftContext,
                                trainerRightContext,
                                trainerBalance,
                                mode == Mode.COMPLETE,
                                (scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition :
                                MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition));

                            }

                            var dir = new DirectoryInfo(Path.GetDirectoryName(infofile));
                            foreach (var file in dir.EnumerateFiles(Path.GetFileName(infofile) + "*"))
                            {
                                file.Delete();
                            }
                        }

                        catch (Exception ex)
                        {
                            logTextBox.Text += ex;
                        }
                    }
                    else
                    {
                        logTextBox.Text += "The model " + trainerOutPath + " already exists\nUse Force checkbox to overwrite the existing model.";
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

                        try
                        {

                            logTextBox.Text += handler.CMLPredictAnnos(mode == Mode.COMPLETE ? tempTrainerPath : trainer.Path,
                            root,
                            Properties.Settings.Default.DatabaseAddress,
                            Properties.Settings.Default.MongoDBUser,
                            MainHandler.Decode(Properties.Settings.Default.MongoDBPass),
                            database,
                            sessionList,
                            scheme.Name,
                            rolesList,
                            annotator.Name,
                            stream.Name + "." + stream.FileExt,
                            trainerLeftContext,
                            trainerRightContext,
                            confidence,
                            minGap,
                            minDur,
                            mode == Mode.COMPLETE,
                            (scheme.Type == AnnoScheme.TYPE.CONTINUOUS) ? MainHandler.Time.CurrentPlayPosition :
                             MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition));
                        }

                        catch (Exception ex)
                        {
                            logTextBox.Text += ex;
                        }
                    }

                }

                if (mode == Mode.EVALUATE)
                {
                    string evalOutPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
                    try

                    {
                        logTextBox.Text += handler.CMLEvaluateModel(evalOutPath,
                            trainer.Path,
                            root,
                            Properties.Settings.Default.DatabaseAddress,
                            Properties.Settings.Default.MongoDBUser,
                            MainHandler.Decode(Properties.Settings.Default.MongoDBPass),
                            database,
                            sessionList,
                            scheme.Name,
                            rolesList,
                            annotator.Name,
                            stream.Name + "." + stream.FileExt,
                            LosoCheckBox.IsChecked.Value);

                        if (File.Exists(evalOutPath))
                        {
                            ConfmatWindow confmat = new ConfmatWindow(evalOutPath);
                            confmat.ShowDialog();
                            File.Delete(evalOutPath);
                        }
                    }

                    catch (Exception ex)
                    {
                        logTextBox.Text += ex;
                    }
                }

                if (mode == Mode.COMPLETE)
                {
                    handler.ReloadAnnoTierFromDatabase(AnnoTierStatic.Selected, false);

                    var dir = new DirectoryInfo(Path.GetDirectoryName(tempTrainerPath));

                    string streamName = "";
                    string[] streamParts = stream.Name.Split('.');
                    if (streamParts.Length <= 1)
                    {
                        streamName = stream.Name;
                    }
                    else
                    {
                        streamName = streamParts[1];
                        for (int i = 2; i < streamParts.Length; i++)
                        {
                            streamName += "." + streamParts[i];
                        }
                    }

                    try
                    {
                        var tempdir = new DirectoryInfo(Path.GetDirectoryName(Properties.Settings.Default.CMLTempTrainerPath));
                        foreach (var file in tempdir.EnumerateFiles(Path.GetFileName(Properties.Settings.Default.CMLTempTrainerPath) + "latestcmlmodel*"))
                        {
                            file.Delete();
                        }
                    }
                    catch { }

                    Properties.Settings.Default.CMLTempTrainerPath = Properties.Settings.Default.CMLDirectory + "\\" + Defaults.CML.ModelsFolderName + "\\" + Defaults.CML.ModelsTrainerFolderName + "\\" + AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower() + "\\" + AnnoTier.Selected.AnnoList.Scheme.Name + "\\" + stream.Type + "{" + streamName + "}\\" + trainer.Name + "\\";
                    Properties.Settings.Default.Save();

                    if (!Directory.Exists(Properties.Settings.Default.CMLDirectory)) 
                        Directory.CreateDirectory(Properties.Settings.Default.CMLTempTrainerPath);

                    foreach (var file in dir.EnumerateFiles(Path.GetFileName(tempTrainerPath) + "*"))
                    {
                        string filename = file.Name;
                        string[] split = file.Name.Split('.');
                        if (split.Length == 2 && split[1] == "trainer")
                        {
                            Properties.Settings.Default.CMLTempTrainerName = split[0];
                            Properties.Settings.Default.Save();

                            split[0] = "latestcmlmodel";
                            filename = string.Join(".", split);
                        }

                        if (File.Exists(Properties.Settings.Default.CMLTempTrainerPath + filename))
                        {
                            File.Delete(Properties.Settings.Default.CMLTempTrainerPath + filename);
                        }
                        file.MoveTo(Properties.Settings.Default.CMLTempTrainerPath + filename);

                        //file.Delete();
                    }
                    Close();
                }
            }
        }

        private String getTrainerOutPath(Trainer trainer, String trainerDir)
        {
            string trainerName = TrainerNameTextBox.Text == "" ? trainer.Name : TrainerNameTextBox.Text;
            return mode == Mode.COMPLETE ? tempTrainerPath : trainerDir + trainerName;
        }

        private String getTrainerDir(Trainer trainer, String streamName, DatabaseScheme scheme, DatabaseStream stream)
        {
            return Properties.Settings.Default.CMLDirectory + "\\" +
                                       Defaults.CML.ModelsFolderName + "\\" +
                                       Defaults.CML.ModelsTrainerFolderName + "\\" +
                                       scheme.Type.ToString().ToLower() + "\\" +
                                       scheme.Name + "\\" +
                                       stream.Type + "{" +
                                       streamName + "}\\" +
                                       trainer.Name + "\\";
        }

        private String getStreamName(DatabaseStream stream)
        {
            string streamName = "";
            string[] streamParts = stream.Name.Split('.');
            if (streamParts.Length <= 1)
            {
                streamName = stream.Name;
            }
            else
            {
                streamName = streamParts[1];
                for (int i = 2; i < streamParts.Length; i++)
                {
                    streamName += "." + streamParts[i];
                }
            }
            return streamName;
        }

        private void handlePythonBackend(Trainer trainer, DatabaseScheme scheme, DatabaseStream stream, DatabaseAnnotator annotator, string database, string trainerLeftContext, string trainerRightContext, string trainerBalance, string rolesList)
        {
            this.ApplyButton.IsEnabled = false;

            string streamName = getStreamName(stream);
            string trainerDir = getTrainerDir(trainer, streamName, scheme, stream);
            string trainerOutPath = getTrainerOutPath(trainer, trainerDir);
            double frameSize = 0;

            if(scheme.Type == AnnoScheme.TYPE.CONTINUOUS || scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON
                || scheme.Type == AnnoScheme.TYPE.POINT || scheme.Type == AnnoScheme.TYPE.POLYGON)
            {
                frameSize =  1000.0 / scheme.SampleRate;

            }
            else if (scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                frameSize = 1000.0 / Properties.Settings.Default.DefaultDiscreteSampleRate;
                // make UI element to adjust
            }

            else if (scheme.Type == AnnoScheme.TYPE.FREE)
            {
                frameSize = 10000.0; // 10 Sekunden make UI element to adjust TODO
            }

                string relativeTrainerOutPath = trainerOutPath.Replace(Properties.Settings.Default.CMLDirectory, "");

            String cmlBeginTime = "0";
            String cmlEndTime = "0";

            if (this.mode == Mode.COMPLETE)
            {

                int cmlBeginFrame = int.Parse(this.CMLBeginFrameTextBox.Text);
                int cmlEndFrame = int.Parse(this.CMLEndFrameTextBox.Text);


                if (cmlBeginFrame >= cmlEndFrame)
                {
                    MessageBox.Show("End-Frame must be greater than Begin-Frame!", "Error", MessageBoxButton.OK);
                    return;
                }
                if (cmlBeginFrame > handler.control.annoListControl.annoDataGrid.Items.Count || cmlEndFrame > handler.control.annoListControl.annoDataGrid.Items.Count)
                {
                    MessageBox.Show("Begin-Frame or End-Frame are greater than the amount of frames of the current file!", "Error", MessageBoxButton.OK);
                    return;
                }

                cmlBeginTime = (frameSize * cmlBeginFrame).ToString();
                cmlEndTime = (frameSize * cmlEndFrame).ToString();


            }

            else
            {
                cmlBeginTime = "-1";
                cmlEndTime = "-1";
            }

            bool test = true;


            //TODO REMOVE
            if (test){
                cmlEndTime = "300s";
            }


            var trainerScriptPath = Directory.GetParent(trainer.Path) + "\\" + trainer.Script;
            string relativetrainerScriptPath = trainerScriptPath.Replace(Properties.Settings.Default.CMLDirectory, "");

            string relativetemplatePath = trainer.Path.Replace(Properties.Settings.Default.CMLDirectory, "");


            if(mode == Mode.PREDICT)
            {
                trainer.Name = trainer.Name.Split(' ')[0];
            }

            MultipartFormDataContent content = new MultipartFormDataContent
            {


                //Int -> MS Float -> S (oder ms,s als string)
                { new StringContent(relativetemplatePath), "templatePath" },
                { new StringContent(relativeTrainerOutPath), "trainerPath" },
                { new StringContent(Properties.Settings.Default.DatabaseDirectory), "dataPath" }, //optional, not used
                { new StringContent(Properties.Settings.Default.DatabaseAddress), "server" },
                { new StringContent(Properties.Settings.Default.MongoDBUser), "username" },
                { new StringContent(MainHandler.Decode(Properties.Settings.Default.MongoDBPass)), "password" },
                { new StringContent(database), "database" },
                { new StringContent(sessionList), "sessions" },
                { new StringContent(scheme.Name), "scheme" },
                { new StringContent(rolesList), "roles" },
                { new StringContent(annotator.Name), "annotator" },
                { new StringContent(stream.Name), "streamName" },
                { new StringContent(stream.Type), "streamType" },
                { new StringContent(trainerLeftContext), "leftContext" },
                { new StringContent(trainerRightContext), "rightContext" },
                { new StringContent(trainerBalance), "balance" },
                { new StringContent(this.mode.ToString()), "mode" },
                { new StringContent("0ms"), "startTime" }, //IN MILLISECONDS
                { new StringContent(cmlBeginTime), "cmlBeginTime" },
                { new StringContent(cmlEndTime), "cmlEndTime" },
                { new StringContent(frameSize.ToString() + "ms"), "frameSize" }, //inms
                { new StringContent(scheme.Type.ToString()), "schemeType" },
                { new StringContent(relativetrainerScriptPath), "trainerScript" },
                { new StringContent(trainer.Name), "trainerScriptName" },
                { new StringContent(trainer.Weight), "weightsPath" }
            };

            if (this.mode == Mode.COMPLETE)
            {
                _ = handler.PythonBackEndComplete(content);
              //  _ = handler.PythonBackEndTraining(content);
              //  _ = handler.PythonBackEndPredict(content);
            }
            else if (this.mode == Mode.TRAIN)
            {
                _ = handler.PythonBackEndTraining(content);
            }
            else if (this.mode == Mode.PREDICT)
            {
                _ = handler.PythonBackEndPredict(content);
            }
        }

        private void changeFrontendInPythonBackEndCase()
        {
            Trainer trainer = (Trainer)TrainerPathComboBox.SelectedItem;
            logThread = new Thread(new ThreadStart(tryToGetLog));
            statusThread = new Thread(new ThreadStart(tryToGetStatus));

            logThread.Start();
            statusThread.Start();

            if (trainer != null && trainer.Backend.ToUpper() == "PYTHON")
            {
                polygonCaseOn = true;
              
               
                this.statusLabel.Visibility = Visibility.Visible;
                this.Cancel_Button.Visibility = Visibility.Visible;

                if (mode == Mode.COMPLETE)
                {
                    this.CMLBeginFrameLabel.Visibility = Visibility.Visible;
                    this.CMLBeginFrameTextBox.Visibility = Visibility.Visible;
                    this.CMLEndFrameLabel.Visibility = Visibility.Visible;
                    this.CMLEndFrameTextBox.Visibility = Visibility.Visible;
                }
                else
                {
                    this.CMLBeginFrameLabel.Visibility = Visibility.Collapsed;
                    this.CMLBeginFrameTextBox.Visibility = Visibility.Collapsed;
                    this.CMLEndFrameLabel.Visibility = Visibility.Collapsed;
                    this.CMLEndFrameTextBox.Visibility = Visibility.Collapsed;
                }
                
               
            }
            else
            {

            
                this.ApplyButton.IsEnabled = true;
                polygonCaseOn = false;
                this.statusLabel.Visibility = Visibility.Collapsed;
                this.Cancel_Button.Visibility = Visibility.Collapsed;
                this.logTextBox.Text = "";
                this.CMLEndFrameLabel.Visibility = Visibility.Collapsed;
                this.CMLEndFrameTextBox.Visibility = Visibility.Collapsed;

                if (mode == Mode.COMPLETE)
                {
                    this.CMLBeginFrameLabel.Visibility = Visibility.Visible;
                    this.CMLBeginFrameTextBox.Visibility = Visibility.Visible;
                    this.CMLEndFrameLabel.Visibility = Visibility.Collapsed;
                    this.CMLEndFrameTextBox.Visibility = Visibility.Collapsed;
                }
                else
                {
                    this.CMLBeginFrameLabel.Visibility = Visibility.Collapsed;
                    this.CMLBeginFrameTextBox.Visibility = Visibility.Collapsed;
                    this.CMLEndFrameLabel.Visibility = Visibility.Collapsed;
                    this.CMLEndFrameTextBox.Visibility = Visibility.Collapsed;
                }



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
                    bool template = mode == Mode.TRAIN || mode == Mode.COMPLETE;
                    if (getTrainer(stream, scheme, template).Count > 0)
                    {

                        if (lockedScheme == null) schemesValid.Add(scheme);
                        else if (scheme.Name == lockedScheme) schemesValid.Add(scheme);
                        break;
                    }
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
                if(mode == Mode.COMPLETE)
                {
                    DatabaseRole role = ((List<DatabaseRole>)RolesBox.ItemsSource).Find(r => r.Name == AnnoTierStatic.Selected.AnnoList.Meta.Role);
                    if (role != null)
                    {
                        RolesBox.SelectedItems.Add(role);
                    }
                }

                else
                {
                    string[] items = Properties.Settings.Default.CMLDefaultRole.Split(';');
                    foreach(string item in items)
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
                        bool template = mode == Mode.TRAIN || mode == Mode.COMPLETE;
                        if (getTrainer(stream, scheme, template).Count > 0)
                        {
                            streamsValid.Add(stream);
                           // break;
                     
                        }
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
                StreamsBox.ScrollIntoView(StreamsBox.SelectedItem);
            }
        }

        public void GetAnnotators()
        {
            AnnotatorsBox.ItemsSource = DatabaseHandler.Annotators;                         

            if (AnnotatorsBox.Items.Count > 0)
            {
                string annotatorName = Properties.Settings.Default.CMLDefaultAnnotator;
                AnnotatorsBox.IsEnabled = mode != Mode.COMPLETE;

                if (mode == Mode.PREDICT)
                {
                    if (DatabaseHandler.CheckAuthentication() <= DatabaseAuthentication.READWRITE)
                    {
                        annotatorName = Properties.Settings.Default.MongoDBUser;
                        AnnotatorsBox.IsEnabled = false;
                    }

                    else
                    {
                        annotatorName = Properties.Settings.Default.CMLDefaultAnnotatorPrediction;
                    }
                }

                else if (DatabaseHandler.CheckAuthentication() > DatabaseAuthentication.READWRITE)
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
            if (SchemesBox.SelectedItem == null || RolesBox.SelectedItem == null || AnnotatorsBox.SelectedItem == null)
            {
                return;
            }            

            if (mode == Mode.TRAIN || mode == Mode.EVALUATE)
            {
                // show user sessions only

                List<DatabaseSession> sessions = new List<DatabaseSession>();
                DatabaseAnnotator annotator = (DatabaseAnnotator)AnnotatorsBox.SelectedItem;
                DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;                
                foreach (DatabaseRole role in RolesBox.SelectedItems)
                {
                    List<DatabaseAnnotation> annotations = DatabaseHandler.GetAnnotations(scheme, role, annotator);
                    foreach (DatabaseAnnotation annotation in annotations)
                    {
                        DatabaseSession session = DatabaseHandler.Sessions.Find(s => s.Name == annotation.Session);
                        if (session != null)
                        {
                            if (!sessions.Contains(session))
                            {
                                sessions.Add(session);
                            }
                        }
                    }
                }
                SessionsBox.ItemsSource = sessions.OrderBy(s => s.Name).ToList();
            }
            else
            {
                SessionsBox.ItemsSource = DatabaseHandler.Sessions;
            }           
        }

        private void GetTrainers()
        {
            if (RolesBox.SelectedItem == null || AnnotatorsBox.SelectedItem == null || SchemesBox.SelectedItem == null)
            {
                return;
            }
            
            TrainerPathComboBox.ItemsSource = null;

            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                DatabaseScheme scheme = (DatabaseScheme)SchemesBox.SelectedItem;

                if (scheme != null)
                {
                    bool isDiscrete = scheme.Type == AnnoScheme.TYPE.DISCRETE;

                    FillGapCheckBox.IsEnabled = isDiscrete;
                    FillGapTextBox.IsEnabled = isDiscrete;
                    RemoveLabelCheckBox.IsEnabled = isDiscrete;
                    RemoveLabelTextBox.IsEnabled = isDiscrete;

                    bool template = mode == Mode.TRAIN || mode == Mode.COMPLETE;

                    List<Trainer> trainers = getTrainer(stream, scheme, template);
                    if (trainers.Count > 0)
                    {
                        TrainerPathComboBox.ItemsSource = trainers;
                    }
                }
            }

            if (TrainerPathComboBox.Items.Count > 0)
            {
                Trainer trainer = ((List<Trainer>)TrainerPathComboBox.ItemsSource).Find(t => t.Name == Properties.Settings.Default.CMLDefaultTrainer);

                if (trainer != null)
                {
                    TrainerPathComboBox.SelectedItem = trainer;
                }
                else
                {
                    TrainerPathComboBox.SelectedIndex = 0;
                }
            }

            if (TrainerPathComboBox.SelectedItem != null)
            {
                Trainer trainer = (Trainer)TrainerPathComboBox.SelectedItem;

                LeftContextTextBox.Text = trainer.LeftContext;
                RightContextTextBox.Text = trainer.RightContext;

                BalanceComboBox.SelectedIndex = 0;
                if (trainer.Balance.ToLower() == "under")
                {
                    BalanceComboBox.SelectedIndex = 1;
                }
                else if (trainer.Balance.ToLower() == "over")
                {
                    BalanceComboBox.SelectedIndex = 2;
                }
                string database = "";
                if (DatabasesBox.SelectedItem != null)
                {
                    database = DatabasesBox.SelectedItem.ToString();
                }

                if(AnnotationSelectionBox.Items.Count > 0)
                {
                    database = "";
                    for(int i=0; i < AnnotationSelectionBox.Items.Count; i++ )
                    {
                        database += ((SelectedDatabaseAndSessions)AnnotationSelectionBox.Items[i]).Database + "+";
                    }
                    database.Remove(database.Length - 1, 1);
                   
                }
                TrainerNameTextBox.Text = mode == Mode.COMPLETE ? Path.GetFileName(tempTrainerPath) : database;

                TrainerPathLabel.Content = trainer.Path;                
            }
        }

        #endregion

        #region Trainer

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
                trainer.Backend = "SSI";

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
                    var backend = node.Attributes["backend"];
                    if (backend != null)
                    {
                        trainer.Backend = backend.Value.ToUpper();
                    }
                    else trainer.Backend = "SSI";
                }

                foreach (XmlNode node in doc.SelectNodes("//model"))
                {
                    var script = node.Attributes["script"];
                    if (script != null)
                    {
                        trainer.Script = script.Value;
                    }

                    var weights = node.Attributes["path"];
                    if (weights != null)
                    {
                        trainer.Weight = weights.Value;
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
            if (stream == null || scheme == null) return trainers;

            if (scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                if (stream.SampleRate != scheme.SampleRate)
                {
                    return trainers;
                }
            }
            string streamName = "";
            string[] streamParts = stream.Name.Split('.');
            if (streamParts.Length <= 1)
            {
                streamName = stream.Name;
            }
            else
            {
                streamName = streamParts[1];
                for (int i = 2; i < streamParts.Length; i++)
                {
                    streamName += "." + streamParts[i];
                }
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

        #endregion

        #region Sets

        private void LoadSessionSets()
        {
            SelectSessionSetComboBox.ItemsSource = null;

            List<SessionSet> sets = new List<SessionSet>();

            if (mode == Mode.PREDICT)
            {
                sets.Add(new SessionSet()
                {
                    Set = SessionSet.Type.ALL,
                });
                sets.Add(new SessionSet()
                {
                    Set = SessionSet.Type.MISSING,
                });
            }

            if (mode == Mode.TRAIN || mode == Mode.EVALUATE)
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

           
            foreach(var location in Defaults.LocalDataLocations())
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
            if (mode == Mode.COMPLETE)
            {
                return;
            }

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
            GetAnnotators();
            GetStreams();
            GetSessions();
            GetTrainers();
            ApplySessionSet();
            UpdateGUI();
        }

        private void SaveDefaults(Mode oldmode)
        {
            if (TrainerPathComboBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultTrainer = ((Trainer)TrainerPathComboBox.SelectedItem).Name;
            }

            if (AnnotatorsBox.SelectedItem != null && oldmode != Mode.PREDICT)
            {
                Properties.Settings.Default.CMLDefaultAnnotator = ((DatabaseAnnotator)AnnotatorsBox.SelectedItem).Name;
            }

            else if (AnnotatorsBox.SelectedItem != null && oldmode == Mode.PREDICT)
            {
                Properties.Settings.Default.CMLDefaultAnnotatorPrediction = ((DatabaseAnnotator)AnnotatorsBox.SelectedItem).Name;
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
            multidatabaseadd.IsEnabled = enable;

            if(AnnotationSelectionBox.Items.Count > 0)
            {
                ApplyButton.IsEnabled = true;
                TrainOptionsPanel.IsEnabled = true;
                ForceCheckBox.IsEnabled = true;
                TrainerPathComboBox.IsEnabled = true;
            }
        }

        #endregion

        #region User

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

        private void GeneralBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            changeFrontendInPythonBackEndCase();

            if (handleSelectionChanged)
            {
                handleSelectionChanged = false;
                Update(mode);
                handleSelectionChanged = true;
                GetStreams();
            }
        }


        private void DatabasesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                LoadSessionSets();
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

        #endregion

        private void AnnotationSelectionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RemovePair_Click(object sender, RoutedEventArgs e)
        {


                selectedDatabaseAndSessions.Remove((SelectedDatabaseAndSessions)AnnotationSelectionBox.SelectedItem);
                AnnotationSelectionBox.Items.Remove(AnnotationSelectionBox.SelectedItem);
            
           

            var selecteddatabase = DatabasesBox.SelectedItem;
            if(AnnotationSelectionBox.Items.Count == 0)
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

                
               
                checkSchemeexistsinotherDatabases();


            }

            string stream = ((DatabaseStream)StreamsBox.SelectedItem).Name + "." + ((DatabaseStream)StreamsBox.SelectedItem).FileExt;



            SelectedDatabaseAndSessions stp = new SelectedDatabaseAndSessions() { Database = DatabasesBox.SelectedItem.ToString(),  Sessions = sessions, Roles = roles, Annotator = AnnotatorsBox.SelectedItem.ToString(), Stream = stream };


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


        private void AnnotationSelectionBox_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                try
                {

              
                string[] lines =  System.IO.File.ReadAllLines(files[0]);

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

        private void CMLBeginFrameLabelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex regexObj = new Regex(@"[^\d]");
            this.CMLBeginFrameTextBox.Text = regexObj.Replace(this.CMLBeginFrameTextBox.Text, "");
        }

        private void CMLEndFrameLabelBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Regex regexObj = new Regex(@"[^\d]");
            this.CMLEndFrameTextBox.Text = regexObj.Replace(this.CMLEndFrameTextBox.Text, "");
        }
    }

    public class SelectedDatabaseAndSessions
    {
        public string Database { get; set; }
        public string Sessions { get; set; }

        public string Roles { get; set; }

        public string Annotator { get; set; }
        public string Stream { get; set; }


    }
}
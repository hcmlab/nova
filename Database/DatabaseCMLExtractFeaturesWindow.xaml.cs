using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseCMLExtractFeaturesWindow : Window
    {
        private MainHandler handler;

        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;

        public class Chain
        {
            public string Path { get; set; }
            public string Name { get; set; }
            public string FrameStep { get; set; }
            public string LeftContext { get; set; }
            public string RightContext { get; set; }
            public override string ToString()
            {
                return Name;
            }
        }

        public enum Mode
        {
            EXTRACT,
            MERGE,            
        }
        Mode mode;

        private string tempInListPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        private string tempOutListPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        public DatabaseCMLExtractFeaturesWindow(MainHandler handler, Mode mode)
        {
            InitializeComponent();            

            this.handler = handler;
            this.mode = mode;            

            if (mode == Mode.MERGE)
            {
                ExtractPanel.Visibility = Visibility.Collapsed;
                NParallelLabel.Visibility = Visibility.Collapsed;
                NParallelTextBox.Visibility = Visibility.Collapsed;
                ExtractButton.Content = "Merge";
                Title = "Merge Features";
                StreamsBox.SelectionMode = SelectionMode.Extended;
            }
            else
            {
                HelpLabel.Content = "Features are extracted every frame over a window that is extended by the left and right context.\r\n1.2s\t= 1.2 Seconds\r\n10ms\t= 10 Milliseconds\r\n500\t= 500 Samples";
            }

            GetDatabases(DatabaseHandler.DatabaseName);
            GetStreams();
            GetRoles();

            Update();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void UpdateFeatureName()
        {
            string name = "";

            if (mode == Mode.MERGE)
            {
                var streams = StreamsBox.SelectedItems;

                string streamList = "";
                HashSet<string> mediaNames = new HashSet<string>();
                HashSet<string> featureNames = new HashSet<string>();
                double sampleRate = 0;
                foreach (DatabaseStream stream in streams)
                {
                    string[] tokens = stream.Name.Split(new char[] { '.' }, 2);
                    mediaNames.Add(tokens[0]);
                    if (tokens.Length > 1)
                    {
                        featureNames.Add(tokens[1]);
                    }

                    if (streamList == "")
                    {
                        sampleRate = stream.SampleRate;
                        streamList = stream.Name;
                    }
                    else
                    {
                        streamList += ";" + stream.Name;
                    }
                }

                string[] arrmedias;
                arrmedias = mediaNames.ToArray();
                Array.Sort(arrmedias);
                mediaNames.Clear();
                mediaNames.UnionWith(arrmedias);

                string medias = "";
                foreach (string media in mediaNames)
                {
                    if (medias == "")
                    {
                        medias = media;
                    }
                    else
                    {
                        medias += "+" + media;
                    }
                }

                string[] arrstreams;
                arrstreams = featureNames.ToArray();
                Array.Sort(arrstreams);
                featureNames.Clear();
                featureNames.UnionWith(arrstreams);

                string features = "";
                foreach (string feature in featureNames)
                {
                    if (features == "")
                    {
                        features = feature;
                    }
                    else
                    {
                        features += "+" + feature;
                    }
                }
                
                name = medias + "." + features;
            }
            else
            {
                if (ChainPathComboBox.SelectedItem != null && StreamsBox.SelectedItem != null)
                {
                    DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                    Chain chain = (Chain)ChainPathComboBox.SelectedItem;

                    string leftContext = LeftContextTextBox.Text;
                    string frameStep = FrameStepTextBox.Text;
                    string rightContext = RightContextTextBox.Text;
                    string streamMeta = "[" + leftContext + "," + frameStep + "," + rightContext + "]";
                    
                    name = stream.Name + "." + chain.Name + streamMeta;;
                }
            }
            
            FeatureNameTextBox.Text = name;
        }

        private void Extract()
        {
            if (ChainPathComboBox.SelectedItem == null)
            {
                MessageTools.Warning("select a chain first");
                return;
            }

            string featureName = FeatureNameTextBox.Text;
            if (featureName == "" || featureName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageTools.Warning("not a valid feature name");
                return;
            }

            Chain chain = (Chain)ChainPathComboBox.SelectedItem;
            bool force = ForceCheckBox.IsChecked.Value;

            if (!File.Exists(chain.Path))
            {
                MessageTools.Warning("file does not exist '" + chain.Path + "'");
                return;
            }

            string database = DatabaseHandler.DatabaseName;
            var sessions = SessionsBox.SelectedItems;
            var roles = RolesBox.SelectedItems;
            DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;

            string leftContext = LeftContextTextBox.Text;
            string frameStep = FrameStepTextBox.Text;
            string rightContext = RightContextTextBox.Text;

            int nParallel = 1;
            int.TryParse(NParallelTextBox.Text, out nParallel);

            logTextBox.Text = "";

            // prepare lists

            int nFiles = 0;
            using (StreamWriter fileListIn = new StreamWriter(tempInListPath))
            {
                using (StreamWriter fileListOut = new StreamWriter(tempOutListPath))
                {
                    foreach (DatabaseSession session in sessions)
                    {
                        foreach (string role in roles)
                        {
                            string fromPath = Properties.Settings.Default.DatabaseDirectory + "\\"
                            + database + "\\"
                            + session.Name + "\\"
                            + role + "." + stream.Name + "." + stream.FileExt;

                            string toPath = Path.GetDirectoryName(fromPath) + "\\"
                            + role + "." + featureName + ".stream";

                            if (force || !File.Exists(toPath))
                            {
                                nFiles++;
                                fileListIn.WriteLine(fromPath);
                                fileListOut.WriteLine(toPath);
                            }
                            else
                            {
                                logTextBox.Text += "skip " + fromPath + "\n";
                            }
                        }
                    }
                }
            }

            // start feature extraction

            Chain selectedChain = (Chain)ChainPathComboBox.SelectedItem;
            DatabaseStream selectedStream = (DatabaseStream)StreamsBox.SelectedItem;

            if (nFiles > 0)
            {            
                string type = Defaults.CML.StreamTypeNameFeature;
                string name = featureName;
                string ext = "stream";

                double sr = frameStepToSampleRate(frameStep, stream.SampleRate);

                DatabaseStream streamType = new DatabaseStream() { Name = name, Type = type, FileExt = ext, SampleRate = sr };
                DatabaseHandler.AddStream(streamType);
                try
                {
                    logTextBox.Text += handler.CMLExtractFeature(chain.Path, nParallel, tempInListPath, tempOutListPath, frameStep, leftContext, rightContext);
                }
                catch (Exception e) { MessageBox.Show("There was an error in the feature extaction pipeline: " + e); }
             

            }

            File.Delete(tempInListPath);
            File.Delete(tempOutListPath);

            GetStreams(selectedStream);
            foreach (Chain item in ChainPathComboBox.Items)
            {
                if (item.Name == selectedChain.Name)
                {
                    ChainPathComboBox.SelectedItem = item;
                    break;
                }
            }

        }

        private void Merge()
        {
            string featureName = FeatureNameTextBox.Text;
            if (featureName == "" || featureName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageTools.Warning("not a valid feature name");
                return;
            }

            string database = (string)DatabasesBox.SelectedItem;

            var sessions = SessionsBox.SelectedItems;
            var roles = RolesBox.SelectedItems;
            var streams = StreamsBox.SelectedItems;
            bool force = ForceCheckBox.IsChecked.Value;

            string sessionList = "";
            foreach (DatabaseSession session in sessions)
            {
                if (sessionList == "")
                {
                    sessionList = session.Name;
                }
                else
                {
                    sessionList += ";" + session.Name;
                }
            }

            string roleList = "";
            foreach (string role in roles)
            {
                if (roleList == "")
                {
                    roleList = role;
                }
                else
                {
                    roleList += ";" + role;
                }
            }

            string streamList = "";
            double sampleRate = 0;
            foreach (DatabaseStream stream in streams)
            {
                if (streamList == "")
                {
                    sampleRate = stream.SampleRate;
                    streamList = stream.Name;
                }
                else
                {
                    streamList += ";" + stream.Name;
                }
            }

            string rootDir = Properties.Settings.Default.DatabaseDirectory + "\\" + database;

            logTextBox.Text = handler.CMLMergeFeature(rootDir, sessionList, roleList, streamList, featureName, force);

            string type = Defaults.CML.StreamTypeNameFeature;
            string ext = "stream";            

            DatabaseStream streamType = new DatabaseStream() { Name = featureName, Type = type, FileExt = ext, SampleRate = sampleRate };
            DatabaseHandler.AddStream(streamType);

            GetStreams(streamType);
        }

        private void ExtractOrMerge_Click(object sender, RoutedEventArgs e)
        {
            if (mode == Mode.MERGE)
            {
                Merge();
            }
            else
            {
                Extract();
            }            
        }

        private double frameStepToSampleRate(string frameStep, double oldSampleRate)
        {
            double sr = 25.0;
            if (frameStep.EndsWith("ms"))
            {
                if (double.TryParse(frameStep.Remove(frameStep.Length-2), out sr))
                {
                    sr = 1000.0 / sr;
                }
            }
            else if (frameStep.EndsWith("s"))
            {
                if (double.TryParse(frameStep.Remove(frameStep.Length - 1), out sr))
                {
                    sr = 1.0 / sr;
                }
            }
            else
            {
                if (double.TryParse(frameStep, out sr))
                {
                    sr = oldSampleRate / sr;
                }
            }

            return sr;
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

        private void DatabaseResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                GetSessions();
                GetStreams();
                GetRoles();
            }

            Update();
        }

        public void GetSessions()
        {            
            if (SessionsBox.HasItems)
            {
                SessionsBox.ItemsSource = null;
            }

            List<DatabaseSession> items = DatabaseHandler.Sessions;            
            SessionsBox.ItemsSource = items;      

            if (SessionsBox.HasItems)
            {
                SessionsBox.SelectedIndex = 0;
            }
        } 

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Update();
        }    

        private void GetStreams(DatabaseStream selected = null)
        {
            List<DatabaseStream> streams = DatabaseHandler.Streams;

            List<DatabaseStream> streamsValid = new List<DatabaseStream>();
            foreach(DatabaseStream stream in streams)
            {
                if (mode == Mode.MERGE)
                {
                    if (stream.Type == Defaults.CML.StreamTypeNameFeature)
                    {
                        streamsValid.Add(stream);
                    }
                }
                else
                {
                    if (getChains(stream).Count > 0)
                    {
                        streamsValid.Add(stream);
                    }
                }
            }

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }            

            StreamsBox.ItemsSource = streamsValid;
            if (StreamsBox.HasItems)
            {                
                StreamsBox.SelectedIndex = 0;
                if (selected != null)
                {
                    StreamsBox.SelectedItem = selected;
                }
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

            if (SessionsBox.HasItems)
            {
                RolesBox.SelectAll();
                //RolesBox.SelectedItem = Properties.Settings.Default.CMLDefaultRole;
            }         
        }

        private bool parseChainFile(ref Chain chain)
        {
            XmlDocument doc = new XmlDocument();

            try
            {
                doc.Load(chain.Path);

                chain.Name = Path.GetFileNameWithoutExtension(chain.Path);

                foreach(XmlNode node in doc.SelectNodes("//meta"))
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
                }                
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
                return false;
            }

            return true;
        }

        private List<Chain> getChains(DatabaseStream stream)
        {
            List<Chain> chains = new List<Chain>();

            string chainDir = Properties.Settings.Default.CMLDirectory +
                    "\\" + Defaults.CML.ChainFolderName +
                    "\\" + stream.Type;
            if (Directory.Exists(chainDir))
            {                
                string[] chainFiles = Directory.GetFiles(chainDir, "*." + Defaults.CML.ChainFileExtension, SearchOption.AllDirectories);                    
                foreach (string chainFile in chainFiles)
                {
                    Chain chain = new Chain() { Path = chainFile };
                    if (parseChainFile(ref chain))
                    {
                        chains.Add(chain);
                    }
                }                         
            }

            return chains;
        }

        private void StreamsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChainPathComboBox.Items.Clear();

            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;

                List<Chain> chains = getChains(stream);
                foreach(Chain chain in chains)
                { 
                    ChainPathComboBox.Items.Add(chain);                     
                }
            }

            if (ChainPathComboBox.Items.Count > 0)
            {
                ChainPathComboBox.SelectedIndex = 0;                
            }

            Update();         
        }

        private void ChainPathComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChainPathComboBox.SelectedItem != null)
            {
                Chain chain = (Chain) ChainPathComboBox.SelectedItem;
                ChainPathLabel.Content = chain.Path;
                LeftContextTextBox.Text = chain.LeftContext;
                FrameStepTextBox.Text = chain.FrameStep;
                RightContextTextBox.Text = chain.RightContext;
            }
        }


        private void Update()
        {
            bool enable = false;

            if (DatabasesBox.SelectedItem != null
                && SessionsBox.SelectedItem != null
                && StreamsBox.SelectedItem != null
                && RolesBox.SelectedItem != null)
            {
                if (mode == Mode.MERGE)
                {
                    if (StreamsBox.SelectedItems.Count > 1)
                    {
                        enable = true;
                        double sr = ((DatabaseStream)StreamsBox.SelectedItems[0]).SampleRate;
                        for (int i = 1; i < StreamsBox.SelectedItems.Count; i++)
                        {
                            if (sr != ((DatabaseStream)StreamsBox.SelectedItems[i]).SampleRate)
                            {
                                enable = false;
                                break; 
                            }
                        }
                    }                   
                }
                else
                {
                    enable = ChainPathComboBox.Items.Count > 0;
                }
            }

            ChainPathComboBox.IsEnabled = enable;
            ForceCheckBox.IsEnabled = enable;
            ExtractButton.IsEnabled = enable;
            LeftContextTextBox.IsEnabled = enable;
            FrameStepTextBox.IsEnabled = enable;
            RightContextTextBox.IsEnabled = enable;
            FeatureNameTextBox.IsEnabled = enable;

            UpdateFeatureName();           
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Escape)
            {
                Close();
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

        private void GenericTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFeatureName();
        }
    }
}
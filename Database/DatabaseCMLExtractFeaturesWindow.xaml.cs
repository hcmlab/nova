using MongoDB.Bson;
using MongoDB.Driver;
using NAudio.CoreAudioApi;
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
using Tamir.SharpSsh.jsch;
using static ssi.DatabaseCMLExtractFeaturesWindow;

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

        private bool handleSelectionChanged = false;


        public class ChainCategories
        {
            HashSet<string> chaincategories = new HashSet<string>();
            public HashSet<string> GetCategories()
            {
                return chaincategories;
            }
            public void AddCategory(String category)
            {
                chaincategories.Add(category);
            }

        }

        public class Transformer
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Script { get; set; }
            public string Syspath { get; set; }
            public string OptStr { get; set; }
            public bool Multi_role_input { get; set; }
        }

        public class Chain
        {

            private List<Transformer> transformers = new List<Transformer>();
            public string Path { get; set; }
            public string Name { get; set; }
            public string FrameStep { get; set; }
            public string LeftContext { get; set; }
            public string RightContext { get; set; }
            public string Backend { get; set; }
            public string Description { get; set; }
            public string Category { get; set; }


            public  List<Transformer> GetTransformers()
            {
                return transformers;
            }
            public void AddTransformer(Transformer transformer)
            {
                transformers.Add(transformer);
            }


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

            handleSelectionChanged = true;
        }

        private void Extract()
        {
            if (ChainsBox.SelectedItem == null)
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

            Chain chain = (Chain)ChainsBox.SelectedItem;
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
                        foreach (DatabaseRole role in roles)
                        {
                            string fromPath = "";
                            bool foundpath = false;
                            foreach(string path in Defaults.LocalDataLocations())
                            {
                                if(File.Exists(path + "\\"
                                + database + "\\"
                                + session.Name + "\\"
                                + role.Name + "." + stream.Name + "." + stream.FileExt))
                                    
                                {
                                    fromPath = path + "\\"
                                    + database + "\\"
                                    + session.Name + "\\"
                                    + role.Name + "." + stream.Name + "." + stream.FileExt;
                                    foundpath = true;
                                    break;
                                }


                            }

                            string toPath = Path.GetDirectoryName(fromPath) + "\\" 
                            + role.Name + "." + featureName + ".stream";

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

            Chain selectedChain = (Chain)ChainsBox.SelectedItem;
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
            foreach (Chain item in ChainsBox.Items)
            {
                if (item.Name == selectedChain.Name)
                {
                    ChainsBox.SelectedItem = item;
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
            foreach (var role in roles)
            {
                if (roleList == "")
                {
                    roleList = ((DatabaseRole) role).Name;
                }
                else
                {
                    roleList += ";" + ((DatabaseRole)role).Name;
                }
            }

            string[] streamsNames = new string[streams.Count];
            int i = 0;
            foreach (DatabaseStream stream in streams)
            {
                streamsNames[i++] = stream.Name;
            }            
            Array.Sort(streamsNames);

            string streamList = "";
            double sampleRate = ((DatabaseStream)streams[0]).SampleRate;
            foreach (string stream in streamsNames)
            {
                if (streamList == "")
                {                    
                    streamList = stream;
                }
                else
                {
                    streamList += ";" + stream;
                }
            }

          
            string fromPath = "";
            bool foundpath = false;
            foreach (string path in Defaults.LocalDataLocations())
            {
                if (File.Exists(path + "\\"
                + database + "\\"
                + sessionList.Split(';')[0] + "\\"
                + roleList.Split(';')[0] + "." + streamList.Split(';')[0] + ".stream"))

                {
                    fromPath = path;
                    foundpath = true;
                    break;
                }


            }
            string rootDir = fromPath + "\\" + database;



            logTextBox.Text = handler.CMLMergeFeature(rootDir, sessionList, roleList, streamList, featureName, force);

            string type = Defaults.CML.StreamTypeNameFeature;
            string ext = "stream";            

            DatabaseStream streamType = new DatabaseStream() { Name = featureName, Type = type, FileExt = ext, SampleRate = sampleRate };
            DatabaseHandler.AddStream(streamType);

            GetStreams(streamType);
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

        public void GetSessions()
        {
            if (RolesBox.SelectedItem == null)
            {
                return;
            }

            SessionsBox.ItemsSource = DatabaseHandler.Sessions;
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

            List<DatabaseStream> selectedStreams = null;
            if (mode == Mode.MERGE)
            {
                selectedStreams = new List<DatabaseStream>();
                foreach (DatabaseStream s in StreamsBox.SelectedItems)
                {
                    selectedStreams.Add(s);
                }                
            }

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }            

            StreamsBox.ItemsSource = streamsValid;

            if (mode == Mode.MERGE && selectedStreams != null)
            {
                foreach (DatabaseStream s in selectedStreams)
                {
                    StreamsBox.SelectedItems.Add(s);
                }
            }

            if (mode == Mode.EXTRACT && StreamsBox.Items.Count > 0)
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
                if (selected != null)
                {
                    StreamsBox.SelectedItem = selected;
                }
                StreamsBox.ScrollIntoView(StreamsBox.SelectedItem);
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

        public void GetChains()
        {
            ChainsBox.Items.Clear();

            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;

                List<Chain> chains = getChains(stream);
                foreach (Chain chain in chains)
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
                LeftContextTextBox.Text = chain.LeftContext;
                FrameStepTextBox.Text = chain.FrameStep;
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
            string[] types = stream.Type.Split(';');


            foreach(string type in types)
            {
            string chainDir = Properties.Settings.Default.CMLDirectory +
                    "\\" + Defaults.CML.ChainFolderName +
                    "\\" + type;
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
            }

            return chains;
        }

        #endregion

        #region Update

        private void Update()
        {
            SaveDefaults();

            GetRoles();            
            GetStreams();
            GetSessions();
            if (mode != Mode.MERGE)
            {
                GetChains();
            }

            UpdateGUI();
        }

        private void UpdateGUI()
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
                    enable = ChainsBox.Items.Count > 0;
                }
            }

            ChainsBox.IsEnabled = enable;
            ForceCheckBox.IsEnabled = enable;
            ExtractButton.IsEnabled = enable;
            LeftContextTextBox.IsEnabled = enable;
            FrameStepTextBox.IsEnabled = enable;
            RightContextTextBox.IsEnabled = enable;
            FeatureNameTextBox.IsEnabled = enable;

            UpdateFeatureName();
        }

        private void SaveDefaults()
        {
            if (RolesBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultRole = "";
                foreach (DatabaseRole item in RolesBox.SelectedItems)
                {
                    Properties.Settings.Default.CMLDefaultRole += item.Name + ";";
                }
            }

            if (mode != Mode.MERGE && StreamsBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultStream = ((DatabaseStream)StreamsBox.SelectedItem).Name;
            }

            if (mode != Mode.MERGE && ChainsBox.SelectedItem != null)
            {
                Properties.Settings.Default.CMLDefaultChain = ((Chain)ChainsBox.SelectedItem).Name;
            }

            Properties.Settings.Default.Save();
        }

        private void UpdateFeatureName()
        {
            string name = "";

            if (mode == Mode.MERGE)
            {
                var streams = StreamsBox.SelectedItems;

                string streamList = "";
                HashSet<string> sourceNames = new HashSet<string>();
                HashSet<string> featureNames = new HashSet<string>();
                double sampleRate = 0;
                foreach (DatabaseStream stream in streams)
                {
                    string[] tokens = stream.Name.Split(new char[] { '.' }, 2);
                    sourceNames.Add(tokens[0]);
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

                string[] sourceArray;
                sourceArray = sourceNames.ToArray();
                Array.Sort(sourceArray);
                sourceNames.Clear();
                sourceNames.UnionWith(sourceArray);

                string medias = "";
                foreach (string media in sourceNames)
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

                string[] featureStreams;
                featureStreams = featureNames.ToArray();
                Array.Sort(featureStreams);
                featureNames.Clear();
                featureNames.UnionWith(featureStreams);

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
                if (StreamsBox.SelectedItem != null & ChainsBox.SelectedItem != null && StreamsBox.SelectedItem != null)
                {
                    DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                    Chain chain = (Chain)ChainsBox.SelectedItem;

                    string leftContext = LeftContextTextBox.Text;
                    string frameStep = FrameStepTextBox.Text;
                    string rightContext = RightContextTextBox.Text;
                    string streamMeta = "[" + leftContext + "," + frameStep + "," + rightContext + "]";

                    name = stream.Name + "." + chain.Name + streamMeta; ;
                }
            }

            FeatureNameTextBox.Text = name;
        }

        #endregion

        #region User

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

        private void GenericTextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateFeatureName();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void GeneralBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (handleSelectionChanged)
            {
                handleSelectionChanged = false;
                Update();
                handleSelectionChanged = true;
            }
        }

        private void DatabaseResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);                
            }

            Update();
        }

        #endregion

        #region Helper

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

    }
}
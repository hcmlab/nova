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
    public partial class DatabaseCMLExtractFeaturesWindow : Window
    {
        private MainHandler handler;

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

        private string tempInListPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
        private string tempOutListPath = Properties.Settings.Default.CMLDirectory + "\\" + Path.GetFileNameWithoutExtension(Path.GetRandomFileName());

        public DatabaseCMLExtractFeaturesWindow(MainHandler handler)
        {
            InitializeComponent();

            this.handler = handler;
            HelpLabel.Content = "Features are extracted every frame over a window that is extended by the left and right context.\r\n1.2s\t= 1.2 Seconds\r\n10ms\t= 10 Milliseconds\r\n500\t= 500 Samples";

            GetDatabases(DatabaseHandler.DatabaseName);
            GetStreams();
            GetRoles();
        }

        private void Done_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Extract_Click(object sender, RoutedEventArgs e)
        {
            if (ChainPathComboBox.SelectedItem == null)
            {
                MessageTools.Warning("select a chain first");
                return;
            }

            Chain chain = (Chain) ChainPathComboBox.SelectedItem;
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
            string streamMeta = "[" + leftContext + "," + frameStep + "," + rightContext + "]";
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
                                + Path.GetFileNameWithoutExtension(fromPath)
                                + "." + chain.Name + streamMeta + ".stream";
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

            if (nFiles > 0)
            {
                logTextBox.Text += handler.CMLExtractFeature(chain.Path, nParallel, tempInListPath, tempOutListPath, frameStep, leftContext, rightContext);

                string type = "feature";
                string name = stream.Name + "." + chain.Name + streamMeta;
                string ext = "stream";

                DatabaseStream streamType = new DatabaseStream() { Name = name, Type = type, FileExt = ext };
                DatabaseHandler.AddStream(streamType);
            }

            File.Delete(tempInListPath);
            File.Delete(tempOutListPath);

            Chain selectedChain = (Chain) ChainPathComboBox.SelectedItem;
            GetStreams(StreamsBox.SelectedItem.ToString());
            foreach(Chain item in ChainPathComboBox.Items)
            {
                if (item.Name == selectedChain.Name)
                {
                    ChainPathComboBox.SelectedItem = item;
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

        private void GetStreams(string selected = null)
        {
            List<DatabaseStream> streams = DatabaseHandler.Streams;

            List<DatabaseStream> streamsValid = new List<DatabaseStream>();
            foreach(DatabaseStream stream in streams)
            {
                if (getChains(stream).Count > 0)
                {
                    streamsValid.Add(stream);
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
                string[] chainDirs = Directory.GetDirectories(chainDir);
                foreach (string searchDir in chainDirs)
                {
                    string[] chainFiles = Directory.GetFiles(searchDir, "*." + Defaults.CML.ChainFileExtension);                    
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
                LeftContextTextBox.Text = chain.LeftContext;
                FrameStepTextBox.Text = chain.FrameStep;
                RightContextTextBox.Text = chain.RightContext;
            }
        }

        private void Update()
        {
            bool enable = false;

            if (ChainPathComboBox.Items.Count > 0
                && DatabasesBox.SelectedItem != null
                && SessionsBox.SelectedItem != null
                && StreamsBox.SelectedItem != null
                && RolesBox.SelectedItem != null)
            {
                enable = true;
            }

            ChainPathComboBox.IsEnabled = enable;
            ForceCheckBox.IsEnabled = enable;
            ExtractButton.IsEnabled = enable;
            LeftContextTextBox.IsEnabled = enable;
            FrameStepTextBox.IsEnabled = enable;
            RightContextTextBox.IsEnabled = enable;

        }
    }
}
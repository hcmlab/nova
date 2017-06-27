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
            public string Step { get; set; }
            public string Left { get; set; }
            public string Right { get; set; }
            public override string ToString()
            {
                return Name + " [step=" + Step + " left=" + Left + " right=" + Right + "]";
            }
        }

        public DatabaseCMLExtractFeaturesWindow(MainHandler handler)
        {
            InitializeComponent();

            this.handler = handler;

            GetDatabases(DatabaseHandler.DatabaseName);
            GetStreams();
            GetRoles();

            RolesBox.SelectAll();
            SessionsBox.SelectAll();
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

            if (DatabaseBox.SelectedItem == null || SessionsBox.SelectedItem == null || StreamsBox.SelectedItem == null || RolesBox.SelectedItem == null)
            {
                MessageTools.Warning("select database, stream, role and session(s) first");
                return;
            }

            string database = DatabaseHandler.DatabaseName;
            var sessions = SessionsBox.SelectedItems;
            var roles = RolesBox.SelectedItems;
            DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;

            logTextBox.Text = "";
            foreach (DatabaseSession session in sessions)
            {
                foreach (string role in roles)
                {                   
                    string fromPath = Properties.Settings.Default.DatabaseDirectory + "\\"
                    + database + "\\"
                    + session.Name + "\\"
                    + role + "." + stream.Name + "." + stream.FileExt;

                    string fileName = role + "." + Path.GetFileNameWithoutExtension(stream.Name) + "." + Path.GetFileNameWithoutExtension(chain.Path);
                    string toPath = Properties.Settings.Default.DatabaseDirectory + "\\"
                        + database + "\\"
                        + session.Name + "\\"
                        + fileName + ".stream";

                    if (force || !File.Exists(toPath))
                    {
                        logTextBox.Text += handler.CMLExtractFeature(chain.Path, fromPath, toPath, chain.Step, chain.Left, chain.Right);

                        string type = "feature";
                        string name = stream.Name + "." + chain.Name;
                        string ext = Path.GetExtension(toPath).Remove(0,1);                        

                        DatabaseStream streamType = new DatabaseStream() { Name = name, Type = type, FileExt = ext };
                        DatabaseHandler.AddStream(streamType);

                    }
                    else
                    {
                        logTextBox.Text += "skip " + toPath + "\n";
                    }
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

        private void DatabaseResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                string name = DatabaseBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                GetSessions();
                GetStreams();
                GetRoles();
            }
        }

        public void GetSessions()
        {            
            if (SessionsBox.HasItems)
            {
                SessionsBox.ItemsSource = null;
            }

            List<DatabaseSession> items = DatabaseHandler.Sessions;            
            SessionsBox.ItemsSource = items;

            SessionsBox.SelectAll();        
        } 

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession) SessionsBox.SelectedItem;            
            }
        }    

        private void GetStreams(string selectedItem = null)
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

            if (selectedItem != null)
            {
                StreamsBox.SelectedItem = streamsValid.Find(item => item.Name == selectedItem);
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

            RolesBox.SelectedItem = Properties.Settings.Default.CMLDefaultRole;
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
                    chain.Step = node.Attributes["frameStep"].Value;
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
                ExtractButton.IsEnabled = true;
            }          
            else
            {
                ExtractButton.IsEnabled = false;
            }
        }
    }
}
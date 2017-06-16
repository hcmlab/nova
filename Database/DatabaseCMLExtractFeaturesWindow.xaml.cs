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
            string fileName = FilenameTextBox.Text;
            bool force = ForceCheckBox.IsChecked.Value;

            if (!File.Exists(chain.Path))
            {
                MessageTools.Warning("file does not exist '" + chain.Path + "'");
                return;
            }

            if (DatabaseBox.SelectedItem == null || SessionsBox.SelectedItem == null || StreamsBox.SelectedItem == null)
            {
                MessageTools.Warning("select database, session(s) and stream first");
                return;
            }

            string database = DatabaseHandler.DatabaseName;
            var sessions = SessionsBox.SelectedItems;
            DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
            if (fileName == "")
            {
                fileName = Path.GetFileNameWithoutExtension(stream.Name) + "." + Path.GetFileNameWithoutExtension(chain.Path);
            }

            logTextBox.Text = "";
            foreach (DatabaseSession session in sessions)
            {
                string fromPath = Properties.Settings.Default.DatabaseDirectory + "\\" + database + "\\" + session.Name + "\\" + stream.Name;
                string toPath = Properties.Settings.Default.DatabaseDirectory + "\\" + database + "\\" + session.Name + "\\" + fileName + ".stream";

                if (force || !File.Exists(toPath))
                {
                    logTextBox.Text += handler.CMLExtractFeature(chain.Path, fromPath, toPath, chain.Step, chain.Left, chain.Right);

                    string type = "feature";
                    string name = Path.GetFileNameWithoutExtension(chain.Path);

                    DatabaseStreamType streamType = new DatabaseStreamType() { Name = name, Type = type };
                    DatabaseHandler.AddStreamType(streamType);
    
                    DatabaseStream features = new DatabaseStream();
                    features.StreamType = type;
                    features.Session = stream.Session;
                    features.Role = stream.Role;
                    features.Subject = stream.Subject;
                    features.Name = fileName + ".stream";
                    features.StreamName = name;
                    DatabaseHandler.AddStream(features);

                    features.Name = fileName + ".stream~";
                    DatabaseHandler.AddStream(features);
                }
                else
                {
                    logTextBox.Text += "skip " + toPath + "\n";
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
                GetSessions();                
            }
        }

        public void GetSessions(string selectedItem = null)
        {
            List<BsonDocument> sessions = DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Sessions, true);

            if (SessionsBox.HasItems)
            {
                SessionsBox.ItemsSource = null;
            }

            List<DatabaseSession> items = new List<DatabaseSession>();
            foreach (var c in sessions)
            {
                items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].ToUniversalTime(), OID = c["_id"].AsObjectId });
            }
            SessionsBox.ItemsSource = items;

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }

            if (selectedItem != null)
            {
                SessionsBox.SelectedItem = items.Find(item => item.Name == selectedItem);
                if (SessionsBox.SelectedItem != null)
                {
                    GetStreams((DatabaseSession)SessionsBox.SelectedItem);
                }                
            }            
        } 

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession) SessionsBox.SelectedItem;
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

        private void StreamsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ChainPathComboBox.Items.Clear();

            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;

                string chainDir = Properties.Settings.Default.DatabaseDirectory + "\\" + Defaults.CML.FolderName + "\\" + Defaults.CML.ChainFolderName + "\\" + stream.StreamType;
                if (Directory.Exists(chainDir))
                {
                    string[] chainFiles = Directory.GetFiles(chainDir, "*." + Defaults.CML.ChainFileExtension);
                    foreach (string chainFile in chainFiles)
                    {
                        Chain chain = new Chain() { Path = chainFile };
                        if (parseChainFile(ref chain))
                        {
                            ChainPathComboBox.Items.Add(chain);
                        }
                    }
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
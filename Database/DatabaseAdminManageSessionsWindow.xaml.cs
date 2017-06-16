using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseAdminManageSessionsWindow : Window
    {
        public DatabaseAdminManageSessionsWindow()
        {
            InitializeComponent();

            GetDatabases(DatabaseHandler.DatabaseName);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
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
                items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].ToUniversalTime(), Id = c["_id"].AsObjectId });
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

        private void AddSession_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                DatabaseSession session = new DatabaseSession() { Date = DateTime.Today };

                DatabaseAdminSessionWindow dialog = new DatabaseAdminSessionWindow(ref session);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    if (DatabaseHandler.AddSession(session))
                    {
                        GetSessions(session.Name);
                    }

                }
            }
        }

        private void EditSession_Click(object sender, RoutedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession) SessionsBox.SelectedItem;
                string name = session.Name;

                DatabaseAdminSessionWindow dialog = new DatabaseAdminSessionWindow(ref session);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    DatabaseHandler.UpdateSession(name, session);

                    var sessions = SessionsBox.SelectedItems;
                    foreach (DatabaseSession s in sessions)
                    {
                        if (session != s)
                        {
                            session.Name = s.Name;
                            DatabaseHandler.UpdateSession(s.Name, session);
                        }
                    }

                    GetSessions(session.Name);
                }                                
            }
        }

        private void DeleteSession_Click(object sender, RoutedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                var sessions = SessionsBox.SelectedItems;
                foreach (DatabaseSession session in sessions)
                {
                    DatabaseHandler.DeleteSession(session.Name);
                }
                GetSessions();
            }            
        }

        private void CopySession_Click(object sender, RoutedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                List<DatabaseStream> streams = DatabaseHandler.GetSessionStreams(session);

                Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                input["names"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = session.Name };
                UserInputWindow dialog = new UserInputWindow("Enter new name (if several separate by ';')", input);
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    string names = dialog.Result("names");
                    string[] tokens = names.Split(';');
                    foreach (string token in tokens)
                    {
                        session.Name = token;
                        if (DatabaseHandler.AddSession(session))
                        {
                            foreach (DatabaseStream stream in streams)
                            {
                                stream.Session = session.Name;
                                DatabaseHandler.AddStream(stream);
                            }
                        }
                    }

                    GetSessions(session.Name);
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


        private void SessionsBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                EditSession_Click(sender, e);
            }
        }

        private void AddStream_Click(object sender, RoutedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                DatabaseStream stream = new DatabaseStream();

                DatabaseAdminStreamWindow dialog = new DatabaseAdminStreamWindow(ref stream);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    var sessions = SessionsBox.SelectedItems;
                    foreach (DatabaseSession s in sessions)
                    {
                        stream.Session = s.Name;
                        DatabaseHandler.AddStream(stream);                        
                    }
                    GetStreams(session, stream.Name);
                }
            }
        }

        private void DeleteStream_Click(object sender, RoutedEventArgs e)
        {
            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                string name = stream.Name;
                List<DatabaseStream> streams = new List<DatabaseStream>();

                var sessions = SessionsBox.SelectedItems;
                foreach (DatabaseSession s in sessions)
                {
                    streams.AddRange(DatabaseHandler.GetSessionStreams(s));
                }

                foreach (DatabaseStream s in streams)
                {
                    if (s.Name == name)
                    {
                        DatabaseHandler.DeleteStream(s);
                    }
                }

                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetStreams(session);
            }
        }


        private void EditStream_Click(object sender, RoutedEventArgs e)
        {
            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream) StreamsBox.SelectedItem;
                string name = stream.Name;                

                DatabaseAdminStreamWindow dialog = new DatabaseAdminStreamWindow(ref stream);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    List<DatabaseStream> streams = new List<DatabaseStream>();
                    var sessions = SessionsBox.SelectedItems;
                    foreach (DatabaseSession s in sessions)
                    {
                        streams.AddRange(DatabaseHandler.GetSessionStreams(s));
                    }

                    foreach (DatabaseStream s in streams)
                    {
                        if (s.Name == name)
                        {                            
                            stream.Session = s.Session;
                            DatabaseHandler.UpdateStream(name, stream);
                        }
                    }

                    DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                    GetStreams(session, stream.Name);
                }
            }
        }

        private void CopyStream_Click(object sender, RoutedEventArgs e)
        {
            if (StreamsBox.SelectedItem != null)
            {
                DatabaseStream stream = (DatabaseStream)StreamsBox.SelectedItem;
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;

                DatabaseAdminStreamWindow dialog = new DatabaseAdminStreamWindow(ref stream);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    if (DatabaseHandler.AddStream(stream))
                    {
                        GetStreams(session, stream.Name);
                    }
                }
            }
        }

        private void StreamsBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as ListViewItem;
            if (item != null && item.IsSelected)
            {
                EditStream_Click(sender, e);
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

    }
}
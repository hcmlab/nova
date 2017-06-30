using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseAdminManageDBWindow : Window
    {
        public DatabaseAdminManageDBWindow()
        {
            InitializeComponent();

            GetDatabases(DatabaseHandler.DatabaseName);
            Refresh();
        }

        private void Refresh()
        {
            GetAnnotators();
            GetStreamTypes();
            //GetSubjects();
            GetRoles();
            GetSchemes();
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
                if(DatabaseHandler.CheckAuthentication(db) > 2)
                {
                    DatabaseBox.Items.Add(db);
                }
               
            }

            Select(DatabaseBox, selectedItem);
        }

        public void GetRoles(string selectedItem = null)
        {
            RolesBox.Items.Clear();

            List<DatabaseRole> items = DatabaseHandler.Roles;
            foreach (DatabaseRole item in items)
            {
                RolesBox.Items.Add(item.Name);
            }

            Select(RolesBox, selectedItem);
        }

        public void GetAnnotators(string selectedItem = null)
        {
            AnnotatorsBox.Items.Clear();

            List<DatabaseAnnotator> items = DatabaseHandler.Annotators;
            foreach (DatabaseAnnotator item in items)
            {
                AnnotatorsBox.Items.Add(item.Name);
            }

            Select(AnnotatorsBox, selectedItem);
        }

        //public void GetSubjects(string selectedItem = null)

        //{
        //    SubjectsBox.Items.Clear();

        //    List<string> items = DatabaseHandler.GetSubjects();
        //    foreach (string item in items)
        //    {
        //        SubjectsBox.Items.Add(item);
        //    }

        //    Select(SubjectsBox, selectedItem);
        //}

        public void GetStreamTypes(string selectedItem = null)
        {
            StreamTypesBox.Items.Clear();

            List<DatabaseStream> items = DatabaseHandler.Streams;
            foreach (DatabaseStream item in items)
            {
                StreamTypesBox.Items.Add(item.Name);
            }

            Select(StreamTypesBox, selectedItem);
        }

        public void GetSchemes(string selectedItem = null)
        {
            SchemesBox.Items.Clear();

            List<DatabaseScheme> items = DatabaseHandler.Schemes;
            foreach (DatabaseScheme item in items)
            {
                SchemesBox.Items.Add(item.Name);
            }

            Select(SchemesBox, selectedItem);
        }

        private void DatabaseBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                string db = DatabaseBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(db);

                if (DatabaseHandler.CheckAuthentication(db) > 3)
                {
                    AddAnnotator.IsEnabled = true;
                    DeleteAnnotator.IsEnabled = true;
                    EditAnnotator.IsEnabled = true;
                }
                else
                {
                    AddAnnotator.IsEnabled = false;
                    DeleteAnnotator.IsEnabled = false;
                    EditAnnotator.IsEnabled = false;
                }

                Refresh();
            }
        }

        private void AddDB_Click(object sender, RoutedEventArgs e)
        {
            DatabaseDBMeta meta = new DatabaseDBMeta();

            DatabaseAdminDBMeta dialog = new DatabaseAdminDBMeta(ref meta);
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {                
                if (DatabaseHandler.AddDB(meta))
                {
                    GetDatabases(meta.Name);
                    Refresh();
                }
            }
        }

        private void EditDB_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                string name = (string)DatabaseBox.SelectedItem;

                DatabaseDBMeta meta = new DatabaseDBMeta { Name = name };
                if (DatabaseHandler.GetDBMeta(ref meta))
                {
                    DatabaseAdminDBMeta dialog = new DatabaseAdminDBMeta(ref meta);
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.ShowDialog();

                    if (dialog.DialogResult == true)
                    {
                        DatabaseHandler.UpdateDBMeta(meta.Name, meta);
                        GetAnnotators();
                    }
                }
            }
        }


        private void DeleteDB_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                string db = (string)DatabaseBox.SelectedItem;
                MessageBoxResult result = MessageBox.Show("Delete database '" + db + "'?", "Question", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    if (DatabaseHandler.DeleteDB(db))
                    {
                        GetDatabases();
                        Refresh();
                    }
                }
            }
        }

        private void AddAnnotator_Click(object sender, RoutedEventArgs e)
        {
            List<string> users = DatabaseHandler.GetUsers();

            if (users.Count > 0)
            {
                DatabaseAnnotator annotator = new DatabaseAnnotator();
                List<string> names = DatabaseHandler.GetUsers();

                foreach(string name in AnnotatorsBox.Items)
                {
                    names.RemoveAll(s => s == name);
                }

                DatabaseAdminAnnotatorWindow dialog = new DatabaseAdminAnnotatorWindow(ref annotator, names);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();
               
                if (dialog.DialogResult == true)
                {
                    if (DatabaseHandler.AddOrUpdateAnnotator(annotator))
                    {
                        GetAnnotators(annotator.Name);
                    }                    
                }
            }
        }

        private void EditAnnotator_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotatorsBox.SelectedItem != null)
            {
                string user = (string)AnnotatorsBox.SelectedItem;

                DatabaseAnnotator annotator = new DatabaseAnnotator { Name = user };
                if (DatabaseHandler.GetAnnotator(ref annotator))
                { 

                    DatabaseAdminAnnotatorWindow dialog = new DatabaseAdminAnnotatorWindow(ref annotator);
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.ShowDialog();
                                        
                    if (dialog.DialogResult == true)
                    {
                        DatabaseHandler.AddOrUpdateAnnotator(annotator);
                        GetAnnotators();
                    }                   
                }
            }
        }

        private void DeleteAnnotator_Click(object sender, RoutedEventArgs e)
        {           
            if (AnnotatorsBox.SelectedItem != null)
            {
                string user = (string)AnnotatorsBox.SelectedItem;

                MessageBoxResult mb = MessageBox.Show("Delete annotator " + user + "?", "Question", MessageBoxButton.YesNo);                
                if (mb == MessageBoxResult.Yes)
                {
                    if (DatabaseHandler.DeleteAnnotator(user))
                    {
                        GetAnnotators();
                    }
                }
            }
        }


        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
            input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = "" };
            input["hasStreams"] = new UserInputWindow.Input() { Label = "Has streams", DefaultValue = "true" };
            UserInputWindow dialog = new UserInputWindow("Add new role", input);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string name = dialog.Result("name");
                bool hasStreams = true;
                bool.TryParse(dialog.Result("hasStreams"), out hasStreams);
                DatabaseRole role = new DatabaseRole() { Name = name, HasStreams = hasStreams };
                if (DatabaseHandler.AddRole(role))
                {
                    GetRoles(dialog.Result("name"));
                }
            }
        }

        private void EditRole_Click(object sender, RoutedEventArgs e)
        {
            if (RolesBox.SelectedItem != null)
            {
                string old_name = (string)RolesBox.SelectedItem;

                DatabaseRole old_role = DatabaseHandler.Roles.Find(r => r.Name == old_name);

                Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = old_name };
                input["hasStreams"] = new UserInputWindow.Input() { Label = "Has streams", DefaultValue = (old_role == null ? "true" : old_role.HasStreams.ToString()) };
                UserInputWindow dialog = new UserInputWindow("Edit role", input);
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    string name = dialog.Result("name");
                    bool hasStreams = true;
                    bool.TryParse(dialog.Result("hasStreams"), out hasStreams);

                    DatabaseRole role = new DatabaseRole() { Name = name, HasStreams = hasStreams };

                    if (DatabaseHandler.UpdateRole(old_name, role))
                    {
                        GetRoles(name);
                    }
                }
            }
        }

        private void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (RolesBox.SelectedItem != null)
            {
                string name = (string)RolesBox.SelectedItem;
                if (DatabaseHandler.DeleteRole(name))
                {
                    GetRoles();
                }
            }
        }

        private void AddStreamType_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
            input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = "" };
            input["fileExt"] = new UserInputWindow.Input() { Label = "File extension", DefaultValue = "" };
            input["type"] = new UserInputWindow.Input() { Label = "Type", DefaultValue = "" };
            UserInputWindow dialog = new UserInputWindow("Add new stream type", input);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string name = dialog.Result("name");
                string fileExt = dialog.Result("fileExt");
                string type = dialog.Result("type");
                DatabaseStream streamType = new DatabaseStream() { Name = name, FileExt = fileExt, Type = type };
                if (DatabaseHandler.AddStream(streamType))
                {
                    GetStreamTypes(dialog.Result("name"));
                }
            }
        }
        private void EditStreamType_Click(object sender, RoutedEventArgs e)
        {
            if (StreamTypesBox.SelectedItem != null)
            {
                string name = (string)StreamTypesBox.SelectedItem;
                DatabaseStream streamType = new DatabaseStream() { Name = name };                
                if (DatabaseHandler.GetStream(ref streamType))
                {
                    Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                    input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = streamType.Name };
                    input["fileExt"] = new UserInputWindow.Input() { Label = "File extension", DefaultValue = streamType.FileExt };                    
                    input["type"] = new UserInputWindow.Input() { Label = "Type", DefaultValue = streamType.Type };
                    UserInputWindow dialog = new UserInputWindow("Edit stream type", input);
                    dialog.ShowDialog();

                    if (dialog.DialogResult == true)
                    {
                        streamType.Name = dialog.Result("name");
                        streamType.FileExt = dialog.Result("fileExt");
                        streamType.Type = dialog.Result("type");
                        if (DatabaseHandler.UpdateStream(name, streamType))
                        {
                            GetStreamTypes(name);
                        }
                    }
                }
            }
        }

        private void DeleteStreamType_Click(object sender, RoutedEventArgs e)
        {
            if (StreamTypesBox.SelectedItem != null)
            {
                string name = (string)StreamTypesBox.SelectedItem;
                if (DatabaseHandler.DeleteStream(name))
                {
                    GetStreamTypes();
                }
            }
        }

        private void AddScheme_Click(object sender, RoutedEventArgs e)
        {
            AnnoScheme scheme = new AnnoScheme();
            if (MainHandler.AddSchemeDialog(ref scheme))
            {
                if (DatabaseHandler.AddScheme(scheme))
                {
                    GetSchemes(scheme.Name);
                }
            }
        }

        private void EditScheme_Click(object sender, RoutedEventArgs e)
        {
            if (SchemesBox.SelectedItem != null)
            {
                string name = (string)SchemesBox.SelectedItem;
                AnnoScheme scheme = DatabaseHandler.GetAnnotationScheme(name);
                if (scheme != null)
                {
                    if (MainHandler.UpdateSchemeDialog(ref scheme))
                    {
                        if (DatabaseHandler.UpdateScheme(name, scheme))
                        {
                            GetSchemes(scheme.Name);
                        }
                    }
                }
            }
        }

        private void DeleteScheme_Click(object sender, RoutedEventArgs e)
        {
            if (SchemesBox.SelectedItem != null)
            {
                string name = (string)SchemesBox.SelectedItem;
                if (DatabaseHandler.DeleteScheme(name))
                {
                    GetSchemes();
                }
            }
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            //DatabaseHandler.UpdateDatabaseLocalLists();
        }
    }
}
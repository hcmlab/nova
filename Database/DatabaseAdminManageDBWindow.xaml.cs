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
            GetSubjects();
            GetRoles();
            GetSchemes();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            DatabaseHandler.UpdateDatabaseLocalLists();
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

        public void GetRoles(string selectedItem = null)
        {
            RolesBox.Items.Clear();

            List<DatabaseRole> items = DatabaseHandler.GetRoles();
            foreach (DatabaseRole item in items)
            {
                RolesBox.Items.Add(item.Name);
            }

            Select(RolesBox, selectedItem);
        }

        public void GetAnnotators(string selectedItem = null)
        {
            AnnotatorsBox.Items.Clear();

            List<DatabaseAnnotator> items = DatabaseHandler.GetAnnotators();
            foreach (DatabaseAnnotator item in items)
            {
                AnnotatorsBox.Items.Add(item.Name);
            }

            Select(AnnotatorsBox, selectedItem);
        }

        public void GetSubjects(string selectedItem = null)

        {
            SubjectsBox.Items.Clear();

            List<string> items = DatabaseHandler.GetSubjects();
            foreach (string item in items)
            {
                SubjectsBox.Items.Add(item);
            }

            Select(SubjectsBox, selectedItem);
        }

        public void GetStreamTypes(string selectedItem = null)
        {
            StreamTypesBox.Items.Clear();

            List<string> items = DatabaseHandler.GetStreamTypes();
            foreach (string item in items)
            {
                StreamTypesBox.Items.Add(item);
            }

            Select(StreamTypesBox, selectedItem);
        }

        public void GetSchemes(string selectedItem = null)
        {
            SchemesBox.Items.Clear();

            List<DatabaseScheme> items = DatabaseHandler.GetSchemes();
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
            UserInputWindow dialog = new UserInputWindow("Add new role", input);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string name = dialog.Result("name");
                DatabaseRole role = new DatabaseRole() { Name = name };
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
                Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = old_name };
                UserInputWindow dialog = new UserInputWindow("Edit role", input);
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    string name = dialog.Result("name");

                    DatabaseRole role = new DatabaseRole() { Name = name };

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
            input["type"] = new UserInputWindow.Input() { Label = "Type", DefaultValue = "" };
            UserInputWindow dialog = new UserInputWindow("Add new stream type", input);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string name = dialog.Result("name");
                string type = dialog.Result("type");
                DatabaseStreamType streamType = new DatabaseStreamType() { Name = name, Type = type };
                if (DatabaseHandler.AddStreamType(streamType))
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
                DatabaseStreamType streamType = new DatabaseStreamType() { Name = name };                
                if (DatabaseHandler.GetStreamType(ref streamType))
                {
                    Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                    input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = streamType.Name };
                    input["type"] = new UserInputWindow.Input() { Label = "Type", DefaultValue = streamType.Type };
                    UserInputWindow dialog = new UserInputWindow("Edit stream type", input);
                    dialog.ShowDialog();

                    if (dialog.DialogResult == true)
                    {
                        streamType.Name = dialog.Result("name");
                        streamType.Type = dialog.Result("type");
                        if (DatabaseHandler.UpdateStreamType(name, streamType))
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
                if (DatabaseHandler.DeleteStreamType(name))
                {
                    GetStreamTypes();
                }
            }
        }


        private void AddSubject_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
            input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = "" };
            input["age"] = new UserInputWindow.Input() { Label = "Age", DefaultValue = "0" };
            input["gender"] = new UserInputWindow.Input() { Label = "Gender", DefaultValue = "" };
            input["culture"] = new UserInputWindow.Input() { Label = "Culture", DefaultValue = "" };
            UserInputWindow dialog = new UserInputWindow("Add new subject", input);
            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string name = dialog.Result("name");
                int age = 0;
                dialog.ResultAsInt("age", out age);
                string gender = dialog.Result("gender");
                string culture = dialog.Result("culture");
                DatabaseSubject subject = new DatabaseSubject() { Name = name, Age = age, Gender = gender, Culture = culture};
                if (DatabaseHandler.AddSubject(subject))
                {
                    GetSubjects(dialog.Result("name"));
                }
            }
        }

        private void EditSubject_Click(object sender, RoutedEventArgs e)
        {
            if (SubjectsBox.SelectedItem != null)
            {
                string name = (string)SubjectsBox.SelectedItem;
                DatabaseSubject subject = new DatabaseSubject() { Name = name };
                if (DatabaseHandler.GetSubject(ref subject))
                {
                    Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                    input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = subject.Name };
                    input["age"] = new UserInputWindow.Input() { Label = "Age", DefaultValue = subject.Age.ToString() };
                    input["gender"] = new UserInputWindow.Input() { Label = "Gender", DefaultValue = subject.Gender };
                    input["culture"] = new UserInputWindow.Input() { Label = "Culture", DefaultValue = subject.Culture };
                    UserInputWindow dialog = new UserInputWindow("Edit subject", input);
                    dialog.ShowDialog();

                    if (dialog.DialogResult == true)
                    {
                        subject.Name = dialog.Result("name");                        
                        int age = 0;
                        dialog.ResultAsInt("age", out age);
                        subject.Age = age;
                        subject.Gender = dialog.Result("gender");
                        subject.Culture = dialog.Result("culture");
                        if (DatabaseHandler.UpdateSubject(name, subject))
                        {
                            GetSubjects(name);
                        }
                    }
                }
            }
        }

        private void DeleteSubject_Click(object sender, RoutedEventArgs e)
        {
            if (SubjectsBox.SelectedItem != null)
            {
                string name = (string)SubjectsBox.SelectedItem;
                if (DatabaseHandler.DeleteSubject(name))
                {
                    GetSubjects();
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

    }
}
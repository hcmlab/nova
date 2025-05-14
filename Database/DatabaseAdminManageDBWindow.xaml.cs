using System;
using System;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            if ((Properties.Settings.Default.DatabaseAddress.Split(':'))[0] != Defaults.checkdb)
            {
                GenerateKey.Visibility = Visibility.Hidden;
            }
            else GenerateKey.Visibility = Visibility.Visible;

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
                if (DatabaseHandler.CheckAuthentication(db) > 2)
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
            StreamsBox.Items.Clear();

            List<DatabaseStream> items = DatabaseHandler.Streams;
            foreach (DatabaseStream item in items)
            {
                StreamsBox.Items.Add(item.Name);
            }

            Select(StreamsBox, selectedItem);
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

                if (DatabaseHandler.CheckAuthentication(db) >= 3)
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
          

            try
            {
                try
                {
                    if (DatabaseHandler.CheckAuthentication(Properties.Settings.Default.MongoDBUser, DatabaseBox.SelectedItem.ToString()) < 4)
                    {
                        MessageBox.Show("Only Server Admins can use this feature");
                        return;
                    }

                }
                catch (Exception ex) {
              
                }
               


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
            catch (Exception ex)
            {
                MessageBox.Show("Menu: "  + ex.Message);
            }
        }

        private void EditDB_Click(object sender, RoutedEventArgs e)
        {
           
            if (DatabaseBox.SelectedItem != null)
            {
                if (DatabaseHandler.CheckAuthentication(Properties.Settings.Default.MongoDBUser, DatabaseBox.SelectedItem.ToString()) < 4)
                {
                    MessageBox.Show("Only Server Admins can use this feature");
                    return;
                }

                string name = (string)DatabaseBox.SelectedItem;

                DatabaseDBMeta meta = new DatabaseDBMeta { Name = name };
                if (DatabaseHandler.GetDBMeta(ref meta))
                {
                    DatabaseAdminDBMeta dialog = new DatabaseAdminDBMeta(ref meta);
                    String old_db_name = meta.Name;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dialog.ShowDialog();

                    if (dialog.DialogResult == true)
                    {
                        DatabaseHandler.UpdateDBMeta(old_db_name, meta);
                        GetDatabases(meta.Name);
                        Refresh();
                    }
                }
            }
        }

        private void DeleteDB_Click(object sender, RoutedEventArgs e)
        {

            if (DatabaseBox.SelectedItem != null)
            {

                if (DatabaseHandler.CheckAuthentication(Properties.Settings.Default.MongoDBUser, DatabaseBox.SelectedItem.ToString()) < 4)
                {
                    MessageBox.Show("Only Server Admins can use this feature");
                    return;
                }

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

                foreach (string name in AnnotatorsBox.Items)
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

        private void AddStream_Click(object sender, RoutedEventArgs e)
        {

            DatabaseStream stream = new DatabaseStream();
            stream.Name = "";
            stream.FileExt = "";
            stream.Type = "";
            stream.SampleRate = 0;
            stream.DimLabels = new Dictionary<int, string>();


            DatabaseAdminStreamWindow wnd = new DatabaseAdminStreamWindow(ref stream);
            wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            wnd.ShowDialog();
            if (wnd.DialogResult == true)
            {
                if (DatabaseHandler.AddStream(stream))
                {
                    GetStreamTypes(stream.Name);
                }
            }

            //Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
            //input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = "" };
            //input["fileExt"] = new UserInputWindow.Input() { Label = "File extension", DefaultValue = "" };
            //input["type"] = new UserInputWindow.Input() { Label = "Type", DefaultValue = "" };
            //input["sr"] = new UserInputWindow.Input() { Label = "Sample rate", DefaultValue = "" };
            //UserInputWindow dialog = new UserInputWindow("Add new stream type", input);
            //dialog.ShowDialog();
            //if (dialog.DialogResult == true)
            //{
            //    string name = dialog.Result("name");
            //    string fileExt = dialog.Result("fileExt");
            //    string type = dialog.Result("type");
            //    double sr = 25.0;
            //    double.TryParse(dialog.Result("sr"), out sr);
            //    DatabaseStream streamType = new DatabaseStream() { Name = name, FileExt = fileExt, Type = type, SampleRate = sr };
            //    if (DatabaseHandler.AddStream(streamType))
            //    {
            //        GetStreamTypes(dialog.Result("name"));
            //    }
            //}
        }

        private void EditStream_Click(object sender, RoutedEventArgs e)
        {
            if (StreamsBox.SelectedItem != null)
            {
                string name = (string)StreamsBox.SelectedItem;
                DatabaseStream stream = new DatabaseStream() { Name = name };
                if (DatabaseHandler.GetStream(ref stream))
                {
                    DatabaseAdminStreamWindow wnd = new DatabaseAdminStreamWindow(ref stream);
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    wnd.ShowDialog();
                    if (wnd.DialogResult == true)
                    {
                        if (DatabaseHandler.UpdateStream(name, stream))
                        {
                            GetStreamTypes(name);
                        }
                    }



                    //Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                    //input["name"] = new UserInputWindow.Input() { Label = "Name", DefaultValue = stream.Name };
                    //input["fileExt"] = new UserInputWindow.Input() { Label = "File extension", DefaultValue = stream.FileExt };
                    //input["type"] = new UserInputWindow.Input() { Label = "Type", DefaultValue = stream.Type };
                    //input["sr"] = new UserInputWindow.Input() { Label = "Sample rate", DefaultValue = stream.SampleRate.ToString() };
                    //UserInputWindow dialog = new UserInputWindow("Edit stream type", input);
                    //dialog.ShowDialog();

                    //if (dialog.DialogResult == true)
                    //{
                    //    stream.Name = dialog.Result("name");
                    //    stream.FileExt = dialog.Result("fileExt");
                    //    stream.Type = dialog.Result("type");
                    //    double sr = 25.0;
                    //    double.TryParse(dialog.Result("sr"), out sr);
                    //    stream.SampleRate = sr;
                    //    if (DatabaseHandler.UpdateStream(name, stream))
                    //    {
                    //        GetStreamTypes(name);
                    //    }
                    //}
                }
            }
        }

        private void DeleteStreamType_Click(object sender, RoutedEventArgs e)
        {
            if (StreamsBox.SelectedItem != null)
            {
                string name = (string)StreamsBox.SelectedItem;

                MessageBoxResult result = MessageBox.Show("Do you want to also delete the remaining local files?\nThis can not be undone.", "Attention", MessageBoxButton.YesNoCancel);

                if (result == MessageBoxResult.Cancel) return;
                else
                {
                    if (result == MessageBoxResult.Yes)
                    {

                        foreach (DatabaseSession session in DatabaseHandler.Sessions)
                        {
                            foreach(DatabaseRole role in DatabaseHandler.Roles)
                            {
                                try
                                {
                                    string ext = DatabaseHandler.Streams.Find(s => s.Name == name).FileExt;


                                    File.Delete(Defaults.LocalDataLocations().First() + "\\" + DatabaseHandler.DatabaseName + "\\" + session.Name + "\\" + role.Name + "." + name + "." + ext);
                                    if (ext == ("stream"))
                                    {
                                        File.Delete(Defaults.LocalDataLocations().First() + "\\" + DatabaseHandler.DatabaseName + "\\" + session.Name + "\\" + role.Name + "." + name + ".stream~");
                                    }
                                }

                                catch
                                {
                                    //go on
                                }
                                
                            }
                        }

                    }

                    if (DatabaseHandler.DeleteStream(name))
                    {
                        GetStreamTypes();
                    }
                }
            }
        }

        private void AddScheme_Click(object sender, RoutedEventArgs e)
        {
            AnnoScheme scheme = MainHandler.AddSchemeDialog();
            if (scheme != null)
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
                if (DatabaseHandler.DeleteSchemeIfNoAnnoExists(name))
                {
                    GetSchemes();
                }
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //DatabaseHandler.UpdateDatabaseLocalLists();
        }

        private void ResampleScheme_Click(object sender, RoutedEventArgs e)
        {

            if (SchemesBox.SelectedItem != null)
            {
                string name = (string)SchemesBox.SelectedItem;
                AnnoScheme oldScheme = DatabaseHandler.GetAnnotationScheme(name);
                AnnoScheme newScheme = DatabaseHandler.GetAnnotationScheme(name);
                if (newScheme.Type != AnnoScheme.TYPE.CONTINUOUS)
                {
                    MessageBox.Show("Only continuous annotations can be resampled");
                    return;
                }

                

                newScheme.Name = newScheme.Name + "_resampled";
                AnnoTierNewContinuousSchemeWindow window = new AnnoTierNewContinuousSchemeWindow(ref newScheme);
                window.ShowDialog();

                if(window.DialogResult == true)
                {
                    if (DatabaseHandler.AddScheme(newScheme))
                    {

                        List<DatabaseAnnotation> existingAnnos =  DatabaseHandler.GetAnnotations(oldScheme);

                        if (existingAnnos.Count > 0)
                        {
                            AnnoList al_t = DatabaseHandler.LoadAnnoList(existingAnnos[0].Id);
                            double old_sr = al_t.Scheme.SampleRate;
                            double factor = 0;

                            if (old_sr > newScheme.SampleRate)
                            {
                                factor = old_sr / newScheme.SampleRate;
                            }
                            else if (old_sr < newScheme.SampleRate)
                            {
                                factor = newScheme.SampleRate / old_sr;
                            }

                            else factor = 1;
                                
                                
                            if (factor % 1 != 0)
                            {
                                MessageBox.Show("New samplerate must be a number divisible by old samplerate.");
                                return;
                            }


                            foreach (DatabaseAnnotation anno in existingAnnos)
                            {
                                AnnoList al = DatabaseHandler.LoadAnnoList(anno.Id);
                                DatabaseHandler.resampleAnnotationtoNewScheme(al, newScheme, al_t.Scheme);

                            }

                            GetSchemes();
                        }
                        else
                        {
                            MessageBox.Show("Scheme created, but no existing Annotations found, nothing was converted.");
                        }
                    }
                }



             







            }

        }

        private void GenerateKey_Click(object sender, RoutedEventArgs e)
        {
            DatabaseAdminGenerateKeyWindow window = new DatabaseAdminGenerateKeyWindow();
            window.ShowDialog();

        }
    }
}
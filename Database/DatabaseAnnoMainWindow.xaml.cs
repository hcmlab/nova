using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace ssi
{
    public class StreamItem
    {
        public string Name { get; set; }

        public string Role { get; set; }

        public string Type { get; set; }

        public string Extension { get; set; }
        public bool Exists { get; set; }
    }

    [ValueConversion(typeof(object), typeof(int))]
    public class StreamItemColorConverter : IValueConverter
    {
        public object Convert(
            object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            bool exists = (bool)System.Convert.ChangeType(value, typeof(bool));

            if (exists)
                return +1;

            return -1;
        }

        public object ConvertBack(
            object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("ConvertBack not supported");
        }
    }


    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoMainWindow : Window
    {
        private List<DatabaseAnnotation> annotations = new List<DatabaseAnnotation>();
        private CancellationTokenSource cancellation = new CancellationTokenSource();
        GridViewColumnHeader _lastHeaderClicked = null;
        ListSortDirection _lastDirection = ListSortDirection.Ascending;


        public DatabaseAnnoMainWindow()
        {
            InitializeComponent();

            serverLogin.Text = Properties.Settings.Default.DataServerLogin;
            serverPassword.Password = MainHandler.Decode(Properties.Settings.Default.DataServerPass);
            
            GetDatabases(DatabaseHandler.DatabaseName);

            showonlymine.IsChecked = Properties.Settings.Default.DatabaseShowOnlyMine;
            showOnlyUnfinished.IsChecked = Properties.Settings.Default.DatabaseShowOnlyFinished;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DataServerLogin = serverLogin.Text;
            Properties.Settings.Default.DataServerPass = MainHandler.Encode(serverPassword.Password);
            Properties.Settings.Default.Save();
            
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                if (DatabaseHandler.ChangeSession(session.Name))
                {
                    DialogResult = true;
                }
            }            
            
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public System.Collections.IList Annotations()
        {
            if (AnnotationsBox.SelectedItems != null)
                return AnnotationsBox.SelectedItems;
            else return null;
        }

        public List<StreamItem> SelectedStreams()
        {
            List<StreamItem> selectedStreams = new List<StreamItem>();
            
            if (StreamsBox.SelectedItems != null)
            {
                foreach (StreamItem stream in StreamsBox.SelectedItems)
                {
                    selectedStreams.Add(stream);                    
                }
            }

            return selectedStreams;            
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
                DatabaseHandler.ChangeDatabase(DatabaseBox.SelectedItem.ToString());
                DatabaseDBMeta meta = new DatabaseDBMeta { Name = DatabaseBox.SelectedItem.ToString() };
                if (DatabaseHandler.GetDBMeta(ref meta)) ServerLoginPanel.Visibility =  meta.ServerAuth? Visibility.Visible : Visibility.Collapsed;

                GetSessions();
                GetStreams();
                
                AnnotationsBox.ItemsSource = null;

                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetAnnotations(session);
            }
        }

        public void GetSessions(string selectedItem = null)
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
                if (selectedItem != null)
                {
                    SessionsBox.SelectedItem = items.Find(item => item.Name == selectedItem);
                }
            }
        }

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                searchTextBox.Text = "";
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetAnnotations(session);
                GetStreams();
            }
        }

        private void GetStreams(string selectedItem = null)
        {
            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseRole> roles = DatabaseHandler.Roles;

            string session = "";
            if (SessionsBox.SelectedItem != null)
            {
                session = SessionsBox.SelectedItem.ToString();
            }

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }

            List<StreamItem> items = new List<StreamItem>();
            foreach (DatabaseStream stream in streams)
            {
                foreach (DatabaseRole role in roles)
                {
                    if (role.HasStreams)
                    {
                        string filename = role.Name + "." + stream.Name + "." + stream.FileExt;
                        string directory = Properties.Settings.Default.DatabaseDirectory + "\\" + DatabaseHandler.DatabaseName + "\\" + session + "\\";
                        string filepath = directory + filename;
                        items.Add(new StreamItem() { Name = filename, Extension = stream.FileExt, Role = role.Name, Type = stream.Name, Exists = File.Exists(filepath) });
                    }
                }
            }

            StreamsBox.ItemsSource = items;
            
            if (StreamsBox.HasItems)
            {
                if (selectedItem != null)
                {
                    StreamsBox.SelectedItem = items.Find(item => item.Name == selectedItem);
                }
            }
            
        }

        public void GetAnnotations(DatabaseSession session)
        {
            if (session == null)
            {
                return;
            }

            Action EmptyDelegate = delegate () { };
            LoadingLabel.Visibility = Visibility.Visible;
            this.UpdateLayout();
            this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            bool onlyme = showonlymine.IsChecked.Value;
            bool onlyunfinished = showOnlyUnfinished.IsChecked.Value;

            AnnotationsBox.ItemsSource = null;
            this.annotations.Clear();

            var annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("session_id", session.Id);
            List<DatabaseAnnotation> list = DatabaseHandler.GetAnnotations(filter, onlyme, onlyunfinished);
           
            AnnotationsBox.ItemsSource = list;
            LoadingLabel.Visibility = Visibility.Collapsed;          
        }

        //not used anymore
        public void addAnnoToList(BsonDocument annotation, DatabaseSession session, bool onlyMe, bool onlyUnfinished)
        {
            ObjectId id = annotation["_id"].AsObjectId;

            string roleName = "";
            DatabaseHandler.GetObjectName(ref roleName, DatabaseDefinitionCollections.Roles, annotation["role_id"].AsObjectId);
            string schemeName = "";
            DatabaseHandler.GetObjectName(ref schemeName, DatabaseDefinitionCollections.Schemes, annotation["scheme_id"].AsObjectId);
            string annotatorName = "";
            DatabaseHandler.GetObjectName(ref annotatorName, DatabaseDefinitionCollections.Annotators, annotation["annotator_id"].AsObjectId);
            string annotatorFullName = DatabaseHandler.GetUserInfo(annotatorName).Fullname;

            //DatabaseHandler.GetObjectField(ref annotatorFullName, DatabaseDefinitionCollections.Annotators, annotation["annotator_id"].AsObjectId, "fullname");

            bool isFinished = false;
            try
            {
                isFinished = annotation["isFinished"].AsBoolean;
               
            }
            catch  { }

            bool islocked = false;
            try
            {
                islocked = annotation["isLocked"].AsBoolean;

            }
            catch  { }

            DateTime date = DateTime.Today;
            try
            {
                date = annotation["date"].ToUniversalTime();                
            }
            catch  { }

            bool isOwner = Properties.Settings.Default.MongoDBUser == annotatorName || DatabaseHandler.CheckAuthentication() > DatabaseAuthentication.DBADMIN;


            if (!onlyMe && !onlyUnfinished ||
                onlyMe && !onlyUnfinished && Properties.Settings.Default.MongoDBUser == annotatorName ||
                !onlyMe && onlyUnfinished && !isFinished ||
                onlyMe && onlyUnfinished && !isFinished && Properties.Settings.Default.MongoDBUser == annotatorName)
            {

                annotations.Add(new DatabaseAnnotation() { Id = id, Role = roleName, Scheme = schemeName, Annotator = annotatorName, AnnotatorFullName = annotatorFullName, Session = session.Name, IsFinished = isFinished, IsLocked = islocked, Date = date, IsOwner = isOwner });
            }
        }
        

        private ObjectId GetIdFromName(string collection, string name)
        {
            ObjectId id = new ObjectId();
            DatabaseHandler.GetObjectID(ref id, collection, name);
            return id;
        }

        private void CopyAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationsBox.SelectedItem != null)
            {
                string annotatorName = DatabaseHandler.SelectAnnotator();

                if (annotatorName != null)
                {
                    DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                    var annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

                    ObjectId annotid_new = new ObjectId();
                    DatabaseHandler.GetObjectID(ref annotid_new, DatabaseDefinitionCollections.Annotators, annotatorName);
     
                    foreach (var item in AnnotationsBox.SelectedItems)
                    {
                        ObjectId roleID = GetIdFromName(DatabaseDefinitionCollections.Roles, ((DatabaseAnnotation)(item)).Role);
                        ObjectId schemeID = GetIdFromName(DatabaseDefinitionCollections.Schemes, ((DatabaseAnnotation)(item)).Scheme);
                        ObjectId annotatorID = GetIdFromName(DatabaseDefinitionCollections.Annotators, ((DatabaseAnnotation)(item)).Annotator);
                        ObjectId sessionID = GetIdFromName(DatabaseDefinitionCollections.Sessions, session.Name);

                        var builder = Builders<BsonDocument>.Filter;
                        var filter = builder.Eq("role_id", roleID) 
                            & builder.Eq("scheme_id", schemeID)
                            & builder.Eq("annotator_id", annotatorID)
                            & builder.Eq("session_id", sessionID);
                        var anno = annotations.Find(filter).Single();

                        anno.Remove("_id");
                        anno["annotator_id"] = annotid_new;
                        try
                        {
                            anno["isFinished"] = false;
                        }
                        catch 
                        { }

                            try
                            {
                                anno["isLocked"] = false;
                            }
                            catch { }

                        UpdateOptions uo = new UpdateOptions();
                        uo.IsUpsert = true;

                        filter = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotid_new) & builder.Eq("session_id", sessionID);
                        var result = annotations.ReplaceOne(filter, anno, uo);

                    }

                    GetAnnotations(session);

                }
              
            }
        }

        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationsBox.SelectedItem != null)
            {
                IMongoCollection<BsonDocument> annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;

                foreach (var item in AnnotationsBox.SelectedItems)
                {
                    ObjectId roleID = GetIdFromName(DatabaseDefinitionCollections.Roles, ((DatabaseAnnotation)(item)).Role);
                    ObjectId schemeID = GetIdFromName(DatabaseDefinitionCollections.Schemes, ((DatabaseAnnotation)(item)).Scheme);
                    ObjectId annotatorID = GetIdFromName(DatabaseDefinitionCollections.Annotators, ((DatabaseAnnotation)(item)).Annotator);
                    ObjectId sessionID = GetIdFromName(DatabaseDefinitionCollections.Sessions, session.Name);

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("role_id", roleID)
                        & builder.Eq("scheme_id", schemeID) 
                        & builder.Eq("annotator_id", annotatorID)
                        & builder.Eq("session_id", sessionID);
                    var result = annotations.Find(filter).ToList();

                    bool isLocked = false;
                    try
                    {
                        isLocked = result[0]["isLocked"].AsBoolean;

                    }
                    catch  { }

                    if (!isLocked)
                    {
                        ObjectId id = result[0]["_id"].AsObjectId;
                        DatabaseHandler.DeleteAnnotation(id);
                    }
                    else
                    {
                        MessageBox.Show("Annotation is locked and cannot be deleted");
                    }
                }

                GetAnnotations(session);
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnnotationsBox.SelectedValue != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                IMongoCollection<BsonDocument> annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

                for (int i = 0; i < AnnotationsBox.SelectedItems.Count; i++)
                {
                    if (DatabaseHandler.CheckAuthentication() > DatabaseAuthentication.READWRITE || Properties.Settings.Default.MongoDBUser == ((DatabaseAnnotation)(AnnotationsBox.SelectedValue)).Annotator)
                    {
                        DeleteAnnotation.Visibility = Visibility.Visible;
                        //CopyAnnotation.Visibility = Visibility.Visible;                       
                    }
                }

                foreach (var item in AnnotationsBox.SelectedItems)
                {
                    ObjectId roleID = GetIdFromName(DatabaseDefinitionCollections.Roles, ((DatabaseAnnotation)(item)).Role);
                    ObjectId schemeID = GetIdFromName(DatabaseDefinitionCollections.Schemes, ((DatabaseAnnotation)(item)).Scheme);
                    ObjectId annotatorID = GetIdFromName(DatabaseDefinitionCollections.Annotators, ((DatabaseAnnotation)(item)).Annotator);
                    ObjectId sessionID = GetIdFromName(DatabaseDefinitionCollections.Sessions, session.Name);

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("session_id", sessionID);
                    var annotation = annotations.Find(filter).ToList();

                    if (annotation.Count == 0)
                    {
                        return;
                    }

                    BsonElement value;
                    if(StreamsBox.Items != null && annotation[0].TryGetElement("streams", out value))
                    {
                        //StreamsBox.SelectedItems.Clear();
                        foreach (BsonString doc in annotation[0]["streams"].AsBsonArray)
                        {
                            string name = doc.AsString;                            
                            foreach (StreamItem stream in StreamsBox.Items)
                            {
                                if (stream.Name == name)
                                {
                                    StreamsBox.SelectedItems.Add(stream);
                                }
                            }
                        }
                    }
                }

            }
        }
        
        private void showOnlyUnfinished_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyFinished = true;
            Properties.Settings.Default.Save();

            if (DatabaseBox.SelectedItem != null && SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetAnnotations(session);
            }
        }

        private void showOnlyUnfinished_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyFinished = false;
            Properties.Settings.Default.Save();

            if (DatabaseBox.SelectedItem != null && SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetAnnotations(session);
            }
        }

        private void ChangeFinishedState(ObjectId id, bool state)
        {
            var annos = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isFinished", state);
            annos.UpdateOne(filter, update);
        }

        private void ChangeLockedState(ObjectId id, bool state)
        {
            var annos = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isLocked", state);
            annos.UpdateOne(filter, update);
        }


        private void IsFinishedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, true);
        }

        private void IsFinishedCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, false);
        }

        private void IsLockedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeLockedState(anno.Id, false);
        }

        private void IsLockedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeLockedState(anno.Id, true);
        }

        private void requiresLogin_Checked(object sender, RoutedEventArgs e)
        {
            serverLogin.IsEnabled = true;
            serverPassword.IsEnabled = true;
        }

        private void requiresLogin_Unchecked(object sender, RoutedEventArgs e)
        {
            serverLogin.IsEnabled = false;
            serverPassword.IsEnabled = false;
        }

        private void showOnlyMine_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyMine = true;
            Properties.Settings.Default.Save();

            if (DatabaseBox.SelectedItem != null && SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetAnnotations(session);
            }
        }

        private void showOnlyMine_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyMine = false;
            Properties.Settings.Default.Save();

            if (DatabaseBox.SelectedItem != null && SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetAnnotations(session);
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(searchTextBox.Text))
                return true;
            else
                return (((item as DatabaseAnnotation).Scheme.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0) 
                    || (item as DatabaseAnnotation).AnnotatorFullName.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0 
                    || (item as DatabaseAnnotation).Annotator.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0
                    || (item as DatabaseAnnotation).Role.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AnnotationsBox.ItemsSource != null)
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(AnnotationsBox.ItemsSource);
                view.Filter = UserFilter;
                CollectionViewSource.GetDefaultView(AnnotationsBox.ItemsSource).Refresh();
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
                    ICollectionView dataView =  CollectionViewSource.GetDefaultView(((ListView)sender).ItemsSource);

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

        private void AnnotationsBox_Click(object sender, RoutedEventArgs e)
        {
            SortListView(sender, e);
        }

        private void SessionsBox_Click(object sender, RoutedEventArgs e)
        {
            SortListView(sender, e);
        }

        private void StreamsBox_Click(object sender, RoutedEventArgs e)
        {
            SortListView(sender, e);
        }
    }
}


using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoMainWindow : Window
    {
        private List<DatabaseAnnotation> annotations = new List<DatabaseAnnotation>();
        private CancellationTokenSource cancellation = new CancellationTokenSource();

        public DatabaseAnnoMainWindow()
        {
            InitializeComponent();

            serverLogin.Text = Properties.Settings.Default.DataServerLogin;
            serverPassword.Password = Properties.Settings.Default.DataServerPass;
            
            GetDatabases(DatabaseHandler.DatabaseName);

            showonlymine.IsChecked = Properties.Settings.Default.DatabaseShowOnlyMine;
            showOnlyUnfinished.IsChecked = Properties.Settings.Default.DatabaseShowOnlyFinished;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DataServerLogin = serverLogin.Text;
            Properties.Settings.Default.DataServerPass = serverPassword.Password;
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

        public List<DatabaseStream> SelectedStreams()
        {
            List<DatabaseStream> selectedStreams = new List<DatabaseStream>();

            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                List<DatabaseStream> streams = DatabaseHandler.GetSessionStreams(session);

                if (StreamsBox.SelectedItems != null)
                {
                    foreach (DatabaseStream stream in StreamsBox.SelectedItems)
                    {
                        selectedStreams.Add(stream);
                        foreach (DatabaseStream stream2 in streams)
                        {
                            if (stream2.Name == stream.Name + "~")
                            {
                                selectedStreams.Add(stream2);
                            }
                        }
                    }
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

               
 
                StreamsBox.ItemsSource = null;
                AnnotationsBox.ItemsSource = null;
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

        private void SessionsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsBox.SelectedItem != null)
            {
                DatabaseSession session = (DatabaseSession)SessionsBox.SelectedItem;
                GetStreams(session);
                GetAnnotations(session);
            }
        }

        private void GetStreams(DatabaseSession session, string selectedItem = null)
        {
            List<DatabaseStream> streams = DatabaseHandler.GetSessionStreams(session);

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }

            List<DatabaseStream> streamsSelection = new List<DatabaseStream>();
            foreach (DatabaseStream stream in streams)
            {
                if (!stream.Name.Contains(".stream~") && !stream.Name.Contains(".stream%7E"))
                {
                    streamsSelection.Add(stream);
                }
            }

            StreamsBox.ItemsSource = streamsSelection;

            if (selectedItem != null)
            {
                StreamsBox.SelectedItem = streamsSelection.Find(item => item.Name == selectedItem);
            }
        }

        public void GetAnnotations(DatabaseSession session)
        {
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

            //try
            //{
            //    CancellationToken ct = cancellation.Token;               
            //    using (var cursor = await annotations.FindAsync(filter))
            //    {
            //        await cursor.ForEachAsync(d => addAnnoToList(d, session, onlyme, onlyunfinished), ct);
            //    }
            //    AnnotationsBox.ItemsSource = this.annotations;
            //}
            //catch (Exception ex)
            //{
            //    if (!ex.Message.Contains("canceled"))
            //    {
            //        MessageBox.Show("At least one Database Entry seems to be corrupt. Entries have not been loaded.");
            //    }
            //}
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
            string annotatorFullName = "";
            DatabaseHandler.GetObjectField(ref annotatorFullName, DatabaseDefinitionCollections.Annotators, annotation["annotator_id"].AsObjectId, "fullname");

            bool isFinished = false;
            try
            {
                isFinished = annotation["isFinished"].AsBoolean;
               
            }
            catch (Exception ex) { }

            bool islocked = false;
            try
            {
                islocked = annotation["isLocked"].AsBoolean;

            }
            catch (Exception ex) { }

            DateTime date = DateTime.Today;
            try
            {
                date = annotation["date"].ToUniversalTime();                
            }
            catch (Exception ex) { }

            if (!onlyMe && !onlyUnfinished ||
                onlyMe && !onlyUnfinished && Properties.Settings.Default.MongoDBUser == annotatorName ||
                !onlyMe && onlyUnfinished && !isFinished ||
                onlyMe && onlyUnfinished && !isFinished && Properties.Settings.Default.MongoDBUser == annotatorName)
            {

                annotations.Add(new DatabaseAnnotation() { Id = id, Role = roleName, Scheme = schemeName, Annotator = annotatorName, AnnotatorFullName = annotatorFullName, Session = session.Name, IsFinished = isFinished, IsLocked = islocked, Date = date });
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
                        catch (Exception ex)
                        { }

                            try
                            {
                                anno["isLocked"] = false;
                            }
                            catch (Exception ex)
                            { }

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
                    catch (Exception ex) { }

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
                    if (DatabaseHandler.CheckAuthentication() > 2 || Properties.Settings.Default.MongoDBUser == ((DatabaseAnnotation)(AnnotationsBox.SelectedValue)).Annotator)
                    {
                        DeleteAnnotation.Visibility = Visibility.Visible;
                        CopyAnnotation.Visibility = Visibility.Visible;                       
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
                    if(StreamsBox.Items != null && annotation[0].TryGetElement("media", out value))
                    {
                        StreamsBox.SelectedItems.Clear();
                        foreach (BsonDocument doc in annotation[0]["media"].AsBsonArray)
                        {
                            string name = "";
                            DatabaseHandler.GetObjectName(ref name, DatabaseDefinitionCollections.Streams, doc["media_id"].AsObjectId);
                            foreach (DatabaseStream stream in StreamsBox.Items)
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

    }
}
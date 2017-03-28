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
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoMainWindow : Window
    {
        IMongoDatabase database;
        MongoClient mongo;

        private int authlevel = 0;
        private int localauthlevel = 0;
        private List<DatabaseMediaInfo> ci;
        private List<DatabaseMediaInfo> files = new List<DatabaseMediaInfo>();
        private List<DatabaseMediaInfo> allfiles = new List<DatabaseMediaInfo>();
        private List<DatabaseAnno> AnnoItems = new List<DatabaseAnno>();
        private List<ObjectId> AnnotationMediaIDS = new List<ObjectId>();
        private CancellationTokenSource cts = new CancellationTokenSource();
        public DatabaseAnnoMainWindow()
        {
            InitializeComponent();

            database = DatabaseHandler.Database;
            mongo = DatabaseHandler.Client;

            this.db_server.Text = Properties.Settings.Default.DatabaseAddress;
            this.db_login.Text = Properties.Settings.Default.MongoDBUser;
            this.db_pass.Password = Properties.Settings.Default.MongoDBPass;
            this.server_login.Text = Properties.Settings.Default.DataServerLogin;
            this.server_pass.Password = Properties.Settings.Default.DataServerPass;
            Autologin.IsEnabled = false;
            allfiles.Clear();

            if (Properties.Settings.Default.DatabaseAutoLogin == true)
            {
                Autologin.IsChecked = true;
            }
            else Autologin.IsChecked = false;

            if (Autologin.IsChecked == true)
            {
                ConnectToDB();
            }
            showonlymine.IsChecked = Properties.Settings.Default.DatabaseShowOnlyMine;
            showonlyunfinished.IsChecked = Properties.Settings.Default.DatabaseShowOnlyFinished;
        }

        private void ConnectToDB()
        {
            Properties.Settings.Default.DatabaseAddress = this.db_server.Text;
            Properties.Settings.Default.MongoDBUser = this.db_login.Text;
            Properties.Settings.Default.MongoDBPass = this.db_pass.Password;
            Properties.Settings.Default.Save();

            try
            {
                int count = 0;
                while (mongo.Cluster.Description.State.ToString() == "Disconnected")
                {
                    Thread.Sleep(100);
                    if (count++ >= 25) throw new MongoException("Unable to connect to the database. Please make sure that " + mongo.Settings.Server.Host + ":" + mongo.Settings.Server.Port + " is online and you entered your credentials correctly!");
                }

                authlevel = DatabaseHandler.CheckAuthentication(this.db_login.Text, "admin");

                if (authlevel > 0)
                {
                    SelectDatabase();
                    Autologin.IsEnabled = true;
                }
                else
                { MessageBox.Show("You have no rights to access the database list");
                authlevel = DatabaseHandler.CheckAuthentication(this.db_login.Text, Properties.Settings.Default.DatabaseName);
                }
            }
            catch (MongoException e)
            {
                MessageBox.Show(e.Message, "Connection failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                mongo.Cluster.Dispose();
            }

            //now that we made sure the user can see database, check if he has any admin/writing rights on this specific database
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnectToDB();
        }

        private void DataBasResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataBasResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.DatabaseName = DataBasResultsBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                localauthlevel = Math.Max(DatabaseHandler.CheckAuthentication(this.db_login.Text, "admin"), DatabaseHandler.CheckAuthentication(this.db_login.Text, Properties.Settings.Default.DatabaseName));
                authlevel = localauthlevel;
                if (localauthlevel > 1) GetSessions();
            }
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            allfiles.Clear();
            if (CollectionResultsBox.SelectedItem != null)
            {
                
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();
                AnnoItems.Clear();
                GetMedia();
                cts.Cancel();
                cts.Dispose();
                cts = new CancellationTokenSource();
                GetAnnotations(showonlymine.IsChecked == true, showonlyunfinished.IsChecked == true);
                //if (authlevel > 2)
                //{
                //    AddAnnotation.Visibility = Visibility.Visible;
                //}

            }
        }

        private void GetMedia()
        {
            files.Clear();
            MediaResultBox.Items.Clear();
            ci = GetMediaFromDB(Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId);

            var colllection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var media = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);

            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), DatabaseDefinitionCollections.Sessions, "name", Properties.Settings.Default.LastSessionId);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("session_id", sessionid);
            var selectedmedialist = media.Find(filter).ToList();


            foreach (DatabaseMediaInfo c in ci)
            {
                files.Add(c);
                if (!c.filename.Contains(".stream~") && !c.filename.Contains(".stream%7E"))
                {
                    MediaResultBox.Items.Add(c.filename);
                }
            }
        }

        private void requireslogin_Checked(object sender, RoutedEventArgs e)
        {
            server_login.IsEnabled = true;
            server_pass.IsEnabled = true;
        }

        private void requireslogin_Unchecked(object sender, RoutedEventArgs e)
        {
            server_login.IsEnabled = false;
            server_pass.IsEnabled = false;
        }

        private void showonlymine_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyMine = true;
            Properties.Settings.Default.Save();


            AnnoItems.Clear();
           if(DataBasResultsBox.SelectedItem != null)   GetAnnotations(true, showonlyunfinished.IsChecked == true);
        }

        private void showonlymine_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyMine = false;
            Properties.Settings.Default.Save();

            AnnoItems.Clear();
            if (DataBasResultsBox.SelectedItem != null) GetAnnotations(false, showonlyunfinished.IsChecked == true);
        }

        public int Authlevel()
        {
            return authlevel;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DataServerLogin = this.server_login.Text;
            Properties.Settings.Default.DataServerPass = this.server_pass.Password;
            Properties.Settings.Default.Save();

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public System.Collections.IList Annotations()
        {
            if (AnnotationResultBox.SelectedItems != null)
                return AnnotationResultBox.SelectedItems;
            else return null;
        }

        public List<DatabaseMediaInfo> MediaConnectionInfo()
        {
            if (ci != null)
                return ci;
            else return null;
        }

        public List<DatabaseMediaInfo> Media()
        {
            if (MediaResultBox.SelectedItems != null)
                return allfiles;
            else return null;
        }



        public async void SelectDatabase()
        {
            DataBasResultsBox.Items.Clear();

            using (var cursor = await DatabaseHandler.Client.ListDatabasesAsync())
            {
                await cursor.ForEachAsync(d => addDbtoList(d["name"].ToString()));
            }
            DataBasResultsBox.SelectedIndex = 0;
        }

        public void addDbtoList(string name)
        {
            if (name != "admin" && name != "local" && DatabaseHandler.CheckAuthentication(Properties.Settings.Default.MongoDBUser, name) >   1)
            {
                DataBasResultsBox.Items.Add(name);
            }
        }

        public void GetSessions()

        {
            var sessioncollection = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var sessions = sessioncollection.Find(_ => true).ToList();

            if (sessions.Count > 0)
            {
                if (CollectionResultsBox.Items != null) CollectionResultsBox.ItemsSource = null;
                List<DatabaseSession> items = new List<DatabaseSession>();
                foreach (var c in sessions)
                {

                    items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].AsDateTime.ToShortDateString(), OID = c["_id"].AsObjectId });
                }

                CollectionResultsBox.ItemsSource = items;
            }
            else CollectionResultsBox.ItemsSource = null;
        }

        public string FetchDBRef(IMongoDatabase database, string collection, string attribute, ObjectId reference)
        {
            string output = "";
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq("_id", reference);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).Single();

            if (result != null)
            {
                output = result[attribute].ToString();
            }

            return output;
        }

        public ObjectId GetObjectID(IMongoDatabase database, string collection, string value, string attribute)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq(value, attribute);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0) id = result[0].GetValue(0).AsObjectId;

            return id;
        }

        public async void GetAnnotations(bool onlyme = false, bool onlyunfinished = false)

        {

           
            AnnotationResultBox.ItemsSource = null;
            //  AnnotationResultBox.Items.Clear();
            List<DatabaseAnno> items = new List<DatabaseAnno>();
            List<string> Collections = new List<string>();

            var sessions = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

            // BsonDocument documents;
            var builder = Builders<BsonDocument>.Filter;

            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), DatabaseDefinitionCollections.Sessions, "name", Properties.Settings.Default.LastSessionId);

            var filter = builder.Eq("session_id", sessionid);

            try
            {
                CancellationToken ct = cts.Token;
                
                using (var cursor = await annotations.FindAsync(filter))
                {

                    await cursor.ForEachAsync(d => addAnnotoList(d, onlyme, onlyunfinished), ct);
                   
                }
                AnnotationResultBox.ItemsSource = AnnoItems;
            }
            catch (Exception ex)
            {
                if(!ex.Message.Contains("canceled"))
                MessageBox.Show("At least one Database Entry seems to be corrupt. Entries have not been loaded.");
            }
        }

        public void addAnnotoList(BsonDocument annos, bool onlyme, bool onlyunfinished)
        {


            // AnnotationResultBox.ItemsSource = null;
            ObjectId id = annos["_id"].AsObjectId;
            string roleid = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), DatabaseDefinitionCollections.Roles, "name", annos["role_id"].AsObjectId);
            string annotid = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), DatabaseDefinitionCollections.Schemes, "name", annos["scheme_id"].AsObjectId);
            string annotatid = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.DatabaseName),  DatabaseDefinitionCollections.Annotators, "name", annos["annotator_id"].AsObjectId);
            string annotatidfn = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.DatabaseName),  DatabaseDefinitionCollections.Annotators, "fullname", annos["annotator_id"].AsObjectId);
          

            bool isfinished = false;
            
            try
            {
                isfinished = annos["isFinished"].AsBoolean;
               
            }
            catch (Exception ex) { }
            bool islocked = false;
            try
            {
                islocked = annos["isLocked"].AsBoolean;

            }
            catch (Exception ex) { }

            DateTime date = annos["date"].ToUniversalTime();
            ;
            if (!onlyme && !onlyunfinished ||
                onlyme && !onlyunfinished && Properties.Settings.Default.MongoDBUser == annotatid ||
                !onlyme && onlyunfinished && !isfinished ||
                onlyme && onlyunfinished && !isfinished && Properties.Settings.Default.MongoDBUser == annotatid)
            {
                bool isOwner = authlevel > 2 || Properties.Settings.Default.MongoDBUser == annotatid;
                AnnoItems.Add(new DatabaseAnno() { Id = id, Role = roleid, AnnoScheme = annotid, AnnotatorFullname = annotatidfn, Annotator = annotatid, IsFinished = isfinished, IsLocked = islocked, IsOwner = isOwner, Date = date.ToShortDateString() + " " + date.ToShortTimeString(),  OID = id });
            }
        }

        public List<DatabaseMediaInfo> GetMediaFromDB(string db, string session)
        {
            List<DatabaseMediaInfo> paths = new List<DatabaseMediaInfo>();
            var colllection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var media = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);

            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.DatabaseName), DatabaseDefinitionCollections.Sessions, "name", Properties.Settings.Default.LastSessionId);

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("session_id", sessionid);
            var selectedmedialist = media.Find(filter).ToList();

            foreach (var selectedmedia in selectedmedialist)
            {
                DatabaseMediaInfo c = new DatabaseMediaInfo();
                string url = selectedmedia["url"].ToString();

                string[] split = url.Split(':');

                c.connection = split[0];

                if (split[0] == "ftp" || split[0] == "sftp")
                {
                    string[] split2 = split[1].Split(new char[] { '/' }, 4);
                    c.ip = split2[2];


                    string filename = split2[3].Substring(split2[3].LastIndexOf("/") + 1, (split2[3].Length - split2[3].LastIndexOf("/") - 1));
                    c.folder = split2[3].Remove(split2[3].Length - filename.Length);
                }

                c.filepath = selectedmedia["url"].ToString();
                c.filename = selectedmedia["name"].ToString();
                c.requiresauth = selectedmedia["requiresAuth"].ToString();

                //Todo: solve references
                c.subject = selectedmedia["subject_id"].ToString();
                c.role = selectedmedia["role_id"].ToString();
                c.mediatype = selectedmedia["mediatype_id"].ToString();
                c.session = selectedmedia["session_id"].ToString();
                paths.Add(c);
            }
            return paths;
        }

        private void MediaResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            allfiles.Clear();
            if (MediaResultBox.SelectedItem != null)
            {
                for (int i = 0; i < MediaResultBox.SelectedItems.Count; i++)
                {
                    for (int j = 0; j < files.Count; j++)
                    {
                        if (files[j].filename.Contains(MediaResultBox.SelectedItems[i].ToString()))
                        {
                            allfiles.Add(files[j]);
                        }
                    }
                }

                foreach (DatabaseMediaInfo c in ci)
                {
                    if (c.filename == MediaResultBox.SelectedItem.ToString())
                    {
                        if (c.requiresauth == "true")
                        {
                            requireslogin.IsChecked = true;
                            requireslogin.IsEnabled = true;
                        }
                        else
                        {
                            requireslogin.IsEnabled = false;
                            requireslogin.IsChecked = false;
                        }
                    }
                }
            }
        }

        private ObjectId GetIdFromName(IMongoCollection<BsonDocument> collection, string name, string Name="name" )
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq(Name, name);
            var result = collection.Find(filter).ToList();
            if (result.Count > 0)
            {
                id = result[0].GetValue(0).AsObjectId;
            }
            return id;
        }

        private void CopyAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationResultBox.SelectedItem != null)
            {
                var sessions = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
                var roles = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
                var annotationschemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
                var annotators = database.GetCollection<BsonDocument>( DatabaseDefinitionCollections.Annotators);


                List<string> annotator_names = new List<string>();
                foreach (var document in annotators.Find(_ => true).ToList())
                {
                    annotator_names.Add(document["fullname"].ToString());
                }

                DatabaseSelectionWindow dbw = new DatabaseSelectionWindow(annotator_names, false, "Select Annotator", "Annotator");
                dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dbw.ShowDialog();

                if (dbw.DialogResult == true)
                {
                    string annotator_name = dbw.Result().ToString();
                    ObjectId annotid_new = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.DatabaseName),  DatabaseDefinitionCollections.Annotators, "fullname", annotator_name);
     

                foreach (var item in AnnotationResultBox.SelectedItems)
                {
                    ObjectId roleid = GetIdFromName(roles, ((DatabaseAnno)(item)).Role);
                    ObjectId annotid = GetIdFromName(annotationschemes, ((DatabaseAnno)(item)).AnnoScheme);
                    ObjectId annotatid = GetIdFromName(annotators, ((DatabaseAnno)(item)).Annotator);
                    ObjectId sessionid = GetIdFromName(sessions, Properties.Settings.Default.LastSessionId);

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
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

                    filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotid_new) & builder.Eq("session_id", sessionid);
                    var result = annotations.ReplaceOne(filter, anno, uo);



                }
                    AnnoItems.Clear();
                    GetAnnotations(showonlymine.IsChecked == true, showonlyunfinished.IsChecked == true);

                }
              
            }
        }

        //private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        //{
        //        var sessions = database.GetCollection<BsonDocument>(DatabaseDefinition.Sessions);
        //        var roles = database.GetCollection<BsonDocument>(DatabaseDefinition.Roles);
        //        var annotationschemes = database.GetCollection<BsonDocument>(DatabaseDefinition.Schemes);
        //        var annotations = database.GetCollection<BsonDocument>(DatabaseDefinition.Annotations);
        //        var annotators = database.GetCollection<BsonDocument>( DatabaseDefinition.Annotators);


        //        List<string> annotator_names = new List<string>();
        //        foreach (var document in annotators.Find(_ => true).ToList())
        //        {
        //            annotator_names.Add(document["fullname"].ToString());
        //        }

        //        List<string> role_names = new List<string>();
        //        foreach (var document in roles.Find(_ => true).ToList())
        //        {
        //            if(document["isValid"].AsBoolean ==  true)
        //            role_names.Add(document["name"].ToString());
        //        }

        //        List<string> annotation_schemes = new List<string>();
        //        foreach (var document in annotationschemes.Find(_ => true).ToList())
        //        {
        //            annotation_schemes.Add(document["name"].ToString());
        //        }


        //        DatabaseUserTableWindow dbw = new DatabaseUserTableWindow(role_names, false, "Select Role", DatabaseDefinition.Roles);
        //        dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        //        dbw.ShowDialog();

        //        if (dbw.DialogResult == true)
        //        {

               
        //        string role_name = dbw.Result().ToString();
              
        //            DatabaseUserTableWindow dbw2 = new DatabaseUserTableWindow(annotation_schemes, false, "Select Scheme", "Schemes");
        //            dbw2.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        //            dbw2.ShowDialog();

        //            if (dbw2.DialogResult == true)
        //            {

        //            string annoscheme_name = dbw2.Result().ToString();

        //            DatabaseUserTableWindow dbw3 = new DatabaseUserTableWindow(annotator_names, false, "Select Annotator", "Annotator");
        //            dbw3.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
        //            dbw3.ShowDialog();

        //            if (dbw3.DialogResult == true)
        //            {
        //                    string annotator_name = dbw3.Result().ToString();
                       
                        
        //                    ObjectId roleid = GetIdFromName(roles, role_name);
        //                    ObjectId annotid = GetIdFromName(annotationschemes, annoscheme_name);
        //                    ObjectId annotatid = GetIdFromName(annotators, annotator_name, "fullname");
        //                    ObjectId sessionid = GetIdFromName(sessions, Properties.Settings.Default.LastSessionId);

        //                    var builder = Builders<BsonDocument>.Filter;
        //                    var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
        //                    var anno = annotations.Find(filter).ToList();

        //                if(anno.Count == 0)
        //                {
        //                    BsonElement user = new BsonElement("annotator_id", annotatid);
        //                    BsonElement role = new BsonElement("role_id", roleid);
        //                    BsonElement annot = new BsonElement("scheme_id", annotid);
        //                    BsonElement session = new BsonElement("session_id", sessionid);
        //                    BsonElement isfinished = new BsonElement("isFinished", false);
        //                    BsonElement date = new BsonElement("date", new BsonDateTime(DateTime.Now));
        //                    BsonArray media = new BsonArray();
        //                    BsonArray data = new BsonArray();
        //                    BsonDocument document = new BsonDocument();


        //                    BsonElement value;
        //                    var filterscheme = builder.Eq("_id", annotid);
        //                    var annosch = annotationschemes.Find(filterscheme).Single();

        //                    if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "CONTINUOUS")
        //                    {

        //                    double min = 0;
        //                    double max = 1;
        //                    double sr = 1;

        //                    if (annosch.TryGetElement("min", out value)) min = double.Parse(annosch["min"].ToString());
        //                    if (annosch.TryGetElement("max", out value)) max = double.Parse(annosch["max"].ToString());
        //                    if (annosch.TryGetElement("sr", out value)) sr = double.Parse(annosch["sr"].ToString());

        //                    double defaultvalue = 0.5

        //                    for (int i = 0; i < a.AnnoList.Count; i++)
        //                    {
        //                        data.Add(new BsonDocument { { "score", 0 }, { "conf", 1.0 } });
        //                    }

        //                    }

        //                    document.Add(session);
        //                    document.Add(user);
        //                    document.Add(role);
        //                    document.Add(annot);
        //                    document.Add(date);
        //                    document.Add(isfinished);
        //                    document.Add("media", media);
        //                    document.Add("labels", data);


        //                    UpdateOptions uo = new UpdateOptions();
        //                    uo.IsUpsert = true;

        //                    filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotid) & builder.Eq("session_id", sessionid);
        //                    var result = annotations.ReplaceOne(filter, document, uo);

        //                }

        //                else
        //                {
        //                    MessageBox.Show("Annotation already exists. Nothing happend.");

        //                }    


                          

        //                AnnoItems.Clear();
        //                GetAnnotations(showonlymine.IsChecked == true, showonlyunfinished.IsChecked == true);
        //            }
        //            }
                   
        //    }
        //}




        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationResultBox.SelectedItem != null)
            {


                var sessions = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
                var roles = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
                var annotationschemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
                var annotators = database.GetCollection<BsonDocument>( DatabaseDefinitionCollections.Annotators);


                foreach (var item in AnnotationResultBox.SelectedItems)
                {
                    ObjectId roleid = GetIdFromName(roles, ((DatabaseAnno)(item)).Role);
                    ObjectId annotid = GetIdFromName(annotationschemes, ((DatabaseAnno)(item)).AnnoScheme);
                    ObjectId annotatid = GetIdFromName(annotators, ((DatabaseAnno)(item)).Annotator);
                    ObjectId sessionid = GetIdFromName(sessions, Properties.Settings.Default.LastSessionId);

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);

                    var result = annotations.Find(filter).Single();


                    bool islocked = false;
                    try
                    {
                        islocked = result["isLocked"].AsBoolean;

                    }
                    catch (Exception ex) { }


                    if (!islocked) annotations.DeleteOne(filter);
                    else MessageBox.Show("Annotaion is locked and therefore can't be deleted");
                }
                AnnoItems.Clear();
                GetAnnotations(showonlymine.IsChecked == true, showonlyunfinished.IsChecked == true);
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            
            if (AnnotationResultBox.SelectedValue != null)
            {
                for (int i = 0; i < AnnotationResultBox.SelectedItems.Count; i++)
                {
                    if (authlevel > 2 || Properties.Settings.Default.MongoDBUser == ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Annotator)
                    {
                        DeleteAnnotation.Visibility = Visibility.Visible;
                        CopyAnnotation.Visibility = Visibility.Visible;
                       
                    }
                }


                MediaResultBox.SelectedItems.Clear();
                for (int i = 0;  i < AnnotationResultBox.SelectedItems.Count; i++)
                {

               
                    var sessions = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
                    var roles = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
                    var annotationschemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                    var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
                    var annotators = database.GetCollection<BsonDocument>( DatabaseDefinitionCollections.Annotators);
                    ObjectId roleid = GetIdFromName(roles, ((DatabaseAnno)(AnnotationResultBox.SelectedItems[i])).Role);
                    ObjectId annotid = GetIdFromName(annotationschemes, ((DatabaseAnno)(AnnotationResultBox.SelectedItems[i])).AnnoScheme);
                    ObjectId annotatid = GetIdFromName(annotators, ((DatabaseAnno)(AnnotationResultBox.SelectedItems[i])).Annotator);
                    ObjectId sessionid = GetIdFromName(sessions, Properties.Settings.Default.LastSessionId);

                    var builder = Builders<BsonDocument>.Filter;
                    var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);

                    var annotation = annotations.Find(filter).Single();

                    BsonElement value;
                    if(annotation.TryGetElement("media", out value))
                    { 
                      

                        foreach (BsonDocument doc in annotation["media"].AsBsonArray)
                        {
                          string name =  FetchDBRef(database,DatabaseDefinitionCollections.Streams, "name", doc["media_id"].AsObjectId);

                          MediaResultBox.SelectedItems.Add(name);
                        }
                    }
                }

            }
        }

        private void Autologin_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseAutoLogin = false;
            Properties.Settings.Default.Save();
        }

        private void Autologin_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseAutoLogin = true;
            Properties.Settings.Default.Save();
        }

        private void db_login_TextChanged(object sender, TextChangedEventArgs e)
        {
            Autologin.IsChecked = false;
            Autologin.IsEnabled = false;
        }

        private void db_pass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Autologin.IsChecked = false;
            Autologin.IsEnabled = false;
        }

        private void showonlyunfinished_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyFinished = true;
            Properties.Settings.Default.Save();

            AnnoItems.Clear();
            GetAnnotations(showonlymine.IsChecked == true, true);
        }

        private void showonlyunfinished_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DatabaseShowOnlyFinished = false;
            Properties.Settings.Default.Save();
            AnnoItems.Clear();
            GetAnnotations(showonlymine.IsChecked == true, false);
        }

        private void ChangeFinishedState(ObjectId id, bool state)
        {
            var annos = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isFinished", state);
            annos.UpdateOne(filter, update);
        }



        private void ChangeLockedState(ObjectId id, bool state)
        {
            var annos = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isLocked", state);
            annos.UpdateOne(filter, update);
        }


        private void IsFinishedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            //Properties.Settings.Default.OnlyFinished = true;
            //Properties.Settings.Default.Save();

            DatabaseAnno anno = (DatabaseAnno)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, true);
        }

        private void IsFinishedCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {

            //Properties.Settings.Default.OnlyFinished = false;
            //Properties.Settings.Default.Save();

            DatabaseAnno anno = (DatabaseAnno)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, false);
        }

        private void db_server_GotMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            db_server.SelectAll();
        }

        private void db_login_GotMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            db_login.SelectAll();
        }

        private void db_pass_GotMouseCapture(object sender, System.Windows.Input.MouseEventArgs e)
        {
            db_pass.SelectAll();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.Return) ConnectToDB(); 
        }

        private void db_server_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            db_server.SelectAll();
        }

        private void db_login_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            db_login.SelectAll();
        }

        private void db_pass_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
        {
            db_pass.SelectAll();
        }

        private void IsLockedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DatabaseAnno anno = (DatabaseAnno)((CheckBox)sender).DataContext;
            ChangeLockedState(anno.Id, false);

        }

        private void IsLockedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DatabaseAnno anno = (DatabaseAnno)((CheckBox)sender).DataContext;
            ChangeLockedState(anno.Id, true);
        }
    }
}
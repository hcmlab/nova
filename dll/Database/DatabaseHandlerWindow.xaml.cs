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
    public partial class DatabaseHandlerWindow : Window
    {
        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        private int authlevel = 0;
        private int localauthlevel = 0;
        private List<DatabaseMediaInfo> ci;
        private List<DatabaseMediaInfo> files = new List<DatabaseMediaInfo>();
        private List<DatabaseMediaInfo> allfiles = new List<DatabaseMediaInfo>();
        private List<DatabaseAnno> AnnoItems = new List<DatabaseAnno>();

        public DatabaseHandlerWindow()
        {
            InitializeComponent();

            this.db_server.Text = Properties.Settings.Default.MongoDBIP;
            this.db_login.Text = Properties.Settings.Default.MongoDBUser;
            this.db_pass.Password = Properties.Settings.Default.MongoDBPass;
            this.server_login.Text = Properties.Settings.Default.DataServerLogin;
            this.server_pass.Password = Properties.Settings.Default.DataServerPass;
            Autologin.IsEnabled = false;

            if (Properties.Settings.Default.Autologin == true)
            {
                Autologin.IsChecked = true;
            }
            else Autologin.IsChecked = false;

            if (Autologin.IsChecked == true)
            {
                ConnecttoDB();
            }
        }

        private void ConnecttoDB()
        {
            Properties.Settings.Default.MongoDBIP = this.db_server.Text;
            Properties.Settings.Default.MongoDBUser = this.db_login.Text;
            Properties.Settings.Default.MongoDBPass = this.db_pass.Password;
            Properties.Settings.Default.Save();

            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;

            try
            {
                mongo = new MongoClient(connectionstring);
                int count = 0;
                while (mongo.Cluster.Description.State.ToString() == "Disconnected")
                {
                    Thread.Sleep(100);
                    if (count++ >= 25) throw new MongoException("Unable to connect to the database. Please make sure that " + mongo.Settings.Server.Host + ":" + mongo.Settings.Server.Port + " is online and you entered your credentials correctly!");
                }

                authlevel = checkAuth(this.db_login.Text, "admin");

                if (authlevel > 0)
                {
                    SelectDatabase();
                    Autologin.IsEnabled = true;
                }
                else MessageBox.Show("You have no rights to access the database list");
                authlevel = checkAuth(this.db_login.Text, Properties.Settings.Default.Database);
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
            ConnecttoDB();
        }

        private void DataBasResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataBasResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.Database = DataBasResultsBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                localauthlevel = Math.Max(checkAuth(this.db_login.Text, "admin"), checkAuth(this.db_login.Text, Properties.Settings.Default.Database));
                if (localauthlevel > 1) GetSessions();
            }
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();
                AnnoItems.Clear();
                GetMedia();
                GetAnnotations();
            }
        }

        private void GetMedia()
        {
            MediaResultBox.Items.Clear();
            ci = GetMediaFromDB(Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId);

            foreach (DatabaseMediaInfo c in ci)
            {
                files.Add(c);
                if (!c.filepath.Contains(".stream~") && !c.filepath.Contains(".stream%7E"))
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
            AnnoItems.Clear();
            GetAnnotations(true, showonlyunfinished.IsChecked == true);
        }

        private void showonlymine_Unchecked(object sender, RoutedEventArgs e)
        {
            AnnoItems.Clear();
            GetAnnotations(false, showonlyunfinished.IsChecked == true);
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

        private int checkAuth(string dbuser, string db = "admin")
        {
            //4 = root
            //3 = admin
            //2 = write
            //1 = read
            //0 = notauthorized

            int auth = 0;
            try
            {
                var adminDB = mongo.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && roles[i]["db"] == db && auth <= 4) auth = 4;
                    else if (roles[i]["role"].ToString() == "userAdminAnyDatabase" || roles[i]["role"].ToString() == "userAdmin" && roles[i]["db"] == db && auth <= 3) auth = 3;
                    else if (roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && roles[i]["db"] == db && auth <= 2) auth = 2;
                    else if (roles[i]["role"].ToString() == "readAnyDatabase" || roles[i]["role"].ToString() == "read" && roles[i]["db"] == db && auth <= 1) auth = 1;
                    else if (auth == 0) auth = 0;

                    //edit/add more roles if you want to change security levels
                }
            }
            catch
            {
                var adminDB = mongo.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && auth < 4) auth = 4;
                    else if (roles[i]["role"].ToString() == "userAdminAnyDatabase" && auth < 3) auth = 3;
                    else if (roles[i]["role"].ToString() == "readWriteAnyDatabase" && auth < 2) auth = 2;
                    else if (roles[i]["role"].ToString() == "readAnyDatabase" && auth < 1) auth = 1;
                    else auth = 0;

                    //edit/add more roles if you want to change security levels
                }
            }

            return auth;
        }

        public async void SelectDatabase()
        {
            DataBasResultsBox.Items.Clear();

            using (var cursor = await mongo.ListDatabasesAsync())
            {
                await cursor.ForEachAsync(d => addDbtoList(d["name"].ToString()));
            }
        }

        public void addDbtoList(string name)
        {
            if (name != "admin" && name != "local" && checkAuth(Properties.Settings.Default.MongoDBUser, name) > 1)
            {
                DataBasResultsBox.Items.Add(name);
            }
        }

        public void GetSessions()

        {
            database = mongo.GetDatabase(Properties.Settings.Default.Database);

            var sessioncollection = database.GetCollection<BsonDocument>("Sessions");
            var sessions = sessioncollection.Find(_ => true).ToList();

            if (sessions.Count > 0)
            {
                if (CollectionResultsBox.Items != null) CollectionResultsBox.ItemsSource = null;
                List<DatabaseSession> items = new List<DatabaseSession>();
                foreach (var c in sessions)
                {
                    //CollectionResultsBox.Items.Add(c.GetElement(1).Value.ToString());
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

            database = mongo.GetDatabase(Properties.Settings.Default.Database);
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var annotations = database.GetCollection<BsonDocument>("Annotations");

            // BsonDocument documents;
            var builder = Builders<BsonDocument>.Filter;

            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);

            var filter = builder.Eq("session_id", sessionid);

            try
            {
                using (var cursor = await annotations.FindAsync(filter))
                {
                    await cursor.ForEachAsync(d => addAnnotoList(d, onlyme, onlyunfinished));
                }
                AnnotationResultBox.ItemsSource = AnnoItems;
            }
            catch (Exception ex){
                MessageBox.Show("At least one Database Entry seems to be corrupt. Entries have not been loaded.");
            }
          
        }

        public void addAnnotoList(BsonDocument annos, bool onlyme, bool onlyunfinished)
        {
            // AnnotationResultBox.ItemsSource = null;
            ObjectId id = annos["_id"].AsObjectId;
            string roleid = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.Database), "Roles", "name", annos["role_id"].AsObjectId);
            string annotid = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.Database), "AnnotationSchemes", "name", annos["scheme_id"].AsObjectId);
            string annotatid = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.Database), "Annotators", "name", annos["annotator_id"].AsObjectId);
            string annotatidfn = FetchDBRef(mongo.GetDatabase(Properties.Settings.Default.Database), "Annotators", "fullname", annos["annotator_id"].AsObjectId);

            bool isfinished = false;
            try
            {
                isfinished = annos["isFinished"].AsBoolean;
            }
            catch(Exception ex) {}

            if (!onlyme && !onlyunfinished ||
                onlyme && !onlyunfinished && Properties.Settings.Default.MongoDBUser == annotatid ||
                !onlyme && onlyunfinished && !isfinished ||
                onlyme && onlyunfinished && !isfinished && Properties.Settings.Default.MongoDBUser == annotatid)
            {
                bool isOwner = authlevel > 2 || Properties.Settings.Default.MongoDBUser == annotatid;
                AnnoItems.Add(new DatabaseAnno() { Id=id, Role = roleid, AnnoType = annotid, AnnotatorFullname = annotatidfn, Annotator = annotatid, IsFinished = isfinished, IsOwner = isOwner, OID= id });                                               
            }
        }

        public List<DatabaseMediaInfo> GetMediaFromDB(string db, string session)
        {
            List<DatabaseMediaInfo> paths = new List<DatabaseMediaInfo>();
            var colllection = database.GetCollection<BsonDocument>("Sessions");
            var media = database.GetCollection<BsonDocument>("Media");

            ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);

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


        private ObjectId GetIdFromName(IMongoCollection<BsonDocument> collection, string name)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var database = collection.Find(filter).ToList();
            if (database.Count > 0) id = database[0].GetValue(0).AsObjectId;

            return id;
        }

        private void CopyAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationResultBox.SelectedItem != null)
            {
                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var roles = database.GetCollection<BsonDocument>("Roles");
                var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
                var annotations = database.GetCollection<BsonDocument>("Annotations");
                var annotators = database.GetCollection<BsonDocument>("Annotators");

                ObjectId roleid = GetIdFromName(roles, ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Role);
                ObjectId annotid = GetIdFromName(annotationschemes, ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).AnnoType);
                ObjectId annotatid = GetIdFromName(annotators, ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Annotator);
                ObjectId sessionid = GetIdFromName(sessions, Properties.Settings.Default.LastSessionId);

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
                var anno = annotations.Find(filter).Single();

                List<string> annotator_names = new List<string>();
                foreach (var document in annotators.Find(_ => true).ToList())
                {
                    annotator_names.Add(document["fullname"].ToString());
                }
                DatabaseUserTableWindow dbw = new DatabaseUserTableWindow(annotator_names, false, "Select Annotator", "Annotator");
                dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dbw.ShowDialog();

                if (dbw.DialogResult == true)
                {
                    string annotator_name = dbw.Result().ToString();
                    ObjectId annotid_new =  GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Annotators", "fullname", annotator_name);
                    //ObjectId annotid_new = GetObjectID(annotators, annotator_name);     
                                   
                    anno.Remove("_id");
                    anno["annotator_id"] = annotid_new;
                    try
                    {
                        anno["isFinished"] = false;
                    }
                    catch (Exception ex)
                    { }

                    UpdateOptions uo = new UpdateOptions();
                    uo.IsUpsert = true;

                    filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotid_new) & builder.Eq("session_id", sessionid);
                    var result = annotations.ReplaceOne(filter, anno, uo);

                    AnnoItems.Clear();
                    GetAnnotations();
                }

               
            }
        }


        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationResultBox.SelectedItem != null)
            {
                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var roles = database.GetCollection<BsonDocument>("Roles");
                var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
                var annotations = database.GetCollection<BsonDocument>("Annotations");
                var annotators = database.GetCollection<BsonDocument>("Annotators");

                ObjectId roleid = GetIdFromName(roles, ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Role);                
                ObjectId annotid = GetIdFromName(annotationschemes, ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).AnnoType);                
                ObjectId annotatid = GetIdFromName(annotators, ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Annotator);                
                ObjectId sessionid = GetIdFromName(sessions, Properties.Settings.Default.LastSessionId);

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
                var result = annotations.DeleteOne(filter);

                AnnoItems.Clear();
                GetAnnotations();
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
            }
        }

        private void Autologin_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Autologin = false;
            Properties.Settings.Default.Save();
        }

        private void Autologin_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Autologin = true;
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
            AnnoItems.Clear();
            GetAnnotations(showonlymine.IsChecked == true, true);
        }

        private void showonlyunfinished_Unchecked(object sender, RoutedEventArgs e)
        {
            AnnoItems.Clear();
            GetAnnotations(showonlymine.IsChecked == true, false);
        }

        private void ChangeFinishedState(ObjectId id, bool state)
        {
            var annos = database.GetCollection<BsonDocument>("Annotations");
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isFinished", state);
            annos.UpdateOne(filter, update);
        }

        private void IsFinishedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DatabaseAnno anno = (DatabaseAnno) ((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, true);
        }

        private void IsFinishedCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DatabaseAnno anno = (DatabaseAnno)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, false);
        }


    }
}
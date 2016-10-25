using MongoDB.Bson;
using MongoDB.Driver;
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
    public partial class DatabaseAdminWindow : Window
    {
        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        private int authlevel = 0;
        private string lastrole = "";

        public DatabaseAdminWindow()
        {
            InitializeComponent();

            this.db_server.Text = Properties.Settings.Default.MongoDBIP;
            this.db_login.Text = Properties.Settings.Default.MongoDBUser;
            this.db_pass.Password = Properties.Settings.Default.MongoDBPass;
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

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            DatabaseMediaWindow dbmw = new DatabaseMediaWindow();
            dbmw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dbmw.ShowDialog();

            if (dbmw.DialogResult == true)
            {
                Properties.Settings.Default.DataServerConnectionType = dbmw.Type();
                Properties.Settings.Default.Filenames = dbmw.Files();
                Properties.Settings.Default.DataServer = dbmw.Server(); ;
                Properties.Settings.Default.DataServerFolder = dbmw.Folder(); ;
                bool requiresAuth = dbmw.Auth();

                string[] fnames = Properties.Settings.Default.Filenames.Split(';');
                AddMediatoDatabase(Properties.Settings.Default.DataServerConnectionType, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.DataServer, Properties.Settings.Default.DataServerFolder, fnames, requiresAuth);
            }
        }

        public void AddMediatoDatabase(string connection, string db, string session, string ip, string folder, string[] filenames, bool auth = false)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);

            if (CollectionResultsBox.SelectedItem != null)
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", session) & builder.Eq("connection", connection);
                var media = database.GetCollection<BsonDocument>("Media");

                ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);

                for (int i = 0; i < filenames.Length; i++)
                {
                    string filename = filenames[i];
                    if (isURL(filenames[i]))
                    {
                        filename = filenames[i].Substring(filenames[i].LastIndexOf("/") + 1, (filenames[i].Length - filenames[i].LastIndexOf("/") - 1));
                    }
                    // string id = media[0]["name"].ToString();

                    string url = "";

                    if (connection == "sftp") url = "sftp://" + ip + folder + "/" + filename;
                    if (connection == "ftp") url = "ftp://" + ip + folder + "/" + filename;
                    if (connection == "http") url = filenames[i];

                    BsonDocument b = new BsonDocument
                    {
                                 { "name", filename },
                                 { "url", url },
                                 { "requiresAuth", auth},
                                 { "session_id", sessionid},
                                 { "mediatype_id", "" },
                                 { "role_id", "" },
                                 { "subject_id", "" }
                    };

                    media.InsertOne(b);
                }

                GetMedia();
            }
        }

        private bool isURL(string url)
        {
            if (url.Contains("http://") || url.Contains("https://"))
                return true;
            else return false;
        }

        private void AddRole_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("New Role", "Enter Subject Role", "");
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                lastrole = l.Result();
                //Todo: Add more meta information about the session (e.g. participants, topic, language)
                BsonArray files = new BsonArray();
                BsonDocument role = new BsonDocument {
                    {"name",  lastrole},
                    {"isValid",  true}
                };

                bool subjectalreadypresent = false;

                var collection = database.GetCollection<BsonDocument>("Roles");
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", lastrole);
                var documents = collection.Find(filter).ToList();

                foreach (var item in documents)
                {
                    if (item["name"].ToString() == lastrole)
                    {
                        subjectalreadypresent = true;
                        var update = Builders<BsonDocument>.Update.Set("isValid", true);
                        collection.UpdateOne(filter, update);
                    }
                }
                if (subjectalreadypresent == false)
                {
                    collection.InsertOne(role);
                }

                GetRoles(l.Result());
            }
        }

        private void AddSession_Click(object sender, RoutedEventArgs e)
        {
            DataBaseSessionWindow dbsw = new DataBaseSessionWindow();
            dbsw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dbsw.ShowDialog();

            if (dbsw.DialogResult == true)
            {
                Properties.Settings.Default.LastSessionId = dbsw.Name();
                Properties.Settings.Default.Save();

                BsonElement name = new BsonElement("name", dbsw.Name());
                BsonElement location = new BsonElement("location", dbsw.Location());
                BsonElement language = new BsonElement("language", dbsw.Language());
                BsonElement date = new BsonElement("date", dbsw.Date());
                BsonElement isValid = new BsonElement("isValid", true);
                BsonDocument document = new BsonDocument();

                document.Add(name);
                document.Add(location);
                document.Add(language);
                document.Add(date);
                document.Add(isValid);

                bool sessionnamealreadypresent = false;
                foreach (var item in CollectionResultsBox.Items)
                {
                    if (item.ToString() == dbsw.Name())
                    {
                        sessionnamealreadypresent = true;
                    }
                }
                if (sessionnamealreadypresent == false)
                {
                    var collection = database.GetCollection<BsonDocument>("Sessions");
                    collection.InsertOne(document);
                    GetSessions();
                }
                else MessageBox.Show("Session is already present, please select another name!");
            }
        }

        private void AddDB_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("New Database", "Enter Database Name", Properties.Settings.Default.Database);
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                Properties.Settings.Default.Database = l.Result();
                Properties.Settings.Default.Save();

                //Todo: Add more meta information about the database (e.g. sensors, topic, intention, country etc)
                BsonDocument meta = new BsonDocument {
                    {"name",  l.Result()},
                    {"description",  ""}
                };

                ;
                database = mongo.GetDatabase(Properties.Settings.Default.Database);
                var collection = database.GetCollection<BsonDocument>("Meta");
                collection.InsertOne(meta);
                GetDatabase();
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
                    if (count++ >= 25) throw new MongoException("Unable to connect to the database. Please make sure that " + mongo.Settings.Server.Host + " is online and you entered your credentials correctly!");
                }

                authlevel = checkAuth(this.db_login.Text, "admin");

                if (authlevel > 0)
                {
                    GetDatabase();
                    Autologin.IsEnabled = true;
                }
                else MessageBox.Show("You have no rights to access the database list");

                if (authlevel > 3)
                {
                    DeleteDB.Visibility = Visibility.Visible;
                    AddDB.Visibility = Visibility.Visible;
                }
            }
            catch (MongoException e)

            {
                MessageBox.Show(e.Message);
                mongo.Cluster.Dispose();
            }
        }

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            ConnecttoDB();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
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
                var adminDB = mongo.GetDatabase(db);
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if (roles[i]["role"] != null)
                    {
                        if (roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && auth < 4) auth = 4;
                        else if (roles[i]["role"].ToString() == "userAdminAnyDatabase" || roles[i]["role"].ToString() == "userAdmin" && auth < 3) auth = 3;
                        else if (roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && auth < 2) auth = 2;
                        else if (roles[i]["role"].ToString() == "readAnyDatabase" || roles[i]["role"].ToString() == "read" && auth < 1) auth = 1;
                        else auth = 0;
                    }
                    else auth = 0;
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

        public void GetDatabase()
        {
            DataBasResultsBox.Items.Clear();

            var databases = mongo.ListDatabasesAsync().Result.ToListAsync().Result;
            foreach (var c in databases)
            {
                if (c.GetElement(0).Value.ToString() != "admin" && c.GetElement(0).Value.ToString() != "local")
                    DataBasResultsBox.Items.Add(c.GetElement(0).Value.ToString());
            }
        }

        public void GetSessions()

        {
            database = mongo.GetDatabase(Properties.Settings.Default.Database);

            var sessioncollection = database.GetCollection<BsonDocument>("Sessions");
            var sessions = sessioncollection.Find(_ => true).ToList();

            if (CollectionResultsBox.HasItems) CollectionResultsBox.ItemsSource = null;
            List<DatabaseSession> items = new List<DatabaseSession>();
            foreach (var c in sessions)
            {
                //CollectionResultsBox.Items.Add(c.GetElement(1).Value.ToString());
                items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].AsDateTime.ToShortDateString() });
            }

            CollectionResultsBox.ItemsSource = items;
        }

        public void GetRoles(string selecteditem = null)

        {
            RolesResultBox.Items.Clear();

            List<string> Collections = new List<string>();
            var roles = database.GetCollection<BsonDocument>("Roles");

            var documents = roles.Find(_ => true).ToList();

            foreach (BsonDocument b in documents)
            {
                if (b["isValid"].AsBoolean == true) RolesResultBox.Items.Add(b["name"].ToString());
            }
            RolesResultBox.SelectedItem = selecteditem;
        }

        public void GetSubjects(string selecteditem = null)

        {
            SubjectsResultBox.Items.Clear();

            List<string> Collections = new List<string>();
            var subjects = database.GetCollection<BsonDocument>("Subjects");

            var documents = subjects.Find(_ => true).ToList();

            foreach (BsonDocument b in documents)
            {
                SubjectsResultBox.Items.Add(b["name"].ToString());
            }
            SubjectsResultBox.SelectedItem = selecteditem;
        }

        public void GetMediaType(string selecteditem = null)

        {
            MediatypeResultsBox.Items.Clear();

            List<string> Collections = new List<string>();
            var mediatype = database.GetCollection<BsonDocument>("MediaTypes");
            var sessions = database.GetCollection<BsonDocument>("Sessions");

            var documents = mediatype.Find(_ => true).ToList();

            foreach (BsonDocument b in documents)
            {
                MediatypeResultsBox.Items.Add(b["name"].ToString() + "#" + b["type"].ToString());
            }
            MediatypeResultsBox.SelectedItem = selecteditem;
        }

        private void GetMedia()
        {
            if (DataBasResultsBox.SelectedItem != null)
            {
                List<DatabaseMediaInfo> ci = new List<DatabaseMediaInfo>();
                MediaResultBox.Items.Clear();
                ci = GetMediafromDB(DataBasResultsBox.SelectedItem.ToString(), Properties.Settings.Default.LastSessionId);

                foreach (DatabaseMediaInfo c in ci)
                {
                    if (!c.filename.Contains(".stream~"))
                    {
                        MediaResultBox.Items.Add(c.filename);
                    }
                }
            }
        }

        public List<DatabaseMediaInfo> GetMediafromDB(string db, string session)
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

                    c.filepath = url;
                    c.filename = selectedmedia["name"].ToString();
                    c.requiresauth = selectedmedia["requiresAuth"].ToString();

                    //Todo: solve references
                    c.subject = selectedmedia["subject_id"].ToString();
                    c.role = selectedmedia["role_id"].ToString();
                    c.mediatype = selectedmedia["mediatype_id"].ToString();
                    c.session = selectedmedia["session_id"].ToString();

                    paths.Add(c);
                }
            }
            return paths;
        }

        private void DataBaseResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataBasResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.Database = DataBasResultsBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                GetSessions();
                if (authlevel > 2)
                {
                    AddSession.Visibility = Visibility.Visible;
                    DeleteSession.Visibility = Visibility.Visible;
                    EditSession.Visibility = Visibility.Visible;
                }
            }
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();

                GetMedia();

                if (authlevel > 2)
                {
                    AddFiles.Visibility = Visibility.Visible;
                    DeleteFiles.Visibility = Visibility.Visible;

                    //Todo. enable when the meta field is ready
                    //  EditSubject.Visibility = Visibility.Visible;
                }
                if (authlevel > 3)
                {
                    AddRole.Visibility = Visibility.Visible;
                    DeleteRole.Visibility = Visibility.Visible;
                    AddSubjects.Visibility = Visibility.Visible;
                    DeleteSubject.Visibility = Visibility.Visible;
                    AddMediaType.Visibility = Visibility.Visible;
                    DeleteMediaType.Visibility = Visibility.Visible;
                }
            }
        }

        private void SubjectsResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SubjectsResultBox.SelectedItem != null)
            {
                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var subjects = database.GetCollection<BsonDocument>("Subjects");
                var media = database.GetCollection<BsonDocument>("Media");
                var builder = Builders<BsonDocument>.Filter;

                ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);
                var filter = builder.Eq("name", SubjectsResultBox.SelectedItem.ToString());
                var subjectsresult = subjects.Find(filter).ToList();

                var filtermedia = builder.Eq("name", MediaResultBox.SelectedItem.ToString()) & builder.Eq("session_id", sessionid);
                var mediadocuments = media.Find(filtermedia).ToList();

                if (mediadocuments.Count > 0)
                {
                    var update = Builders<BsonDocument>.Update.Set("subject_id", subjectsresult[0].GetValue(0).AsObjectId);
                    media.UpdateOne(filtermedia, update);
                }
            }
        }

        private void MediaResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MediaResultBox.SelectedItem != null)
            {
                GetSubjects();
                GetRoles();
                GetMediaType();

                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var roles = database.GetCollection<BsonDocument>("Roles");
                var subjects = database.GetCollection<BsonDocument>("Subjects");
                var mediatypes = database.GetCollection<BsonDocument>("MediaTypes");
                var media = database.GetCollection<BsonDocument>("Media");

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                var documents = sessions.Find(filter).ToList();
                ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);
                var filtermedia = builder.Eq("name", MediaResultBox.SelectedItem.ToString()) & builder.Eq("session_id", sessionid);
                var mediadoc = media.Find(filtermedia).Single();

                // BsonArray files = documents[0]["media"].AsBsonArray;

                var filter2 = builder.Eq("_id", mediadoc["role_id"]);
                var rolescollection = roles.Find(filter2).ToList();
                if (rolescollection.Count > 0)
                {
                    var role = rolescollection[0];
                    foreach (var Item in RolesResultBox.Items)
                    {
                        if (Item.ToString() == role["name"].ToString())
                        {
                            RolesResultBox.SelectedItem = Item;
                            break;
                        }
                    }
                }

                var filter3 = builder.Eq("_id", mediadoc["subject_id"]);
                var subjectcollection = subjects.Find(filter3).ToList();

                if (subjectcollection.Count > 0)
                {
                    var subject = subjectcollection[0];
                    foreach (var Item in SubjectsResultBox.Items)
                    {
                        if (Item.ToString() == subject["name"].ToString())
                        {
                            SubjectsResultBox.SelectedItem = Item;
                            break;
                        }
                    }
                }
                var filter4 = builder.Eq("_id", mediadoc["mediatype_id"]);
                var mediatypecollection = mediatypes.Find(filter4).ToList();
                if (mediatypecollection.Count > 0)
                {
                    var mediatype = mediatypecollection[0];
                    foreach (var Item in MediatypeResultsBox.Items)
                    {
                        if (Item.ToString() == (mediatype["name"] + "#" + mediatype["type"]).ToString())
                        {
                            MediatypeResultsBox.SelectedItem = Item;
                            break;
                        }
                    }
                }
            }
        }

        private void DeleteDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (authlevel > 3) mongo.DropDatabase(Properties.Settings.Default.Database);
                else MessageBox.Show("You are not authorized to delete this Database");

                GetDatabase();
                GetSessions();
                GetRoles();
                GetMedia();
            }
            catch
            {
                //    MessageBox.Show("Didnt find database");
            }
        }

        private void DeleteSession_Click(object sender, RoutedEventArgs e)
        {
            if (authlevel > 2)
            {
                if (CollectionResultsBox.SelectedItem != null)
                {
                    for (int i = 0; i < CollectionResultsBox.SelectedItems.Count; i++)
                    {
                        var builder = Builders<BsonDocument>.Filter;

                        var filter = builder.Eq("name", ((DatabaseSession)CollectionResultsBox.SelectedItem).Name);
                        var session = database.GetCollection<BsonDocument>("Sessions").Find(filter).Single();
                        ObjectId sessionid = session.GetValue(0).AsObjectId;

                        var filtersession = builder.Eq("session_id", sessionid);
                        database.GetCollection<BsonDocument>("Annotations").DeleteManyAsync(filtersession);
                        database.GetCollection<BsonDocument>("Media").DeleteManyAsync(filtersession);
                        database.GetCollection<BsonDocument>("Sessions").DeleteOne(filter);
                    }
                }
            }
            else { MessageBox.Show("You are not authorized to delete this Session"); }

            GetSessions();
        }

        private void DeleteSubject_Click(object sender, RoutedEventArgs e)
        {
            if (SubjectsResultBox.SelectedItem != null)
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", SubjectsResultBox.SelectedItem.ToString());
                var result = database.GetCollection<BsonDocument>("Subjects").DeleteOne(filter);

                GetSubjects();
            }
        }

        private void DeleteFiles_Click(object sender, RoutedEventArgs e)
        {
            if (MediaResultBox.SelectedItem != null)
            {
                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var media = database.GetCollection<BsonDocument>("Media");

                var builder = Builders<BsonDocument>.Filter;

                ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);
                var filtermedia = builder.Eq("name", MediaResultBox.SelectedItem.ToString()) & builder.Eq("session_id", sessionid);
                var mediadocuments = media.Find(filtermedia).ToList();

                if (mediadocuments.Count > 0)
                {
                    media.DeleteOne(filtermedia);
                }

                //For Stream files we also delete the stream~ without the user having to care for it.
                if (MediaResultBox.SelectedItem.ToString().EndsWith(".stream"))
                {
                    var filtermedia2 = builder.Eq("name", MediaResultBox.SelectedItem.ToString() + "~") & builder.Eq("session_id", sessionid);
                    var mediadocuments2 = media.Find(filtermedia2).ToList();
                    if (mediadocuments2.Count > 0)
                    {
                        media.DeleteOne(filtermedia2);
                    }
                }

                RolesResultBox.Items.Clear();
                SubjectsResultBox.Items.Clear();
                MediatypeResultsBox.Items.Clear();
                GetMedia();
            }
        }

        private void EditSubject_Click(object sender, RoutedEventArgs e)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", SubjectsResultBox.SelectedValue.ToString());
            var session = database.GetCollection<BsonDocument>("Subjects").Find(filter).ToList();

            if (session.Count > 0)
            {
                DatabaseSubjectWindow dbsw = new DatabaseSubjectWindow(session[0]["name"].ToString(), session[0]["gender"].ToString(), session[0]["age"].ToString(), session[0]["culture"].ToString());
                dbsw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dbsw.ShowDialog();

                if (dbsw.DialogResult == true)
                {
                    var updatelocation = Builders<BsonDocument>.Update.Set("gender", dbsw.Gender());
                    database.GetCollection<BsonDocument>("Subjects").UpdateOne(filter, updatelocation);

                    var updatelanguage = Builders<BsonDocument>.Update.Set("age", dbsw.Age());
                    database.GetCollection<BsonDocument>("Subjects").UpdateOne(filter, updatelanguage);

                    var updatedate = Builders<BsonDocument>.Update.Set("culture", dbsw.Culture());
                    database.GetCollection<BsonDocument>("Subjects").UpdateOne(filter, updatedate);

                    var updatename = Builders<BsonDocument>.Update.Set("name", dbsw.Name());
                    database.GetCollection<BsonDocument>("Subjects").UpdateOne(filter, updatename);
                }

                GetSubjects(dbsw.Name());
            }
        }

        private void AddSubject_Click(object sender, RoutedEventArgs e)
        {
            DatabaseSubjectWindow l = new DatabaseSubjectWindow();
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                //Todo: Add more
                BsonDocument subject = new BsonDocument {
                    {"name",  l.Name()},
                    {"gender",  l.Gender()},
                    {"age",  l.Age()},
                    {"culture",  l.Culture()},
                    {"education",  ""},
                    {"personality",  ""},
                };

                bool subjectalreadypresent = false;
                foreach (var item in SubjectsResultBox.Items)
                {
                    if (item.ToString() == l.Name())
                    {
                        subjectalreadypresent = true;
                    }
                }
                if (subjectalreadypresent == false)
                {
                    var collection = database.GetCollection<BsonDocument>("Subjects");
                    collection.InsertOne(subject);
                    GetSubjects(l.Name());
                }
                else MessageBox.Show("Subject already exists!");
            }
        }

        private void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (RolesResultBox.SelectedItem != null)
            {
                var collection = database.GetCollection<BsonDocument>("Roles");
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", RolesResultBox.SelectedItem.ToString());
                var update = Builders<BsonDocument>.Update.Set("isValid", false);
                collection.UpdateOne(filter, update);

                //var result = database.GetCollection<BsonDocument>("Roles").DeleteOne(filter);

                GetRoles();
            }
        }

        private void RolesResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RolesResultBox.SelectedItem != null)
            {
                lastrole = RolesResultBox.SelectedItem.ToString();

                var roles = database.GetCollection<BsonDocument>("Roles");
                var media = database.GetCollection<BsonDocument>("Media");
                var builder = Builders<BsonDocument>.Filter;

                ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);
                var filter = builder.Eq("name", RolesResultBox.SelectedItem.ToString());
                var rolesresult = roles.Find(filter).ToList();

                var filtermedia = builder.Eq("name", MediaResultBox.SelectedItem.ToString()) & builder.Eq("session_id", sessionid);
                var mediadocuments = media.Find(filtermedia).ToList();

                if (mediadocuments.Count > 0)
                {
                    var update = Builders<BsonDocument>.Update.Set("role_id", rolesresult[0].GetValue(0).AsObjectId);
                    media.UpdateOne(filtermedia, update);
                }
            }
        }

        private void MediatypeResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MediatypeResultsBox.SelectedItem != null)
            {
                var mediatypes = database.GetCollection<BsonDocument>("MediaTypes");
                var media = database.GetCollection<BsonDocument>("Media");
                var builder = Builders<BsonDocument>.Filter;

                ObjectId sessionid = GetObjectID(mongo.GetDatabase(Properties.Settings.Default.Database), "Sessions", "name", Properties.Settings.Default.LastSessionId);
                string[] split = MediatypeResultsBox.SelectedItem.ToString().Split('#');

                var filter2 = builder.Eq("name", split[0]) & builder.Eq("type", split[1]);
                var mediatyperesult = mediatypes.Find(filter2).Single();

                var filtermedia = builder.Eq("name", MediaResultBox.SelectedItem.ToString()) & builder.Eq("session_id", sessionid);

                var update = Builders<BsonDocument>.Update.Set("mediatype_id", mediatyperesult.GetValue(0).AsObjectId);
                media.UpdateOne(filtermedia, update);
            }
        }

        private void AddMediaType_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("New Mediatype", "Enter Media type and attribute", "", null, 2, "");
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                BsonDocument mediatype = new BsonDocument {
                    {"name",  l.Result()},
                    {"type",  l.Result2()}
                };

                bool subjectalreadypresent = false;
                foreach (var item in MediatypeResultsBox.Items)
                {
                    if (item.ToString() == l.Result() + "#" + l.Result2())
                    {
                        subjectalreadypresent = true;
                    }
                }
                if (subjectalreadypresent == false)
                {
                    var collection = database.GetCollection<BsonDocument>("MediaTypes");
                    collection.InsertOne(mediatype);
                    GetMediaType(l.Result() + "#" + l.Result2());
                }
                else MessageBox.Show("Role already exists!");
            }
        }

        private void DeleteMediaType_Click(object sender, RoutedEventArgs e)
        {
            if (MediatypeResultsBox.SelectedItem != null)
            {
                string[] split = MediatypeResultsBox.SelectedItem.ToString().Split('#');
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", split[0]) & builder.Eq("type", split[1]);
                var result = database.GetCollection<BsonDocument>("MediaTypes").DeleteOne(filter);

                GetMediaType();
            }
        }

        private void EditSession_Click(object sender, RoutedEventArgs e)
        {
            if (CollectionResultsBox.SelectedItem != null)
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name);
                var session = database.GetCollection<BsonDocument>("Sessions").Find(filter).ToList();

                if (session.Count > 0)
                {
                    DataBaseSessionWindow dbsw = new DataBaseSessionWindow(session[0]["name"].ToString(), session[0]["language"].ToString(), session[0]["location"].ToString(), session[0]["date"].AsDateTime);
                    dbsw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    dbsw.ShowDialog();

                    if (dbsw.DialogResult == true)
                    {
                        var updatelocation = Builders<BsonDocument>.Update.Set("location", dbsw.Location());
                        database.GetCollection<BsonDocument>("Sessions").UpdateOne(filter, updatelocation);

                        var updatelanguage = Builders<BsonDocument>.Update.Set("language", dbsw.Language());
                        database.GetCollection<BsonDocument>("Sessions").UpdateOne(filter, updatelanguage);

                        var updatedate = Builders<BsonDocument>.Update.Set("date", dbsw.Date());
                        database.GetCollection<BsonDocument>("Sessions").UpdateOne(filter, updatedate);

                        var updatename = Builders<BsonDocument>.Update.Set("name", dbsw.Name());
                        database.GetCollection<BsonDocument>("Sessions").UpdateOne(filter, updatename);
                    }
                }
                GetSessions();
            }
        }

        public string FetchDBRef(IMongoDatabase database, string collection, string attribute, ObjectId reference)
        {
            string output = "";
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq("_id", reference);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0)
            {
                output = result[0][attribute].ToString();
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

        private void Autologin_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Autologin = true;
            Properties.Settings.Default.Save();
        }

        private void Autologin_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Autologin = false;
            Properties.Settings.Default.Save();
        }

        private void db_pass_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Autologin.IsChecked = false;
            Autologin.IsEnabled = false;
        }

        private void db_login_TextChanged(object sender, TextChangedEventArgs e)
        {
            Autologin.IsChecked = false;
            Autologin.IsEnabled = false;
        }
    }
}
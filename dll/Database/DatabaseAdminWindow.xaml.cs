using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
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
        }

        private void AddFiles_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox inputBox2 = new LabelInputBox("MongoDB Connection", "Enter sftp, httpGet or httpPost", Properties.Settings.Default.DataServerConnectionType);
            inputBox2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            inputBox2.ShowDialog();
            inputBox2.Close();

            if (inputBox2.DialogResult == true)
            {
                Properties.Settings.Default.DataServerConnectionType = inputBox2.Result();
                Properties.Settings.Default.Save();

                if (Properties.Settings.Default.DataServerConnectionType == "sftp")
                {
                    LabelInputBox inputBox3 = new LabelInputBox("Storage Data", "Files, seperated by ; server & folder ", Properties.Settings.Default.DataServer, null, 3, Properties.Settings.Default.DataServerFolder, Properties.Settings.Default.Filenames);
                    inputBox3.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    inputBox3.ShowDialog();
                    inputBox3.Close();

                    if (inputBox3.DialogResult == true)
                    {
                        Properties.Settings.Default.Filenames = inputBox3.Result3();
                        Properties.Settings.Default.DataServer = inputBox3.Result(); ;
                        Properties.Settings.Default.DataServerFolder = inputBox3.Result2(); ;
                        Properties.Settings.Default.Save();

                        string[] fnames = Properties.Settings.Default.Filenames.Split(';');
                        AddMediatoDatabase(Properties.Settings.Default.DataServerConnectionType, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.DataServer, Properties.Settings.Default.DataServerFolder, fnames);
                    }
                }
                else if (Properties.Settings.Default.DataServerConnectionType == "httpPost")
                {
                    LabelInputBox inputBox3 = new LabelInputBox("Storage Data", "Files, seperated by ; Url ", Properties.Settings.Default.DataServer, null, 2, Properties.Settings.Default.Filenames);
                    inputBox3.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    inputBox3.ShowDialog();
                    inputBox3.Close();

                    if (inputBox3.DialogResult == true)
                    {
                        Properties.Settings.Default.Filenames = inputBox3.Result();
                        Properties.Settings.Default.DataServer = inputBox3.Result2(); ;
                        Properties.Settings.Default.Save();

                        string[] fnames = Properties.Settings.Default.Filenames.Split(';');
                        AddMediatoDatabase(Properties.Settings.Default.DataServerConnectionType, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.DataServer, Properties.Settings.Default.DataServerFolder, fnames);
                    }
                }
                else if (Properties.Settings.Default.DataServerConnectionType == "httpGet")
                {
                    LabelInputBox inputBox3 = new LabelInputBox("Storage Data", "Files, seperated by ;", Properties.Settings.Default.Filenames, null, 1);
                    inputBox3.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    inputBox3.ShowDialog();
                    inputBox3.Close();

                    if (inputBox3.DialogResult == true)
                    {
                        Properties.Settings.Default.Filenames = inputBox3.Result();
                        Properties.Settings.Default.Save();

                        string[] fnames = Properties.Settings.Default.Filenames.Split(';');
                        AddMediatoDatabase(Properties.Settings.Default.DataServerConnectionType, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, "", "", fnames);
                    }
                }
            }
        }

        public void AddMediatoDatabase(string connection, string db, string session, string ip, string folder, string[] filenames)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);

            BsonArray files = new BsonArray();
            if (CollectionResultsBox.SelectedItem != null)
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", session);
                var sessions = database.GetCollection<BsonDocument>("Sessions").Find(filter).ToList();

                if (sessions.Count > 0)
                {
                    string id = sessions[0]["name"].ToString();
                    files = sessions[0]["media"].AsBsonArray;
                    bool requiresauth = false;

                    if (connection.Contains("sftp") || connection.Contains("httpPost")) requiresauth = true;

                    for (int i = 0; i < filenames.Length; i++)
                    {
                        string filename = filenames[i];
                        if (isURL(filenames[i]))
                        {
                            filename = filenames[i].Substring(filenames[i].LastIndexOf("/") + 1, (filenames[i].Length - filenames[i].LastIndexOf("/") - 1));
                        }

                        files.Add(new BsonDocument
                            {
                                 { "connection", connection },
                                 { "ip", ip },
                                 { "folder", folder },
                                 { "fileName", filename },
                                 { "filePath", filenames[i] },
                                 { "requiresAuth", requiresauth },
                                 { "mediatype_id", "" },
                                 { "role_id", "" },
                                 { "subject_id", "" }
                            });
                    }
                }

                var update = Builders<BsonDocument>.Update.Set("media", files);
                var result = database.GetCollection<BsonDocument>("Sessions").UpdateOne(filter, update);

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
                };

                bool subjectalreadypresent = false;
                foreach (var item in RolesResultBox.Items)
                {
                    if (item.ToString() == lastrole)
                    {
                        subjectalreadypresent = true;
                    }
                }
                if (subjectalreadypresent == false)
                {
                    var collection = database.GetCollection<BsonDocument>("Roles");
                    collection.InsertOne(role);
                    GetRoles(l.Result());
                }
                else MessageBox.Show("Role already exists!");

                //var builder1 = Builders<BsonDocument>.Filter;
                //var filter1 = builder1.Eq("name", lastrole);
                //var documents1 = database.GetCollection<BsonDocument>("Roles").Find(filter1).ToList();
                //ObjectId roleid = documents1[0].GetValue(0).AsObjectId;

                //IMongoCollection<BsonDocument> sessions = database.GetCollection<BsonDocument>("Sessions");

                //var builder = Builders<BsonDocument>.Filter;
                //var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                //var documents = sessions.Find(filter).ToList();

                ////Should always be one entry, if not use first..
                //var participants = documents[0]["participants"].AsBsonArray;
                //BsonArray media = new BsonArray();
                //BsonArray annotations = new BsonArray();
                //participants.Add(new BsonDocument { { "role_id", roleid }, { "subject_id", "" }, {"media", media }, {"annotations", annotations } }) ;

                //var update = Builders<BsonDocument>.Update.Set("participants", participants);
                //var result = sessions.UpdateOne(filter, update);
            }
        }

        private void AddSession_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("New Session", "Enter Session Name", Properties.Settings.Default.LastSessionId);
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                Properties.Settings.Default.LastSessionId = l.Result();
                Properties.Settings.Default.Save();

                BsonElement name = new BsonElement("name", l.Result());
                BsonElement location = new BsonElement("location", "");
                BsonElement language = new BsonElement("language", "");
                BsonElement date = new BsonElement("date", new BsonDateTime(0));
                BsonDocument document = new BsonDocument();

                BsonArray media = new BsonArray();
                BsonArray annotations = new BsonArray();

                // participants.Add(new BsonDocument { { "role_id", "" }, { "subject_id", "" }, {"media", media }, {"annotations", annotations } }) ;

                document.Add(name);
                document.Add(location);
                document.Add(language);
                document.Add(date);
                document.Add("media", media);
                document.Add("annotations", annotations);

                bool sessionnamealreadypresent = false;
                foreach (var item in CollectionResultsBox.Items)
                {
                    if (item.ToString() == l.Result())
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

        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.MongoDBIP = this.db_server.Text;
            Properties.Settings.Default.MongoDBUser = this.db_login.Text;
            Properties.Settings.Default.MongoDBPass = this.db_pass.Password;
            Properties.Settings.Default.Save();

            connectionstring = "mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP;

            try
            {
                mongo = new MongoClient(connectionstring);

                authlevel = checkAuth(this.db_login.Text, "admin");

                if (authlevel > 0) GetDatabase();
                else MessageBox.Show("You have no aceess rights to load the database list");
            }
            catch { MessageBox.Show("Could not connect to Database!"); }

            if (authlevel > 3)
            {
                DeleteDB.Visibility = Visibility.Visible;
                AddDB.Visibility = Visibility.Visible;
            }
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
            var collectionsdb = database.GetCollection<BsonDocument>("Sessions");
            var documents = collectionsdb.Find(_ => true).ToList();

            if (CollectionResultsBox.Items != null) CollectionResultsBox.Items.Clear();
            foreach (var c in documents)
            {
                CollectionResultsBox.Items.Add(c.GetElement(1).Value.ToString());
            }
        }

        public void GetRoles(string selecteditem = null)

        {
            RolesResultBox.Items.Clear();

            List<string> Collections = new List<string>();
            var roles = database.GetCollection<BsonDocument>("Roles");

            var documents = roles.Find(_ => true).ToList();

            foreach (BsonDocument b in documents)
            {
                RolesResultBox.Items.Add(b["name"].ToString());
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
                ci = GetMediafromDB(DataBasResultsBox.SelectedItem.ToString(), CollectionResultsBox.SelectedItem.ToString());

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
            BsonElement value;
            List<DatabaseMediaInfo> paths = new List<DatabaseMediaInfo>();
            var colllection = database.GetCollection<BsonDocument>("Sessions");

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", session);
            var result = colllection.Find(filter);

            var documents = colllection.Find(filter).ToList();

            foreach (var document in documents)
            {
                string id;
                if (document.TryGetElement("name", out value)) id = document["name"].ToString();
                if (document.TryGetElement("media", out value))
                {
                    BsonArray files = document["media"].AsBsonArray;

                    for (int i = 0; i < files.Count; i++)
                    {
                        DatabaseMediaInfo c = new DatabaseMediaInfo();
                        c.connection = files[i]["connection"].ToString();
                        c.ip = files[i]["ip"].ToString();
                        c.folder = files[i]["folder"].ToString();
                        c.filepath = files[i]["filePath"].ToString();
                        c.filename = files[i]["fileName"].ToString();
                        c.requiresauth = files[i]["requiresAuth"].ToString();

                        //Todo: solve references
                        c.subject = files[i]["subject_id"].ToString();
                        c.role = files[i]["role_id"].ToString();
                        c.mediatype = files[i]["mediatype_id"].ToString();

                        paths.Add(c);
                    }
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
                }
            }
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = CollectionResultsBox.SelectedItem.ToString();
                Properties.Settings.Default.Save();

                GetMedia();

                if (authlevel > 2)
                {
                    AddFiles.Visibility = Visibility.Visible;
                    DeleteFiles.Visibility = Visibility.Visible;
                   
                    //Todo. enable when the meta field is ready
                    //  EditSubject.Visibility = Visibility.Visible;
                }
                if(authlevel > 3)
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
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                var documents = sessions.Find(filter).ToList();

                var filter2 = builder.Eq("name", SubjectsResultBox.SelectedItem.ToString());
                var subs = subjects.Find(filter2).ToList();

                BsonArray files = documents[0]["media"].AsBsonArray;

                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i]["fileName"].ToString() == MediaResultBox.SelectedItem.ToString())
                    {
                        files[i]["subject_id"] = subs[0].GetValue(0).AsObjectId;
                        break;
                    }
                }

                var update = Builders<BsonDocument>.Update.Set("media", files);
                sessions.UpdateOne(filter, update);
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

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                var documents = sessions.Find(filter).ToList();
                BsonArray files = documents[0]["media"].AsBsonArray;

                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i]["fileName"].ToString() == MediaResultBox.SelectedItem.ToString())
                    {
                        var filter2 = builder.Eq("_id", files[i]["role_id"]);
                        var rolescollection = roles.Find(filter2).ToList();
                        if (rolescollection.Count > 0)
                        {
                            var role = rolescollection[0];
                            foreach (var Item in RolesResultBox.Items)
                            {
                                if (Item.ToString() == role["name"].ToString())
                                {
                                    RolesResultBox.SelectedItem = Item;
                                }
                            }
                        }

                        var filter3 = builder.Eq("_id", files[i]["subject_id"]);
                        var subjectcollection = subjects.Find(filter3).ToList();

                        if (subjectcollection.Count > 0)
                        {
                            var subject = subjectcollection[0];
                            foreach (var Item in SubjectsResultBox.Items)
                            {
                                if (Item.ToString() == subject["name"].ToString())
                                {
                                    SubjectsResultBox.SelectedItem = Item;
                                }
                            }

                            var filter4 = builder.Eq("_id", files[i]["mediatype_id"]);
                            var mediatypecollection = mediatypes.Find(filter4).ToList();
                            if (mediatypecollection.Count > 0)
                            {
                                var mediatype = mediatypecollection[0];
                                foreach (var Item in MediatypeResultsBox.Items)
                                {
                                    if (Item.ToString() == (mediatype["name"] + "#" + mediatype["type"]).ToString())
                                    {
                                        MediatypeResultsBox.SelectedItem = Item;
                                    }
                                }
                            }
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
                        var filter = builder.Eq("name", CollectionResultsBox.SelectedItem.ToString());
                        var result = database.GetCollection<BsonDocument>("Sessions").DeleteOne(filter);
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

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                var documents = sessions.Find(filter).ToList();
                BsonArray files = documents[0]["media"].AsBsonArray;

                for (int j = 0; j < files.Count; j++)
                {
                    if (files[j]["fileName"].ToString() == MediaResultBox.SelectedItem.ToString())
                    {
                        files.RemoveAt(j);
                        break;
                    }
                }

                var update = Builders<BsonDocument>.Update.Set("media", files);
                sessions.UpdateOne(filter, update);

                RolesResultBox.Items.Clear();
                SubjectsResultBox.Items.Clear();
                MediatypeResultsBox.Items.Clear();

                GetMedia();
            }
        }

        private void EditSubject_Click(object sender, RoutedEventArgs e)
        {
            DatabaseEditSubjectWindow dbesw = new DatabaseEditSubjectWindow(lastrole);
            dbesw.Show();
        }

        private void AddSubject_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("New Subject", "Enter Subject Name", "");
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                //Todo: Add more
                BsonDocument subject = new BsonDocument {
                    {"name",  l.Result()},
                    {"gender",  ""},
                    {"age",  ""},
                    {"culture",  ""},
                    {"education",  ""},
                    {"personality",  ""},
                };

                bool subjectalreadypresent = false;
                foreach (var item in SubjectsResultBox.Items)
                {
                    if (item.ToString() == l.Result())
                    {
                        subjectalreadypresent = true;
                    }
                }
                if (subjectalreadypresent == false)
                {
                    var collection = database.GetCollection<BsonDocument>("Subjects");
                    collection.InsertOne(subject);
                    GetSubjects(l.Result());
                }
                else MessageBox.Show("Subject already exists!");
            }
        }

        private void DeleteRole_Click(object sender, RoutedEventArgs e)
        {
            if (RolesResultBox.SelectedItem != null)
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", RolesResultBox.SelectedItem.ToString());
                var result = database.GetCollection<BsonDocument>("Roles").DeleteOne(filter);

                GetRoles();
            }
        }

        private void RolesResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (RolesResultBox.SelectedItem != null)
            {
                lastrole = RolesResultBox.SelectedItem.ToString();

                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var roles = database.GetCollection<BsonDocument>("Roles");
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                var documents = sessions.Find(filter).ToList();

                var filter2 = builder.Eq("name", RolesResultBox.SelectedItem.ToString());
                var rolesresult = roles.Find(filter2).ToList();

                BsonArray files = documents[0]["media"].AsBsonArray;

                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i]["fileName"].ToString() == MediaResultBox.SelectedItem.ToString())
                    {
                        files[i]["role_id"] = rolesresult[0].GetValue(0).AsObjectId;
                        break;
                    }
                }

                var update = Builders<BsonDocument>.Update.Set("media", files);
                sessions.UpdateOne(filter, update);
            }
        }

        private void MediatypeResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MediatypeResultsBox.SelectedItem != null)
            {
                var sessions = database.GetCollection<BsonDocument>("Sessions");
                var subjects = database.GetCollection<BsonDocument>("MediaTypes");
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
                var documents = sessions.Find(filter).ToList();

                string[] split = MediatypeResultsBox.SelectedItem.ToString().Split('#');
                var filter2 = builder.Eq("name", split[0]) & builder.Eq("type", split[1]);
                var subs = subjects.Find(filter2).ToList();

                BsonArray files = documents[0]["media"].AsBsonArray;

                for (int i = 0; i < files.Count; i++)
                {
                    if (files[i]["fileName"].ToString() == MediaResultBox.SelectedItem.ToString())
                    {
                        files[i]["mediatype_id"] = subs[0].GetValue(0).AsObjectId;
                        break;
                    }
                }

                var update = Builders<BsonDocument>.Update.Set("media", files);
                sessions.UpdateOne(filter, update);
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
    }
}
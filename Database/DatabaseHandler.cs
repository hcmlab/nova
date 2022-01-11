using MongoDB.Bson;
using MongoDB.Driver;
using ssi.Types.Polygon;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace ssi
{
    internal class DatabaseHandler
    {
        private static string GARBAGELABEL = "GARBAGE";
        private static Color GARBAGECOLOR = Colors.Black;

        private static MongoClient client = null;
        private static string clientAddress = null;
        private static IMongoDatabase database = null;
        private static string databaseName = null;
        private static string sessionName = null;
        private static string serverName = null;

        private static List<DatabaseStream> streams = new List<DatabaseStream>();
        public static List<DatabaseStream> Streams { get { return streams; } }

        private static List<DatabaseRole> roles = new List<DatabaseRole>();
        public static List<DatabaseRole> Roles { get { return roles; } }

        private static List<DatabaseSession> sessions = new List<DatabaseSession>();
        public static List<DatabaseSession> Sessions { get { return sessions; } }

        private static List<DatabaseScheme> schemes = new List<DatabaseScheme>();
        public static List<DatabaseScheme> Schemes { get { return schemes; } }

        private static List<DatabaseAnnotator> annotators = new List<DatabaseAnnotator>();
        public static List<DatabaseAnnotator> Annotators { get { return annotators; } }

        private static List<DatabaseUser> users = new List<DatabaseUser>();
        private static bool throwOnInvalidBytes;

        private static List<DatabaseBounty> bounties = new List<DatabaseBounty>();
        public static List<DatabaseBounty> Bounties { get { return bounties; } }


        public static List<DatabaseUser> Users { get { return users; } }

        #region CONNECT AND AUTH

        public static string ServerInfo
        {
            get { return "Server [" + (IsConnected ? Properties.Settings.Default.MongoDBUser + "@" + Properties.Settings.Default.DatabaseAddress : "not connected") + "]"; }
        }

        public static string DatabaseInfo
        {
            get { return "Database [" + (IsDatabase ? DatabaseName : "none") + "]"; }
        }

        public static string SessionInfo
        {
            get { return "Session [" + (IsSession ? sessionName.Replace('_', '-') : "none") + "]"; }
        }

        public static bool Connect()
        {
            return Connect(Properties.Settings.Default.MongoDBUser, Properties.Settings.Default.MongoDBPass, Properties.Settings.Default.DatabaseAddress);
        }

        public static  bool Connect(string user, string password, string address)
        {
            client = null;
            databaseName = null;
            sessionName = null;
            database = null;

            Properties.Settings.Default.MongoDBUser = user;
            Properties.Settings.Default.MongoDBPass = password;
            Properties.Settings.Default.DatabaseAddress = address;
            Properties.Settings.Default.Save();

            clientAddress = "mongodb://" + user + ":" + MainHandler.Decode(password) + "@" + address;

            client = Client;

            int count = 0;
            while (client.Cluster.Description.State.ToString() == "Disconnected")
            {
                Thread.Sleep(100);
                if (count++ >= 25)
                {
                    client.Cluster.Dispose();
                    client = null;
                    return false;
                }
            }

 
            return true;
        }

        public static bool IsDatabase
        {
            get { return database != null; }
        }

        public static bool IsSession
        {
            get { return sessionName != null; }
        }

        public static bool IsConnected
        {
            get { return client != null; }
        }

        public static MongoClient Client
        {
            get
            {
                if (client == null)
                {
                    MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(clientAddress));
                    settings.ReadEncoding = new UTF8Encoding(false, throwOnInvalidBytes);
                    client = new MongoClient(settings);
                }
                return client;
            }
        }

        public static IMongoDatabase Database
        {
            get { return database; }
        }

        public static string DatabaseName
        {
            get { return databaseName; }
        }

        public static string SessionName
        {
            get { return sessionName; }
        }

        public static bool ChangeSession(string name)
        {
            if (!IsConnected || !IsDatabase)
            {
                return false;
            }

            if (name == null)
            {
                sessionName = null;
            }
            else
            {
                if (!SessionExists(name))
                {
                    return false;
                }
                sessionName = name;
                Properties.Settings.Default.LastSessionId = name;
                Properties.Settings.Default.Save();
            }

            return true;
        }

        public static bool ChangeDatabase(string name)
        {
            if (!IsConnected || name == null || name == "")
            {
                databaseName = null;
                database = null;
                sessionName = null;
                return false;
            }
            else if (!DatabaseExists(name))
            {
                return false;
            }
            else if (databaseName != name)
            {
                Properties.Settings.Default.DatabaseName = name;
                Properties.Settings.Default.LastSessionId = null;
                Properties.Settings.Default.Save();
                serverName = Properties.Settings.Default.DataServer;
                databaseName = name;
                sessionName = null;
                database = Client.GetDatabase(databaseName);

                UpdateDatabaseLocalLists();
            }

            return true;
        }

        static public void UpdateDatabaseLocalLists()
        {
            //Fill the lists each time we change the database, so references are solved only once.
            streams = GetStreams();
            roles = GetRoles();
            sessions = GetSessions();
            schemes = GetSchemes();
            annotators = GetAnnotators();
        }

        static public DatabaseAuthentication CheckAuthentication()
        {
            return (DatabaseAuthentication)CheckAuthentication(Properties.Settings.Default.MongoDBUser, Properties.Settings.Default.DatabaseName);
        }

        static public int CheckAuthentication(string database)
        {
            return CheckAuthentication(Properties.Settings.Default.MongoDBUser, database);
        }

        static public int CheckAuthentication(string user, string db)
        {
            if (!IsConnected)
            {
                return 0;
            }

            //4 = root
            //3 = admin
            //2 = write
            //1 = read
            //0 = not authorized

            int auth = 0;
            try
            {
                var adminDB = client.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", user);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if ((roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && roles[i]["db"] == db || (roles[i]["role"].ToString() == "userAdminAnyDatabase" || roles[i]["role"].ToString() == "dbAdminAnyDatabase")) && auth <= 4) { auth = 4; }
                    else if ((roles[i]["role"].ToString() == "dbAdmin" && roles[i]["db"] == db) && auth <= 3) { auth = 3; }
                    else if ((roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && roles[i]["db"] == db || roles[i]["role"].ToString() == "read" && roles[i]["db"] == db) && auth <= 2) { auth = 2; }
                    else if ((roles[i]["role"].ToString() == "readAnyDatabase") && auth <= 1) { auth = 1; }
                }
            }
            catch
            { }

            return auth;
        }

        #endregion CONNECT AND AUTH

        #region GETTER

        public static bool DatabaseExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            List<string> databases = GetDatabasesAll();
            return databases.Any(s => name.Equals(s));
        }

        public static List<string> GetDatabases(DatabaseAuthentication level = DatabaseAuthentication.READWRITE)
        {
            List<string> items = new List<string>();

            if (IsConnected)
            {
                var databases = client.ListDatabasesAsync().Result.ToListAsync().Result;
                foreach (var c in databases)
                {
                    string db = c.GetElement(0).Value.ToString();
                    if (c.GetElement(0).Value.ToString() != "admin" && c.GetElement(0).Value.ToString() != "local" && c.GetElement(0).Value.ToString() != "config" && CheckAuthentication(db) >= (int)level)
                    {
                        items.Add(db);
                    }
                }
                items.Sort();
            }

            return items;
        }

        public static List<string> GetDatabasesAll()
        {
            List<string> items = new List<string>();

            if (IsConnected)
            {
                var databases = client.ListDatabasesAsync().Result.ToListAsync().Result;
                foreach (var c in databases)
                {
                    string db = c.GetElement(0).Value.ToString();
                    items.Add(db);
                }
                items.Sort();
            }

            return items;
        }

        public static bool GetObjectID(ref ObjectId id, string collection, string name)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var result = database.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                id = result[0].GetValue(0).AsObjectId;
                return true;
            }

            return false;
        }

        public static bool GetObjectID(ref ObjectId id, IMongoDatabase db, string collection, string name)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var result = db.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                id = result[0].GetValue(0).AsObjectId;
                return true;
            }

            return false;
        }

        public static bool GetObjectName(ref string name, string collection, ObjectId id)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", id);
            var result = database.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                name = result[0]["name"].ToString();
                return true;
            }

            return false;
        }

        public static bool GetObjectName(ref string name, IMongoDatabase db,  string collection, ObjectId id)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", id);
            var result = db.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                name = result[0]["name"].ToString();
                return true;
            }

            return false;
        }

        public static bool GetObjectField(ref string name, string collection, ObjectId id, string field)
        {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", id);
            var result = database.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                name = result[0][field].ToString();
                return true;
            }

            return false;
        }

        public static bool SessionExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            return Sessions.Exists(item => item.Name == name);
        }

        public static bool SchemeExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            return Schemes.Exists(item => item.Name == name);
        }

        public static bool AnnotatorExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            List<DatabaseAnnotator> annotators = GetAnnotators();
            return annotators.Any(s => name.Equals(s.Name));
        }

        public static bool UserExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            List<string> users = GetUsers();
            return users.Any(s => name.Equals(s));
        }

        public static bool RoleExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            return Roles.Exists(item => item.Name == name);
        }

        public static bool StreamExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            return Streams.Exists(item => item.Name == name);
        }

        public static bool AnnotationExists(ObjectId annotatorId, ObjectId sessionId, ObjectId roleId, ObjectId schemeId)
        {
            if (!IsConnected)
            {
                return false;
            }

            var builder = Builders<BsonDocument>.Filter;

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

            var filterAnnotation = builder.Eq("role_id", roleId) & builder.Eq("scheme_id", schemeId) & builder.Eq("annotator_id", annotatorId) & builder.Eq("session_id", sessionId);
            List<BsonDocument> annotationDocs = annotations.Find(filterAnnotation).ToList();

            if (annotationDocs.Count == 0)
            {
                return false;
            }

            return true;
        }

        public static bool AnnotationExists(string annotator, string session, string role, string scheme)
        {
            if (!IsConnected)
            {
                return false;
            }

            ObjectId annotatorId = new ObjectId();
            if (!GetObjectID(ref annotatorId, DatabaseDefinitionCollections.Annotators, annotator))
            {
                return false;
            }

            ObjectId sessionId = new ObjectId();
            if (!GetObjectID(ref sessionId, DatabaseDefinitionCollections.Sessions, session))
            {
                return false;
            }

            ObjectId roleId = new ObjectId();
            if (!GetObjectID(ref roleId, DatabaseDefinitionCollections.Roles, role))
            {
                return false;
            }

            ObjectId schemeId = new ObjectId();
            if (!GetObjectID(ref schemeId, DatabaseDefinitionCollections.Schemes, scheme))
            {
                return false;
            }

            return (AnnotationExists(annotatorId, sessionId, roleId, schemeId));
        }

        public static List<string> GetUsers()
        {
            List<string> items = new List<string>();

            if (IsConnected)
            {
                try
                {
                    MongoClient mongo = Client;
                    IMongoDatabase database = mongo.GetDatabase("admin");
                    IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("system.users");
                    var documents = collection.Find(_ => true).ToList();
                    foreach (var document in documents)
                    {
                        items.Add(document["user"].ToString());
                    }
                    items.Sort();
                }
                catch
                {
                    MessageBox.Show("Not authorized on admin database!");
                }
            }

            return items;
        }

        private static List<string> GetCollectionField(string collectionName, string field, bool onlyValid = true)
        {
            List<string> items = new List<string>();

            if (IsConnected && IsDatabase)
            {
                IMongoCollection<BsonDocument> collection = Database.GetCollection<BsonDocument>(collectionName);
                List<BsonDocument> documents = collection.Find(_ => true).ToList();
                foreach (BsonDocument document in documents)
                {
                    if (onlyValid)
                    {
                        if (!document.Contains("isValid") || document["isValid"].AsBoolean == true)
                        {
                            items.Add(document[field].ToString());
                        }
                    }
                    else
                    {
                        items.Add(document[field].ToString());
                    }
                }
                items.Sort();
            }

            return items;
        }

        public static List<BsonDocument> GetCollection(string collectionName, bool onlyValid = true, FilterDefinition<BsonDocument> filter = null)
        {
            List<BsonDocument> items = new List<BsonDocument>();

            if (IsConnected && IsDatabase)
            {
                IMongoCollection<BsonDocument> collection = Database.GetCollection<BsonDocument>(collectionName);
                List<BsonDocument> documents = null;
                if (filter == null)
                {
                    documents = collection.Find(_ => true).ToList();
                }
                else
                {
                    documents = collection.Find(filter).ToList();
                }

                foreach (BsonDocument document in documents)
                {
                    if (onlyValid)
                    {
                        if (!document.Contains("isValid") || document["isValid"].AsBoolean == true)
                        {
                            items.Add(document);
                        }
                    }
                    else
                    {
                        items.Add(document);
                    }
                }
                items.Sort();
            }

            return items;
        }

        public static string SelectCollectionField(string title, string collection, string field)
        {
            string item = null;

            if (IsConnected && IsDatabase)
            {
                List<string> items = GetCollectionField(collection, field);

                DatabaseSelectionWindow dbw = new DatabaseSelectionWindow(items, title);
                dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
                dbw.ShowDialog();

                if (dbw.DialogResult == true)
                {
                    if (dbw.Result() == null) return null;
                    item = dbw.Result().ToString();
                }
            }

            return item;
        }

        public static string SelectRole()
        {
            return SelectCollectionField("Select role", DatabaseDefinitionCollections.Roles, "name");
        }

        public static string SelectScheme()
        {
            return SelectCollectionField("Select scheme", DatabaseDefinitionCollections.Schemes, "name");
        }

        public static string SelectAnnotator()
        {
            return SelectCollectionField("Select annotator", DatabaseDefinitionCollections.Annotators, "name");
        }

        public static string SelectStreams()
        {
            return SelectCollectionField("Select stream type", DatabaseDefinitionCollections.Streams, "name");
        }

        #endregion GETTER

        #region SETTER

        public static bool GetDBMeta(ref DatabaseDBMeta meta)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!DatabaseExists(meta.Name))
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", meta.Name);
                var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Meta).Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    BsonElement value;
                    meta.Description = "";
                    if (document.TryGetElement("description", out value))
                    {
                        meta.Description = document["description"].ToString();
                    }
                    meta.Server = "";
                    if (document.TryGetElement("server", out value))
                    {
                        meta.Server = document["server"].ToString();
                    }
                    meta.ServerAuth = false;
                    if (document.TryGetElement("serverAuth", out value))
                    {
                        meta.ServerAuth = bool.Parse(document["serverAuth"].ToString());
                    }

                    if (document.TryGetElement("urlFormat", out value))
                    {
                        UrlFormat format = UrlFormat.GENERAL;
                        Enum.TryParse<UrlFormat>(document["urlFormat"].AsString, out format);
                        meta.UrlFormat = format;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AddOrUpdateDBMeta(DatabaseDBMeta meta)
        {
            if (meta.Name == "")
            {
                return false;
            }

            BsonDocument document = new BsonDocument {
                {"name", meta.Name},
                {"description", meta.Description == null ? "" : meta.Description},
                {"server", meta.Server == null ? "" : meta.Server},
                {"serverAuth", meta.ServerAuth.ToString()},
                {"urlFormat", meta.UrlFormat.ToString()}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", meta.Name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Meta).ReplaceOne(filter, document, updateOptions);

            return true;
        }

        public static bool UpdateDBMeta(string name, DatabaseDBMeta meta)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!DatabaseExists(name))
            {
                return false;
            }

            if (name != meta.Name)
            {
                return false;
            }

            return AddOrUpdateDBMeta(meta);
        }

        public static bool AddDB(DatabaseDBMeta meta)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (DatabaseExists(meta.Name))
            {
                return false;
            }

            database = client.GetDatabase(meta.Name);
            databaseName = meta.Name;

            database.CreateCollection(DatabaseDefinitionCollections.Meta);
            database.CreateCollection(DatabaseDefinitionCollections.Annotations);
            database.CreateCollection(DatabaseDefinitionCollections.AnnotationData);
            database.CreateCollection(DatabaseDefinitionCollections.Annotators);
            database.CreateCollection(DatabaseDefinitionCollections.Roles);
            database.CreateCollection(DatabaseDefinitionCollections.Schemes);
            database.CreateCollection(DatabaseDefinitionCollections.Sessions);
            database.CreateCollection(DatabaseDefinitionCollections.Streams);
            database.CreateCollection(DatabaseDefinitionCollections.Subjects);
            database.CreateCollection(DatabaseDefinitionCollections.Bounties);

            AddOrUpdateDBMeta(meta);

            return true;
        }

        public static bool DeleteDB(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "" || !DatabaseExists(name))
            {
                return false;
            }

            DatabaseAuthentication authLevel = CheckAuthentication();
            if (authLevel <= DatabaseAuthentication.DBADMIN)
            {
                return false;
            }

            if (name == databaseName)
            {
                databaseName = null;
                database = null;
            }

            Client.DropDatabase(name);

            return true;
        }

        public static bool AddUser(DatabaseUser user, bool isAdmin)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (user.Name == "")
            {
                return false;
            }

            if (UserExists(user.Name))
            {
                return false;
            }

            if (user.Password == null || user.Password == "")
            {
                return false;
            }

            var adminDatabase = Client.GetDatabase("admin");
            BsonDocument createUser;
            if (isAdmin)
            {
                createUser = new BsonDocument {
                    { "createUser", user.Name },
                    { "pwd", user.Password },
                    { "roles", new BsonArray {
                        new BsonDocument { { "role", "readAnyDatabase" }, { "db", "admin" } },
                        new BsonDocument { { "role", "readWrite" }, { "db", "admin" } },
                        new BsonDocument { { "role", "userAdminAnyDatabase" }, { "db", "admin" } },
                        new BsonDocument { { "role", "root" }, { "db", "admin" } },
                        new BsonDocument { { "role", "changeOwnPasswordCustomDataRole" }, { "db", "admin" } },
                    } } };
            }
            else
            {
                createUser = new BsonDocument {
                    { "createUser", user.Name },
                    { "pwd", user.Password },
                    { "roles", new BsonArray {
                        new BsonDocument { { "role", "readAnyDatabase" }, { "db", "admin" } },
                        new BsonDocument { { "role", "changeOwnPasswordCustomDataRole" }, { "db", "admin" } },
                    } } };
            }

            user.ln_admin_key = "";
            user.ln_invoice_key = "";
            user.ln_user_id = "";
            user.ln_wallet_id = "";
            try
            {
                adminDatabase.RunCommand<BsonDocument>(createUser);
                ChangeUserCustomData(user);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool DeleteUser(string user)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (user == null || user == "")
            {
                return false;
            }

            if (user == Properties.Settings.Default.MongoDBUser)
            {
                return false;
            }

            int auth = CheckAuthentication(user, "admin");
            if (auth < 4)
            {
                var database = Client.GetDatabase("admin");
                var dropuser = new BsonDocument { { "dropUser", user } };
                try
                {
                    database.RunCommand<BsonDocument>(dropuser);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                MessageBox.Show("Can't delete Admin User");
                return false;
            }

            return true;
        }

        public static bool ChangeUserPassword(DatabaseUser user)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (user.Password == null || user.Password == "")
            {
                return false;
            }

            var database = Client.GetDatabase("admin");
            var changepw = new BsonDocument { { "updateUser", user.Name }, { "pwd", user.Password } };
            try
            {
                database.RunCommand<BsonDocument>(changepw);
            }
            catch
            {
                return false;
            }

           

            return true;
        }

        public static DatabaseUser GetUserInfo(string username)
        {
            DatabaseUser dbuser = new DatabaseUser();
            dbuser.Name = username;
            return GetUserInfo(dbuser);
        }

        public static DatabaseUser GetUserInfo(DatabaseUser dbuser)
        {
            var adminDB = client.GetDatabase("admin");
            var cmd = new BsonDocument("usersInfo", dbuser.Name);
            var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
            var Customdata = (BsonDocument)queryResult[0][0]["customData"];
            try
            {
                dbuser.Fullname = Customdata["fullname"].ToString();
            }
            catch
            {
                dbuser.Fullname = dbuser.Name;
            };
            try
            {
                dbuser.Email = Customdata["email"].ToString();
            }
            catch
            {
                dbuser.Email = "";
            };
            try
            {
                dbuser.Expertise = Customdata["expertise"].AsInt32;
            }
            catch
            {
                dbuser.Expertise = 0;
            };

            try
            {
                dbuser.ln_invoice_key = Customdata["ln_invoice_key"].ToString();
            }
            catch
            {
                dbuser.ln_invoice_key = "";
            };

            try
            {
                dbuser.ln_admin_key = MainHandler.Cipher.DecryptString(Customdata["ln_admin_key"].ToString(), MainHandler.Decode(Properties.Settings.Default.MongoDBPass));

            }
            catch
            {
                dbuser.ln_admin_key = "";
            };

            try
            {
                dbuser.ln_wallet_id = Customdata["ln_wallet_id"].ToString();

            }
            catch
            {
                dbuser.ln_wallet_id = "";
            };

            try
            {
                dbuser.ln_user_id = Customdata["ln_user_id"].ToString();

            }
            catch
            {
                dbuser.ln_user_id = "";
            };




            return dbuser;
        }

        public static bool ChangeUserCustomData(DatabaseUser user)
        {
            if (!IsConnected)
            {
                return false;
            }

            var database = Client.GetDatabase("admin");
            var updatecustomdata = new BsonDocument { { "updateUser", user.Name }, { "customData", new BsonDocument { { "fullname", user.Fullname }, { "email", user.Email }, { "expertise", user.Expertise }, { "ln_admin_key", user.ln_admin_key }, { "ln_invoice_key", user.ln_invoice_key }, { "ln_wallet_id", user.ln_wallet_id }, { "ln_user_id", user.ln_user_id } } } };
            try
            {
                database.RunCommand<BsonDocument>(updatecustomdata);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static bool RevokeUserRole(string name, string role, string db)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            if (!UserExists(name))
            {
                return false;
            }

            try
            {
                var admindatabase = Client.GetDatabase("admin");
                var updateroles = new BsonDocument {
                    { "revokeRolesFromUser", name },
                    { "roles", new BsonArray {
                        { new BsonDocument { { "role", role }, { "db", db } } }
                    } } };
                admindatabase.RunCommand<BsonDocument>(updateroles);
            }
            catch
            {
                return false;
            }

            return true;
        }

        public static bool GrantUserRole(string name, string role, string db)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            if (!UserExists(name))
            {
                return false;
            }

            try
            {
                var admindatabase = Client.GetDatabase("admin");
                var updateroles = new BsonDocument {
                    { "grantRolesToUser", name },
                    { "roles", new BsonArray { {
                            new BsonDocument { { "role", role }, { "db", db } } }
                    } } };
                admindatabase.RunCommand<BsonDocument>(updateroles);
            }
            catch
            {
                return false;
            }

            return true;
        }

        private static string GetRoleString(int role)
        {
            if (role == 0)
            {
                return "read";
            }
            else if (role == 1)
            {
                return "readWrite";
            }
            else if (role == 2)
            {
                return "dbAdmin";
            }
            else if (role == 3)
            {
                return "userAdminAnyDatabase";
            }
            return "read";
        }

        private static int GetRoleIndex(string role)
        {
            if (role == "read")
            {
                return 0;
            }
            else if (role == "readWrite")
            {
                return 1;
            }
            else if (role == "dbAdmin")
            {
                return 2;
            }
            else if (role == "userAdminAnyDatabase")
            {
                return 3;
            }
            return -1;
        }

        public static bool AddOrUpdateAnnotator(DatabaseAnnotator annotator)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!UserExists(annotator.Name))
            {
                return false;
            }

            BsonDocument document = new BsonDocument {
                        {"name",  annotator.Name}
                        //{"fullname", annotator.FullName == null || annotator.FullName == "" ? annotator.Name : annotator.FullName },
                        //{"email", annotator.Email == null ? "" : annotator.Email },
                        //{"expertise", annotator.Expertise },
                    };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", annotator.Name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            ReplaceOneResult result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).ReplaceOne(filter, document, updateOptions);
            annotators = GetAnnotators();

            RevokeUserRole(annotator.Name, "readWrite", databaseName);
            RevokeUserRole(annotator.Name, "dbAdmin", databaseName);
            RevokeUserRole(annotator.Name, "readWrite", "admin");
            RevokeUserRole(annotator.Name, "userAdminAnyDatabase", "admin");
            if (annotator.Role == "read")
            {
                GrantUserRole(annotator.Name, "read", databaseName);
            }
            else if (annotator.Role == "readWrite")
            {
                GrantUserRole(annotator.Name, "readWrite", databaseName);
            }
            else if (annotator.Role == "dbAdmin")
            {
                GrantUserRole(annotator.Name, "readWrite", databaseName);
                GrantUserRole(annotator.Name, "dbAdmin", databaseName);
            }
            else
            {
                return false;
            }

            return true;
        }

        private static List<DatabaseAnnotator> GetAnnotators(bool onlyValid = true)
        {
            List<string> names = GetCollectionField(DatabaseDefinitionCollections.Annotators, "name", true);
            List<DatabaseAnnotator> items = new List<DatabaseAnnotator>();
            foreach (string name in names)
            {
                DatabaseAnnotator annotator = new DatabaseAnnotator() { Name = name };
                if (GetAnnotator(ref annotator))
                {
                    items.Add(annotator);
                }
            }

            return items.OrderBy(i => i.FullName).ToList();
        }

        public static bool GetAnnotator(ref DatabaseAnnotator annotator)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (CheckAuthentication() > DatabaseAuthentication.DBADMIN)
            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("user", annotator.Name);
                var adminDatabase = Client.GetDatabase("admin");
                var documents = adminDatabase.GetCollection<BsonDocument>("system.users").Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    int role = -1;
                    BsonArray roles = document["roles"].AsBsonArray;
                    for (int i = 0; i < roles.Count; i++)
                    {
                        if (roles[i]["db"] == databaseName)
                        {
                            role = Math.Max(role, GetRoleIndex(roles[i]["role"].AsString));
                        }
                    }
                    annotator.Role = GetRoleString(role);
                }
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", annotator.Name);
                var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    BsonElement value;
                    annotator.Id = document["_id"].AsObjectId;
                }
            }

            DatabaseUser user = DatabaseHandler.GetUserInfo(annotator.Name);
            annotator.FullName = user.Fullname;
            annotator.Expertise = user.Expertise;
            annotator.Email = user.Email;

            //{
            //    var builder = Builders<BsonDocument>.Filter;
            //    var filter = builder.Eq("name", annotator.Name);
            //    var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filter).ToList();
            //    if (documents.Count > 0)
            //    {
            //        var document = documents[0];
            //        BsonElement value;
            //        annotator.Id = document["_id"].AsObjectId;
            //        if (document.TryGetElement("fullname", out value))
            //        {
            //            annotator.FullName = document["fullname"].ToString();
            //        }
            //        else
            //        {
            //            annotator.FullName = annotator.Name;
            //        }
            //        if (document.TryGetElement("email", out value))
            //        {
            //            annotator.Email = document["email"].ToString();
            //        }
            //        else
            //        {
            //            annotator.Email = "";
            //        }
            //        annotator.Expertise = 2;
            //        if (document.TryGetElement("expertise", out value))
            //        {
            //            int expertise;
            //            if (int.TryParse(document["expertise"].ToString(), out expertise))
            //            {
            //                annotator.Expertise = expertise;
            //            }
            //        }
            //    }
            // }

            return true;
        }

        public static bool DeleteAnnotator(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!UserExists(name))
            {
                return false;
            }

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filter).Single();
            string user = result["name"].AsString;
            var del = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).DeleteOne(filter);

            annotators = GetAnnotators();

            RevokeUserRole(user, "read", databaseName);
            RevokeUserRole(user, "readWrite", databaseName);
            RevokeUserRole(user, "dbAdmin", databaseName);

            return true;
        }

        private static bool AddOrUpdateRole(string name, DatabaseRole role)
        {
            if (role.Name == "")
            {
                return false;
            }

            BsonDocument document = new BsonDocument {
                    {"name",  role.Name},
                    {"hasStreams",  role.HasStreams},
                    {"isValid",  true}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles).ReplaceOne(filter, document, updateOptions);
            roles = GetRoles();

            return true;
        }

        public static bool AddRole(DatabaseRole role)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (RoleExists(role.Name))
            {
                return false;
            }

            return AddOrUpdateRole(role.Name, role);
        }

        private static List<DatabaseRole> GetRoles(bool onlyValid = true)
        {
            List<string> names = GetCollectionField(DatabaseDefinitionCollections.Roles, "name", true);
            List<DatabaseRole> items = new List<DatabaseRole>();
            foreach (string name in names)
            {
                DatabaseRole role = new DatabaseRole() { Name = name };
                if (GetRole(ref role))
                {
                    items.Add(role);
                }
            }

            return items.OrderBy(i => i.Name).ToList();
        }

        private static bool GetRole(ref DatabaseRole role)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", role.Name);
                var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles).Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    BsonElement value;
                    role.Id = document["_id"].AsObjectId;
                    role.HasStreams = true;
                    if (document.TryGetElement("hasStreams", out value))
                    {
                        role.HasStreams = document["hasStreams"].AsBoolean;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static bool UpdateRole(string name, DatabaseRole role)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!RoleExists(name))
            {
                return false;
            }

            if (name != role.Name && RoleExists(role.Name))
            {
                return false;
            }

            return AddOrUpdateRole(name, role);
        }

        public static bool DeleteRole(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!RoleExists(name))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var update = Builders<BsonDocument>.Update.Set("isValid", false);
            collection.UpdateOne(filter, update);

            roles = GetRoles();

            return true;
        }

        private static List<DatabaseStream> GetStreams(bool onlyValid = true)
        {
            List<string> names = GetCollectionField(DatabaseDefinitionCollections.Streams, "name", true);
            List<DatabaseStream> items = new List<DatabaseStream>();
            foreach (string name in names)
            {
                DatabaseStream stream = new DatabaseStream() { Name = name };
                if (GetStream(ref stream))
                {
                    items.Add(stream);
                }
            }

            return items.OrderBy(i => i.Name).ToList();
        }

        public static bool GetStream(ref DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", stream.Name);
                var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams).Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    BsonElement value;
                    stream.Id = document["_id"].AsObjectId;
                    stream.FileExt = "";
                    if (document.TryGetElement("fileExt", out value))
                    {
                        stream.FileExt = document["fileExt"].ToString();
                    }
                    stream.Type = "";
                    if (document.TryGetElement("type", out value))
                    {
                        stream.Type = document["type"].ToString();
                    }
                    stream.SampleRate = 25.0;
                    if (document.TryGetElement("sr", out value))
                    {
                        stream.SampleRate = document["sr"].ToDouble();
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AddOrUpdateStream(string name, DatabaseStream stream)
        {
            if (stream.Name == "")
            {
                return false;
            }

            BsonDocument document = new BsonDocument {
                    {"name",  stream.Name},
                    {"fileExt",  stream.FileExt},
                    {"type",  stream.Type},
                    {"isValid",  true},
                    {"sr", stream.SampleRate }
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            ReplaceOneResult result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams).ReplaceOne(filter, document, updateOptions);
            streams = GetStreams();

            return true;
        }

        public static bool AddStream(DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (StreamExists(stream.Name))
            {
                return false;
            }

            return AddOrUpdateStream(stream.Name, stream);
        }

        public static bool UpdateStream(string name, DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamExists(name))
            {
                return false;
            }

            if (name != stream.Name && StreamExists(stream.Name))
            {
                return false;
            }

            return AddOrUpdateStream(name, stream);
        }

        public static bool DeleteStream(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamExists(name))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            collection.DeleteOne(filter);
            streams = GetStreams();

            return true;
        }

        private static bool AddOrUpdateScheme(string name, AnnoScheme scheme)
        {
            if (scheme.Name == "")
            {
                return false;
            }

            BsonDocument document = new BsonDocument();
            BsonElement documentName = new BsonElement("name", scheme.Name);
            BsonElement documentType = new BsonElement("type", scheme.Type.ToString());
            BsonElement documentIsValid = new BsonElement("isValid", true);
            BsonElement documentSr = new BsonElement("sr", scheme.SampleRate);
            BsonElement documentMin = new BsonElement("min", scheme.MinScore);
            BsonElement documentMax = new BsonElement("max", scheme.MaxScore);
            BsonElement documentMinColor = new BsonElement("min_color", new SolidColorBrush(scheme.MinOrBackColor).Color.ToString());
            BsonElement documentColor = new BsonElement("color", new SolidColorBrush(scheme.MinOrBackColor).Color.ToString());
            BsonElement documentMaxColor = new BsonElement("max_color", new SolidColorBrush(scheme.MaxOrForeColor).Color.ToString());
            BsonElement documentPointsNum = new BsonElement("num", scheme.NumberOfPoints);
            BsonElement documentDefaultLabel = new BsonElement("default-label", scheme.DefaultLabel);
            BsonElement documentDefaultLabelColor = new BsonElement("default-label-color", new SolidColorBrush(scheme.DefaultColor).Color.ToString());



            document.Add(documentName);
            document.Add(documentType);

            if (scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                BsonArray labels = new BsonArray();
                int index = 0;
                foreach (AnnoScheme.Label label in scheme.Labels)
                {
                    if (label.Name == "GARBAGE")
                    {
                        continue;
                    }
                    labels.Add(new BsonDocument() {
                    { "id", index++ },
                    { "name", label.Name },
                    { "color", label.Color.ToString() },
                    { "isValid", true } });
                }
                document.Add(documentColor);
                document.Add("labels", labels);
            }
            else if (scheme.Type == AnnoScheme.TYPE.POINT)
            {
                document.Add(documentPointsNum);
                document.Add(documentSr);
                document.Add(documentColor);
            }
            else if (scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                BsonArray labels = new BsonArray();
                int index = 0;
                foreach (AnnoScheme.Label label in scheme.Labels)
                {
                    labels.Add(new BsonDocument() {
                    { "id", index++ },
                    { "name", label.Name },
                    { "color", label.Color.ToString() },
                    { "isValid", true } });
                }
                document.Add("labels", labels);
                document.Add(documentSr);
                document.Add(documentDefaultLabel);
                document.Add(documentDefaultLabelColor);
                document.Add(documentColor);
            }
            else if(scheme.Type == AnnoScheme.TYPE.POLYGON)
            {
                document.Add(documentSr);
                document.Add(documentDefaultLabel);
                document.Add(documentDefaultLabelColor);
                document.Add(documentColor);
            }
            else if (scheme.Type == AnnoScheme.TYPE.FREE)
            {
                document.Add(documentColor);
            }
            else if (scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                document.Add(documentSr);
                document.Add(documentMin);
                document.Add(documentMax);
                document.Add(documentMinColor);
                document.Add(documentMaxColor);
            }
            document.Add(documentIsValid);

            var coll = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions update = new UpdateOptions();
            update.IsUpsert = true;

            var result = coll.ReplaceOne(filter, document, update);
            schemes = GetSchemes();

            return true;
        }

        public static bool AddScheme(AnnoScheme scheme)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (SchemeExists(scheme.Name))
            {
                return false;
            }

            return AddOrUpdateScheme(scheme.Name, scheme);
        }

        private static List<DatabaseScheme> GetSchemes(bool onlyValid = true)
        {
            List<string> names = GetCollectionField(DatabaseDefinitionCollections.Schemes, "name", true);
            List<DatabaseScheme> items = new List<DatabaseScheme>();
            foreach (string name in names)
            {
                DatabaseScheme scheme = new DatabaseScheme() { Name = name };
                if (GetScheme(ref scheme))
                {
                    items.Add(scheme);
                }
            }

            return items.OrderBy(i => i.Name).ToList();
        }

        private static bool GetScheme(ref DatabaseScheme scheme)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", scheme.Name);
                var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes).Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    scheme.Id = document["_id"].AsObjectId;
                    scheme.Type = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), document["type"].AsString);
                    scheme.SampleRate = 0;
                   
                    if (document.Contains("sr"))
                    {
                        scheme.SampleRate = document["sr"].AsDouble;
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        public static bool UpdateScheme(string name, AnnoScheme scheme)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SchemeExists(name))
            {
                return false;
            }

            if (name != scheme.Name && SchemeExists(scheme.Name))
            {
                return false;
            }

            return AddOrUpdateScheme(name, scheme);
        }

        public static AnnoScheme GetAnnotationScheme(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return null;
            }

            if (!SchemeExists(name))
            {
                return null;
            }

            AnnoScheme scheme = new AnnoScheme();

            var annoSchemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var builder = Builders<BsonDocument>.Filter;

            FilterDefinition<BsonDocument> annoSchemeFilter = builder.Eq("name", name);
            BsonDocument annoSchemeDocument = null;
            try
            {
                annoSchemeDocument = annoSchemes.Find(annoSchemeFilter).Single();

                scheme.Name = annoSchemeDocument["name"].ToString();
                scheme.Type = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), annoSchemeDocument["type"].ToString());
                if (scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    scheme.MinScore = annoSchemeDocument["min"].ToDouble();
                    scheme.MaxScore = annoSchemeDocument["max"].ToDouble();
                    scheme.SampleRate = annoSchemeDocument["sr"].ToDouble();
                    scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["min_color"].ToString());
                    scheme.MaxOrForeColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["max_color"].ToString());
                }
                else if (scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                    BsonArray schemeLabelsArray = annoSchemeDocument["labels"].AsBsonArray;
                    string SchemeLabel = "";
                    string SchemeColor = "#000000";
                    for (int j = 0; j < schemeLabelsArray.Count; j++)
                    {
                        try
                        {
                            if (schemeLabelsArray[j]["isValid"].AsBoolean == true)
                            {
                                SchemeLabel = schemeLabelsArray[j]["name"].ToString();
                                SchemeColor = schemeLabelsArray[j]["color"].ToString();
                                AnnoScheme.Label lcp = new AnnoScheme.Label(schemeLabelsArray[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemeLabelsArray[j]["color"].ToString()));
                                scheme.Labels.Add(lcp);
                            }
                        }
                        catch
                        {
                            SchemeLabel = schemeLabelsArray[j]["name"].ToString();
                            SchemeColor = schemeLabelsArray[j]["color"].ToString();
                            AnnoScheme.Label lcp = new AnnoScheme.Label(schemeLabelsArray[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemeLabelsArray[j]["color"].ToString()));
                            scheme.Labels.Add(lcp);
                        }
                    }
                }
                else if (scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                }
                else if (scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    scheme.NumberOfPoints = annoSchemeDocument["num"].ToInt32();
                    scheme.SampleRate = annoSchemeDocument["sr"].ToDouble();
                    scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                }
                else if(scheme.Type == AnnoScheme.TYPE.POLYGON || scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                {
                    scheme.SampleRate = annoSchemeDocument["sr"].ToDouble();
                    scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                    scheme.DefaultColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["default-label-color"].ToString());
                    scheme.DefaultLabel = annoSchemeDocument["default-label"].ToString();

                    if(scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                    {
                        BsonArray schemeLabelsArray = annoSchemeDocument["labels"].AsBsonArray;
                        string SchemeLabel = "";
                        string SchemeColor = "#000000";
                        for (int j = 0; j < schemeLabelsArray.Count; j++)
                        {
                            try
                            {
                                if (schemeLabelsArray[j]["isValid"].AsBoolean == true)
                                {
                                    SchemeLabel = schemeLabelsArray[j]["name"].ToString();
                                    SchemeColor = schemeLabelsArray[j]["color"].ToString();
                                    AnnoScheme.Label lcp = new AnnoScheme.Label(schemeLabelsArray[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemeLabelsArray[j]["color"].ToString()));
                                    scheme.Labels.Add(lcp);
                                }
                            }
                            catch
                            {
                                SchemeLabel = schemeLabelsArray[j]["name"].ToString();
                                SchemeColor = schemeLabelsArray[j]["color"].ToString();
                                AnnoScheme.Label lcp = new AnnoScheme.Label(schemeLabelsArray[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemeLabelsArray[j]["color"].ToString()));
                                scheme.Labels.Add(lcp);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageTools.Warning(ex.ToString());
            }

            return scheme;
        }


        public static bool DeleteBounty(ObjectId id)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            try
            {
                var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("_id", id);
                collection.DeleteOne(filter);
                return true;
            }
            catch(Exception ex)
            {
                return false;
            }
           
        }
        public static bool DeleteScheme(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SchemeExists(name))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var update = Builders<BsonDocument>.Update.Set("isValid", false);
            collection.UpdateOne(filter, update);

            schemes = GetSchemes();

            return true;
        }

        private static List<DatabaseSession> GetSessions(bool onlyValid = true)
        {
            List<string> names = GetCollectionField(DatabaseDefinitionCollections.Sessions, "name", true);
            List<DatabaseSession> items = new List<DatabaseSession>();
            foreach (string name in names)
            {
                DatabaseSession session = new DatabaseSession() { Name = name };
                if (GetSession(ref session))
                {
                    items.Add(session);
                }
            }

            return items.OrderBy(i => i.Name).ToList();
        }

        public static bool GetSession(ref DatabaseSession session)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", session.Name);
                var documents = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions).Find(filter).ToList();
                if (documents.Count > 0)
                {
                    var document = documents[0];
                    BsonElement value;
                    session.Id = document["_id"].AsObjectId;
                    session.Language = "";
                    if (document.TryGetElement("language", out value))
                    {
                        session.Language = document["language"].ToString();
                    }
                    session.Location = "";
                    if (document.TryGetElement("location", out value))
                    {
                        session.Location = document["location"].ToString();
                    }

                    session.Duration = 0.0;
                    if (document.TryGetElement("duration", out value))
                    {
                        session.Duration = document["duration"].AsDouble;
                    }

                    session.Date = new DateTime();
                    if (document.TryGetElement("date", out value))
                    {
                        session.Date = document["date"].ToUniversalTime();
                    }
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AddOrUpdateSession(string name, DatabaseSession session)
        {
            if (session.Name == "")
            {
                return false;
            }

            DateTime date = DateTime.SpecifyKind(session.Date, DateTimeKind.Utc);

            BsonDocument document = new BsonDocument {
                    {"name",  session.Name},
                    {"location",  session.Location},
                    {"language",  session.Language},
                    {"date",  new BsonDateTime(date)},
                    {"duration",  session.Duration},
                    {"isValid",  true}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions).ReplaceOne(filter, document, updateOptions);
            sessions = GetSessions();

            return true;
        }

        public static bool AddSession(DatabaseSession session)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (SessionExists(session.Name))
            {
                return false;
            }

            return AddOrUpdateSession(session.Name, session);
        }

        public static bool UpdateSession(string name, DatabaseSession session)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            return AddOrUpdateSession(name, session);
        }

        public static bool DeleteSession(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SessionExists(name))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var update = Builders<BsonDocument>.Update.Set("isValid", false);
            collection.UpdateOne(filter, update);

            sessions = GetSessions();

            return true;
        }

        #endregion SETTER

        #region Annotation

        private static BsonArray BountyListToBsonArray(List<DatabaseUser> annotators, int contractvalue)
        {
            BsonArray data = new BsonArray();
            for (int i = 0; i < annotators.Count; i++)
            {
                data.Add(new BsonDocument { { "name", annotators[i].Name }, { "status", "default" }, { "value", contractvalue} });
            }
            return data;
        }

        //private static List<DatabaseUser> BsonArrayToBountyList(BsonArray annotators)
        //{
        //    List<DatabaseUser> users = new List<DatabaseUser>();
        //    for (int i = 0; i < annotators.Count; i++)
        //    {
        //        data.Add(new BsonDocument { { "name", annotators[i].Name }, { "value", contractvalue } });
        //    }
        //    return data;
        //}

        private static BsonArray AnnoListToBsonArray(AnnoList annoList, BsonDocument schemeDoc)
        {
            BsonArray data = new BsonArray();
            AnnoScheme.TYPE schemeType = annoList.Scheme.Type;

            if (schemeType == AnnoScheme.TYPE.DISCRETE)
            {
                BsonArray labels = schemeDoc["labels"].AsBsonArray;
                int index = 0;
                for (int i = 0; i < annoList.Count; i++)
                {
                    for (int j = 0; j < labels.Count; j++)
                    {
                        if (annoList[i].Label == labels[j]["name"].ToString())
                        {
                            index = labels[j]["id"].AsInt32;
                            data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "id", index }, { "conf", annoList[i].Confidence }, { "meta", annoList[i].Meta } });
                            break;
                        }
                        else if (annoList[i].Label == GARBAGELABEL)
                        {
                            data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "id", -1 }, { "conf", annoList[i].Confidence } });
                            break;
                        }
                    }
                }
            }
            else if (schemeType == AnnoScheme.TYPE.FREE)
            {
                for (int i = 0; i < annoList.Count; i++)
                {
                    data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "name", annoList[i].Label }, { "conf", annoList[i].Confidence }, { "meta", annoList[i].Meta } });
                }
            }
            else if (schemeType == AnnoScheme.TYPE.CONTINUOUS)
            {
                for (int i = 0; i < annoList.Count; i++)
                {
                    data.Add(new BsonDocument { { "score", annoList[i].Score }, { "conf", annoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                }
            }
            else if (schemeType == AnnoScheme.TYPE.POINT)
            {
                BsonDocument singlepoint;
                for (int i = 0; i < annoList.Count; i++)
                {
                    BsonArray Points = new BsonArray();
                    for (int j = 0; j < annoList.Scheme.NumberOfPoints; j++)
                    {
                        singlepoint = new BsonDocument();
                        singlepoint.Add(new BsonElement("label", annoList[i].Points[j].Label));
                        singlepoint.Add(new BsonElement("x", annoList[i].Points[j].XCoord));
                        singlepoint.Add(new BsonElement("y", annoList[i].Points[j].YCoord));
                        singlepoint.Add(new BsonElement("conf", annoList[i].Points[j].Confidence));

                        Points.Add(singlepoint);
                    }

                    data.Add(new BsonDocument { { "label", annoList[i].Label }, { "conf", annoList[i].Confidence }, { "points", Points } });
                }
            }
            else if(schemeType == AnnoScheme.TYPE.POLYGON || schemeType == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                BsonArray polygons;
                BsonDocument polygon = null;
                BsonArray points;
                BsonDocument point;

                foreach (AnnoListItem item in annoList)
                {
                    polygons = new BsonArray();

                    foreach(PolygonLabel label in item.PolygonList.Polygons)
                    {
                        if (schemeType == AnnoScheme.TYPE.DISCRETE_POLYGON)
                        {
                            BsonArray labels = schemeDoc["labels"].AsBsonArray;
                            int index = 0;
                            for (int j = 0; j < labels.Count; j++)
                            {
                                if (label.Label == labels[j]["name"].ToString())
                                {
                                    index = labels[j]["id"].AsInt32;
                                    polygon = new BsonDocument
                                    {
                                        new BsonElement("label", index),
                                    };

                                    break;
                                }
                            }
                        }
                        else
                        {
                            polygon = new BsonDocument
                            {
                                new BsonElement("label", label.Label),
                                new BsonElement("label_color", new SolidColorBrush(label.Color).Color.ToString())
                            };
                        }

                        points = new BsonArray();
                        foreach (PolygonPoint polygonPoint in label.Polygon)
                        {
                            point = new BsonDocument
                            {
                                new BsonElement("x", Convert.ToInt32(polygonPoint.X)),
                                new BsonElement("y", Convert.ToInt32(polygonPoint.Y))
                            };

                            points.Add(point);
                        }

                        polygon.Add(new BsonElement("points", points));
                        polygons.Add(polygon);
                    }
                    data.Add(new BsonDocument { { "name", item.Label.Split(' ')[1] }, { "polygons", polygons } });
                }
            }

            return data;
        }

        public static bool SaveBounty(DatabaseBounty bounty)
        {
            ChangeDatabase(bounty.Database);
            if (!IsConnected && !IsDatabase && !IsSession)
            {
                return false;
            }

            if (CheckAuthentication() == 0)
            {
                return false;
            }

            string dbuser = Properties.Settings.Default.MongoDBUser;

            // resolve references

            var builder = Builders<BsonDocument>.Filter;
            var bounties = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
           
            // search if bounty exists
            ObjectId roleID = GetObjectID(DatabaseDefinitionCollections.Roles, "name", bounty.Role);
            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, "name", bounty.Scheme);
            ObjectId contractorID = GetObjectID(DatabaseDefinitionCollections.Annotators, "name", bounty.Contractor.Name);
            ObjectId sessionID = GetObjectID(DatabaseDefinitionCollections.Sessions, "name", bounty.Session);


            var filter_bounty = builder.Eq("scheme_id", schemeID)
                    & builder.Eq("role_id", roleID)
                    & builder.Eq("contractor_id", contractorID)
                    & builder.Eq("session_id", sessionID);

            var bountyDoc = bounties.Find(filter_bounty).ToList();

                BsonArray candidates = BountyListToBsonArray(bounty.annotatorsJobCandidates, bounty.valueInSats);
                BsonArray jobdone = BountyListToBsonArray(bounty.annotatorsJobDone, bounty.valueInSats);
                BsonArray streamArray = new BsonArray();
                if (bounty.streams != null)
                {
                    foreach (StreamItem dmi in bounty.streams)
                    {
                        streamArray.Add(new BsonString(dmi.Name));
                    }
                }
               
                BsonDocument newBountyDoc = new BsonDocument();

                // insert/update annotation
                newBountyDoc.Add(new BsonElement("contractor_id", contractorID));
                newBountyDoc.Add(new BsonElement("role_id", roleID));
                newBountyDoc.Add(new BsonElement("scheme_id", schemeID));
                newBountyDoc.Add(new BsonElement("session_id", sessionID));
                newBountyDoc.Add(new BsonElement("valueInSats", bounty.valueInSats));
                newBountyDoc.Add(new BsonElement("type", bounty.Type));
                newBountyDoc.Add(new BsonElement("numOfAnnotations", bounty.numOfAnnotations));
                newBountyDoc.Add(new BsonElement("numOfAnnotationsNeededCurrent", bounty.numOfAnnotationsNeededCurrent));
                newBountyDoc.Add("annotatorsJobCandidates", candidates);
                newBountyDoc.Add("annotatorsJobDone", jobdone);
                newBountyDoc.Add("streams", streamArray);


            UpdateOptions newAnnotationDocUpdate = new UpdateOptions();
                newAnnotationDocUpdate.IsUpsert = true;
                bounties.ReplaceOne(filter_bounty, newBountyDoc, newAnnotationDocUpdate);

                if (bountyDoc.Count > 0)
                {
                    bounty.OID = bountyDoc[0]["_id"].AsObjectId;
                }
                else bounty.OID = ObjectId.GenerateNewId();

            return true;
        }

        public static bool SaveAnnoList(AnnoList annoList, List<string> linkedStreams = null, bool force = false, bool markAsFinished = false, bool keepOriginalAnnotator = false)
        {
            if (!IsConnected && !IsDatabase && !IsSession)
            {
                return false;
            }

            if (CheckAuthentication() == 0)
            {
                return false;
            }

            string dbuser = Properties.Settings.Default.MongoDBUser;

            // resolve references

            var builder = Builders<BsonDocument>.Filter;

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationData = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.AnnotationData);
            var annotators = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators);
            var schemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);

            ObjectId roleID = Roles.Find(role => role.Name == annoList.Meta.Role).Id;
            ObjectId sessionID = Sessions.Find(session => session.Name == annoList.Source.Database.Session).Id;

            ObjectId schemeID;
            AnnoScheme.TYPE schemeType;
            BsonDocument schemeDoc;
            {
                var filter = builder.Eq("name", annoList.Scheme.Name);
                var documents = schemes.Find(filter).ToList();
                if (documents.Count == 0)
                {
                    return false;
                }
                schemeDoc = documents[0];
                schemeID = schemeDoc["_id"].AsObjectId;
                string type = schemeDoc["type"].AsString;
                schemeType = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), type);
            }

            if (!keepOriginalAnnotator && annoList.Meta.Annotator != dbuser)
            {
                ObjectId userID = GetObjectID(DatabaseDefinitionCollections.Annotators, "name", dbuser);
                if (AnnotationExists(userID, sessionID, roleID, schemeID))
                {
                    MessageBoxResult mbres = MessageBox.Show("An own annotation already exists, overwrite it?", "Attention", MessageBoxButton.YesNo);
                    if (mbres == MessageBoxResult.No)
                    {
                        return false;
                    }
                }
                annoList.Meta.Annotator = dbuser;
                annoList.Meta.AnnotatorFullName = GetUserInfo(dbuser).Fullname; //   FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", userID);
                annoList.Source.Database.DataOID = new ObjectId();
            }

            // add admin users if not yet assigned as annotator

            ObjectId annotatorID;
            if (!AnnotatorExists(annoList.Meta.Annotator))
            {
                BsonDocument annotatorDoc = new BsonDocument();
                annotatorDoc.Add(new BsonElement("name", annoList.Meta.Annotator));
                //annotatorDoc.Add(new BsonElement("fullname", annoList.Meta.AnnotatorFullName == "" ? annoList.Meta.Annotator : annoList.Meta.AnnotatorFullName));
                var filter = builder.Eq("name", annoList.Meta.Annotator);
                UpdateOptions update = new UpdateOptions();
                update.IsUpsert = true;
                annotators.ReplaceOne(filter, annotatorDoc, update);
                annotatorID = annotators.Find(filter).Single()["_id"].AsObjectId;

                DatabaseHandler.annotators = GetAnnotators();
            }
            else
            {
                var filter = builder.Eq("name", annoList.Meta.Annotator);
                var documents = annotators.Find(filter).ToList();
                if (documents.Count == 0)
                {
                    return false;
                }
                annotatorID = documents[0].GetValue(0).AsObjectId;
            }

            // search if annotation exists

            var filter_anno = builder.Eq("scheme_id", schemeID)
                    & builder.Eq("role_id", roleID)
                    & builder.Eq("annotator_id", annotatorID)
                    & builder.Eq("session_id", sessionID);

            var annotationDoc = annotations.Find(filter_anno).ToList();

            bool isLocked = false;
            if (annotationDoc.Count > 0)
            {
                try
                {
                    isLocked = annotationDoc[0]["isLocked"].AsBoolean;
                }
                catch { }
            }

            // are we allowed to save the annotation?

            if (force || !isLocked)
            {
                if (annotationDoc.Count > 0 && (Properties.Settings.Default.DatabaseAskBeforeOverwrite && !force))
                {
                    MessageBoxResult mbres = MessageBox.Show("Overwrite existing annotation?", "Attention", MessageBoxButton.YesNo);
                    if (mbres == MessageBoxResult.No)
                    {
                        return false;
                    }
                }

                // delete and replace backup annotation data

                if (annoList.Source.Database.DataBackupOID != AnnoSource.DatabaseSource.ZERO)
                {
                    var filterAnnotationDataBackupDoc = builder.Eq("_id", annoList.Source.Database.DataBackupOID);
                    annotationData.DeleteOne(filterAnnotationDataBackupDoc);
                }
                annoList.Source.Database.DataBackupOID = annoList.Source.Database.DataOID;

                // insert new annotation data

                annoList.Source.Database.DataOID = ObjectId.GenerateNewId();

                BsonArray data = AnnoListToBsonArray(annoList, schemeDoc);
                BsonDocument newAnnotationDataDoc = new BsonDocument();
                newAnnotationDataDoc.Add(new BsonElement("_id", annoList.Source.Database.DataOID));
                newAnnotationDataDoc.Add("labels", data);
                annotationData.InsertOne(newAnnotationDataDoc);

                // insert/update annotation

                BsonDocument newAnnotationDoc = new BsonDocument();
                newAnnotationDoc.Add(new BsonElement("data_id", annoList.Source.Database.DataOID));
                if (annoList.Source.Database.DataBackupOID != AnnoSource.DatabaseSource.ZERO)
                {
                    newAnnotationDoc.Add(new BsonElement("data_backup_id", annoList.Source.Database.DataBackupOID));
                }

                bool isfinished = annoList.Meta.isFinished;
                if (markAsFinished)
                { 
                    isfinished = true;

                    if (annoList.Source.Database.HasBounty)
                    {
                        DatabaseBounty bounty = GetBountybyID(annoList.Source.Database.BountyID);
                        //Simple Case, should probably not be used.
                        //if(bounty.Type == "Trust")
                        {
                            DatabaseUser user = GetUserInfo(dbuser);
                            int index = bounty.annotatorsJobCandidates.FindIndex(s => s.Name == user.Name);
                            if (index > -1)
                            {
                                bounty.annotatorsJobCandidates.RemoveAt(index);
                                bounty.annotatorsJobDone.Add(user);
                                bounty.numOfAnnotationsNeededCurrent -= 1;
                                MessageBox.Show("Bounty submitted, please wait for Contractor to approve.");
                                SaveBounty(bounty);
                            }
                           
                        }
                       

                    }
                }
                newAnnotationDoc.Add(new BsonElement("annotator_id", annotatorID));
                newAnnotationDoc.Add(new BsonElement("role_id", roleID));
                newAnnotationDoc.Add(new BsonElement("scheme_id", schemeID));
                newAnnotationDoc.Add(new BsonElement("session_id", sessionID));
                newAnnotationDoc.Add(new BsonElement("isFinished", isfinished));
                newAnnotationDoc.Add(new BsonElement("isLocked", isLocked));
                if (annoList.Source.Database.HasBounty)
                {
                    newAnnotationDoc.Add(new BsonElement("bounty_id", annoList.Source.Database.BountyID));
                    newAnnotationDoc.Add(new BsonElement("bountyIsPaid", annoList.Source.Database.BountyIsPaid));
                }
               
                newAnnotationDoc.Add(new BsonElement("date", new BsonDateTime(DateTime.Now)));
                BsonArray streamArray = new BsonArray();
                if (linkedStreams != null)
                {
                    foreach (string dmi in linkedStreams)
                    {
                        streamArray.Add(new BsonString(dmi));
                    }
                }
                newAnnotationDoc.Add("streams", streamArray);

                UpdateOptions newAnnotationDocUpdate = new UpdateOptions();
                newAnnotationDocUpdate.IsUpsert = true;
                annotations.ReplaceOne(filter_anno, newAnnotationDoc, newAnnotationDocUpdate);

                if (annotationDoc.Count > 0)
                {
                    annotationDoc = annotations.Find(filter_anno).ToList();
                    annoList.Source.Database.OID = annotationDoc[0]["_id"].AsObjectId;
                }
            }
            else
            {
                MessageBox.Show("Cannot save an annotation that is locked");
                return false;
            };

            return true;
        }

        public static ObjectId GetAnnotationId(string role, string scheme, string annotator, string session)
        {
            ObjectId id = new ObjectId();

            var builder = Builders<BsonDocument>.Filter;
            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

            ObjectId roleID = GetObjectID(DatabaseDefinitionCollections.Roles, "name", role);
            string roleName = FetchDBRef(DatabaseDefinitionCollections.Roles, "name", roleID);

            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, "name", scheme);
            string schemeName = FetchDBRef(DatabaseDefinitionCollections.Schemes, "name", schemeID);

            ObjectId annotatorID = GetObjectID(DatabaseDefinitionCollections.Annotators, "name", annotator);
            string annotatorName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "name", annotatorID);
            DatabaseHandler.GetUserInfo(annotator);
            //string annotatorFullName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", annotatorID);
            string annotatorFullName = DatabaseHandler.GetUserInfo(annotator).Fullname;

            ObjectId sessionID = GetObjectID(DatabaseDefinitionCollections.Sessions, "name", session);
            string sessionName = FetchDBRef(DatabaseDefinitionCollections.Sessions, "name", sessionID);

            var filterAnnotation = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("session_id", sessionID);
            var annotationDocs = annotations.Find(filterAnnotation).ToList();

            if (annotationDocs.Count > 0)
            {
                id = annotationDocs[0]["_id"].AsObjectId;
            }

            return id;
        }


        public static ObjectId GetAnnotationId(string role, string scheme, string annotator, string session, string dbname)
        {
            ObjectId id = new ObjectId();

            IMongoDatabase db = DatabaseHandler.Client.GetDatabase(dbname);
            var builder = Builders<BsonDocument>.Filter;
            var annotations = db.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

            ObjectId roleID = GetObjectID(DatabaseDefinitionCollections.Roles, db, "name", role);
            string roleName = FetchDBRef(DatabaseDefinitionCollections.Roles, db, "name", roleID);

            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, db, "name", scheme);
            string schemeName = FetchDBRef(DatabaseDefinitionCollections.Schemes, db, "name", schemeID);

            ObjectId annotatorID = GetObjectID(DatabaseDefinitionCollections.Annotators, db, "name", annotator);
            string annotatorName = FetchDBRef(DatabaseDefinitionCollections.Annotators, db, "name", annotatorID);
            DatabaseHandler.GetUserInfo(annotator);
            //string annotatorFullName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", annotatorID);
            string annotatorFullName = DatabaseHandler.GetUserInfo(annotator).Fullname;

            ObjectId sessionID = GetObjectID(DatabaseDefinitionCollections.Sessions, db, "name", session);
            string sessionName = FetchDBRef(DatabaseDefinitionCollections.Sessions, db, "name", sessionID);

            var filterAnnotation = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("session_id", sessionID);
            var annotationDocs = annotations.Find(filterAnnotation).ToList();

            if (annotationDocs.Count > 0)
            {
                id = annotationDocs[0]["_id"].AsObjectId;
            }

            return id;
        }

        public static bool DeleteAnnotation(ObjectId id)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            var annotationCollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationDataCollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.AnnotationData);

            // find annotation

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", id);
            List<BsonDocument> annotations = annotationCollection.Find(filter).ToList();
            if (annotations.Count == 0)
            {
                return false;
            }
            BsonDocument annotation = annotations[0];

            // remove annotation data

            if (annotation.Contains("data_id"))
            {
                ObjectId data_id = annotation["data_id"].AsObjectId;
                var filterData = builder.Eq("_id", data_id);
                annotationDataCollection.DeleteOne(filterData);
            }

            // remove annotation backup data
            if (annotation.Contains("data_backup_id"))
            {
                ObjectId data_id = annotation["data_backup_id"].AsObjectId;
                var filterData = builder.Eq("_id", data_id);
                annotationDataCollection.DeleteOne(filterData);
            }

            // remove annotation

            annotationCollection.DeleteOne(filter);

            return true;
        }

        public static DatabaseBounty GetBountybyID(ObjectId id)
        {
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;

            var bountiesDB = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
            //maybe need some more logic here
            var filter = builder.Eq("_id", id);
            BsonDocument doc = null;
            var doclist = bountiesDB.Find(filter).ToList();
            if (doclist.Count == 0) return null;
            else doc = doclist[0];

            DatabaseBounty bounty = new DatabaseBounty();

            bounty.OID = id;

            ObjectId contractorID = doc["contractor_id"].AsObjectId;
            string contractorname = "";
            GetObjectName(ref contractorname, database, DatabaseDefinitionCollections.Annotators, contractorID);
            bounty.Contractor = GetUserInfo(contractorname);

            ObjectId roleID = doc["role_id"].AsObjectId;
            string role = "";
            GetObjectName(ref role, database, DatabaseDefinitionCollections.Roles, roleID);
            bounty.Role = role;

            ObjectId sessionID = doc["session_id"].AsObjectId;
            string session = "";
            GetObjectName(ref session, database, DatabaseDefinitionCollections.Sessions, sessionID);
            bounty.Session = session;

            ObjectId schemeID = doc["scheme_id"].AsObjectId;
            string scheme = "";
            GetObjectName(ref scheme, database, DatabaseDefinitionCollections.Schemes, schemeID);
            bounty.Scheme = scheme;

            bounty.valueInSats = doc["valueInSats"].AsInt32;
            bounty.numOfAnnotations = doc["numOfAnnotations"].AsInt32;
            bounty.Type = doc["type"].AsString;
            bounty.numOfAnnotationsNeededCurrent = doc["numOfAnnotationsNeededCurrent"].AsInt32;
            bounty.Database = database.DatabaseNamespace.DatabaseName;

            bounty.annotatorsJobCandidates = new List<DatabaseUser>();
            foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
            {
                DatabaseUser user = GetUserInfo(cand["name"].AsString);
                bounty.annotatorsJobCandidates.Add(user);
            }

            bounty.annotatorsJobDone = new List<DatabaseUser>();
            bool foundmine = false;
            foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
            {
                DatabaseUser user = GetUserInfo(cand["name"].AsString);
                bounty.annotatorsJobDone.Add(user);
            }

            bounty.streams = new List<StreamItem>();
            foreach (BsonString stream in doc["streams"].AsBsonArray)
            {
                string str = stream.AsString;
                StreamItem dbst = new StreamItem();
                dbst.Name = str;
                bounty.streams.Add(dbst);
            }
            return bounty;
        }

        public static List<DatabaseBounty> LoadAcceptedBounties(IMongoDatabase database)
        {

            List<DatabaseBounty> bounties = new List<DatabaseBounty>();
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;

            var bountiesDB = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
            //maybe need some more logic here
            var filter = builder.Gt("numOfAnnotationsNeededCurrent", 0);
            var bountiesDoc = bountiesDB.Find(filter).ToList();
            if (bountiesDoc.Count == 0) return null;
            foreach (BsonDocument doc in bountiesDoc)
            {
                DatabaseBounty bounty = new DatabaseBounty();

                bounty.OID = doc["_id"].AsObjectId;

                ObjectId contractorID = doc["contractor_id"].AsObjectId;
                string contractorname = "";
                GetObjectName(ref contractorname, database, DatabaseDefinitionCollections.Annotators, contractorID);
                bounty.Contractor = GetUserInfo(contractorname);

                ObjectId roleID = doc["role_id"].AsObjectId;
                string role = "";
                GetObjectName(ref role, database, DatabaseDefinitionCollections.Roles, roleID);
                bounty.Role = role;

                ObjectId sessionID = doc["session_id"].AsObjectId;
                string session = "";
                GetObjectName(ref session, database, DatabaseDefinitionCollections.Sessions, sessionID);
                bounty.Session = session;

                ObjectId schemeID = doc["scheme_id"].AsObjectId;
                string scheme = "";
                GetObjectName(ref scheme, database, DatabaseDefinitionCollections.Schemes, schemeID);
                bounty.Scheme = scheme;

                bounty.valueInSats = doc["valueInSats"].AsInt32;
                bounty.numOfAnnotations = doc["numOfAnnotations"].AsInt32;
                bounty.Type = doc["type"].AsString;
                bounty.numOfAnnotationsNeededCurrent = doc["numOfAnnotationsNeededCurrent"].AsInt32;
                bounty.Database = database.DatabaseNamespace.DatabaseName;

                bounty.annotatorsJobCandidates = new List<DatabaseUser>();
                bool foundmine = false;
                foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
                {
                    DatabaseUser user = GetUserInfo(cand["name"].AsString);
                    bounty.annotatorsJobCandidates.Add(user);

                    //Don't return contract if already accepted.
                    if (user.Name == Properties.Settings.Default.MongoDBUser)
                    {
                        foundmine = true;
                    }
                }

                bounty.annotatorsJobDone = new List<DatabaseUser>();
               
                foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
                {
                    DatabaseUser user = GetUserInfo(cand["name"].AsString);
                    bounty.annotatorsJobDone.Add(user);
                }

                bounty.streams = new List<StreamItem>();
                foreach (BsonString stream in doc["streams"].AsBsonArray)
                {
                    string str = stream.AsString;
                    StreamItem dbst = new StreamItem();
                    dbst.Name = str;
                    bounty.streams.Add(dbst);
                }
                //todo ListofAnnotators
                if (foundmine) bounties.Add(bounty);
            }

            return bounties;
        }

        public static List<DatabaseBounty> LoadCreatedBounties(string databaseName)
        {
            IMongoDatabase db = DatabaseHandler.Client.GetDatabase(databaseName);
            List<DatabaseBounty> bounties = new List<DatabaseBounty>();
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;

            var bountiesDB = db.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
            //maybe need some more logic here


            ObjectId id = new ObjectId();
            GetObjectID(ref id, db, DatabaseDefinitionCollections.Annotators, Properties.Settings.Default.MongoDBUser);

        


            var filter = builder.Eq("contractor_id", id);
            var bountiesDoc = bountiesDB.Find(filter).ToList();
            if (bountiesDoc.Count == 0) return null;
            foreach (BsonDocument doc in bountiesDoc)
            {
                DatabaseBounty bounty = new DatabaseBounty();

                bounty.OID = doc["_id"].AsObjectId;
                bounty.Contractor = GetUserInfo(Properties.Settings.Default.MongoDBUser);

                ObjectId roleID = doc["role_id"].AsObjectId;
                string role = "";
                GetObjectName(ref role, db, DatabaseDefinitionCollections.Roles, roleID);
                bounty.Role = role;

                ObjectId sessionID = doc["session_id"].AsObjectId;
                string session = "";
                GetObjectName(ref session, db, DatabaseDefinitionCollections.Sessions, sessionID);
                bounty.Session = session;

                ObjectId schemeID = doc["scheme_id"].AsObjectId;
                string scheme = "";
                GetObjectName(ref scheme, db, DatabaseDefinitionCollections.Schemes, schemeID);
                bounty.Scheme = scheme;

                bounty.valueInSats = doc["valueInSats"].AsInt32;
                bounty.numOfAnnotations = doc["numOfAnnotations"].AsInt32;
                bounty.Type = doc["type"].AsString;
                bounty.numOfAnnotationsNeededCurrent = doc["numOfAnnotationsNeededCurrent"].AsInt32;
                bounty.Database = databaseName;

                bounty.annotatorsJobCandidates = new List<DatabaseUser>();
                foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
                {
                    DatabaseUser user = GetUserInfo(cand["name"].AsString);
                    bounty.annotatorsJobCandidates.Add(user);
                }

                bounty.annotatorsJobDone = new List<DatabaseUser>();

                foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
                {
                    DatabaseUser user = GetUserInfo(cand["name"].AsString);
                    bounty.annotatorsJobDone.Add(user);
                }

                bounty.streams = new List<StreamItem>();
                foreach (BsonString stream in doc["streams"].AsBsonArray)
                {
                    string str = stream.AsString;
                    StreamItem dbst = new StreamItem();
                    dbst.Name = str;
                    bounty.streams.Add(dbst);
                }
                //todo ListofAnnotators
               bounties.Add(bounty);
            }

            return bounties;
        }



        public static List<DatabaseBounty> LoadActiveBounties(IMongoDatabase database)
        {
            List<DatabaseBounty> bounties = new List<DatabaseBounty>();
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;
            var bountiesDB = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
            //maybe need some more logic here
            var filter = builder.Gt("numOfAnnotationsNeededCurrent", 0);
            var bountiesDoc = bountiesDB.Find(filter).ToList();
            if (bountiesDoc.Count == 0) return null;
            foreach(BsonDocument doc in bountiesDoc)
            {
                DatabaseBounty bounty = new DatabaseBounty();

                bounty.OID = doc["_id"].AsObjectId;

                ObjectId contractorID = doc["contractor_id"].AsObjectId;
                string contractorname = "";
                GetObjectName(ref contractorname, database, DatabaseDefinitionCollections.Annotators, contractorID);
                bounty.Contractor = GetUserInfo(contractorname);

                ObjectId roleID = doc["role_id"].AsObjectId;
                string role = "";
                GetObjectName(ref role, database, DatabaseDefinitionCollections.Roles, roleID);
                bounty.Role = role;

                ObjectId sessionID = doc["session_id"].AsObjectId;
                string session = "";
                GetObjectName(ref session, database, DatabaseDefinitionCollections.Sessions, sessionID);
                bounty.Session = session;

                ObjectId schemeID = doc["scheme_id"].AsObjectId;
                string scheme = "";
                GetObjectName(ref scheme, database, DatabaseDefinitionCollections.Schemes, schemeID);
                bounty.Scheme = scheme;

                bounty.valueInSats = doc["valueInSats"].AsInt32;
                bounty.numOfAnnotations = doc["numOfAnnotations"].AsInt32;
                bounty.Type = doc["type"].AsString;
                bounty.numOfAnnotationsNeededCurrent = doc["numOfAnnotationsNeededCurrent"].AsInt32;
                bounty.Database = database.DatabaseNamespace.DatabaseName;

                bool handled = false;
                bounty.annotatorsJobCandidates = new List<DatabaseUser>();
                foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
                {
                    DatabaseUser user = GetUserInfo(cand["name"].AsString);
                    bounty.annotatorsJobCandidates.Add(user);
                    //Don't return contract if already accepted.
                    if (user.Name == Properties.Settings.Default.MongoDBUser)
                    {
                        handled = true;
                    }
                }

                bounty.annotatorsJobDone = new List<DatabaseUser>();
                foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
                {
                    DatabaseUser user = GetUserInfo(cand["name"].AsString);
                    bounty.annotatorsJobDone.Add(user);

                    //Don't return contract if already finished.
                    if (user.Name == Properties.Settings.Default.MongoDBUser)
                    {
                        handled = true;
                    }
                }

               
                bounty.streams = new List<StreamItem>();
                foreach (BsonString stream in (doc["streams"].AsBsonArray))
                {
                    string str = stream.AsString;
                    StreamItem dbst = new StreamItem();
                    dbst.Name = str;
                    bounty.streams.Add(dbst);
                }

                //todo ListofAnnotators

                if(!handled) bounties.Add(bounty);
            }

            return bounties;
        }

        public static AnnoList LoadAnnoList(string oid)
        {
            return LoadAnnoList(new ObjectId(oid));
        }


        public static AnnoList LoadAnnoList(ObjectId oid)
        {
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var filter = builder.Eq("_id", oid);
            var annotationDocs = annotations.Find(filter).ToList();

            if (annotationDocs.Count == 0)
            {
                return null;
            }

            BsonDocument annotationDoc = annotationDocs[0];

            ObjectId roleID = annotationDoc["role_id"].AsObjectId;
            ObjectId schemeID = annotationDoc["scheme_id"].AsObjectId;
            ObjectId annotatorID = annotationDoc["annotator_id"].AsObjectId;
            ObjectId sessionID = annotationDoc["session_id"].AsObjectId;

            string role = "";
            string scheme = "";
            string annotator = "";
            string session = "";

            GetObjectName(ref role, DatabaseDefinitionCollections.Roles, roleID);
            GetObjectName(ref scheme, DatabaseDefinitionCollections.Schemes, schemeID);
            GetObjectName(ref annotator, DatabaseDefinitionCollections.Annotators, annotatorID);
            GetObjectName(ref session, DatabaseDefinitionCollections.Sessions, sessionID);

            if (role == "" || scheme == "" || annotator == "" || session == "")
            {
                return null;
            }

            ChangeSession(session);
            DatabaseAnnotation annotation = new DatabaseAnnotation()
            {
                Role = role,
                Scheme = scheme,
                Annotator = annotator,
                Session = session
            };

            return LoadAnnoList(annotation, false);
        }

        private static void loadAnnoListSchemeAndData(ref AnnoList annoList, BsonDocument scheme, BsonDocument data)
        {
            BsonElement value;
            var builder = Builders<BsonDocument>.Filter;

            annoList.Scheme = new AnnoScheme();
            annoList.Scheme.Type = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), scheme["type"].AsString);
            annoList.Scheme.Name = scheme["name"].ToString();

            if (scheme.TryGetElement("bounty_id", out value))
            {
                annoList.Source.Database.HasBounty = true;
                annoList.Source.Database.BountyID = scheme["bounty_id"].AsObjectId;
                annoList.Source.Database.BountyIsPaid = scheme["bountyIsPaid"].AsBoolean;
            }


            BsonArray labels = data["labels"].AsBsonArray;
            if (labels != null)
            {
                if (annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    if (scheme.TryGetElement("min", out value)) annoList.Scheme.MinScore = double.Parse(scheme["min"].ToString());
                    if (scheme.TryGetElement("max", out value)) annoList.Scheme.MaxScore = double.Parse(scheme["max"].ToString());
                    if (scheme.TryGetElement("sr", out value)) annoList.Scheme.SampleRate = double.Parse(scheme["sr"].ToString());

                    if (scheme.TryGetElement("min_color", out value)) annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme["min_color"].ToString());
                    if (scheme.TryGetElement("max_color", out value)) annoList.Scheme.MaxOrForeColor = (Color)ColorConverter.ConvertFromString(scheme["max_color"].ToString());

                    annoList.Scheme.MinScore = annoList.Scheme.MinScore;
                    annoList.Scheme.MaxScore = annoList.Scheme.MaxScore;
                    annoList.Scheme.SampleRate = annoList.Scheme.SampleRate;

                    for (int i = 0; i < labels.Count; i++)
                    {
                        double score = labels[i]["score"].ToDouble();
                        string confidence = labels[i]["conf"].ToString();
                        double start = i / annoList.Scheme.SampleRate;
                        double dur = 1 / annoList.Scheme.SampleRate;

                        AnnoListItem ali = new AnnoListItem(start, dur, score, "", Colors.Black, double.Parse(confidence));

                        annoList.Add(ali);
                    }
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme["color"].ToString());

                    annoList.Scheme.Labels = new List<AnnoScheme.Label>();

                    BsonArray schemelabels = scheme["labels"].AsBsonArray;

                    for (int j = 0; j < schemelabels.Count; j++)
                    {
                        try
                        {
                            if (schemelabels[j]["isValid"].AsBoolean == true) annoList.Scheme.Labels.Add(new AnnoScheme.Label(schemelabels[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString())));
                        }
                        catch
                        {
                            annoList.Scheme.Labels.Add(new AnnoScheme.Label(schemelabels[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString())));
                        }
                    }

                    annoList.Scheme.Labels.Add(new AnnoScheme.Label(GARBAGELABEL, GARBAGECOLOR));

                    for (int i = 0; i < labels.Count; i++)
                    {
                        string SchemeLabel = "";
                        Color SchemeColor = Colors.Black;
                        bool idfound = false;
                        for (int j = 0; j < schemelabels.Count; j++)
                        {
                            if (labels[i]["id"].AsInt32 == schemelabels[j]["id"].AsInt32)
                            {
                                SchemeLabel = schemelabels[j]["name"].ToString();
                                SchemeColor = (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString());
                                idfound = true;
                                break;
                            }
                        }

                        if (labels[i]["id"].AsInt32 == -1 || idfound == false)
                        {
                            SchemeLabel = GARBAGELABEL;
                            SchemeColor = GARBAGECOLOR;
                        }

                        double start = double.Parse(labels[i]["from"].ToString());
                        double stop = double.Parse(labels[i]["to"].ToString());
                        double duration = stop - start;
                        string label = SchemeLabel;
                        string confidence = labels[i]["conf"].ToString();
                        string meta = "";
                        try
                        {
                            meta = labels[i]["meta"].ToString();
                        }
                        catch (Exception e)
                        {
                          
                        }

                        AnnoListItem ali = new AnnoListItem(start, duration, label, meta, SchemeColor, double.Parse(confidence));
                        annoList.AddSorted(ali);
                    }
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme["color"].ToString());

                    for (int i = 0; i < labels.Count; i++)
                    {
                        double start = double.Parse(labels[i]["from"].ToString());
                        double stop = double.Parse(labels[i]["to"].ToString());
                        double duration = stop - start;
                        string label = labels[i]["name"].ToString();
                        string confidence = labels[i]["conf"].ToString();
                        string meta = "";
                        try
                        {
                            meta = labels[i]["meta"].ToString();
                        }
                        catch (Exception e)
                        {

                        }
                        //string colorstring = labels[i]["color"].ToString();
                        AnnoListItem ali = new AnnoListItem(start, duration, label, meta, Colors.Black, double.Parse(confidence));
                        annoList.AddSorted(ali);
                    }
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme["color"].ToString());

                    if (scheme.TryGetElement("sr", out value)) 
                        annoList.Scheme.SampleRate = double.Parse(scheme["sr"].ToString());

                    for (int i = 0; i < labels.Count; i++)
                    {
                        BsonDocument entry = labels[i].AsBsonDocument;
                        string label = entry["label"].AsString;
                        double confidence = entry["conf"].AsDouble;
                        PointList pl = new PointList();
                        BsonArray points = entry["points"].AsBsonArray;

                        foreach (BsonDocument b in points)
                        {
                            int x = b["x"].ToInt32();
                            int y = b["y"].ToInt32();
                            string l = b["label"].ToString();
                            double c = b["conf"].ToDouble();
                            PointListItem pli = new PointListItem(x, y, l, c);
                            pl.Add(pli);
                        }

                        double start = i / annoList.Scheme.SampleRate;
                        double dur = 1 / annoList.Scheme.SampleRate;

                        AnnoListItem ali = new AnnoListItem(start, dur, label, "", annoList.Scheme.MinOrBackColor, confidence, AnnoListItem.TYPE.POINT, pl);
                        annoList.Add(ali);
                    }
                }
                else if(annoList.Scheme.Type == AnnoScheme.TYPE.POLYGON || annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                {
                    BsonArray schemelabels = null;
                    annoList.Scheme.Labels.Clear();

                    if (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                    {
                        annoList.Scheme.Labels = new List<AnnoScheme.Label>();
                        schemelabels = scheme["labels"].AsBsonArray;

                        for (int j = 0; j < schemelabels.Count; j++)
                        {
                            try
                            {
                                if (schemelabels[j]["isValid"].AsBoolean == true) annoList.Scheme.Labels.Add(new AnnoScheme.Label(schemelabels[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString())));
                            }
                            catch
                            {
                                annoList.Scheme.Labels.Add(new AnnoScheme.Label(schemelabels[j]["name"].ToString(), (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString())));
                            }
                        }
                    }

                    double start = 0.0;

                    if (scheme.TryGetElement("sr", out value))
                        annoList.Scheme.SampleRate = double.Parse(value.Value.ToString());
                    if (scheme.TryGetElement("default-label", out value))
                        annoList.Scheme.DefaultLabel = value.Value.ToString();
                    if (scheme.TryGetElement("default-label-color", out value))
                        annoList.Scheme.DefaultColor = (Color)ColorConverter.ConvertFromString(value.Value.ToString());
                    if (scheme.TryGetElement("color", out value))
                        annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(value.Value.ToString());

                    foreach (BsonDocument entry in labels)
                    {
                        string frameName = "Frame " + entry["name"].AsString;
                        List<PolygonLabel> polygonLabels = new List<PolygonLabel>();

                        BsonArray polygons = entry["polygons"].AsBsonArray;

                        if (polygons.Count > 0)
                        {
                            foreach (BsonDocument polygon in polygons)
                            {
                                String label = "";
                                Color labelColor = Colors.Black;

                                if (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                                {
                                    for (int j = 0; j < schemelabels.Count; j++)
                                    {
                                        if (polygon["label"].AsInt32 == schemelabels[j]["id"].AsInt32)
                                        {
                                            label = schemelabels[j]["name"].ToString();
                                            labelColor = (Color)ColorConverter.ConvertFromString(schemelabels[j]["color"].ToString());
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    label = polygon["label"].ToString();
                                    labelColor = (Color)ColorConverter.ConvertFromString(polygon["label_color"].ToString());
                                }

                                BsonArray b_points = polygon["points"].AsBsonArray;
                                List<PolygonPoint> points = new List<PolygonPoint>();
                                
                                if(b_points.Count > 0)
                                {
                                    foreach (BsonDocument point in b_points)
                                    {
                                        points.Add(new PolygonPoint(point["x"].ToDouble(), point["y"].ToDouble()));
                                    }

                                    polygonLabels.Add(new PolygonLabel(points, label, labelColor));
                                }
                            }
                        }

                        const double defaultConfidence = 1.0;
                        annoList.Add(new AnnoListItem(start, 1 / annoList.Scheme.SampleRate, frameName, "", annoList.Scheme.MinOrBackColor, defaultConfidence, AnnoListItem.TYPE.POLYGON, polygonList: new PolygonList(polygonLabels)));
                        start += 1 / annoList.Scheme.SampleRate;
                    }
                }
            }
        }





        public static double convertRange(
            double originalStart, double originalEnd, // original range
            double newStart, double newEnd, // desired range
            double value) // value to convert
        {
            double scale = (double)(newEnd - newStart) / (originalEnd - originalStart);
            return (double)(newStart + ((value - originalStart) * scale));
        }





        public static void resampleAnnotationtoNewScheme(AnnoList al, AnnoScheme newScheme, AnnoScheme oldScheme)
        {
            bool isSaved = false;
            AnnoList newList = new AnnoList();


            

            double new_Samplerate = newScheme.SampleRate;
            double old_samplerate = oldScheme.SampleRate;

            if (old_samplerate > new_Samplerate)
            {
                double factor = old_samplerate / new_Samplerate;
              
                int index = 0;
                double mean = 0;
                double meanconf = 0;
                double start = double.MaxValue;
                foreach (AnnoListItem ali in al)
                {
                    if (index < factor-1)
                    {
                        mean += ali.Score;
                        meanconf += ali.Confidence;
                        start = ali.Start < start ? ali.Start : start;
                        index++;
                    }

                    else
                    {

                        mean += ali.Score;
                        meanconf += ali.Confidence;
                        start = ali.Start < start ? ali.Start : start;
                        index++;
                    
                        double duration = 1000.0 / new_Samplerate;
                        double score = mean / factor;
                        double conf = meanconf / factor;


                        if(oldScheme.MinScore != newScheme.MinScore || oldScheme.MaxScore != newScheme.MaxScore)
                        {
                            score = convertRange(oldScheme.MinScore, oldScheme.MaxScore, newScheme.MinScore, newScheme.MaxScore, score);
                        }
                       

                        AnnoListItem newali = new AnnoListItem(start, duration, score.ToString(), ali.Meta, ali.Color, conf);
                        newList.Add(newali);
                        start = start + duration;
                        index = 0;
                        mean = 0;
                        meanconf = 0;

                    }    
                }
            }


            else if (old_samplerate < new_Samplerate)
            {
                double factor = new_Samplerate/old_samplerate;
                double duration = 1000.0 / new_Samplerate;

                foreach (AnnoListItem ali in al)
                {
                    double start = ali.Start;
                   
                    double score = ali.Score;
                    if (oldScheme.MinScore != newScheme.MinScore || oldScheme.MaxScore != newScheme.MaxScore)
                    {
                        score = convertRange(oldScheme.MinScore, oldScheme.MaxScore, newScheme.MinScore, newScheme.MaxScore, score);
                    }
                    double conf = ali.Confidence;
                    string meta = ali.Meta;
                    Color color = ali.Color;
                    for (int i=0; i< factor; i++)
                    {
                        AnnoListItem newal = new AnnoListItem(start, duration, score.ToString(), meta, color, conf);
                        start = start + duration;
                        newList.Add(newal);
                    } 
  
                }
            }

            else if(old_samplerate == new_Samplerate)
            {
                foreach (AnnoListItem ali in al)
                {
                    if (oldScheme.MinScore != newScheme.MinScore || oldScheme.MaxScore != newScheme.MaxScore)
                    {
                        double score = convertRange(oldScheme.MinScore, oldScheme.MaxScore, newScheme.MinScore, newScheme.MaxScore, ali.Score);
                        AnnoListItem newal = new AnnoListItem(ali.Start, ali.Duration, score.ToString(), ali.Meta, ali.Color, ali.Confidence);
                        newList.AddSorted(newal);
                    }
                    else
                    {
                        newList.AddSorted(ali);
                    }
                   
                }
            }

            newList.Scheme = newScheme;
            newList.Meta = al.Meta;
            newList.Source.StoreToDatabase = true;
            newList.Source.Database.Session = al.Source.Database.Session;
            newList.HasChanged = true;

            if (newList != null)
            {
                isSaved = newList.Save(null, false, true);
            }
        }


        public static AnnoList LoadAnnoList(string Scheme, string Session, string Role, string Annotator, bool loadBackup)
        {
            var builder = Builders<BsonDocument>.Filter;

            // resolve references

            ObjectId roleID = GetObjectID(DatabaseDefinitionCollections.Roles, "name", Role);
            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, "name", Scheme);
            ObjectId annotatorID = GetObjectID(DatabaseDefinitionCollections.Annotators, "name", Annotator);
            ObjectId sessionID = GetObjectID(DatabaseDefinitionCollections.Sessions, "name", Session);

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationsData = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.AnnotationData);
            var filterAnnotation = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("session_id", sessionID);
            var annotationDocs = annotations.Find(filterAnnotation).ToList();

            // does annotation exist?

            if (annotationDocs.Count > 0)
            {
                AnnoList annoList = new AnnoList();
                BsonDocument annotationDoc = annotationDocs[0];

                ObjectId dataID = annotationDoc["data_id"].AsObjectId;
                ObjectId dataBackupID = AnnoSource.DatabaseSource.ZERO;
                if (annotationDoc.Contains("data_backup_id"))
                {
                    dataBackupID = annotationDoc["data_backup_id"].AsObjectId;
                }

                if (loadBackup && dataBackupID != AnnoSource.DatabaseSource.ZERO)
                {
                    // in case backup is loaded we swap data ids first

                    ObjectId tmp = dataBackupID;
                    dataBackupID = dataID;
                    dataID = tmp;

                    var updateDataIds = Builders<BsonDocument>.Update
                        .Set("data_id", dataID)
                        .Set("data_backup_id", dataBackupID);

                    annotations.UpdateOne(filterAnnotation, updateDataIds);
                }

                var schemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                var filterScheme = builder.Eq("_id", schemeID);
                BsonDocument scheme = schemes.Find(filterScheme).Single();

                // update meta

                annoList.Meta.Annotator = Annotator;
                annoList.Meta.AnnotatorFullName = GetUserInfo(Annotator).Fullname;
                annoList.Meta.Role = Role;
               // annoList.Meta.isLocked = IsLocked;
               // annoList.Meta.isFinished = IsFinished;

                // load scheme and data

                var filterData = builder.Eq("_id", dataID);
                var annotationDataDoc = annotationsData.Find(filterData).ToList();
                if (annotationDataDoc.Count > 0) loadAnnoListSchemeAndData(ref annoList, scheme, annotationDataDoc[0]);

                // update source

                annoList.Source.Database.OID = annotationDoc["_id"].AsObjectId;
                annoList.Source.Database.DataOID = dataID;
                annoList.Source.Database.DataBackupOID = dataBackupID;
                annoList.Source.Database.Session = Session;

                return annoList;
            }

            return null;
        }

        public static AnnoList LoadAnnoList(DatabaseAnnotation annotation, bool loadBackup)
        {
            var builder = Builders<BsonDocument>.Filter;

            // resolve references

            ObjectId roleID = GetObjectID(DatabaseDefinitionCollections.Roles, "name", annotation.Role);
            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, "name", annotation.Scheme);
            ObjectId annotatorID = GetObjectID(DatabaseDefinitionCollections.Annotators, "name", annotation.Annotator);
            ObjectId sessionID = GetObjectID(DatabaseDefinitionCollections.Sessions, "name", annotation.Session);

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationsData = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.AnnotationData);
            var filterAnnotation = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("session_id", sessionID);
            var annotationDocs = annotations.Find(filterAnnotation).ToList();

            // does annotation exist?

            if (annotationDocs.Count > 0)
            {
                AnnoList annoList = new AnnoList();
                BsonDocument annotationDoc = annotationDocs[0];

                ObjectId dataID = annotationDoc["data_id"].AsObjectId;
                ObjectId dataBackupID = AnnoSource.DatabaseSource.ZERO;
                if (annotationDoc.Contains("data_backup_id"))
                {
                    dataBackupID = annotationDoc["data_backup_id"].AsObjectId;
                }

                if (loadBackup && dataBackupID != AnnoSource.DatabaseSource.ZERO)
                {
                    // in case backup is loaded we swap data ids first

                    ObjectId tmp = dataBackupID;
                    dataBackupID = dataID;
                    dataID = tmp;

                    var updateDataIds = Builders<BsonDocument>.Update
                        .Set("data_id", dataID)
                        .Set("data_backup_id", dataBackupID);

                    annotations.UpdateOne(filterAnnotation, updateDataIds);
                }

                var schemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
                var filterScheme = builder.Eq("_id", schemeID);
                BsonDocument scheme = schemes.Find(filterScheme).Single();

                // update meta

                annoList.Meta.Annotator = annotation.Annotator;
                annoList.Meta.AnnotatorFullName = annotation.AnnotatorFullName;
                annoList.Meta.Role = annotation.Role;
                annoList.Meta.isLocked = annotation.IsLocked;
                annoList.Meta.isFinished = annotation.IsFinished;

                // load scheme and data

                var filterData = builder.Eq("_id", dataID);
                var annotationDataDoc = annotationsData.Find(filterData).ToList();
                if (annotationDataDoc.Count > 0) loadAnnoListSchemeAndData(ref annoList, scheme, annotationDataDoc[0]);

                // update source

                annoList.Source.Database.OID = annotationDoc["_id"].AsObjectId;
                annoList.Source.Database.DataOID = dataID;
                annoList.Source.Database.DataBackupOID = dataBackupID;
                annoList.Source.Database.Session = annotation.Session;

                return annoList;
            }

            return null;
        }

        public static List<DatabaseAnnotation> GetAnnotations(DatabaseScheme scheme, DatabaseRole role, DatabaseAnnotator annotator)
        {
            FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
            FilterDefinition<BsonDocument> filter = builder.Eq("scheme_id", scheme.Id) & builder.Eq("role_id", role.Id) & builder.Eq("annotator_id", annotator.Id);
            return GetAnnotations(filter);
        }

        public static List<DatabaseAnnotation> GetAnnotations(AnnoScheme scheme)
        {
            FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, "name", scheme.Name);
            FilterDefinition<BsonDocument> filter = builder.Eq("scheme_id", schemeID);
            return GetAnnotations(filter);
        }

        public static List<DatabaseAnnotation> GetAnnotations(FilterDefinition<BsonDocument> filter = null, bool onlyMe = false, bool onlyUnfinished = false)
        {
            List<BsonDocument> annotations;

            if (filter != null)
            {
                annotations = Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations).Find(filter).ToList();
            }
            else
            {
                annotations = GetCollection(DatabaseDefinitionCollections.Annotations, false);
            }

            List<DatabaseAnnotation> items = new List<DatabaseAnnotation>();

            foreach (var annotation in annotations)
            {
                ObjectId id = annotation["_id"].AsObjectId;

                ObjectId sessionid = annotation["session_id"].AsObjectId;
                ObjectId roleid = annotation["role_id"].AsObjectId;
                ObjectId schemeid = annotation["scheme_id"].AsObjectId;
                ObjectId annotatorid = annotation["annotator_id"].AsObjectId;
                BsonElement value;
                bool isPaid = true;
                if (annotation.TryGetElement("bountyIsPaid", out value))
                {
                    isPaid = annotation["bountyIsPaid"].AsBoolean;
                }

                DatabaseSession session = sessions.Find(s => s.Id == sessionid);
                DatabaseRole role = Roles.Find(r => r.Id == roleid);
                DatabaseScheme scheme = schemes.Find(s => s.Id == schemeid);
                DatabaseAnnotator annotator = annotators.Find(a => a.Id == annotatorid);

                if (session == null || role == null || scheme == null || annotator == null)
                {
                    continue;
                }

                string sessionName = session.Name;
                string roleName = role.Name;
                string schemeName = scheme.Name;

                string annotatorName = annotator.Name;
                string annotatorFullName = annotator.FullName;

                bool isFinished = false;
                if (annotation.Contains("isFinished"))
                {
                    isFinished = annotation["isFinished"].AsBoolean;
                }

                bool islocked = false;
                if (annotation.Contains("isLocked"))
                {
                    islocked = annotation["isLocked"].AsBoolean;
                }

                DateTime date = DateTime.Today;
                if (annotation.Contains("date"))
                {
                    date = annotation["date"].ToUniversalTime();
                }

                bool isOwner = Properties.Settings.Default.MongoDBUser == annotatorName || DatabaseHandler.CheckAuthentication() >= DatabaseAuthentication.DBADMIN;

                if (!onlyMe && !onlyUnfinished ||
                   onlyMe && !onlyUnfinished && Properties.Settings.Default.MongoDBUser == annotatorName ||
                   !onlyMe && onlyUnfinished && !isFinished ||
                   onlyMe && onlyUnfinished && !isFinished && Properties.Settings.Default.MongoDBUser == annotatorName)
                {
                    DatabaseAnnotation anno = new DatabaseAnnotation() { Id = id, Role = roleName, Scheme = schemeName, Annotator = annotatorName, AnnotatorFullName = annotatorFullName, Session = sessionName, IsFinished = isFinished, IsLocked = islocked, Date = date, IsOwner = isOwner };
                   
                    if (isPaid || Properties.Settings.Default.MongoDBUser == annotatorName) items.Add(anno);
                }
            }

            return items;
        }

        #endregion Annotation


   
             public static List<AnnoList> LoadSession(System.Collections.IList collections, System.Collections.IList sessions)
        {
            if (!IsConnected)
            {
                return null;
            }

            List<AnnoList> annoLists = new List<AnnoList>();

            foreach (DatabaseSession session in sessions)
            {
                foreach (DatabaseAnnotation annotation in collections)
                {
                    AnnoList annoList = LoadAnnoList(annotation.Scheme, session.Name, annotation.Role, annotation.Annotator, false);
                    if (annoList != null)
                    {
                        annoLists.Add(annoList);
                    }
                }
            }

          

            return annoLists;
        }
        public static List<AnnoList> LoadSession(System.Collections.IList collections)
        {
            if (!IsConnected)
            {
                return null;
            }

            List<AnnoList> annoLists = new List<AnnoList>();

            foreach (DatabaseAnnotation annotation in collections)
            {
                AnnoList annoList = LoadAnnoList(annotation, false);
                if (annoList != null)
                {
                    annoLists.Add(annoList);
                }
            }

            return annoLists;
        }

        public static string FetchDBRef(string collection, string attribute, ObjectId reference)
        {
            if (!IsConnected)
            {
                return null;
            }

            string output = "";
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", reference);
            var result = database.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                output = result[0][attribute].ToString();
            }

            return output;
        }


        public static string FetchDBRef(string collection, IMongoDatabase db, string attribute, ObjectId reference)
        {
            if (!IsConnected)
            {
                return null;
            }

            string output = "";
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", reference);
            var result = db.GetCollection<BsonDocument>(collection).Find(filter).ToList();

            if (result.Count > 0)
            {
                output = result[0][attribute].ToString();
            }

            return output;
        }

        public static ObjectId GetObjectID(string collection, string value, string attribute)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq(value, attribute);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0) id = result[0].GetValue(0).AsObjectId;

            return id;
        }

        public static ObjectId GetObjectID(string collection, IMongoDatabase db, string value, string attribute)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq(value, attribute);
            var result = db.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0) id = result[0].GetValue(0).AsObjectId;

            return id;
        }
    }

    #region DATABASE TYPES

    public enum UrlFormat
    {
        GENERAL = 0,
        NEXTCLOUD = 1
    }

    public class DatabaseDBMeta
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Server { get; set; }
        public bool ServerAuth { get; set; }
        public UrlFormat UrlFormat { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseSession
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string Language { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }

        public double Duration { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseUser
    {
        public string Name { get; set; }
        public string Password { get; set; }

        public string Fullname { get; set; }

        public string Email { get; set; }

        public int Expertise { get; set; }

        //key for Lightning
        public string ln_admin_key { get; set; }
        public string ln_invoice_key { get; set; }
        public string ln_wallet_id{ get; set; }
        public string ln_user_id { get; set; }




        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseRole
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public bool HasStreams { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseScheme
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public AnnoScheme.TYPE Type { get; set; }
        public double SampleRate { get; set; }
 

        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseBounty
    {
        public ObjectId OID { get; set; }
        public int valueInSats { get; set; }
        public DatabaseUser Contractor { get; set; }
        public string Scheme { get; set; }
        public string Role { get; set; }
        public string Session { get; set; }
        public int numOfAnnotations { get; set; }
        public int numOfAnnotationsNeededCurrent { get; set; }
        public string Database { get; set; }
        public string Type { get; set; }
        public List<DatabaseUser> annotatorsJobCandidates { get; set; }
        public List<DatabaseUser> annotatorsJobDone { get; set; }
        public List<StreamItem> streams { get; set; }

    }


    public class DatabaseStream
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string FileExt { get; set; }
        public string Type { get; set; }
        public double SampleRate { get; set; }
        public double Duration { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseAnnotator
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public int Expertise { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class DatabaseAnnotation
    {
        public ObjectId Id { get; set; }

        public string Role { get; set; }

        public string Scheme { get; set; }

        public string Annotator { get; set; }

        public string AnnotatorFullName { get; set; }

        public string Session { get; set; }

        public bool IsOwner { get; set; }

        public bool IsFinished { get; set; }

        public bool IsLocked { get; set; }

        public DateTime Date { get; set; }

        public ObjectId Data_id { get; set; }
    }

    public static class DatabaseDefinitionCollections
    {
        public static string Annotations = "Annotations";
        public static string AnnotationData = "AnnotationData";
        public static string Annotators = "Annotators";
        public static string Sessions = "Sessions";
        public static string Roles = "Roles";
        public static string Subjects = "Subjects";
        public static string Streams = "Streams";
        public static string Schemes = "Schemes";
        public static string Meta = "Meta";
        public static string Bounties = "Bounties";
    }

    public enum DatabaseAuthentication
    {
        NONE = 0,
        READ = 1,
        READWRITE = 2,
        DBADMIN = 3,
        ROOT = 4,
    }

    #endregion DATABASE TYPES
}
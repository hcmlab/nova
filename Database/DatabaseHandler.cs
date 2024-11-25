using CsvHelper;
using CsvHelper.Configuration;
using Dicom;
using DnsClient;
using MongoDB.Bson;
using MongoDB.Driver;
using Octokit;
using ssi.Controls.Annotation.Polygon;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Web.UI.DataVisualization.Charting;
using System.Windows;
using System.Windows.Media;
using static System.Runtime.InteropServices.Marshal;

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
            get { return "Session [" + (IsSession ? sessionName.Replace("_", "_") : "none") + "]"; }
        }

        public static bool Connect()
        {
            return Connect(Properties.Settings.Default.MongoDBUser, Properties.Settings.Default.MongoDBPass, Properties.Settings.Default.DatabaseAddress);
        }

        public static bool Connect(string user, string password, string address)
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



            int count = 0;

            try
            {

                client = Client;
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


                var adminDB = client.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", user);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);

            }
            catch
            {
                client = null;
                return false;
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
            get { return client != null && client.Cluster.Description.State.ToString() != "Disconnected"; }
        }

        public static MongoClient Client
        {
            get
            {

                if (client == null)
                {

                    clientAddress = clientAddress.Replace(" ", "");
                    MongoClientSettings settings = MongoClientSettings.FromUrl(new MongoUrl(clientAddress));

                    settings.ReadEncoding = new UTF8Encoding(false, throwOnInvalidBytes);

                    client = new MongoClient(settings);

                }


                return client;
            }
        }


        public static void reconnectClient()
        {
            if (client != null)
            {

                // client.Cluster.Dispose();
                // client.Cluster.StartSession();

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
            catch (Exception e)
            {
            }

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

            List<string> databases = GetDatabases();
            return databases.Any(s => name.Equals(s));
        }

        public static List<string> GetDatabases(string username = "", DatabaseAuthentication level = DatabaseAuthentication.READWRITE)
        {
            List<string> items = new List<string>();

            if (username == "") username = Properties.Settings.Default.MongoDBUser; 

            if (IsConnected)
            {


                if (CheckAuthentication(username, "admin") >= 3)
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

                else
                {

                    string user = username;
                    try
                    {
                        var adminDB = client.GetDatabase("admin");
                        var cmd = new BsonDocument("usersInfo", user);
                        var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                        var roles = (BsonArray)queryResult[0][0]["roles"];

                        for (int i = 0; i < roles.Count; i++)
                        {

                            if (roles[i]["db"].ToString() != "admin" && roles[i]["db"].ToString() != "local" && roles[i]["db"].ToString() != "config" && !items.Contains(roles[i]["db"].ToString())  && CheckAuthentication(roles[i]["db"].ToString()) >= (int)level)
                            {
                                items.Add(roles[i]["db"].ToString());
                            }

                        }
                    }
                    catch
                    { }

                    items.Sort();
                }


                return items;
            }
            else return new List<string>();
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

        public static bool GetObjectName(ref string name, IMongoDatabase db, string collection, ObjectId id)
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

        public static bool AnnotationExists(ObjectId annotationId)
        {
            if (!IsConnected)
            {
                return false;
            }

            var builder = Builders<BsonDocument>.Filter;

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);

            var filterAnnotation = builder.Eq("_id", annotationId);
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
            ReplaceOptions updateOptions = new ReplaceOptions();
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
            string temp = databaseName;
            ChangeDatabase(name);

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

            List<DatabaseAnnotator> annotators = GetAnnotators();
            foreach(DatabaseAnnotator annotator in annotators)
            {

                DatabaseHandler.DeleteAnnotator(annotator.Name, name);
            }
           

            if (name == databaseName)
            {
                databaseName = null;
                database = null;
            }

            Client.DropDatabase(name);
            ChangeDatabase(temp);
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
                        new BsonDocument { { "role", "changeOwnPasswordCustomDataRole" }, { "db", "admin" } },
                    } } };
            }

            user.ln_admin_key = "";
            user.ln_invoice_key = "";
            user.ln_user_id = "";
            user.ln_wallet_id = "";
            user.ln_addressname = "";
            user.ln_addresspin = "";
            user.XP = 0;
            user.ratingcount = 0;
            user.ratingoverall = 0;

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



            dbuser = GetUserInfo(dbuser);
            if (dbuser.ln_admin_key != "" && dbuser.ln_admin_key != null && username  == Properties.Settings.Default.MongoDBUser)
            {
                dbuser.ln_admin_key = MainHandler.Cipher.AES.DecryptText(dbuser.ln_admin_key, MainHandler.Decode(Properties.Settings.Default.MongoDBPass));
            }

            if (dbuser.ln_admin_key_locked != "" && dbuser.ln_admin_key_locked != null && username == Properties.Settings.Default.MongoDBUser)
            {
                dbuser.ln_admin_key_locked = MainHandler.Cipher.AES.DecryptText(dbuser.ln_admin_key_locked, MainHandler.Decode(Properties.Settings.Default.MongoDBPass));
            }

            return dbuser;
        }

        public static DatabaseUser GetUserInfo(DatabaseUser dbuser)
        {
            var adminDB = client.GetDatabase("admin");
            var cmd = new BsonDocument("usersInfo", dbuser.Name);
            var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
            BsonDocument Customdata = new BsonDocument();
            if (queryResult != null)
            {
                try
                {
                    Customdata = (BsonDocument)queryResult[0][0]["customData"];
                }
                catch { }

            }

                if (Customdata.Contains("fullname"))
                    dbuser.Fullname = Customdata["fullname"].ToString();
                else dbuser.Fullname = dbuser.Name;
    
                if (Customdata.Contains("email"))
                    dbuser.Email = Customdata["email"].ToString();
                else dbuser.Email = "";

                if (Customdata.Contains("expertise"))
                    dbuser.Expertise = Customdata["expertise"].AsInt32;
                else dbuser.Expertise = 0;

                if (Customdata.Contains("xp"))
                    dbuser.XP = Customdata["xp"].AsInt32;
                else dbuser.XP = 0;

                if (Customdata.Contains("ratingoverall"))
                    dbuser.ratingoverall = Customdata["ratingoverall"].AsDouble;
                else dbuser.ratingoverall = 0;

                if (Customdata.Contains("ratingcount"))
                    dbuser.ratingcount = Customdata["ratingcount"].AsInt32;
                else dbuser.ratingcount = 0;


            if (Customdata.Contains("ratingContractoroverall"))
                dbuser.ratingContractoroverall = Customdata["ratingContractoroverall"].AsDouble;
            else dbuser.ratingContractoroverall = 0;

            if (Customdata.Contains("ratingContractorcount"))
                dbuser.ratingContractorcount = Customdata["ratingContractorcount"].AsInt32;
            else dbuser.ratingContractorcount = 0;



            if (Customdata.Contains("ln_invoice_key"))
                    dbuser.ln_invoice_key = Customdata["ln_invoice_key"].ToString();
                else dbuser.ln_invoice_key = "";

                if (Customdata.Contains("ln_admin_key"))
                {

                string ln_admin_key = Customdata["ln_admin_key"].ToString();
                if (ln_admin_key == "Length of the data to decrypt is invalid." || ln_admin_key.StartsWith("The input is not a valid Base-64"))
                    dbuser.ln_admin_key = "";
                else
                    dbuser.ln_admin_key = ln_admin_key;
                }
                else dbuser.ln_admin_key = "";


                if (Customdata.Contains("ln_wallet_id"))
                    dbuser.ln_wallet_id = Customdata["ln_wallet_id"].ToString();
                else dbuser.ln_wallet_id = "";

            if (Customdata.Contains("ln_invoice_key_locked"))
                dbuser.ln_invoice_key_locked = Customdata["ln_invoice_key_locked"].ToString();
            else dbuser.ln_invoice_key_locked = "";

            if (Customdata.Contains("ln_admin_key_locked"))
            {

                string ln_admin_key_locked = Customdata["ln_admin_key_locked"].ToString();
                if (ln_admin_key_locked == "Length of the data to decrypt is invalid." || ln_admin_key_locked.StartsWith("The input is not a valid Base-64"))
                    dbuser.ln_admin_key_locked = "";
                else
                    dbuser.ln_admin_key_locked = ln_admin_key_locked;
            }
            else dbuser.ln_admin_key_locked = "";


            if (Customdata.Contains("ln_wallet_id_locked"))
                dbuser.ln_wallet_id_locked = Customdata["ln_wallet_id_locked"].ToString();
            else dbuser.ln_wallet_id_locked = "";

            if (Customdata.Contains("ln_user_id"))
                    dbuser.ln_user_id = Customdata["ln_user_id"].ToString();
                else dbuser.ln_user_id = "";

                if(Customdata.Contains("ln_addresspin"))
                dbuser.ln_addresspin = Customdata["ln_addresspin"].ToString();
                else dbuser.ln_addresspin = "";

                if (Customdata.Contains("ln_addressname"))
                    dbuser.ln_addressname = Customdata["ln_addressname"].ToString();
                else dbuser.ln_addressname = "";

            return dbuser;
        }

        public static bool ChangeUserCustomData(DatabaseUser user)
        {
            if (!IsConnected)
            {
                return false;
            }

            var database = Client.GetDatabase("admin");
            var updatecustomdata = new BsonDocument { { "updateUser", user.Name }, { "customData", new BsonDocument { { "fullname", user.Fullname }, { "email", user.Email }, { "expertise", user.Expertise }, 
                { "ln_admin_key", user.ln_admin_key }, { "ln_invoice_key", user.ln_invoice_key }, { "ln_wallet_id", user.ln_wallet_id }, 
                { "ln_admin_key_locked", user.ln_admin_key_locked }, { "ln_invoice_key_locked", user.ln_invoice_key_locked }, { "ln_wallet_id_locked", user.ln_wallet_id_locked },
                { "ln_user_id", user.ln_user_id }, { "ln_addressname", user.ln_addressname }, { "ln_addresspin", user.ln_addresspin },
                {"xp", user.XP }, { "ratingoverall", user.ratingoverall}, { "ratingcount", user.ratingcount},
                { "ratingContractoroverall", user.ratingContractoroverall}, { "ratingContractorcount", user.ratingContractorcount}} } };
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
            ReplaceOptions updateOptions = new ReplaceOptions();
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


        public static bool DeleteAnnotator(string name, string databasename)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!UserExists(name))
            {
                return false;
            }

            try
            {
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filter).Single();
            string user = result["name"].AsString;
           
                var del = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).DeleteOne(filter);
            }
            catch
            {
                Console.Write("Database does not exist, remove access");
            }
           

            //annotators = GetAnnotators();

            RevokeUserRole(name, "read", databasename);
            RevokeUserRole(name, "readWrite", databasename);
            RevokeUserRole(name, "dbAdmin", databasename);

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
            ReplaceOptions updateOptions = new ReplaceOptions();
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
                    stream.DimLabels = new Dictionary<int, string>();
                    if (document.TryGetElement("dimlabels", out value))
                    {
                        BsonArray array = document["dimlabels"].AsBsonArray;

                        foreach (var element in array)
                        {
                            try
                            {
                                stream.DimLabels.Add(element["id"].AsInt32, element["name"].AsString);
                            }

                            catch
                            {
                                stream.DimLabels.Add(Int32.Parse(element["id"].AsString), element["name"].AsString);
                            }
                        }

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
            BsonArray array = new BsonArray();
            if (stream.DimLabels == null)
            {
                stream.DimLabels = new Dictionary<int, string>();
            }
            foreach (var item in stream.DimLabels)
            {
                array.Add(new BsonDocument() {
                    { "id", item.Key },
                    { "name", item.Value }
                    }
                  );
            }




            BsonDocument document = new BsonDocument {
                    {"name",  stream.Name},
                    {"fileExt",  stream.FileExt},
                    {"type",  stream.Type},
                    {"isValid",  true},
                    {"sr", stream.SampleRate },
                    {"dimlabels", array }
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            ReplaceOptions updateOptions = new ReplaceOptions();
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
            BsonElement documentDescription = new BsonElement("description", scheme.Description);
 
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
            document.Add(documentDescription);
            document.Add(documentType);


            //add attriutes to scheme.
            BsonArray attributes = new BsonArray();
            foreach (AnnoScheme.Attribute attribute in scheme.LabelAttributes)
            {

                BsonArray values = new BsonArray();
                foreach(string value in attribute.Values)
                {
                    values.Add(new BsonDocument() {
                    { "value", value},
                    });
                }

                attributes.Add(new BsonDocument() {
                    { "name", attribute.Name },
                    { "type", attribute.AttributeType.ToString() },
                    { "values", values },
                    { "isValid", true } });
            }



            document.Add("attributes", attributes);


            BsonArray examples = new BsonArray();
            foreach (AnnoScheme.Example example in scheme.Examples)
            {

                examples.Add(new BsonDocument() {
                    { "value", example.Value },
                    { "label", example.Annotation },
                    { "isValid", true } });
            }



            document.Add("examples", examples);

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
                int index = 1;
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
            else if (scheme.Type == AnnoScheme.TYPE.POLYGON)
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
            ReplaceOptions update = new ReplaceOptions();
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

                if (annoSchemeDocument.Contains("attributes"))
                {
                    BsonArray attributesArray = annoSchemeDocument["attributes"].AsBsonArray;
                    foreach(BsonDocument doc in attributesArray)
                    {
                        List<string> values = new List<string>();
                        BsonArray bsonvalues = doc["values"].AsBsonArray;
                        AnnoScheme.AttributeTypes type = AnnoScheme.AttributeTypes.LIST;
                        foreach (BsonDocument value in bsonvalues)
                        {
                            values.Add(value["value"].ToString());
                        }
                        if (values[0].ToLower().Contains("true") || values[0].ToLower().Contains("false")){
                           type = AnnoScheme.AttributeTypes.BOOLEAN;
                        } 
                        if (values.Count == 1){
                            type = AnnoScheme.AttributeTypes.STRING;
                        }
                       
                        AnnoScheme.Attribute attr = new AnnoScheme.Attribute(doc["name"].ToString(), values, type);
                        scheme.LabelAttributes.Add(attr);
                    }
                }


                if (annoSchemeDocument.Contains("examples") && annoSchemeDocument["examples"] != "")
                {
                    BsonArray examplesArray = annoSchemeDocument["examples"].AsBsonArray;
                    foreach (BsonDocument doc in examplesArray)
                    {

                        AnnoScheme.Example attr = new AnnoScheme.Example(doc["value"].ToString(), doc["label"].ToString());
                        scheme.Examples.Add(attr);
                    }
                }



                scheme.Name = annoSchemeDocument["name"].ToString();
                if (annoSchemeDocument.Contains("description"))
                {
                    scheme.Description = annoSchemeDocument["description"].ToString();
                }
                else scheme.Description = "";

       


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
                else if (scheme.Type == AnnoScheme.TYPE.POLYGON || scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                {
                    scheme.SampleRate = annoSchemeDocument["sr"].ToDouble();
                    scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["color"].ToString());
                    scheme.DefaultColor = (Color)ColorConverter.ConvertFromString(annoSchemeDocument["default-label-color"].ToString());
                    scheme.DefaultLabel = annoSchemeDocument["default-label"].ToString();

                    if (scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
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
            catch (Exception ex)
            {
                return false;
            }

        }


        public static bool DeleteSchemeIfNoAnnoExists(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SchemeExists(name))
            {
                return false;
            }

            var builder = Builders<BsonDocument>.Filter;
            var schemeCollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var filter = builder.Eq("name", name);
            var schemeResult = schemeCollection.Find(filter).ToList();
            ObjectId schemeID = ((BsonDocument)schemeResult[0])["_id"].AsObjectId;

            var annoCollection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var filterData = builder.Eq("scheme_id", schemeID);
            var collections = annoCollection.Find(filterData).ToList();

            if (collections.Count == 0)
            {
                MessageBoxResult mbres = MessageBox.Show("You try to delete the scheme \"" + name + "\". The deletion process cannot be undone. Delete anyway?", "Attention", MessageBoxButton.YesNo);
                if (mbres == MessageBoxResult.Yes)
                {
                    var deleteFilter = Builders<BsonDocument>.Filter.Eq("_id", schemeID);
                    schemeCollection.DeleteOne(filter);

                    schemes = GetSchemes();

                    return true;
                }
                else
                    return false;

            }
            else
            {
                MessageBox.Show("It is not possible to delete a scheme that is still used as an annotations. Please delete the annotations that are based on this scheme (DATABASE ➙ Administration ➙ Manage Annotations ➙ Remove the mentioned annotations).", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
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
            ReplaceOptions updateOptions = new ReplaceOptions();
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

        private static BsonArray BountyListToBsonArray(List<BountyJob> annotators, int contractvalue)
        {
            BsonArray data = new BsonArray();
            for (int i = 0; i < annotators.Count; i++)
            {
                data.Add(new BsonDocument { { "name", annotators[i].user.Name }, { "status", annotators[i].status }, { "value", contractvalue }, /*{ "LNURLW", annotators[i].LNURLW }, { "pickedLNURL", annotators[i].pickedLNURL },*/ { "rating", annotators[i].rating }, { "ratingContractor", annotators[i].ratingContractor } });
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

        public static BsonArray AnnoListToBsonArray(AnnoList annoList, BsonDocument schemeDoc)
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
                    data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "name", annoList[i].Label }, { "conf", annoList[i].Confidence }, { "color", annoList[i].Color.ToString() }, { "meta", annoList[i].Meta } });
                }
            }
            else if (schemeType == AnnoScheme.TYPE.CONTINUOUS)
            {
                for (int i = 0; i < annoList.Count; i++)
                {
                    data.Add(new BsonDocument { { "score", annoList[i].Score }, { "conf", annoList[i].Confidence } });
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
            else if (schemeType == AnnoScheme.TYPE.POLYGON || schemeType == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                BsonArray polygons;
                BsonDocument polygon = null;
                BsonArray points;
                BsonDocument point;

                foreach (AnnoListItem item in annoList)
                {
                    polygons = new BsonArray();

                    foreach (PolygonLabel label in item.PolygonList.Polygons)
                    {
                        if (schemeType == AnnoScheme.TYPE.DISCRETE_POLYGON)
                        {
                            BsonArray labels = schemeDoc["labels"].AsBsonArray;
                            int index = 1;
                            for (int j = 0; j < labels.Count; j++)
                            {
                                if (label.Label == labels[j]["name"].ToString())
                                {
                                    index = labels[j]["id"].AsInt32;
                                    polygon = new BsonDocument
                                    {
                                        new BsonElement("label", index),
                                        new BsonElement("confidence", label.Confidence)
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
                                new BsonElement("label_color", new SolidColorBrush(label.Color).Color.ToString()),
                                new BsonElement("confidence", label.Confidence)
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


            ReplaceOptions newAnnotationDocUpdate = new ReplaceOptions();
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
                ReplaceOptions update = new ReplaceOptions();
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
                    // if the anno is very large, we have splitted parts -> delete all of them
                    deleteTailIfNecessary(annoList, annotationData, builder);
                    annotationData.DeleteOne(filterAnnotationDataBackupDoc);
                }
                annoList.Source.Database.DataBackupOID = annoList.Source.Database.DataOID;

                // insert new annotation data
                // if the data is too large we split it and bind the parts with the next-part-id (the last part got not such id)
                annoList.Source.Database.DataOID = ObjectId.GenerateNewId();
                BsonArray data = AnnoListToBsonArray(annoList, schemeDoc);
                BsonDocument newAnnotationDataDoc = new BsonDocument();
                newAnnotationDataDoc.Add(new BsonElement("_id", annoList.Source.Database.DataOID));
                newAnnotationDataDoc.Add("labels", data);

                const long MAX_DOCUMENT_SIZE = 16000000; // with buffer
                int current_lenght = newAnnotationDataDoc.ToBson().Length + 1;

                if (current_lenght >= MAX_DOCUMENT_SIZE)
                {
                    DatabaseSaveProgress progressWindow = new DatabaseSaveProgress(annoList, schemeDoc, annotationData);
                    progressWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    progressWindow.ShowDialog();
                }
                else
                {
                    annotationData.InsertOne(newAnnotationDataDoc);
                }

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
                            BountyJob job = new BountyJob();
                            job.user = GetUserInfo(dbuser);

                            job.rating = 0;
                            job.status = "finished";
                            //job.pickedLNURL = false;
                            //job.LNURLW = "TODO";

                            

                            DatabaseUser user = GetUserInfo(dbuser);
                            int index = bounty.annotatorsJobCandidates.FindIndex(s => s.user.Name == user.Name);
                            if (index > -1)
                            {
                                bounty.annotatorsJobCandidates.RemoveAt(index);
                                bounty.annotatorsJobDone.Add(job);
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

                ReplaceOptions newAnnotationDocUpdate = new ReplaceOptions();
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

        private static void deleteTailIfNecessary(AnnoList annoList, IMongoCollection<BsonDocument> annotationData, FilterDefinitionBuilder<BsonDocument> builder)
        {
            var condition = builder.Eq("_id", annoList.Source.Database.DataBackupOID);
            var fields = Builders<BsonDocument>.Projection.Include("nextEntry");
            var id = annotationData.Find(condition).Project<BsonDocument>(fields).ToList();
            if (id.Count > 0 && id[0].Contains("nextEntry"))
            {
                ObjectId nextEntryID = id[0].GetValue("nextEntry").AsObjectId;
                bool nextEntryAvailable = true;
                while(nextEntryAvailable)
                {
                    condition = builder.Eq("_id", nextEntryID);
                    id = annotationData.Find(condition).Project<BsonDocument>(fields).ToList();
                    if (id.Count > 0 && id[0].Contains("nextEntry"))
                        nextEntryID = id[0].GetValue("nextEntry").AsObjectId;
                    else
                        nextEntryAvailable = false;

                    annotationData.DeleteOne(condition);
                }
            }
        }

        public static List<AnnoList> splitDataInFittingParts(AnnoList annoList, BsonDocument schemeDoc, long max_size)
        {
            AnnoList first = getHalfAnnoList(annoList, 0, annoList.Count / 2);
            AnnoList second = getHalfAnnoList(annoList, annoList.Count / 2, annoList.Count);

            ObjectId testID = ObjectId.GenerateNewId();
            BsonArray data = AnnoListToBsonArray(first, schemeDoc);
            BsonDocument newAnnotationDataDoc = new BsonDocument();
            newAnnotationDataDoc.Add(new BsonElement("_id", testID));
            newAnnotationDataDoc.Add("labels", data);

            List<AnnoList> resultList = new List<AnnoList>();
            if (newAnnotationDataDoc.ToBson().Length >= max_size)
            {
                resultList.AddRange(splitDataInFittingParts(first, schemeDoc, max_size));
            }
            else
            {
                resultList.Add(first);
            }

            data = AnnoListToBsonArray(second, schemeDoc);
            newAnnotationDataDoc = new BsonDocument();
            newAnnotationDataDoc.Add(new BsonElement("_id", testID));
            newAnnotationDataDoc.Add("labels", data);

            if (newAnnotationDataDoc.ToBson().Length >= max_size)
            {
                resultList.AddRange(splitDataInFittingParts(second, schemeDoc, max_size));
            }
            else
            {
                resultList.Add(second);
            }

            return resultList;
        }

        private static AnnoList getHalfAnnoList(AnnoList completeList, int counter, int end)
        {
            AnnoList annoList = new AnnoList();
            annoList.Scheme = completeList.Scheme;
            annoList.Meta = completeList.Meta;

            for (; counter < end; counter++)
            {
                annoList.Add(completeList[counter]);
            }

            return annoList;
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

            bounty.annotatorsJobCandidates = new List<BountyJob>();
            foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
            {
        
                BountyJob job = new BountyJob();
                job.user = GetUserInfo(cand["name"].AsString);
                if (cand.Contains("rating"))
                {
                    job.rating = cand["rating"].AsDouble;
                    job.status = cand["status"].AsString;
                    job.ratingContractor = 0;
                    //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                    //job.LNURLW = cand["LNURLW"].AsString;
                }
                bounty.annotatorsJobCandidates.Add(job);
            }

            bounty.annotatorsJobDone = new List<BountyJob>();
            foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
            {
                BountyJob job = new BountyJob();
                job.user = GetUserInfo(cand["name"].AsString);
                if (cand.Contains("rating"))
                {
                    job.rating = cand["rating"].AsDouble;
                    job.ratingContractor = cand["ratingContractor"].AsDouble;
                    job.status = cand["status"].AsString;
                    //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                    //job.LNURLW = cand["LNURLW"].AsString;
                }
                bounty.annotatorsJobDone.Add(job);
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

        public static List<DatabaseBounty> LoadAcceptedBounties(IMongoDatabase database, bool finished)
        {

            List<DatabaseBounty> bounties = new List<DatabaseBounty>();
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;

            var bountiesDB = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Bounties);
            //maybe need some more logic here
            List<BsonDocument> bountiesDoc;
            if (!finished)
            {
                var filter = builder.Gt("numOfAnnotationsNeededCurrent", 0);
                 bountiesDoc = bountiesDB.Find(filter).ToList();
            }
            else
            {
                var filter = builder.Gt("numOfAnnotationsNeededCurrent", -1); 
                 bountiesDoc = bountiesDB.Find(filter).ToList();
            }
          
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

                bounty.annotatorsJobCandidates = new List<BountyJob>();
                bool foundmine = false;
                foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
                {
                    BountyJob job = new BountyJob();
                    job.user = GetUserInfo(cand["name"].AsString);
                    if (cand.Contains("rating"))
                    {
                        job.rating = cand["rating"].AsDouble;
                        job.status = cand["status"].AsString;
                        //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                        //job.LNURLW = cand["LNURLW"].AsString;
                    }
                    else job.status = "default";
                  
                  

                    //Don't return contract if already accepted.
                    if (job.user.Name == Properties.Settings.Default.MongoDBUser &&!finished)
                    {
                      
                        foundmine = true;
                    }
                    bounty.annotatorsJobCandidates.Add(job);
                }

                bounty.annotatorsJobDone = new List<BountyJob>();

                foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
                {
                    BountyJob job = new BountyJob();
                    job.user = GetUserInfo(cand["name"].AsString);
                    if (cand.Contains("rating"))
                    {
                        job.rating = cand["rating"].AsDouble;
                        if (cand.Contains("ratingContractor"))
                        {
                            job.ratingContractor = cand["ratingContractor"].AsDouble;
                        }
                        job.status = cand["status"].AsString;
                        //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                        //job.LNURLW = cand["LNURLW"].AsString;
                    }

                   
                    if (job.user.Name == Properties.Settings.Default.MongoDBUser && finished)
                    {
                        bounty.RatingTemp = job.rating;
                        bounty.RatingContractorTemp = job.ratingContractor;
                        foundmine = true;
                    }
                    bounty.annotatorsJobDone.Add(job);
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

                bounty.annotatorsJobCandidates = new List<BountyJob>();
                foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
                {
                    BountyJob job = new BountyJob();
                    job.user = GetUserInfo(cand["name"].AsString);
                    if (cand.Contains("rating"))
                    {
                        job.rating = cand["rating"].AsDouble;
                        job.status = cand["status"].AsString;
                        job.ratingContractor = 0;
                        //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                        //job.LNURLW = cand["LNURLW"].AsString;
                    }
                    else job.status = "default";

                    bounty.annotatorsJobCandidates.Add(job);
                }

                bounty.annotatorsJobDone = new List<BountyJob>();

                foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
                {
                    BountyJob job = new BountyJob();
                    job.user = GetUserInfo(cand["name"].AsString);
                    if (cand.Contains("rating"))
                    {
                        job.rating = cand["rating"].AsDouble;
                        job.ratingContractor = cand["ratingContractor"].AsDouble;
                        job.status = cand["status"].AsString;
                        //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                        //job.LNURLW = cand["LNURLW"].AsString;
                    }
                    else job.status = cand["status"].AsString;

                    bounty.annotatorsJobDone.Add(job);
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

                bool handled = false;
                bounty.annotatorsJobCandidates = new List<BountyJob>();
                foreach (BsonDocument cand in doc["annotatorsJobCandidates"].AsBsonArray)
                {
                    BountyJob job = new BountyJob();
                    job.user = GetUserInfo(cand["name"].AsString);
                    if (cand.Contains("rating"))
                    {
                        job.rating = cand["rating"].AsDouble;
                        job.status = cand["status"].AsString;
                        //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                        //job.LNURLW = cand["LNURLW"].AsString;
                    }
                    job.status = "default";

                    bounty.annotatorsJobCandidates.Add(job);
                    //Don't return contract if already accepted.
                    if (job.user.Name == Properties.Settings.Default.MongoDBUser)
                    {
                        handled = true;
                    }
                }

                bounty.annotatorsJobDone = new List<BountyJob>();
                foreach (BsonDocument cand in doc["annotatorsJobDone"].AsBsonArray)
                {
                    BountyJob job = new BountyJob();
                    job.user = GetUserInfo(cand["name"].AsString);
                    if (cand.Contains("rating"))
                    {
                        job.rating = cand["rating"].AsDouble;
                        job.status = cand["status"].AsString;
                        //job.pickedLNURL = cand["pickedLNURL"].AsBoolean;
                        //job.LNURLW = cand["LNURLW"].AsString;
                    }
                    job.status = "default";

                    bounty.annotatorsJobDone.Add(job);

                    //Don't return contract if already finished.
                    if (job.user.Name == Properties.Settings.Default.MongoDBUser)
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

                if (!handled) bounties.Add(bounty);
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

        private static void loadAnnoListSchemeAndData(ref AnnoList annoList, BsonDocument scheme, BsonArray labels)
        {
            BsonElement value;
            var builder = Builders<BsonDocument>.Filter;

            annoList.Scheme = new AnnoScheme();
            annoList.Scheme.Type = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), scheme["type"].AsString);
            annoList.Scheme.Name = scheme["name"].ToString();

            if (scheme.Contains("bounty_id"))
            {
                if (scheme.TryGetElement("bounty_id", out value))
                {
                    annoList.Source.Database.HasBounty = true;
                    annoList.Source.Database.BountyID = scheme["bounty_id"].AsObjectId;
                    annoList.Source.Database.BountyIsPaid = scheme["bountyIsPaid"].AsBoolean;
                }
            }
         

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
                    //BsonArray attributelabels = scheme["attributes"].AsBsonArray;

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

                    if (scheme.Contains("attributes"))
                    {
                        BsonArray attributesArray = scheme["attributes"].AsBsonArray;
                        foreach (BsonDocument doc in attributesArray)
                        {
                            List<string> values = new List<string>();
                            BsonArray bsonvalues = doc["values"].AsBsonArray;
                            AnnoScheme.AttributeTypes type = AnnoScheme.AttributeTypes.LIST;
                            foreach (BsonDocument val in bsonvalues)
                            {
                                values.Add(val["value"].ToString());
                            }
                            if (values[0].ToLower().Contains("true") || values[0].ToLower().Contains("false"))
                            {
                                type = AnnoScheme.AttributeTypes.BOOLEAN;
                            }
                            if (values.Count == 1)
                            {
                                type = AnnoScheme.AttributeTypes.STRING;
                            }

                            AnnoScheme.Attribute attr = new AnnoScheme.Attribute(doc["name"].ToString(), values, type);
                            annoList.Scheme.LabelAttributes.Add(attr);
                        }
                    }

                

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
                            if (labels[i].ToString().Contains("meta")) meta = labels[i]["meta"].ToString();
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
                    if (scheme.Contains("attributes"))
                    {
                        BsonArray attributesArray = scheme["attributes"].AsBsonArray;
                        foreach (BsonDocument doc in attributesArray)
                        {
                            List<string> values = new List<string>();
                            BsonArray bsonvalues = doc["values"].AsBsonArray;
                            AnnoScheme.AttributeTypes type = AnnoScheme.AttributeTypes.LIST;
                            foreach (BsonDocument val in bsonvalues)
                            {
                                values.Add(val["value"].ToString());
                            }
                            if (values[0].ToLower().Contains("true") || values[0].ToLower().Contains("false"))
                            {
                                type = AnnoScheme.AttributeTypes.BOOLEAN;
                            }
                            if (values.Count == 1)
                            {
                                type = AnnoScheme.AttributeTypes.STRING;
                            }

                            AnnoScheme.Attribute attr = new AnnoScheme.Attribute(doc["name"].ToString(), values, type);
                            annoList.Scheme.LabelAttributes.Add(attr);
                        }
                    }



                    for (int i = 0; i < labels.Count; i++)
                    {
                        double start = double.Parse(labels[i]["from"].ToString());
                        double stop = double.Parse(labels[i]["to"].ToString());
                        double duration = stop - start;
                        string label = labels[i]["name"].ToString();
                        string confidence = labels[i]["conf"].ToString();
                        string meta = "";
                        Color color = Colors.Black;
                       
                        try
                        {
                            meta = labels[i]["meta"].ToString();
                        }
                        catch (Exception e)
                        {

                        }

                        try
                        {
                            color = (Color)ColorConverter.ConvertFromString(labels[i]["color"].ToString());
                        }
                        catch (Exception e)
                        {

                        }

                        AnnoListItem ali = new AnnoListItem(start, duration, label, meta, color, double.Parse(confidence));
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
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.POLYGON || annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                {
                    int videoCount = 0;
                    foreach (IMedia media in MainHandler.mediaList)
                    {
                        if (media.GetMediaType() == MediaType.VIDEO)
                            videoCount++;
                    }

                    if (videoCount > 1)
                    {
                        foreach (AnnoList anno in MainHandler.annoLists)
                        {
                            if (anno.Scheme.Type == AnnoScheme.TYPE.POLYGON || anno.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                            {
                                MessageBox.Show("Polygon annotations are only allowed with exactly one media file!", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                                return;
                            }
                        }
                    }

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

                        BsonArray polygons = null;
                        if (entry.TryGetElement("polygons", out value))
                            polygons = value.Value.AsBsonArray;

                        if (polygons != null && polygons.Count > 0)
                        {
                            foreach (BsonDocument polygon in polygons)
                            {
                                String label = "";
                                Color labelColor = Colors.Black;
                                String conf = "100";
                                if (polygon.TryGetElement("confidence", out value))
                                    conf = value.Value.ToString();

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

                                if (b_points.Count > 0)
                                {
                                    foreach (BsonDocument point in b_points)
                                    {
                                        points.Add(new PolygonPoint(point["x"].ToDouble(), point["y"].ToDouble()));
                                    }
                                    polygonLabels.Add(new PolygonLabel(points, label, labelColor, conf));
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
                    if (index < factor - 1)
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


                        if (oldScheme.MinScore != newScheme.MinScore || oldScheme.MaxScore != newScheme.MaxScore)
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
                double factor = new_Samplerate / old_samplerate;
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
                    for (int i = 0; i < factor; i++)
                    {
                        AnnoListItem newal = new AnnoListItem(start, duration, score.ToString(), meta, color, conf);
                        start = start + duration;
                        newList.Add(newal);
                    }

                }
            }

            else if (old_samplerate == new_Samplerate)
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
                BsonArray annotationData = getData(dataID, annotationsData);

                if(scheme["type"] == "DISCRETE" || scheme["type"] == "FREE" || annotationData.Count > 0)
                {
                    loadAnnoListSchemeAndData(ref annoList, scheme, annotationData);
                }
          

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

                BsonArray annotationData = getData(dataID, annotationsData);
                if (scheme["type"] == "DISCRETE" || scheme["type"] == "FREE" || annotationData.Count > 0)
                {
                    loadAnnoListSchemeAndData(ref annoList, scheme, annotationData);
                }


                // update source

                annoList.Source.Database.OID = annotationDoc["_id"].AsObjectId;
                annoList.Source.Database.DataOID = dataID;
                annoList.Source.Database.DataBackupOID = dataBackupID;
                annoList.Source.Database.Session = annotation.Session;

                return annoList;
            }

            return null;
        }

        private static BsonArray getData(ObjectId dataID, IMongoCollection<BsonDocument> annotationData)
        {
            var builder = Builders<BsonDocument>.Filter;

            var filterData = builder.Eq("_id", dataID);
            var headList = annotationData.Find(filterData).ToList();

            if (headList.Count == 0)
                return new BsonArray();

            BsonArray body = headList[0]["labels"].AsBsonArray;
            BsonValue current_val = null;

            if (!headList[0].TryGetValue("nextEntry", out current_val))
                return body;

            ObjectId currentID = current_val.AsObjectId;
            while (current_val != null)
            {
                var tailData = builder.Eq("_id", currentID);
                var tailList = annotationData.Find(tailData).ToList();

                if (tailList.Count == 0)
                    break;

                BsonDocument tail = (BsonDocument)tailList[0];
                body.AddRange(tail["labels"].AsBsonArray);
                tail.TryGetValue("nextEntry", out current_val);
                if (current_val != null)
                    currentID = current_val.AsObjectId;
                else
                    current_val = null;
            }

            return body;
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
                    try { date = annotation["date"].ToUniversalTime(); }
                    catch(Exception e)
                    {
                        try { date =  DateTime.Parse(annotation["date"].AsString).ToUniversalTime(); }
                        catch (Exception ex)
                        {
                            date = DateTime.Today;
                        }
                      
                    }
                  
                    
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
                if (!AnnotationExists(annotation.Id)) {
                    continue;
                }

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


        public static AnnoList ConvertDiscreteAnnoListToContinuousList(AnnoList annolist, double chunksize, double end, string restclass = "Rest")
        {
            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;
            result.Source.StoreToDatabase = true;
            result.Source.Database.Session = annolist.Source.Database.Session;
            double currentpos = 0;

            bool foundlabel = false;

            while (currentpos < end)
            {
                foundlabel = false;
                foreach (AnnoListItem orgitem in annolist)
                {
                    if (orgitem.Start < currentpos && orgitem.Stop > currentpos)
                    {
                        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                        result.Add(ali);
                        foundlabel = true;
                        break;
                    }
                }

                if (foundlabel == false)
                {
                    AnnoListItem ali = new AnnoListItem(currentpos, chunksize, restclass);
                    result.Add(ali);
                }

                currentpos = currentpos + chunksize;
            }

            return result;
        }


        public static bool ExportMultipleCSV(List<DatabaseAnnotation> annos, List<StreamItem> streams, DatabaseSession session, string delimiter = ";")
        {



            //Get Storage Location
            string filePath = FileTools.SaveFileDialog(SessionName, ".csv", "Annotation(*.csv)|*.csv", "");
            if (filePath == null) return false;




            //make some more advanced logic to get maximum/fill with zeros
            // int length = annoLists.Min(annoLists)






            List<Signal> signals = new List<Signal>();
            




            foreach (var file in streams)
            {

                if (file.Extension == "stream")
                {


                    DatabaseStream temp = new DatabaseStream();
                    string filename = Path.GetFileNameWithoutExtension(file.Name);
                    string[] withoutRole = filename.Split('.');
                    string fname = "";
                    //add all strings but the first (role)
                    for (int i = 1; i < withoutRole.Length; i++)
                    {
                        fname = fname + withoutRole[i] + ".";
                    }

                    temp.Name = fname.Remove(fname.Length - 1, 1);
                    temp.FileExt = file.Extension;
                    temp.Type = file.Type;
                    DatabaseHandler.GetStream(ref temp);


                    Signal signal = null;
                    string localPath = "";
                    foreach (var path in Defaults.LocalDataLocations())
                    {
                        if (File.Exists(path + "\\" + DatabaseHandler.DatabaseName + "\\" + session.Name + "\\" + file.Name))
                        {
                            localPath = path + "\\" + DatabaseHandler.DatabaseName + "\\" + session.Name + "\\" + file.Name;
                            break;
                        } 
                    }
                   


                    signal = Signal.LoadStreamFile(localPath, temp.DimLabels);
                    signals.Add(signal);
                }
            }


            double time = 0;
            double sr = 25;
            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
            {
                sr = Properties.Settings.Default.DefaultDiscreteSampleRate;
            }


            //if (signals.Count > 0)
            //{
            //    sr = signals[0].rate;
     
            //}
             
            double srtime = Math.Round(1 / sr, 2);



            //Fetch actual Annolists from DatabaseAnnotations
            List<AnnoList> annoLists = new List<AnnoList>();
            foreach (DatabaseAnnotation annotation in annos)
            {
                AnnoList annoList = LoadAnnoList(annotation, false);


                //Only continuous for now, need convertion for discrete labels;
                if (annoList != null && annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    annoLists.Add(annoList);
                }


            }

            int length = 0;
            if (annoLists.Count > 0)
            {
                length = annoLists.Max(e => e.Count);
            }
            if (signals.Count > 0)
            {
                length = (int)Math.Max(length, signals.Max(e => e.number));
            }

            else
            {
                
                foreach (DatabaseAnnotation anno in annos)
                {
                    AnnoList annoList = LoadAnnoList(anno, false);
                    if (annoList.Count > 0)
                    {
                        if (annoList[annoList.Count - 1].Stop * sr > length)
                        {
                            length =  (int)(annoList[annoList.Count - 1].Stop * sr);
                        }
                    }
                  
                }
             
            }


            foreach (DatabaseAnnotation annotation in annos)
            {
                AnnoList annoList = LoadAnnoList(annotation, false);

                if (annoList != null && (annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || annoList.Scheme.Type == AnnoScheme.TYPE.FREE))
                {
                    AnnoList converted = ConvertDiscreteAnnoListToContinuousList(annoList, srtime, length * srtime, "None");
                    annoLists.Add(converted);
                }


            }











            var records = new List<object>();
            for (int i = 0; i < length; i++)
            {

                dynamic dynamicLineObject = new ExpandoObject();
                IDictionary<string, object> dictobject = dynamicLineObject;

                //Add ID to columns
                dictobject.Add("Session", session.Name);

                //Add time to columns
                dictobject.Add("Time", Math.Round(time, 2));
                time += srtime;


                //Add annotations to columns
                for (int j = 0; j < annoLists.Count; j++)
                {
                    if (i < annoLists[j].Count && annoLists[j].Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) dictobject.Add(annoLists[j].Meta.Role + "." + annoLists[j].Scheme.Name + "." + annoLists[j].Meta.Annotator, annoLists[j][i].Score); // Adding dynamically named property     
                    else if (i < annoLists[j].Count && (annoLists[j].Scheme.Type == AnnoScheme.TYPE.DISCRETE || annoLists[j].Scheme.Type == AnnoScheme.TYPE.FREE)) dictobject.Add(annoLists[j].Meta.Role + "." + annoLists[j].Scheme.Name + "." + annoLists[j].Meta.Annotator, annoLists[j][i].Label);
                }


                for (int j = 0; j < signals.Count; j++)
                {
                    for (int k = 0; k < signals[j].dim; k++)
                    {
                        string value;
                        if (signals[j].DimLabels.Count > 0)
                        {
                            if (signals[j].DimLabels.TryGetValue(k, out value))
                            {
                                if (i < (signals[j].data.Length / signals[j].dim)) dictobject.Add(signals[j].Name + " (" + value + ")", signals[j].data[i * signals[j].dim + k]); // Adding dynamically named property    
                            }
                            else if (i < (signals[j].data.Length / signals[j].dim)) dictobject.Add(signals[j].Name + " (Dim " + k + ")", signals[j].data[i * signals[j].dim + k]); // Adding dynamically named property  


                        }
                        else if (i < (signals[j].data.Length / signals[j].dim)) dictobject.Add(signals[j].Name + " (Dim " + k + ")", signals[j].data[i * signals[j].dim + k]); // Adding dynamically named property    

                    }

                }


                records.Add(dynamicLineObject);


            }


            using (var textWriter = new StreamWriter(filePath))
            {
                var config = new CsvConfiguration(CultureInfo.InstalledUICulture)
                {
                    PrepareHeaderForMatch = args => args.Header.ToLower(),
                    Delimiter = delimiter
                };

                using (var csv = new CsvWriter(textWriter, config))
                {

                    foreach (var record in records)
                    {
                        csv.WriteRecord(record);
                        csv.NextRecord();
                    }
                }

            }



            return true;


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

        public bool hasMatchingAnnotations { get; set; }
    }

    public class DatabaseUser
    {
        public string Name { get; set; }
        public string Password { get; set; }

        public string Fullname { get; set; }

        public string Email { get; set; }

        public int Expertise { get; set; }

        public int XP { get; set; }

        public double ratingoverall { get; set; }
        public int ratingcount { get; set; }

        public double Rating
        {
            get { return (ratingcount > 0) ? ratingoverall / ratingcount : 0; }
        }

        public double ratingContractoroverall { get; set; }
        public int ratingContractorcount { get; set; }

        public double RatingContractor
        {
            get { return (ratingContractoroverall > 0) ?  ratingContractoroverall / ratingContractorcount : 0; }
        }

        //key for Lightning
        public string ln_admin_key { get; set; }
        public string ln_invoice_key { get; set; }
        public string ln_wallet_id { get; set; }

        public string ln_admin_key_locked { get; set; }
        public string ln_invoice_key_locked { get; set; }
        public string ln_wallet_id_locked { get; set; }

        public string ln_user_id { get; set; }
        public string ln_addressname { get; set; }
        public string ln_addresspin { get; set; }





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
        public double RatingTemp { get; set; }
        public double RatingContractorTemp { get; set; }
        public string LNURLW { get; set; }
        public List<BountyJob> annotatorsJobCandidates { get; set; }
        public List<BountyJob> annotatorsJobDone { get; set; }
        public List<StreamItem> streams { get; set; }

    }


    public class BountyJob
    {
      public DatabaseUser user { get; set; }
      public double rating { get; set; }
      public double ratingContractor { get; set; }
        //public bool pickedLNURL { get; set; }
        //public string LNURLW { get; set; }
        public string status { get; set; }
        //open, finished

    }

    public class DatabaseStream
    {
        public ObjectId Id { get; set; }
        public string Name { get; set; }
        public string FileExt { get; set; }
        public string Type { get; set; }
        public double SampleRate { get; set; }
        public double Duration { get; set; }
        public Dictionary<int, string> DimLabels { get; set; }

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

    public class SelectedDatabaseAndSessions
    {
        public string Database { get; set; }
        public string Sessions { get; set; }

        public string Roles { get; set; }

        public string Annotator { get; set; }
        public string Stream { get; set; }


    }

    #endregion DATABASE TYPES
}
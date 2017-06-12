using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
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

            clientAddress = "mongodb://" + user + ":" + password + "@" + address;

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
                    client = new MongoClient(clientAddress);
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
                databaseName = name;
                sessionName = null;
                database = Client.GetDatabase(databaseName);
            }

            return true;            
        }

        static public int CheckAuthentication()
        {
            return CheckAuthentication(Properties.Settings.Default.MongoDBUser, Properties.Settings.Default.DatabaseName);
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
                    if ((roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && roles[i]["db"] == db || (roles[i]["role"].ToString() == "userAdminAnyDatabase"  || roles[i]["role"].ToString() == "dbAdminAnyDatabase")) && auth <= 4) { auth = 4; }
                    else if ((roles[i]["role"].ToString() == "dbAdmin" && roles[i]["db"] == db) && auth <= 3) { auth = 3; }
                    else if ((roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && roles[i]["db"] == db || roles[i]["role"].ToString() == "read" && roles[i]["db"] == db) && auth <= 2) { auth = 2; }
                    else if ((roles[i]["role"].ToString() == "readAnyDatabase") && auth <= 1) { auth = 1; }
                }
            }
            catch(Exception e)
            {
            }

            return auth;
        }

        #endregion

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

        public static List<string> GetDatabases()
        {
            List<string> items = new List<string>();

            if (IsConnected)
            {
                var databases = client.ListDatabasesAsync().Result.ToListAsync().Result;
                foreach (var c in databases)
                {
                    string db = c.GetElement(0).Value.ToString();
                    if (c.GetElement(0).Value.ToString() != "admin" && c.GetElement(0).Value.ToString() != "local" && CheckAuthentication(db) > 1)
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

        public static bool IsObjectID(ObjectId id)
        {
            return id != new ObjectId();
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

            List<string> sessions = GetSessionsAll();
            return sessions.Any(s => name.Equals(s));
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

            List<string> schemes = GetSchemesAll();
            return schemes.Any(s => name.Equals(s));
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

            List<string> annotators = GetAnnotators();
            return annotators.Any(s => name.Equals(s));
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

            List<string> roles = GetRolesAll();
            return roles.Any(s => name.Equals(s));
        }

        public static bool StreamTypeExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            List<string> roles = GetStreamTypesAll();
            return roles.Any(s => name.Equals(s));
        }

        public static bool SubjectExists(string name)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            List<string> subjects = GetSubjectsAll();
            return subjects.Any(s => name.Equals(s));
        }

        public static bool StreamExists(string name, string session)
        {
            if (!IsConnected)
            {
                return false;
            }

            if (name == null || name == "")
            {
                return false;
            }

            ObjectId sessionId = new ObjectId();
            if (!GetObjectID(ref sessionId, DatabaseDefinitionCollections.Sessions, session))
            {                
                return false;
            }

            List<BsonDocument> streams = GetCollection(DatabaseDefinitionCollections.Streams);
            return streams.Any(s => name.Equals(s["name"].AsString) && sessionId.Equals(s["session_id"].AsObjectId));
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
            //List<BsonDocument> annotations = GetCollection(DatabaseDefinitionCollections.Annotations);

            //return annotations.Any(s => annotatorId.Equals(s["annotator_id"].AsObjectId)
            //    && sessionId.Equals(s["session_id"].AsObjectId)
            //    && roleId.Equals(s["role_id"].AsObjectId)
            //    && schemeId.Equals(s["scheme_id"].AsObjectId));
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

        static List<BsonDocument> fast_documents = new List<BsonDocument>();

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

        public static List<string> GetSchemes()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Schemes, "name");
        }

        public static List<string> GetSchemesAll()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Schemes, "name", false);
        }

        public static List<string> GetRoles()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Roles, "name");
        }

        public static List<string> GetRolesAll()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Roles, "name", false);
        }

        public static List<string> GetSubjects()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Subjects, "name");
        }

        public static List<string> GetSubjectsAll()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Subjects, "name", false);
        }

        public static List<string> GetAnnotators()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Annotators, "name");
        }

        public static List<string> GetAnnotatorsFull()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Annotators, "fullname");
        }

        public static List<string> GetStreamTypesAll()
        {
            return GetCollectionField(DatabaseDefinitionCollections.StreamTypes, "name", false);
        }

        public static List<string> GetStreamTypes()
        {
            return GetCollectionField(DatabaseDefinitionCollections.StreamTypes, "name");
        }

        public static List<string> GetSessionsAll()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Sessions, "name", false);
        }

        public static List<string> GetSessions()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Sessions, "name");
        }

        public static List<string> GetStreams()
        {
            return GetCollectionField(DatabaseDefinitionCollections.Streams, "name");
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

        public static string SelectStreamType()
        {
            return SelectCollectionField("Select stream type", DatabaseDefinitionCollections.StreamTypes, "name");
        }

        #endregion

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
                var document = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Meta).Find(filter).Single();
                if (document != null)
                {
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
                {"serverAuth", meta.ServerAuth.ToString()}
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
            database.CreateCollection(DatabaseDefinitionCollections.Annotators);
            database.CreateCollection(DatabaseDefinitionCollections.Roles);
            database.CreateCollection(DatabaseDefinitionCollections.Schemes);
            database.CreateCollection(DatabaseDefinitionCollections.Sessions);
            database.CreateCollection(DatabaseDefinitionCollections.Streams);
            database.CreateCollection(DatabaseDefinitionCollections.StreamTypes);
            database.CreateCollection(DatabaseDefinitionCollections.Subjects);

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

            int authLevel = CheckAuthentication();
            if (authLevel <= 3)
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
                    } } };
            }
            else
            {
                createUser = new BsonDocument {
                    { "createUser", user.Name },
                    { "pwd", user.Password },
                    { "roles", new BsonArray {
                        new BsonDocument { { "role", "readAnyDatabase" }, { "db", "admin" } },
                    } } };
            }

            try
            {
                adminDatabase.RunCommand<BsonDocument>(createUser);
            }
            catch (Exception ex)
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
                catch (Exception ex)
                {
                    return false;
                }
            }
            else
            {
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

            if (!UserExists(user.Name))
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
            catch (Exception ex)
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
            catch (Exception ex)
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
            catch (Exception ex)
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
                        {"name",  annotator.Name},
                        {"fullname", annotator.FullName == null || annotator.FullName == "" ? annotator.Name : annotator.FullName },
                        {"email", annotator.Email == null ? "" : annotator.Email },
                        {"expertise", annotator.Expertise },
                    };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", annotator.Name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).ReplaceOne(filter, document, updateOptions);

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

        public static bool GetAnnotator(ref DatabaseAnnotator annotator)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!UserExists(annotator.Name))
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("user", annotator.Name);
                var adminDatabase = Client.GetDatabase("admin");
                var document = adminDatabase.GetCollection<BsonDocument>("system.users").Find(filter).Single();
                int role = -1;
                if (document != null)
                {
                    BsonArray roles = document["roles"].AsBsonArray;
                    for (int i = 0; i < roles.Count; i++)
                    {
                        if (roles[i]["db"] == databaseName)
                        {
                            role = Math.Max(role, GetRoleIndex(roles[i]["role"].AsString));
                        }
                    }                    
                }
                annotator.Role = GetRoleString(role);
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", annotator.Name);
                var document = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filter).Single();
                if (document != null)
                {
                    BsonElement value;
                    if (document.TryGetElement("fullname", out value))
                    {
                        annotator.FullName = document["fullname"].ToString();
                    }
                    else
                    {
                        annotator.FullName = annotator.Name;
                    }
                    if (document.TryGetElement("email", out value))
                    {
                        annotator.Email = document["email"].ToString();
                    }
                    else
                    {
                        annotator.Email = "";
                    }
                    annotator.Expertise = 2;
                    if (document.TryGetElement("expertise", out value))
                    {
                        int expertise;
                        if (int.TryParse(document["expertise"].ToString(), out expertise))
                        {
                            annotator.Expertise = expertise;
                        }
                    }                    
                }
            }

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
                    {"isValid",  true}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles).ReplaceOne(filter, document, updateOptions);

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

            return true;
        }

        public static bool GetStreamType(ref DatabaseStreamType streamType)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamTypeExists(streamType.Name))
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", streamType.Name);
                var document = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.StreamTypes).Find(filter).Single();
                if (document != null)
                {
                    BsonElement value;
                    streamType.Type = "";
                    if (document.TryGetElement("type", out value))
                    {
                        streamType.Type = document["type"].ToString();
                    }
                }
            }

            return true;
        }

        private static bool AddOrUpdateStreamType(string name, DatabaseStreamType streamType)
        {
            if (streamType.Name == "")
            {
                return false;
            }

            BsonDocument document = new BsonDocument {
                    {"name",  streamType.Name},
                    {"type",  streamType.Type},
                    {"isValid",  true}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.StreamTypes).ReplaceOne(filter, document, updateOptions);

            return true;
        }

        public static bool AddStreamType(DatabaseStreamType streamType)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (StreamTypeExists(streamType.Name))
            {
                return false;
            }

            return AddOrUpdateStreamType(streamType.Name, streamType);
        }

        public static bool UpdateStreamType(string name, DatabaseStreamType streamType)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamTypeExists(name))
            {
                return false;
            }

            if (name != streamType.Name && StreamTypeExists(streamType.Name))
            {
                return false;
            }

            return AddOrUpdateStreamType(name, streamType);
        }

        public static bool DeleteStreamType(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamTypeExists(name))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.StreamTypes);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var update = Builders<BsonDocument>.Update.Set("isValid", false);
            collection.UpdateOne(filter, update);

            return true;
        }


        public static bool GetSubject(ref DatabaseSubject subject)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SubjectExists(subject.Name))
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", subject.Name);
                var document = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Subjects).Find(filter).Single();
                if (document != null)
                {
                    BsonElement value;
                    subject.Age = 0;
                    if (document.TryGetElement("age", out value))
                    {
                        int v;
                        if (int.TryParse(document["age"].ToString(), out v))
                        {
                            subject.Age = v;
                        }
                    }
                    subject.Gender = "";
                    if (document.TryGetElement("gender", out value))
                    {
                        subject.Gender = document["gender"].ToString();
                    }
                    subject.Culture = "";
                    if (document.TryGetElement("culture", out value))
                    {
                        subject.Culture = document["culture"].ToString();
                    }
                }
            }

            return true;
        }

        private static bool AddOrUpdateSubject(string name, DatabaseSubject subject)
        {
            if (subject.Name == "")
            {
                return false;
            }

            BsonDocument document = new BsonDocument {
                    {"name",  subject.Name},
                    {"age",  subject.Age},
                    {"gender",  subject.Gender},
                    {"culture",  subject.Culture},
                    {"isValid",  true}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Subjects).ReplaceOne(filter, document, updateOptions);

            return true;
        }

        public static bool AddSubject(DatabaseSubject subject)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (StreamTypeExists(subject.Name))
            {
                return false;
            }

            return AddOrUpdateSubject(subject.Name, subject);
        }

        public static bool UpdateSubject(string name, DatabaseSubject subject)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SubjectExists(name))
            {
                return false;
            }

            if (name != subject.Name && SubjectExists(subject.Name))
            {
                return false;
            }

            return AddOrUpdateSubject(name, subject);
        }

        public static bool DeleteSubject(string name)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SubjectExists(name))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Subjects);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            var update = Builders<BsonDocument>.Update.Set("isValid", false);
            collection.UpdateOne(filter, update);

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
            }
            catch (Exception ex)
            {
                MessageTools.Warning(ex.ToString());
            }

            return scheme;
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

            return true;
        }


        public static bool GetSession(ref DatabaseSession session)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!SessionExists(session.Name))
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", session.Name);
                var document = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions).Find(filter).Single();
                if (document != null)
                {
                    BsonElement value;
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
                    session.Date = new DateTime();
                    if (document.TryGetElement("date", out value))
                    {
                        session.Date = document["date"].ToUniversalTime();
                    }
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
                    {"isValid",  true}
            };

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", name);
            UpdateOptions updateOptions = new UpdateOptions();
            updateOptions.IsUpsert = true;

            var result = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions).ReplaceOne(filter, document, updateOptions);

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

            if (!SessionExists(name))
            {
                return false;
            }

            if (name != session.Name && SessionExists(session.Name))
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

            return true;
        }

        public static List<DatabaseStream> GetSessionStreams(DatabaseSession session)
        {
            List<DatabaseStream> items = new List<DatabaseStream>();

            if (IsConnected && IsDatabase)
            {
                ObjectId sessionId = new ObjectId();
                if (GetObjectID(ref sessionId, DatabaseDefinitionCollections.Sessions, session.Name))
                {
                    IMongoCollection<BsonDocument> collection = Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);
                    List<BsonDocument> streams = collection.Find(item => item["session_id"] == sessionId).ToList();
                    foreach (BsonDocument document in streams)
                    {                        
                        DatabaseStream stream = new DatabaseStream() { Name = document["name"].ToString(), Session = session.Name };
                        GetStream(ref stream);
                        items.Add(stream);                        
                    }
                }
            }

            return items;
        }

        public static bool GetStream(ref DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamExists(stream.Name, stream.Session))
            {
                return false;
            }

            {
                var builder = Builders<BsonDocument>.Filter;
                ObjectId sessionId = new ObjectId();
                GetObjectID(ref sessionId, DatabaseDefinitionCollections.Sessions, stream.Session);               
                var filter = builder.And(new FilterDefinition<BsonDocument>[] { builder.Eq("name", stream.Name), builder.Eq("session_id", sessionId) });
                var document = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams).Find(filter).Single();
                if (document != null)
                {
                    BsonElement value;
                    
                    string role = "";
                    if (document.TryGetElement("role_id", out value))
                    {
                        if (document["role_id"].IsObjectId)
                        {
                            GetObjectName(ref role, DatabaseDefinitionCollections.Roles, document["role_id"].AsObjectId);
                        }
                    }
                    stream.Role = role;

                    string subject = "";
                    if (document.TryGetElement("subject_id", out value))
                    {
                        if (document["subject_id"].IsObjectId)
                        {
                            GetObjectName(ref subject, DatabaseDefinitionCollections.Subjects, document["subject_id"].AsObjectId);
                        }
                    }
                    stream.Subject = subject;

                    string streamName = "";
                    string streamType = "";
                    if (document.TryGetElement("mediatype_id", out value))
                    {
                        if (document["mediatype_id"].IsObjectId)
                        {
                            GetObjectName(ref streamName, DatabaseDefinitionCollections.StreamTypes, document["mediatype_id"].AsObjectId);
                            GetObjectField(ref streamType, DatabaseDefinitionCollections.StreamTypes, document["mediatype_id"].AsObjectId, "type");
                        }
                    }
                    stream.StreamName = streamName;
                    stream.StreamType = streamType;

                    stream.URL = "";
                    if (document.TryGetElement("url", out value))
                    {
                        stream.URL = document["url"].ToString();
                    }

                    stream.ServerAuth = false;
                    if (document.TryGetElement("requiresAuth", out value))
                    {
                        stream.ServerAuth= document["requiresAuth"].AsBoolean;
                    }
                }
            }

            return true;
        }

        private static bool AddOrUpdateStream(string name, DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            ObjectId sessionId = new ObjectId();
            if (!GetObjectID(ref sessionId, DatabaseDefinitionCollections.Sessions, stream.Session))
            {
                return false;
            }

            ObjectId roleId = new ObjectId();
            if (stream.Role != null && stream.Role != "" && !GetObjectID(ref roleId, DatabaseDefinitionCollections.Roles, stream.Role))
            {
                return false;
            }

            ObjectId streamTypeId = new ObjectId();
            if (stream.StreamName != null && stream.StreamName != "" && !GetObjectID(ref streamTypeId, DatabaseDefinitionCollections.StreamTypes, stream.StreamName))
            {
                return false;
            }

            ObjectId subjectId = new ObjectId();
            if (stream.Subject != null && stream.Subject != "" && !GetObjectID(ref subjectId, DatabaseDefinitionCollections.Subjects, stream.Subject))
            {
                return false;
            }

            {
                var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);
                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.And(new FilterDefinition<BsonDocument>[] { builder.Eq("name", name), builder.Eq("session_id", sessionId) });                                 
                UpdateOptions updateOptions = new UpdateOptions();
                updateOptions.IsUpsert = true;

                BsonDocument document = new BsonDocument
                {
                    { "name", stream.Name },
                    { "url", stream.URL == null ? "" : stream.URL },
                    { "requiresAuth", stream.ServerAuth},
                    { "session_id", sessionId },
                    { "mediatype_id", streamTypeId },
                    { "role_id", roleId },
                    { "subject_id", subjectId }
                };

                collection.ReplaceOne(filter, document, updateOptions);
            }

            return true;
        }

        public static bool AddStream(DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (StreamExists(stream.Name, stream.Session))
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

            if (!StreamExists(name, stream.Session))
            {
                return false;
            }

            if (name != stream.Name && StreamExists(stream.Name, stream.Session))
            {
                return false;
            }

            return AddOrUpdateStream(name, stream);
        }

        public static bool DeleteStream(DatabaseStream stream)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }

            if (!StreamExists(stream.Name, stream.Session))
            {
                return false;
            }

            ObjectId sessionId = new ObjectId();
            if (!GetObjectID(ref sessionId, DatabaseDefinitionCollections.Sessions, stream.Session))
            {
                return false;
            }

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.And(new FilterDefinition<BsonDocument>[] { builder.Eq("name", stream.Name), builder.Eq("session_id", sessionId) });
            collection.DeleteOne(filter);

            return true;
        }

        #endregion

        public static bool SaveAnnoList(AnnoList annoList, List<DatabaseStream> linkedStreams = null, bool setFinished = false)
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

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotators = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators);
            var sessions = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var roles = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
            var streams = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Streams);
            var schemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);

            var builder = Builders<BsonDocument>.Filter;

            ObjectId roleID;
            {
                var filter = builder.Eq("name", annoList.Meta.Role);
                var documents = roles.Find(filter).ToList();
                if (documents.Count == 0)
                {
                    return false;
                }
                roleID = documents[0].GetValue(0).AsObjectId;
            }

            ObjectId sessionID;
            {
                var filter = builder.Eq("name", annoList.Source.Database.Session);
                var documents = sessions.Find(filter).ToList();
                if (documents.Count == 0)
                {
                    return false;
                }
                sessionID = documents[0].GetValue(0).AsObjectId;
            }

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
                schemeID = schemeDoc.GetValue(0).AsObjectId;
                string type = schemeDoc["type"].AsString;
                schemeType = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), type);                
            }
       
            if (!(dbuser == "system" ||
                annoList.Meta.Annotator == "RootMeanSquare" ||
                annoList.Meta.Annotator == "Mean" ||
                annoList.Meta.Annotator == "Merge") 
                && annoList.Meta.Annotator != dbuser)
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
                annoList.Meta.AnnotatorFullName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", userID);              
            }

            ObjectId annotatorID;
            if (!AnnotatorExists(annoList.Meta.Annotator))
            {
                BsonDocument annotatorDoc = new BsonDocument();
                annotatorDoc.Add(new BsonElement("name", annoList.Meta.Annotator));
                annotatorDoc.Add(new BsonElement("fullname", annoList.Meta.AnnotatorFullName));               
                var filter = builder.Eq("name", annoList.Meta.Annotator);
                UpdateOptions update = new UpdateOptions();
                update.IsUpsert = true;
                annotators.ReplaceOne(filter, annotatorDoc, update);
                annotatorID = annotators.Find(filter).Single()["_id"].AsObjectId;               
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
            
            BsonElement annotator = new BsonElement("annotator_id", annotatorID);
            BsonElement role = new BsonElement("role_id", roleID);
            BsonElement scheme = new BsonElement("scheme_id", schemeID);
            BsonElement sessionid = new BsonElement("session_id", sessionID);
            BsonElement isfinished = new BsonElement("isFinished", setFinished);
            BsonElement date = new BsonElement("date", new BsonDateTime(DateTime.Now));
            
            BsonArray streamArray = new BsonArray();
            if (linkedStreams != null)
            {
                foreach (DatabaseStream dmi in linkedStreams)
                {
                    BsonDocument mediadocument = new BsonDocument();
                    ObjectId mediaid;
                    var filtermedia = builder.Eq("name", dmi.Name) & builder.Eq("session_id", sessionID);
                    var mediadb = streams.Find(filtermedia).ToList();
                    if (mediadb.Count > 0)
                    {
                        mediaid = mediadb[0].GetValue(0).AsObjectId;
                        BsonElement media_id = new BsonElement("media_id", mediaid);
                        mediadocument.Add(media_id);
                        streamArray.Add(mediadocument);
                    }
                }
            }

            BsonArray data = new BsonArray();
            BsonDocument newAnnotationDoc = new BsonDocument();
            newAnnotationDoc.Add(sessionid);
            newAnnotationDoc.Add(annotator);
            newAnnotationDoc.Add(role);
            newAnnotationDoc.Add(scheme);
            newAnnotationDoc.Add(date);
            newAnnotationDoc.Add(isfinished);
            newAnnotationDoc.Add("media", streamArray);

            if (schemeType == AnnoScheme.TYPE.DISCRETE)
            {
                BsonArray Labels = schemeDoc["labels"].AsBsonArray;
                int index = 0;
                for (int i = 0; i < annoList.Count; i++)
                {
                    for (int j = 0; j < Labels.Count; j++)
                    {
                        if (annoList[i].Label == Labels[j]["name"].ToString())
                        {
                            index = Labels[j]["id"].AsInt32;
                            data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "id", index }, { "conf", annoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                            break;
                        }
                        else if (annoList[i].Label == GARBAGELABEL)
                        {
                            data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "id", -1 }, { "conf", annoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                            break;
                        }
                    }
                }
            }
            else if (schemeType == AnnoScheme.TYPE.FREE)
            {
                for (int i = 0; i < annoList.Count; i++)
                {
                    data.Add(new BsonDocument { { "from", annoList[i].Start }, { "to", annoList[i].Stop }, { "name", annoList[i].Label }, { "conf", annoList[i].Confidence } });
                }
            }
            else if (schemeType == AnnoScheme.TYPE.CONTINUOUS)
            {
                for (int i = 0; i < annoList.Count; i++)
                {
                    data.Add(new BsonDocument { { "score", annoList[i].Label }, { "conf", annoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
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
            newAnnotationDoc.Add("labels", data);

            var filter2 = builder.Eq("scheme_id", schemeID) 
                & builder.Eq("role_id", roleID)
                & builder.Eq("annotator_id", annotatorID)
                & builder.Eq("session_id", sessionID);

            var annotationDoc = annotations.Find(filter2).ToList();

            bool islocked = false;
            if (annotationDoc.Count > 0)
            {
                try
                {
                    islocked = annotationDoc[0]["isLocked"].AsBoolean;
                }
                catch (Exception ex) { }
            }

            if (!islocked)
            {
                if (annotationDoc.Count > 0 && Properties.Settings.Default.DatabaseAskBeforeOverwrite)
                {
                    MessageBoxResult mbres = MessageBox.Show("Save annotation?", "Attention", MessageBoxButton.YesNo);
                    if (mbres == MessageBoxResult.No)
                    {
                        return false;                        
                    }                    
                }

                UpdateOptions update2 = new UpdateOptions();
                update2.IsUpsert = true;
                annotations.ReplaceOne(filter2, newAnnotationDoc, update2);

                if (annotationDoc.Count == 0)
                {
                    annotationDoc = annotations.Find(filter2).ToList();
                    annoList.Source.Database.OID = annotationDoc[0].GetElement(0).Value.AsObjectId;
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
            string annotatorFullName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", annotatorID);

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

        public static bool DeleteAnnotation(ObjectId id)
        {
            if (!IsConnected && !IsDatabase)
            {
                return false;
            }            

            var collection = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("_id", id);
            collection.DeleteOne(filter);

            return true;
        }

        public static AnnoList LoadAnnoList(string oid)
        {
            if (!IsConnected)
            {
                return null;
            }

            var builder = Builders<BsonDocument>.Filter;

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var filter = builder.Eq("_id", new ObjectId(oid));
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

            return LoadAnnoList(annotation);
        }

        public static AnnoList LoadAnnoList(DatabaseAnnotation annotation)
        {
            AnnoList annoList = null;

            BsonElement value;            
            var builder = Builders<BsonDocument>.Filter;

            var annotations = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var roles = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);
            var schemes = database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);

            ObjectId roleID = GetObjectID(DatabaseDefinitionCollections.Roles, "name", annotation.Role);
            string roleName = FetchDBRef(DatabaseDefinitionCollections.Roles, "name", roleID);

            ObjectId schemeID = GetObjectID(DatabaseDefinitionCollections.Schemes, "name", annotation.Scheme);
            string schemeName = FetchDBRef(DatabaseDefinitionCollections.Schemes, "name", schemeID);

            ObjectId annotatorID = GetObjectID(DatabaseDefinitionCollections.Annotators, "name", annotation.Annotator);
            string annotatorName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "name", annotatorID);
            string annotatorFullName = FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", annotatorID);

            ObjectId sessionID = GetObjectID(DatabaseDefinitionCollections.Sessions, "name", annotation.Session);
            string sessionName = FetchDBRef(DatabaseDefinitionCollections.Sessions, "name", sessionID);

            var filterScheme = builder.Eq("_id", schemeID);            
            var scheme = schemes.Find(filterScheme).Single();

            var filterAnnotation = builder.Eq("role_id", roleID) & builder.Eq("scheme_id", schemeID) & builder.Eq("annotator_id", annotatorID) & builder.Eq("session_id", sessionID);
            var annotationDocs = annotations.Find(filterAnnotation).ToList();

            if (annotationDocs.Count > 0)
            {
                annoList = new AnnoList();
                BsonDocument annotationDoc = annotationDocs[0];

                annoList.Meta.Annotator = annotatorName;
                annoList.Meta.AnnotatorFullName = annotatorFullName;

                annoList.Scheme = new AnnoScheme();
                if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.DISCRETE.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.DISCRETE;
                }
                else if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.FREE.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.FREE;
                }
                else if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.CONTINUOUS.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.CONTINUOUS;
                }

                else if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.POINT.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.POINT;
                }

                else if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.POLYGON.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.POLYGON;
                }

                else if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.SEGMENTATION.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.SEGMENTATION;
                }

                else if (scheme.TryGetElement("type", out value) && scheme["type"].ToString() == AnnoScheme.TYPE.GRAPH.ToString())
                {
                    annoList.Scheme.Type = AnnoScheme.TYPE.GRAPH;
                }

                annoList.Meta.Role = roleName;
                annoList.Scheme.Name = scheme["name"].ToString();
                var labels = annotationDoc["labels"].AsBsonArray;
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
                        string label = labels[i]["score"].ToString();
                        string confidence = labels[i]["conf"].ToString();
                        double start = i / annoList.Scheme.SampleRate;
                        double dur = 1 / annoList.Scheme.SampleRate;

                        AnnoListItem ali = new AnnoListItem(start, dur, label, "", Colors.Black, double.Parse(confidence));

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
                        //in case flag is set, if not ignore isValid
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

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", SchemeColor, double.Parse(confidence));
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

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", Colors.Black, double.Parse(confidence));
                        annoList.AddSorted(ali);
                    }
                }
                else if (annoList.Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    annoList.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme["color"].ToString());

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

                        AnnoListItem ali = new AnnoListItem(start, dur, label, "", annoList.Scheme.MinOrBackColor, confidence, true, pl);
                        annoList.Add(ali);
                    }
                }

                annoList.Source.Database.OID = annotationDoc["_id"].AsObjectId;
                annoList.Source.Database.Session = sessionName;
            }
            
            return annoList;
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
                AnnoList annoList = LoadAnnoList(annotation);
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

        public static ObjectId GetObjectID(string collection, string value, string attribute)
        {
            ObjectId id = new ObjectId();
            var builder = Builders<BsonDocument>.Filter;
            var filtera = builder.Eq(value, attribute);
            var result = database.GetCollection<BsonDocument>(collection).Find(filtera).ToList();

            if (result.Count > 0) id = result[0].GetValue(0).AsObjectId;

            return id;
        }
    }

    #region DATABASE TYPES

    public class DatabaseDBMeta
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Server { get; set; }
        public bool ServerAuth { get; set; }
    }

    public class DatabaseStream
    {
        public string Name { get; set; }

        public string Session { get; set; }

        public string Role { get; set; }

        public string Subject { get; set; }

        public string StreamName { get; set; }

        public string StreamType { get; set; }

        public string URL { get; set; }
        public bool ServerAuth { get; set; }
    }

    public class DatabaseSession
    {
        public string Name { get; set; }
        public string Language { get; set; }
        public string Location { get; set; }
        public DateTime Date { get; set; }

        public ObjectId OID { get; set; }
    }

    public class DatabaseUser
    {
        public string Name { get; set; }
        public string Password { get; set; }        
    }

    public class DatabaseRole
    {
        public string Name { get; set; }
    }

    public class DatabaseStreamType
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class DatabaseSubject
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Culture { get; set; }        
    }

    public class DatabaseAnnotator
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public int Expertise { get; set; }
    }

    public class DatabaseAnnotation
    {
        public ObjectId Id { get; set; }

        public string Role { get; set; }

        public string Scheme { get; set; }

        public string Annotator { get; set; }

        public string AnnotatorFullName { get; set; }

        public string Session { get; set; }

        public bool IsFinished { get; set; }

        public bool IsLocked { get; set; }

        public DateTime Date { get; set; }
    }

    public static class DatabaseDefinitionCollections
    {
        public static string Annotations = "Annotations";
        public static string Annotators = "Annotators";
        public static string Sessions = "Sessions";
        public static string Roles = "Roles";
        public static string Subjects = "Subjects";
        public static string Streams = "Streams";
        public static string StreamTypes = "StreamTypes";
        public static string Schemes = "Schemes";
        public static string Meta = "Meta";
    }

    #endregion
}
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver.Core;

namespace ssi
{
    internal class DatabaseHandler
    {
        private IMongoDatabase database;
        private MongoClient mongo;
        private string connectionstring = "mongodb://127.0.0.1:27017";

        public DatabaseHandler(string constr)
        {
            this.connectionstring = constr;
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

        public string LoadRoles(string db, string tier)
        {
            BsonElement value;
            string role = "None";
            List<string> roles = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("Roles");

            var sessions = collection.Find(_ => true).ToList();


            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true) roles.Add(document["name"].ToString());
            }

            //DataBaseResultsWindow dbw = new DataBaseResultsWindow(roles, false, "On tier " +tier + ": Who?");
            //dbw.SetSelectMultiple(false);
            //dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            //dbw.ShowDialog();

            int auth = checkAuth(Properties.Settings.Default.MongoDBUser);
            bool hasauth = false;
            if (auth > 3) hasauth = true;


            DatabaseUserTableWindow dbw = new DatabaseUserTableWindow(roles, hasauth, "On tier " + tier + ": Who ? ", "Roles");
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                role = dbw.Result().ToString();
            }
            return role;
        }


        public string LoadAnnotypes(string db, string tier)
        {
            BsonElement value;
            string annotype = "None";
            List<string> annotypes = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("AnnoTypes");

            var sessions = collection.Find(_ => true).ToList();


            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true) annotypes.Add(document["name"].ToString());
            }



            int auth = checkAuth(Properties.Settings.Default.MongoDBUser);
            bool hasauth = false;
            if (auth > 2) hasauth = true;

            DatabaseUserTableWindow dbw = new DatabaseUserTableWindow(annotypes, hasauth, "On tier " + tier + ": What ? ", "AnnoTypes");
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();


            if (dbw.DialogResult == true)
            {
                annotype = dbw.Result().ToString();
            }
            return annotype;
        }

        public void StoretoDatabase(string db, string session, string dbuser, List<AnnoTrack> anno_tracks = null)
        {

            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var annotations = database.GetCollection<BsonDocument>("Annotations");
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var annotypes = database.GetCollection<BsonDocument>("AnnoTypes");


            foreach (AnnoTrack a in anno_tracks)
            {

                string lowb = "", highb = "";
                if (!a.isDiscrete)
                {
                    lowb = a.AnnoList.Lowborder.ToString();
                    highb = a.AnnoList.Highborder.ToString();
                }


                ObjectId roleid;

             //   if (a.AnnoList.Role == null || a.AnnoList.Role == a.AnnoList.Name)
                    a.AnnoList.Role = LoadRoles(db, a.AnnoList.Name);

                var builder = Builders<BsonDocument>.Filter;
                var filter = builder.Eq("name", a.AnnoList.Role);
                var roledb = roles.Find(filter).ToList();
                if (roledb.Count > 0)
                {
                    roleid = roledb[0].GetValue(0).AsObjectId;
                    var update = Builders<BsonDocument>.Update.Set("isValid", true);
                    roles.UpdateOne(filter, update);
                }
                else
                {
                    BsonDocument b = new BsonDocument();
                    BsonElement n = new BsonElement("name", a.AnnoList.Name);
                    BsonElement m = new BsonElement("isValid", true);

                    b.Add(n);
                    b.Add(m);
                    roles.InsertOne(b);
                    roleid = b.GetValue(0).AsObjectId;
                }


                ObjectId annotid;

                string annotype = LoadAnnotypes(db, a.AnnoList.Name);
                var filtera = builder.Eq("name", annotype);
                var annotdb = annotypes.Find(filtera).ToList();
                if (annotdb.Count > 0)
                {
                    annotid = annotdb[0].GetValue(0).AsObjectId;
                    var update = Builders<BsonDocument>.Update.Set("isValid", true);
                    annotypes.UpdateOne(filter, update);
                }
                else
                {
                    BsonDocument b = new BsonDocument();
                    BsonElement n = new BsonElement("name", annotype);
                    BsonElement m = new BsonElement("isValid", true);
                    b.Add(n);
                    b.Add(m);
                    annotypes.InsertOne(b);
                    annotid = b.GetValue(0).AsObjectId;
                }


                BsonElement user = new BsonElement("annotator", dbuser);
                BsonElement annot = new BsonElement("annotype_id", annotid);
                BsonElement role = new BsonElement("role_id", roleid);
                BsonElement isdiscrete = new BsonElement("isDiscrete", a.isDiscrete.ToString());
                BsonElement rangeMin = new BsonElement("rangeMin", lowb);
                BsonElement rangeMax = new BsonElement("rangeMax", highb);
                BsonDocument document = new BsonDocument();


                BsonArray data = new BsonArray();
                if (a != null)
                {

                    for (int i = 0; i < a.AnnoList.Count; i++)
                    {

                        data.Add(new BsonDocument { { "Start", a.AnnoList[i].Start }, { "Stop", a.AnnoList[i].Stop }, { "Label", a.AnnoList[i].Label }, { "Confidence", a.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });

                    }

                }

                document.Add(user);
                document.Add(annot);
                document.Add(role);
                document.Add(isdiscrete);
                document.Add(rangeMin);
                document.Add(rangeMax);


                document.Add("annotation", data);

                var filter2 = builder.Eq("annotype_id", annotid) & builder.Eq("role_id", roleid) & builder.Eq("annotator", dbuser);
                var result = annotations.DeleteOne(filter2);
                annotations.InsertOne(document);


                ObjectId annoid = document.GetValue(0).AsObjectId;


                BsonArray annos = new BsonArray();
                bool annoalreadypresent = false;

                var filter3 = builder.Eq("name", session);
                var documents = sessions.Find(filter3).ToList();


                if (documents.Count > 0)
                {
                    string id = documents[0]["name"].ToString();
                    annos = documents[0]["annotations"].AsBsonArray;


                    for (int j = 0; j < annos.Count; j++)
                    {
                        if (annos[j]["annotation_id"].ToString() == annoid.ToString())
                        {
                            annoalreadypresent = true;
                        }
                    }
                }


                if (!annoalreadypresent)
                {
                    annos.Add(new BsonDocument { { "annotation_id", annoid } });
                    var update2 = Builders<BsonDocument>.Update.Set("annotations", annos);
                    sessions.UpdateOne(filter3, update2);
                }




            }

        }



        public List<AnnoList> LoadfromDatabase(System.Collections.IList collections, string db, string session, string dbuser)
        {
            BsonElement value;
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var collection = database.GetCollection<BsonDocument>("Annotations");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var annotypes = database.GetCollection<BsonDocument>("AnnoTypes");



            List<AnnoList> l = new List<AnnoList>();

            foreach (DatabaseAnno s in collections)
            {


                AnnoList al = new AnnoList();
               
                ObjectId roleid = GetObjectID(database, "Roles", "name", s.Role);
                string roledb = FetchDBRef(database, "Roles", "name", roleid);


                ObjectId annotid = GetObjectID(database, "AnnoTypes", "name", s.AnnoType);
                string annotdb = FetchDBRef(database, "AnnoTypes", "name", annotid);


                var builder = Builders<BsonDocument>.Filter;
              

                var filter = builder.Eq("role_id", roleid) & builder.Eq("annotype_id", annotid) & builder.Eq("annotator", s.Annotator);
                var result = collection.Find(filter);
                var documents = collection.Find(filter).ToList();
                if (documents[0].TryGetElement("isDiscrete", out value) && documents[0]["isDiscrete"].ToString().Contains("True")) al.isDiscrete = true;
                else al.isDiscrete = false;

                al.Role = roledb;
                al.Name = al.Role + "_" + annotdb;

                if (!al.isDiscrete)
                {
                    if (documents[0].TryGetElement("rangeMin", out value)) al.Lowborder = double.Parse(documents[0]["rangeMin"].ToString());
                    if (documents[0].TryGetElement("rangeMax", out value)) al.Highborder = double.Parse(documents[0]["rangeMax"].ToString());
                }

                var annotation = documents[0]["annotation"].AsBsonArray;

                for (int i = 0; i < annotation.Count; i++)
                {

                    double start = double.Parse(annotation[i]["Start"].ToString());
                    double stop = double.Parse(annotation[i]["Stop"].ToString());
                    double duration = stop - start;
                    string label = annotation[i]["Label"].ToString();
                    string confidence = annotation[i]["Confidence"].ToString();
                    // string  color = annotation[i]["Color"].ToString();
                    AnnoListItem ali = new AnnoListItem(start, duration, label, "", al.Name, "#000000", double.Parse(confidence));
                    al.Add(ali);
                }

                l.Add(al);
            }


            return l;

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

    }


    public class DatabaseAnno
    {
        public string Role { get; set; }

        public string AnnoType { get; set; }

        public string Annotator { get; set; }
    }


    public class DatabaseSession
    {
        public string Name { get; set; }

        public string Language { get; set; }

        public string Location { get; set; }

        public string Date { get; set; }
    }

    public class DatabaseMediaInfo
    {
        public string connection;
        public string ip;
        public string folder;
        public string login;
        public string pw;
        public string filepath;
        public string filename;
        public string requiresauth;
        public string role;
        public string subject;
        public string mediatype;
    }
}



            /*
             * * Legacy code: example how to write/read subcollections
             *  
             *  
                        BsonElement name = new BsonElement("document", "mediainfo");
                        BsonDocument document = new BsonDocument();
                        BsonDocument[] file = new BsonDocument[filenames.Length];


                        for (int i = 0; i< filenames.Length;i++)
                        {
                            file[i] = new BsonDocument
                            {
                                 { "connection", connection },
                                 { "ip", ip },
                                 { "folder", folder },
                                 { "FilePath", filenames[i] }

                            };
                        }

                        BsonDocument filecontainer = new BsonDocument();
                        for(int i=0; i<filenames.Length;i++)
                        {
                            filecontainer.Add(i.ToString(), file[i]);
                        }

                        document.Add(name);
                        document.Add("files", filecontainer); 

                        var builder = Builders<BsonDocument>.Filter;
                        var filter= builder.Eq("document", "mediainfo") /*& builder.Eq("files.Filename1.FilePath", "somepath");
                        var result = collections.DeleteOne(filter);
                        collections.InsertOne(document);
        }

    load 
    var builder = Builders<BsonDocument>.Filter;
    var filter = builder.Eq("document", "mediainfo") /*& builder.Eq("files.Filename1.FilePath", "somepath");
    var result = colllection.Find(filter);

    var bson = result.ToBsonDocument();

    var documents = colllection.Find(filter).ToList();
    var files = documents[0]["files"];

    for (int i = 0; i < files.ToBsonDocument().ElementCount; i++)
    {
        DatabaseMediaInfo c = new DatabaseMediaInfo();
        c.connection = files.ToBsonDocument()[i]["connection"].ToString();
        c.ip = files.ToBsonDocument()[i]["ip"].ToString();
        c.folder = files.ToBsonDocument()[i]["folder"].ToString();
        c.filename = files.ToBsonDocument()[i]["FilePath"].ToString();

       do something

    }



*/

﻿using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;

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

        public string LoadRoles(string db, AnnoTrack tier)
        {
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

            string name = "New track";
            if (tier != null) name = tier.AnnoList.Name;

            DatabaseUserTableWindow dbw = new DatabaseUserTableWindow(roles, hasauth, "Tier: " + name + ". Who is annotated? ", "Roles");
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                role = dbw.Result().ToString();
            }
            return role;
        }

        public string LoadAnnotationSchemes(string db, AnnoTrack tier, int type = 0)
        {
            string annotype = "None";
            List<string> AnnotationSchemes = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("AnnotationSchemes");
   

            var sessions = collection.Find(_ => true).ToList();

            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true)
                {
                    if (type == 0) AnnotationSchemes.Add(document["name"].ToString());
                    else if (type == 1 && document["type"].ToString() == "DISCRETE") AnnotationSchemes.Add(document["name"].ToString());
                    else if (type == 2 && document["type"].ToString() == "CONTINUOUS") AnnotationSchemes.Add(document["name"].ToString());
                }
            }

            int auth = checkAuth(Properties.Settings.Default.MongoDBUser);
            bool hasauth = false;
            if (auth > 2) hasauth = true;

            string name = "New Track";
            if (tier != null) name = tier.AnnoList.Name;

            DatabaseUserTableWindow dbw = new DatabaseUserTableWindow(AnnotationSchemes, hasauth, "Tier: " + name + ". What is annotated? ", "AnnotationSchemes", type, true, tier);
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                annotype = dbw.Result().ToString();
            }
            return annotype;
        }

        public void StoretoDatabase(string db, string session, string dbuser, List<AnnoTrack> anno_tracks = null, List<DatabaseMediaInfo> loadedDBmedia = null)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);

            BsonArray labels = new BsonArray();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var annotations = database.GetCollection<BsonDocument>("Annotations");
            var annotators = database.GetCollection<BsonDocument>("Annotators");
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var medias = database.GetCollection<BsonDocument>("Media");
            var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");

            foreach (AnnoTrack a in anno_tracks)
            {
                string lowb = "", highb = "";
                if (!a.isDiscrete)
                {
                    lowb = a.AnnoList.Lowborder.ToString();
                    highb = a.AnnoList.Highborder.ToString();
                }

                ObjectId roleid;

                if (a.AnnoList.Role == null || a.AnnoList.Role == a.AnnoList.Name)
                    a.AnnoList.Role = LoadRoles(db, a);

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

                BsonDocument annotatordoc = new BsonDocument();

                //We could choose here if we want to overwrite other peoples annotations. For now, we we might want to overwrite automatically created annotations and own annotations only

                if (!(a.AnnoList.Annotator == null || a.AnnoList.Annotator == dbuser || a.AnnoList.Annotator == "RMS" || a.AnnoList.Annotator == "Median")) break;
                if (a.AnnoList.Annotator == null) a.AnnoList.Annotator = dbuser;

                BsonElement annotatorname = new BsonElement("name", a.AnnoList.Annotator);
                BsonElement annotatoremail = new BsonElement("email", "");
                BsonElement annotatorexpertise = new BsonElement("expertise", "");

                annotatordoc.Add(annotatorname);
                annotatordoc.Add(annotatoremail);
                annotatordoc.Add(annotatorexpertise);

                var filterannotator = builder.Eq("name", a.AnnoList.Annotator);
                UpdateOptions uoa = new UpdateOptions();
                uoa.IsUpsert = true;
                var resann = annotators.ReplaceOne(filterannotator, annotatordoc, uoa);
                ObjectId annotatoroID = annotators.Find(filterannotator).Single()["_id"].AsObjectId;

                ObjectId sessionID;

                var filtersid = builder.Eq("name", session);
                var ses = sessions.Find(filtersid).Single();
                sessionID = ses.GetValue(0).AsObjectId;

                ObjectId annotid;
                string annotype = null;
                if (a.AnnoList.AnnotationScheme != null) annotype = a.AnnoList.AnnotationScheme.name;


                if (annotype == null) annotype = LoadAnnotationSchemes(db, a);
                var filtera = builder.Eq("name", annotype);
                var annotdb = annotationschemes.Find(filtera).ToList();
                annotid = annotdb[0].GetValue(0).AsObjectId;
                var update2 = Builders<BsonDocument>.Update.Set("isValid", true);
                annotationschemes.UpdateOne(filter, update2);
                
                //else
                //{
                //    string type = "DISCRETE";
                //    if (a.AnnoList.isDiscrete) type = "CONTINUOUS";

                //    BsonDocument b = new BsonDocument();
                //    BsonElement n = new BsonElement("name", annotype);
                //    BsonElement t = new BsonElement("type", type);
                //    BsonElement m = new BsonElement("isValid", true);
                //    b.Add(n);
                //    b.Add(t);
                //    b.Add(m);

                //    if (type == "DISCRETE")
                //    {
                //        BsonElement co = new BsonElement("color", a.AnnoList.AnnotationScheme.mincolor);
                //        int index = 0;

                //        foreach (AnnoListItem ali in a.AnnoList)
                //        {
                //            labels.Add(new BsonDocument { { "id", index++ }, { "name", ali.Label }, { "color", ali.Bg }, { "IsValid", true } });
                //        }
                //        b.Add(co);
                //        b.Add("labels", labels);
                //    }


                //    else if(type == "CONTINUOUS")
                //    {
                //        BsonElement sr = new BsonElement("sr", a.samplerate);
                //        BsonElement min = new BsonElement("min", a.AnnoList.Lowborder);
                //        BsonElement max = new BsonElement("max", a.AnnoList.Highborder);
                //        //TODO
                //        BsonElement mincol = new BsonElement("min_color", a.BackgroundColor.ToString());
                //        BsonElement maxcol = new BsonElement("max_color", a.BackgroundColor.ToString());

                //        b.Add(sr);
                //        b.Add(min);
                //        b.Add(max);
                //        b.Add(mincol);
                //        b.Add(maxcol);
                //    }

                //    annotationschemes.InsertOne(b);
                //    annotid = b.GetValue(0).AsObjectId;
                //}

                BsonElement user = new BsonElement("annotator_id", annotatoroID);
                BsonElement role = new BsonElement("role_id", roleid);
                BsonElement annot = new BsonElement("scheme_id", annotid);
                BsonElement sessionid = new BsonElement("session_id", sessionID);
                BsonElement date = new BsonElement("date", new BsonDateTime(DateTime.Now));
                BsonDocument document = new BsonDocument();

                BsonArray media = new BsonArray();

                if (loadedDBmedia != null)
                {
                    foreach (DatabaseMediaInfo dmi in loadedDBmedia)
                    {
                        BsonDocument mediadocument = new BsonDocument();
                        ObjectId mediaid;

                        var filtermedia = builder.Eq("name", dmi.filename) & builder.Eq("session_id", sessionID);
                        var mediadb = medias.Find(filtermedia).ToList();

                        if (mediadb.Count > 0)
                        {
                            mediaid = mediadb[0].GetValue(0).AsObjectId;

                            BsonElement media_id = new BsonElement("media_id", mediaid);
                            mediadocument.Add(media_id);
                            media.Add(mediadocument);
                        }
                    }
                }

                BsonArray data = new BsonArray();
                document.Add(sessionid);
                document.Add(user);
                document.Add(role);
                document.Add(annot);
                document.Add(date);
                document.Add("media", media);

                if (a != null)
                {
                
                    if  (a.AnnoList.AnnotationType == 0)
                    {
                        BsonArray Labels = annotdb[0]["labels"].AsBsonArray;
                        int index = 0;
                        for (int i = 0; i < a.AnnoList.Count; i++)
                        {
                            for (int j = 0; j < Labels.Count; j++)
                            {
                                if (a.AnnoList[i].Label == Labels[j]["name"].ToString())
                                {
                                    index = Labels[j]["id"].AsInt32;
                                    data.Add(new BsonDocument { { "from", a.AnnoList[i].Start }, { "to", a.AnnoList[i].Stop }, { "id", index }, { "conf", a.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                                    break;
                                }
                            }
                        }

                    }

                    else if (a.AnnoList.AnnotationType == 1)
                    {
                        for (int i = 0; i < a.AnnoList.Count; i++)
                        {
                            data.Add(new BsonDocument { { "from", a.AnnoList[i].Start }, { "to", a.AnnoList[i].Stop }, { "name", a.AnnoList[i].Label }, { "conf", a.AnnoList[i].Confidence } } );
                               
                        }

                    }



                    else if  (a.AnnoList.AnnotationType == 2)
                    {
                        for (int i = 0; i < a.AnnoList.Count; i++)
                        {
                            data.Add(new BsonDocument { { "score", a.AnnoList[i].Label }, { "conf", a.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                        }

                    }

                    document.Add("labels", data);
                }

                var filter2 = builder.Eq("scheme_id", annotid) & builder.Eq("role_id", roleid) & builder.Eq("annotator_id", annotatoroID) & builder.Eq("session_id", sessionID);

                ObjectId annoid = new ObjectId();
                var res = annotations.Find(filter2).ToList();
                if (res.Count > 0)
                {
                    annoid = res[0].GetElement(0).Value.AsObjectId;
                }

                UpdateOptions uo = new UpdateOptions();
                uo.IsUpsert = true;
                var result = annotations.ReplaceOne(filter2, document, uo);
            }
        }

        public AnnotationScheme GetAnnotationScheme(string name, int isDiscrete)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(Properties.Settings.Default.Database);
            BsonElement value;
            AnnotationScheme Scheme = new AnnotationScheme();
            Scheme.LabelsAndColors = new List<LabelColorPair>();
            var annoschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
            var builder = Builders<BsonDocument>.Filter;
            string type = "DISCRETE";
            if (isDiscrete == 1) type = "FREE";
            if (isDiscrete == 2) type = "CONTINUOUS";
            var filterscheme = builder.Eq("name", name) & builder.Eq("type", type);
            var annosch = annoschemes.Find(filterscheme).ToList();

            if (annosch[0].TryGetElement("type", out value)) Scheme.type = annosch[0]["type"].ToString();
            Scheme.name = annosch[0]["name"].ToString();
            if (Scheme.type == "CONTINUOUS")
            {
                if (annosch[0].TryGetElement("min", out value)) Scheme.minborder = annosch[0]["min"].ToDouble();
                if (annosch[0].TryGetElement("max", out value)) Scheme.maxborder = annosch[0]["max"].ToDouble();
                if (annosch[0].TryGetElement("sr", out value)) Scheme.sr = annosch[0]["sr"].ToDouble();

                if (annosch[0].TryGetElement("min_color", out value)) Scheme.mincolor = annosch[0]["min_color"].ToString();
                if (annosch[0].TryGetElement("max_color", out value)) Scheme.maxcolor = annosch[0]["max_color"].ToString();
            }
            else if(type == "DISCRETE")
            {
                if (annosch[0].TryGetElement("color", out value)) Scheme.mincolor = annosch[0]["color"].ToString();
                BsonArray schemelabels = annosch[0]["labels"].AsBsonArray;
                string SchemeLabel = "";
                string SchemeColor = "#000000";
                for (int j = 0; j < schemelabels.Count; j++)
                {
                    SchemeLabel = schemelabels[j]["name"].ToString();
                    SchemeColor = schemelabels[j]["color"].ToString();
                    LabelColorPair lcp = new LabelColorPair(schemelabels[j]["name"].ToString(), schemelabels[j]["color"].ToString());
                    bool alreadyinscheme = false;

                    Scheme.LabelsAndColors.Add(lcp);
                }
            }


            else if (type == "FREE")
            {
                if (annosch[0].TryGetElement("color", out value)) Scheme.mincolor = annosch[0]["color"].ToString();
              
            }
            return Scheme;
        }

        public List<AnnoList> LoadfromDatabase(System.Collections.IList collections, string db, string session, string dbuser)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var collection = database.GetCollection<BsonDocument>("Annotations");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var annoschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");

            List<AnnoList> l = new List<AnnoList>();

            foreach (DatabaseAnno s in collections)
            {
                BsonElement value;
                AnnoList al = new AnnoList();

                ObjectId roleid = GetObjectID(database, "Roles", "name", s.Role);
                string roledb = FetchDBRef(database, "Roles", "name", roleid);

                ObjectId annotid = GetObjectID(database, "AnnotationSchemes", "name", s.AnnoType);
                string annotdb = FetchDBRef(database, "AnnotationSchemes", "name", annotid);

                ObjectId annotatid = GetObjectID(database, "Annotators", "name", s.Annotator);
                string annotatdb = FetchDBRef(database, "Annotators", "name", annotatid);
                al.Annotator = annotatdb;

                ObjectId sessionid = GetObjectID(database, "Sessions", "name", session);
                string sessiondb = FetchDBRef(database, "Sessions", "name", sessionid);
                al.Annotator = annotatdb;

                var builder = Builders<BsonDocument>.Filter;

                var filterscheme = builder.Eq("_id", annotid);
                var result = collection.Find(filterscheme);
                var annosch = annoschemes.Find(filterscheme).Single();

                var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
                var documents = collection.Find(filter).ToList();

                if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "DISCRETE")
                {
                    al.AnnotationType = 0;
                }

                else if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "FREE")
                {
                    al.AnnotationType = 1;
                }
                else if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "CONTINUOUS")
                {
                    al.AnnotationType = 2;
                }

                al.Role = roledb;
                al.Name = al.Role + " #" + annotdb + " #" + annotatdb;

                al.AnnotationScheme = new AnnotationScheme();
                al.AnnotationScheme.name = annosch["name"].ToString();
                var annotation = documents[0]["labels"].AsBsonArray;
                if (al.AnnotationType == 2)
                {
                    if (annosch.TryGetElement("min", out value)) al.Lowborder = double.Parse(annosch["min"].ToString());
                    if (annosch.TryGetElement("max", out value)) al.Highborder = double.Parse(annosch["max"].ToString());
                    if (annosch.TryGetElement("sr", out value)) al.SR = double.Parse(annosch["sr"].ToString());

                    if (annosch.TryGetElement("min_color", out value)) al.AnnotationScheme.mincolor = annosch["min_color"].ToString();
                    if (annosch.TryGetElement("max_color", out value)) al.AnnotationScheme.maxcolor = annosch["max_color"].ToString();

                    al.AnnotationScheme.minborder = al.Lowborder;
                    al.AnnotationScheme.maxborder = al.Highborder;
                    al.AnnotationScheme.sr = al.SR;

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        string label = annotation[i]["score"].ToString();
                        string confidence = annotation[i]["conf"].ToString();
                        double start = i * ((1000.0 / al.SR) / 1000.0);
                        double dur = (1000.0 / al.SR) / 1000.0;

                        AnnoListItem ali = new AnnoListItem(start, dur, label, "", al.Name, "#000000", double.Parse(confidence));

                        al.Add(ali);
                    }
                
                }
                else if (al.AnnotationType == 0)
                {


                    al.AnnotationScheme.mincolor = annosch["color"].ToString();

                    al.AnnotationScheme.LabelsAndColors = new List<LabelColorPair>();

                    BsonArray schemelabels = annosch["labels"].AsBsonArray;

                    for (int j = 0; j < schemelabels.Count; j++)
                    {
                        al.AnnotationScheme.LabelsAndColors.Add(new LabelColorPair(schemelabels[j]["name"].ToString(), schemelabels[j]["color"].ToString()));
                    }

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        string SchemeLabel = "";
                        string SchemeColor = "#000000";

                        for (int j = 0; j < schemelabels.Count; j++)
                        {
                            if (annotation[i]["id"].AsInt32 == schemelabels[j]["id"].AsInt32)
                            {
                                SchemeLabel = schemelabels[j]["name"].ToString();
                                SchemeColor = schemelabels[j]["color"].ToString();
                                break;
                            }
                        }

                        double start = double.Parse(annotation[i]["from"].ToString());
                        double stop = double.Parse(annotation[i]["to"].ToString());
                        double duration = stop - start;
                        string label = SchemeLabel;
                        string confidence = annotation[i]["conf"].ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", al.Name, SchemeColor, double.Parse(confidence));
                        al.Add(ali);


                    }
                }

                else if (al.AnnotationType == 1)
                {

                    al.AnnotationScheme.mincolor = annosch["color"].ToString();

                    for (int i = 0; i < annotation.Count; i++)
                    {
                       
                        double start = double.Parse(annotation[i]["from"].ToString());
                        double stop = double.Parse(annotation[i]["to"].ToString());
                        double duration = stop - start;
                        string label = annotation[i]["name"].ToString();
                        string confidence = annotation[i]["conf"].ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", al.Name, "#000000", double.Parse(confidence));
                        al.Add(ali);
                    }

                }
             
                l.Add(al);
                al = null;
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

        public string Session { get; set; }
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
        public string session;
    }
}
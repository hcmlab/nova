using MongoDB.Bson;
using MongoDB.Driver;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Windows;

namespace ssi
{
    internal class DatabaseHandler
    {
        private IMongoDatabase database;
        private MongoClient mongo;
        private string connectionstring = "mongodb://127.0.0.1:27017";

        private string GARBAGELABEL = "GARBAGE";
        private string GARBAGECOLOR = "#FF000000";

        public DatabaseHandler(string constr)
        {
            this.connectionstring = constr;
        }

        public int checkAuth(string dbuser, string db = "admin")
        {
            //4 = root
            //3 = admin
            //2 = write
            //1 = read
            //0 = notauthorized

            int auth = 0;
            try
            {
                mongo = new MongoClient(connectionstring);
                var adminDB = mongo.GetDatabase("admin");
                var cmd = new BsonDocument("usersInfo", dbuser);
                var queryResult = adminDB.RunCommand<BsonDocument>(cmd);
                var roles = (BsonArray)queryResult[0][0]["roles"];

                for (int i = 0; i < roles.Count; i++)
                {
                    if ((roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && roles[i]["db"] == db || (roles[i]["role"].ToString() == "userAdminAnyDatabase"  || roles[i]["role"].ToString() == "dbAdminAnyDatabase")) && auth <= 4) { auth = 4; }
                    else if ((roles[i]["role"].ToString() == "dbAdmin" && roles[i]["db"] == db) && auth <= 3) { auth = 3; }
                    else if ((roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && roles[i]["db"] == db || roles[i]["role"].ToString() == "read" && roles[i]["db"] == db) && auth <= 2) { auth = 2; }
                    else if ((roles[i]["role"].ToString() == "readAnyDatabase") && auth <= 1) { auth = 1; }


                    //edit/add more roles if you want to change security levels
                }
            }
            catch(Exception e)
            {
               
               
            }

            return auth;
        }



        //public void sendmail(string adressto, string subject, string body)
        //{
        //    //dummy code, need some logic but hey, automated notifications
        //    MailMessage objeto_mail = new MailMessage();
        //    SmtpClient client = new SmtpClient();
        //    client.Port = 25;
        //    client.Host = "smtp.internal.mycompany.com";
        //    client.Timeout = 10000;
        //    client.DeliveryMethod = SmtpDeliveryMethod.Network;
        //    client.UseDefaultCredentials = false;
        //    client.Credentials = new System.Net.NetworkCredential("user", "Password");
        //    objeto_mail.From = new MailAddress("from@server.com");
        //    objeto_mail.To.Add(new MailAddress(adressto));
        //    objeto_mail.Subject = subject;
        //    objeto_mail.Body = body;
        //    client.Send(objeto_mail);
        //}


        public string LoadRoles(string db, AnnoTrack tier)
        {
            string role = null;
            List<string> roles = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("Roles");

            var sessions = collection.Find(_ => true).ToList();

            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true) roles.Add(document["name"].ToString());
            }

           
            int auth = checkAuth(Properties.Settings.Default.MongoDBUser, db);
            bool hasauth = false;
            if (auth > 3) hasauth = true;

            string name = "New track";
            if (tier != null) name = tier.AnnoList.Name;

            DatabaseSelectionWindow dbw = new DatabaseSelectionWindow(roles, hasauth, "Tier: " + name + ". Who is annotated? ", "Roles");
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                role = dbw.Result().ToString();
            }

            return role;
        }

        public string LoadAnnotationSchemes(string db, AnnoTrack tier, AnnoType type = AnnoType.DISCRETE)
        {
            string annotype = null;
            List<string> AnnotationSchemes = new List<string>();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            IMongoCollection<BsonDocument> collection = database.GetCollection<BsonDocument>("AnnotationSchemes");

            var sessions = collection.Find(_ => true).ToList();

            foreach (var document in sessions)
            {
                if (document["isValid"].AsBoolean == true)
                {
                    if ((type == AnnoType.DISCRETE && document["type"].ToString() == "DISCRETE")) AnnotationSchemes.Add(document["name"].ToString());
                    else if  (type == AnnoType.FREE && (document["type"].ToString() == "FREE")) AnnotationSchemes.Add(document["name"].ToString());
                    else if (type == AnnoType.CONTINUOUS && document["type"].ToString() == "CONTINUOUS") AnnotationSchemes.Add(document["name"].ToString());
                }
            }

            int auth = checkAuth(Properties.Settings.Default.MongoDBUser, db);
            bool hasauth = false;
            if (auth > 2) hasauth = true;

            string name = "New Track";
            if (tier != null) name = tier.AnnoList.Name;

            DatabaseSelectionWindow dbw = new DatabaseSelectionWindow(AnnotationSchemes, hasauth, "Tier: " + name + ". What is annotated? ", "AnnotationSchemes", type, true, tier);
            dbw.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            dbw.ShowDialog();

            if (dbw.DialogResult == true)
            {
                annotype = dbw.Result().ToString();
            }
            return annotype;
        }

        public AnnotationScheme GetAnnotationScheme(string name, AnnoType isDiscrete)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(Properties.Settings.Default.Database);
            BsonElement value;
            AnnotationScheme Scheme = new AnnotationScheme();
            Scheme.LabelsAndColors = new List<LabelColorPair>();
            var annoschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
            var builder = Builders<BsonDocument>.Filter;

            FilterDefinition<BsonDocument> filterscheme;
            if (isDiscrete != AnnoType.CONTINUOUS)
            {
                filterscheme = builder.Eq("name", name) & (builder.Eq("type", "DISCRETE") | builder.Eq("type", "FREE"));
            }
            else filterscheme = builder.Eq("name", name) & builder.Eq("type", "CONTINUOUS");
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
            else if (Scheme.type == "DISCRETE")
            {
                if (annosch[0].TryGetElement("color", out value)) Scheme.mincolor = annosch[0]["color"].ToString();
                BsonArray schemelabels = annosch[0]["labels"].AsBsonArray;
                string SchemeLabel = "";
                string SchemeColor = "#000000";
                for (int j = 0; j < schemelabels.Count; j++)
                {
                    try
                    {
                        if (schemelabels[j]["isValid"].AsBoolean == true)
                        {
                            SchemeLabel = schemelabels[j]["name"].ToString();
                            SchemeColor = schemelabels[j]["color"].ToString();
                            LabelColorPair lcp = new LabelColorPair(schemelabels[j]["name"].ToString(), schemelabels[j]["color"].ToString());

                            Scheme.LabelsAndColors.Add(lcp);
                        }

                    }
                    catch
                    {
                        SchemeLabel = schemelabels[j]["name"].ToString();
                        SchemeColor = schemelabels[j]["color"].ToString();
                        LabelColorPair lcp = new LabelColorPair(schemelabels[j]["name"].ToString(), schemelabels[j]["color"].ToString());

                        Scheme.LabelsAndColors.Add(lcp);
                    }
                  
                }
            }
            else if (Scheme.type == "FREE")
            {
                if (annosch[0].TryGetElement("color", out value)) Scheme.mincolor = annosch[0]["color"].ToString();
            }
            return Scheme;
        }

        public string StoreToDatabase(string db, string session, string dbuser, AnnoTrack annoTrack, List<DatabaseMediaInfo> loadedDBmedia = null, bool isfin = false)
        {
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            string annotator = null;

            BsonArray labels = new BsonArray();
            mongo = new MongoClient(connectionstring);
            database = mongo.GetDatabase(db);
            var annotations = database.GetCollection<BsonDocument>("Annotations");
            var annotators = database.GetCollection<BsonDocument>("Annotators");
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var roles = database.GetCollection<BsonDocument>("Roles");
            var medias = database.GetCollection<BsonDocument>("Media");
            var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");

            string lowb = "", highb = "";
            if (!annoTrack.isDiscrete)
            {
                lowb = annoTrack.AnnoList.Lowborder.ToString();
                highb = annoTrack.AnnoList.Highborder.ToString();
            }

            ObjectId roleid;

            if (annoTrack.AnnoList.Role == null || annoTrack.AnnoList.Role == annoTrack.AnnoList.Name)
            {
                annoTrack.AnnoList.Role = LoadRoles(db, annoTrack);
                if (annoTrack.AnnoList.Role == null)
                {
                    return null;
                }
            }

            var builder = Builders<BsonDocument>.Filter;
            var filter = builder.Eq("name", annoTrack.AnnoList.Role);
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
                BsonElement n = new BsonElement("name", annoTrack.AnnoList.Name);
                BsonElement m = new BsonElement("isValid", true);

                b.Add(n);
                b.Add(m);
                roles.InsertOne(b);
                roleid = b.GetValue(0).AsObjectId;
            }

            BsonDocument annotatordoc = new BsonDocument();

            //We could choose here if we want to overwrite other peoples annotations. For now, we we might 
            //want to overwrite automatically created annotations and own annotations only. Here are two examples to e.g. let an admin allow to change existing annotations or a user called "system

            // if(checkAuth(dbuser) > 3)
            if (dbuser == "system")
            {

                if (annoTrack.AnnoList.Annotator == null && annoTrack.AnnoList.AnnotatorFullName != null)
                {
                    ObjectId annotatid = GetObjectID(database, "Annotators", "fullname", annoTrack.AnnoList.AnnotatorFullName);
                    annoTrack.AnnoList.Annotator = FetchDBRef(database, "Annotators", "name", annotatid);
                }
                else if (annoTrack.AnnoList.Annotator == null && annoTrack.AnnoList.AnnotatorFullName == null)
                {
                    annoTrack.AnnoList.Annotator = dbuser;
                }

                if (annoTrack.AnnoList.AnnotatorFullName == null)
                {
                    try
                    {
                        ObjectId annotatid = GetObjectID(database, "Annotators", "name", annoTrack.AnnoList.Annotator);
                        annoTrack.AnnoList.AnnotatorFullName = FetchDBRef(database, "Annotators", "fullname", annotatid);
                    }
                    catch
                    {
                        annoTrack.AnnoList.AnnotatorFullName = Properties.Settings.Default.Annotator;
                    }
                }
            }

            else if (!(annoTrack.AnnoList.AnnotatorFullName == "RMS" || annoTrack.AnnoList.AnnotatorFullName == "Median"))
            {
                try
                {
                    annoTrack.AnnoList.Annotator = dbuser;
                    ObjectId annotatid = GetObjectID(database, "Annotators", "name", dbuser);
                    annoTrack.AnnoList.AnnotatorFullName = FetchDBRef(database, "Annotators", "fullname", annotatid);
                }
                catch
                {
                    annoTrack.AnnoList.AnnotatorFullName = Properties.Settings.Default.Annotator;
                }

            }
            else annoTrack.AnnoList.Annotator = annoTrack.AnnoList.AnnotatorFullName;

            annotator = annoTrack.AnnoList.AnnotatorFullName;

            //  if (!(a.AnnoList.Annotator == null || a.AnnoList.Annotator == dbuser || a.AnnoList.Annotator == "RMS" || a.AnnoList.Annotator == "Median")) break; //?? Not called at the moment

            BsonElement annotatorname = new BsonElement("name", annoTrack.AnnoList.Annotator);
            BsonElement annotatornamefull = new BsonElement("fullname", annoTrack.AnnoList.AnnotatorFullName);

            annotatordoc.Add(annotatorname);
            annotatordoc.Add(annotatornamefull);

            var filterannotator = builder.Eq("name", annoTrack.AnnoList.Annotator);
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
            if (annoTrack.AnnoList.AnnotationScheme != null) annotype = annoTrack.AnnoList.AnnotationScheme.name;

            if (annotype == null)
            {
                annotype = LoadAnnotationSchemes(db, annoTrack, annoTrack.AnnoList.AnnotationType);
            }

            if (annotype == null)
            {
                return null;
            }
            var filtera = builder.Eq("name", annotype);
            var annotdb = annotationschemes.Find(filtera).ToList();
            annotid = annotdb[0].GetValue(0).AsObjectId;
            var annoschemetypedb = annotdb[0]["type"];
            var update2 = Builders<BsonDocument>.Update.Set("isValid", true);
            annotationschemes.UpdateOne(filter, update2);

            BsonElement user = new BsonElement("annotator_id", annotatoroID);
            BsonElement role = new BsonElement("role_id", roleid);
            BsonElement annot = new BsonElement("scheme_id", annotid);
            BsonElement sessionid = new BsonElement("session_id", sessionID);
            BsonElement isfinished = new BsonElement("isFinished", isfin);
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
            document.Add(isfinished);
            document.Add("media", media);

            if (annoTrack != null)
            {
                if (annoschemetypedb == "DISCRETE")
                {
                    BsonArray Labels = annotdb[0]["labels"].AsBsonArray;
                    int index = 0;
                    for (int i = 0; i < annoTrack.AnnoList.Count; i++)
                    {
                        for (int j = 0; j < Labels.Count; j++)
                        {
                            if (annoTrack.AnnoList[i].Label == Labels[j]["name"].ToString())
                            {
                                index = Labels[j]["id"].AsInt32;
                                data.Add(new BsonDocument { { "from", annoTrack.AnnoList[i].Start }, { "to", annoTrack.AnnoList[i].Stop }, { "id", index }, { "conf", annoTrack.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                                break;
                            }
                            else if (annoTrack.AnnoList[i].Label == GARBAGELABEL)
                            {
                                data.Add(new BsonDocument { { "from", annoTrack.AnnoList[i].Start }, { "to", annoTrack.AnnoList[i].Stop }, { "id", -1 }, { "conf", annoTrack.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
                                break;
                            }
                        }
                    }
                }
                else if (annoschemetypedb == "FREE")
                {
                    for (int i = 0; i < annoTrack.AnnoList.Count; i++)
                    {
                        data.Add(new BsonDocument { { "from", annoTrack.AnnoList[i].Start }, { "to", annoTrack.AnnoList[i].Stop }, { "name", annoTrack.AnnoList[i].Label }, { "conf", annoTrack.AnnoList[i].Confidence } });
                    }
                }
                else if (annoschemetypedb == "CONTINUOUS")
                {
                    for (int i = 0; i < annoTrack.AnnoList.Count; i++)
                    {
                        data.Add(new BsonDocument { { "score", annoTrack.AnnoList[i].Label }, { "conf", annoTrack.AnnoList[i].Confidence }, /*{ "Color", a.AnnoList[i].Bg }*/ });
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

            var checklock = annotations.Find(filter2).Single();

            bool islocked = false;
            try
            {
                islocked = checklock["isLocked"].AsBoolean;

            }
            catch (Exception ex) { }

            if (!islocked)
            {
                var result = annotations.ReplaceOne(filter2, document, uo);
            }
            else {
                MessageBox.Show("Annotaion is locked and therefore can't be overwritten");
                    return null;
                    };


            return annotator;
        }

        public List<AnnoList> LoadFromDatabase(System.Collections.IList collections, string db, string session, string dbuser)
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

                ObjectId annotatid = GetObjectID(database, "Annotators", "fullname", s.AnnotatorFullname);
                string annotatdb = FetchDBRef(database, "Annotators", "name", annotatid);
                string annotatdbfn = FetchDBRef(database, "Annotators", "fullname", annotatid);
                al.Annotator = annotatdb;
                al.AnnotatorFullName = annotatdbfn;

                ObjectId sessionid = GetObjectID(database, "Sessions", "name", session);
                string sessiondb = FetchDBRef(database, "Sessions", "name", sessionid);

                var builder = Builders<BsonDocument>.Filter;

                var filterscheme = builder.Eq("_id", annotid);
                var result = collection.Find(filterscheme);
                var annosch = annoschemes.Find(filterscheme).Single();

                var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid) & builder.Eq("session_id", sessionid);
                var documents = collection.Find(filter).ToList();

                if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "DISCRETE")
                {
                    al.AnnotationType = AnnoType.DISCRETE;
                    al.usesAnnoScheme = true;
                }
                else if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "FREE")
                {
                    al.AnnotationType = AnnoType.FREE;
                    al.usesAnnoScheme = false;
                }
                else if (annosch.TryGetElement("type", out value) && annosch["type"].ToString() == "CONTINUOUS")
                {
                    al.AnnotationType = AnnoType.CONTINUOUS;
                    al.usesAnnoScheme = true;
                }

                al.Role = roledb;
                al.Name = "#" +al.Role + " #" + annotdb + " #" + annotatdbfn;

                al.AnnotationScheme = new AnnotationScheme();
                al.AnnotationScheme.name = annosch["name"].ToString();
                var annotation = documents[0]["labels"].AsBsonArray;
                if (al.AnnotationType == AnnoType.CONTINUOUS)
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

                        AnnoListItem ali = new AnnoListItem(start, dur, label, "", "#000000", double.Parse(confidence));

                        al.Add(ali);
                    }
                }
                else if (al.AnnotationType == AnnoType.DISCRETE)
                {
                    al.AnnotationScheme.mincolor = annosch["color"].ToString();

                    al.AnnotationScheme.LabelsAndColors = new List<LabelColorPair>();

                    BsonArray schemelabels = annosch["labels"].AsBsonArray;

                    for (int j = 0; j < schemelabels.Count; j++)
                    {
                        //in case flag is set, if not ignore isValid
                        try
                        {
                            if (schemelabels[j]["isValid"].AsBoolean == true) al.AnnotationScheme.LabelsAndColors.Add(new LabelColorPair(schemelabels[j]["name"].ToString(), schemelabels[j]["color"].ToString()));
                        }
                        catch
                        {
                            al.AnnotationScheme.LabelsAndColors.Add(new LabelColorPair(schemelabels[j]["name"].ToString(), schemelabels[j]["color"].ToString()));
                        }
                    }

                    al.AnnotationScheme.LabelsAndColors.Add(new LabelColorPair(GARBAGELABEL, GARBAGECOLOR));

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        string SchemeLabel = "";
                        string SchemeColor = "#000000";
                        bool idfound = false;
                        for (int j = 0; j < schemelabels.Count; j++)
                        {
                            if (annotation[i]["id"].AsInt32 == schemelabels[j]["id"].AsInt32)
                            {
                                SchemeLabel = schemelabels[j]["name"].ToString();
                                SchemeColor = schemelabels[j]["color"].ToString();
                                idfound = true;
                                break;
                            }
                        }


                        if (annotation[i]["id"].AsInt32 == -1 || idfound == false)
                        {
                            SchemeLabel = GARBAGELABEL;
                            SchemeColor = GARBAGECOLOR;
                        }

                        double start = double.Parse(annotation[i]["from"].ToString());
                        double stop = double.Parse(annotation[i]["to"].ToString());
                        double duration = stop - start;
                        string label = SchemeLabel;
                        string confidence = annotation[i]["conf"].ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", SchemeColor, double.Parse(confidence));
                        al.AddSorted(ali);
                    }
                }
                else if (al.AnnotationType == AnnoType.FREE)
                {
                    al.AnnotationScheme.mincolor = annosch["color"].ToString();

                    for (int i = 0; i < annotation.Count; i++)
                    {
                        double start = double.Parse(annotation[i]["from"].ToString());
                        double stop = double.Parse(annotation[i]["to"].ToString());
                        double duration = stop - start;
                        string label = annotation[i]["name"].ToString();
                        string confidence = annotation[i]["conf"].ToString();

                        AnnoListItem ali = new AnnoListItem(start, duration, label, "", "#000000", double.Parse(confidence));
                        al.AddSorted(ali);
                    }
                }

                al.FromDB = true;
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
        public ObjectId Id { get; set; }

        public string Role { get; set; }

        public string AnnoType { get; set; }

        public string Annotator { get; set; }
        public string AnnotatorFullname { get; set; }

        public string Session { get; set; }

        public bool IsFinished { get; set; }

        public bool IsLocked { get; set; }
        

        public bool IsOwner { get; set; }

        public string Date { get; set; }

        public ObjectId OID { get; set; }
    }

    public class DatabaseSession
    {
        public string Name { get; set; }

        public string Language { get; set; }

        public string Location { get; set; }

        public string Date { get; set; }

        public ObjectId OID { get; set; }
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
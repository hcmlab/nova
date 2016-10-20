using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseFunctions : Window
    {
        private MongoClient mongo;
        private IMongoDatabase database;
        private string connectionstring = "mongodb://127.0.0.1:27017";
        private int authlevel = 0;
        private List<DatabaseMediaInfo> ci;
        private List<DatabaseMediaInfo> files = new List<DatabaseMediaInfo>();
        private List<DatabaseMediaInfo> allfiles = new List<DatabaseMediaInfo>();
        AnnoList median;
        public DatabaseFunctions()
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

                authlevel = checkAuth(this.db_login.Text, "admin");

                if (authlevel > 0)
                {
                    Autologin.IsEnabled = true;
                    SelectDatabase();
                }
                  
                else MessageBox.Show("You have no rights to access the database list");
            }
            catch { MessageBox.Show("Could not connect to Database!"); }

            authlevel = checkAuth(this.db_login.Text, Properties.Settings.Default.Database);
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

                GetSessions();
            }
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CollectionResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();


                GetAnnotationSchemes();

           //     GetAnnotations();
              
            }
        }



        private void Ok_Click(object sender, RoutedEventArgs e)
        {
         
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
                    if (roles[i]["role"].ToString() == "root" || roles[i]["role"].ToString() == "dbOwner" && auth < 4) auth = 4;
                    else if (roles[i]["role"].ToString() == "userAdminAnyDatabase" || roles[i]["role"].ToString() == "userAdmin" && auth < 3) auth = 3;
                    else if (roles[i]["role"].ToString() == "readWriteAnyDatabase" || roles[i]["role"].ToString() == "readWrite" && auth < 2) auth = 2;
                    else if (roles[i]["role"].ToString() == "readAnyDatabase" || roles[i]["role"].ToString() == "read" && auth < 1) auth = 1;
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

        public void SelectDatabase()
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

            if (sessions.Count > 0)
            {



                if (CollectionResultsBox.Items != null) CollectionResultsBox.Items.Clear();
                List<DatabaseSession> items = new List<DatabaseSession>();
                foreach (var c in sessions)
                {
                    //CollectionResultsBox.Items.Add(c.GetElement(1).Value.ToString());
                    items.Add(new DatabaseSession() { Name = c["name"].ToString(), Location = c["location"].ToString(), Language = c["language"].ToString(), Date = c["date"].AsDateTime.ToShortDateString() });
                }

                CollectionResultsBox.ItemsSource = items;

            }
            else CollectionResultsBox.ItemsSource = null;
        }

        public void GetAnnotations(bool onlyme = false)

        {
            AnnotationResultBox.ItemsSource = null;
            //  AnnotationResultBox.Items.Clear();
            List<DatabaseAnno> items = new List<DatabaseAnno>();
            List<string> Collections = new List<string>();

            database = mongo.GetDatabase(Properties.Settings.Default.Database);
            var sessions = database.GetCollection<BsonDocument>("Sessions");
            var annotations = database.GetCollection<BsonDocument>("Annotations");
            var annotationschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");


            BsonDocument documents;
            var builder = Builders<BsonDocument>.Filter;


            ObjectId schemeid = new ObjectId();
            var filteras = builder.Eq("name", AnnoSchemesBox.SelectedValue.ToString());
            var result = annotationschemes.Find(filteras).Single();

            schemeid = result.GetValue(0).AsObjectId;

       
            var filter = builder.Eq("name", Properties.Settings.Default.LastSessionId);
            documents = sessions.Find(filter).Single();


            foreach (var c in documents["annotations"].AsBsonArray)
            {
                var filteranno = builder.Eq("_id", c["annotation_id"].AsObjectId);
                var annos = annotations.Find(filteranno).Single();


                var filtera = builder.Eq("_id", annos["role_id"]);
                var roledb = database.GetCollection<BsonDocument>("Roles").Find(filtera).Single();
                string roleid = roleid = roledb.GetValue(1).ToString();

                var filterb = builder.Eq("_id", annos["scheme_id"]);
                var annotdb = database.GetCollection<BsonDocument>("AnnotationSchemes").Find(filterb).Single();
                string annotid = annotid = annotdb.GetValue(1).ToString();


                var filterc = builder.Eq("_id", annos["annotator_id"]);
                var annotatdb = database.GetCollection<BsonDocument>("Annotators").Find(filterc).Single();
                string annotatid = annotatdb.GetValue(1).ToString();

                // AnnotationResultBox.Items.Add(roleid + "#" + annotid + "#" + a["annotator"].ToString());
                //  AnnotationResultBox.Items.Add(roleid);

          
                if (annos["scheme_id"].AsObjectId == schemeid)
                    {
                        items.Add(new DatabaseAnno() { Role = roleid, AnnoType = annotid, Annotator = annotatid });
                    }

               // else items.Add(new DatabaseAnno() { Role = roleid, AnnoType = annotid, Annotator = annotatid });

            }

            AnnotationResultBox.ItemsSource = items;

        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = database.GetCollection<BsonDocument>("AnnotationSchemes");
            var annosch = annoschemes.Find(_ => true).ToList();


            if (annosch.Count > 0)
            {



                if (AnnoSchemesBox.Items != null) AnnoSchemesBox.Items.Clear();

                foreach (var c in annosch)
                {

                    AnnoSchemesBox.Items.Add(c["name"]);

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

                ObjectId roleid = new ObjectId();
                var builder = Builders<BsonDocument>.Filter;
                var filtera = builder.Eq("name", ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Role);
                var roledb = roles.Find(filtera).ToList();
                if (roledb.Count > 0) roleid = roledb[0].GetValue(0).AsObjectId;

                ObjectId annotid = new ObjectId(); ;
                var filterb = builder.Eq("name", ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).AnnoType);
                var annotdb = annotationschemes.Find(filterb).ToList();
                if (annotdb.Count > 0) annotid = annotdb[0].GetValue(0).AsObjectId;


                ObjectId annotatid = new ObjectId(); ;
                var filterc = builder.Eq("name", ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Annotator);
                var annotatdb = annotators.Find(filterc).ToList();
                if (annotatdb.Count > 0) annotatid = annotatdb[0].GetValue(0).AsObjectId;

                var filter = builder.Eq("role_id", roleid) & builder.Eq("scheme_id", annotid) & builder.Eq("annotator_id", annotatid);
                var anno = annotations.Find(filter).ToList();

                var filter2 = builder.Eq("name", ((DatabaseSession)(CollectionResultsBox.SelectedValue)).Name);
                var session = sessions.Find(filter2).ToList();
                if (session.Count > 0)

                {
                    var annos = session[0]["annotations"].AsBsonArray;

                    for (int i = 0; i < annos.Count; i++)
                    {
                        if (annos[i]["annotation_id"] == anno[0]["_id"])
                        {
                            annos.RemoveAt(i);
                            break;
                        }
                    }

                    var update = Builders<BsonDocument>.Update.Set("annotations", annos);
                    sessions.UpdateOne(filter2, update);
                }

                var result = annotations.DeleteOne(filter);
                GetAnnotations();
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnnotationResultBox.SelectedValue != null)
            {
                for (int i = 0; i < AnnotationResultBox.SelectedItems.Count; i++)
                {
                    if (authlevel > 2 || Properties.Settings.Default.MongoDBUser == ((DatabaseAnno)(AnnotationResultBox.SelectedValue)).Annotator) DeleteAnnotation.Visibility = Visibility.Visible;
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

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            GetAnnotations();
        }



        private AnnoList calculateMedian()
        {
            int numberoftracks = AnnotationResultBox.SelectedItems.Count;

            DatabaseHandler db = new DatabaseHandler(connectionstring);
            List<AnnoList> al = db.LoadfromDatabase(AnnotationResultBox.SelectedItems, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);

            AnnoList merge = new AnnoList();
            double[] array = new double[al[0].Count];
            foreach(AnnoList a in al)
            {
                for(int i = 0; i < a.Count; i ++)
                {
                    array[i] = array[i] + double.Parse(a[i].Label);
                }
            }

            merge = al[0];
            for (int i= 0; i<array.Length; i++)
            {
                merge[i].Label = (array[i] / numberoftracks).ToString();

            }
            merge.Name = merge.Name + " #Median";


            merge.SR = 1000.0/ ((merge[0].Stop - merge[0].Start) * 1000.0);

            MessageBox.Show("Median of all Annotations has been calculated");

            Ok.IsEnabled = true;
            return merge;


        }


        public AnnoList Median()
        {
            return median;
        }

        private void CalculateMedian_Click(object sender, RoutedEventArgs e)
        {

            Ok.IsEnabled = false;
            median = calculateMedian();

            //todo do something

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
    }
}
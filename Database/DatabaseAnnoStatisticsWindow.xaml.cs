using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.Statistics;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using System.Web.UI.DataVisualization.Charting;
using System.IO;
using MongoDB.Driver.Linq;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseHandlerWindow.xaml
    /// </summary>
    public partial class DatabaseAnnoStatisticsWindow : System.Windows.Window
    {
        private bool selectedisContinuous = false;
        private readonly object syncLock = new object();
        private string defaultlabeltext = "Hover here to calculate correlations";


        private CultureInfo culture = CultureInfo.InvariantCulture;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        public DatabaseAnnoStatisticsWindow()
        {
            InitializeComponent();

          
            GetDatabases(DatabaseHandler.DatabaseName);
            GetSessions();
        }

        private void CollectionResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SessionsResultsBox.SelectedItem != null)
            {
                Properties.Settings.Default.LastSessionId = ((DatabaseSession)(SessionsResultsBox.SelectedValue)).Name;
                Properties.Settings.Default.Save();
                GetAnnotations();
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

        public void GetSessions(string selectedItem = null)

        {
            if (SessionsResultsBox.HasItems)
            {
                SessionsResultsBox.ItemsSource = null;
            }

            List<DatabaseSession> items = DatabaseHandler.Sessions;
            SessionsResultsBox.ItemsSource = items;

            if (SessionsResultsBox.HasItems)
            {
                SessionsResultsBox.SelectedIndex = 0;
                if (selectedItem != null)
                {
                    SessionsResultsBox.SelectedItem = items.Find(item => item.Name == selectedItem);
                }
            }
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


        public void GetAnnotations(bool onlyme = false, string sessionname = null)

        {
           // GetAnnotationSchemes();
            AnnotationResultBox.ItemsSource = null;
            //AnnoSchemesBox.ItemsSource = null;
            //AnnoSchemesBox.Items.Clear();
            //  AnnotationResultBox.Items.Clear();
            List<DatabaseAnnotation> items = new List<DatabaseAnnotation>();
            List<string> Collections = new List<string>();

            var sessions = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Sessions);
            var annotations = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var annotationschemes = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var roles = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles);

            var builder = Builders<BsonDocument>.Filter;

            ObjectId schemeid = new ObjectId();
            BsonDocument result = new BsonDocument();
            if (AnnoSchemesBox.SelectedValue != null)
            {
                var filteras = builder.Eq("name", AnnoSchemesBox.SelectedValue.ToString());
                try
                {
                    result = annotationschemes.Find(filteras).Single();
                    if (result.ElementCount > 0) schemeid = result.GetValue(0).AsObjectId;
                    string type = result.GetValue(2).ToString();
                    if (type == "CONTINUOUS")
                    {
                        selectedisContinuous = true;
                    }
                    else
                    {
                        selectedisContinuous = false;
                    }
                }
                catch
                {
                    ;
                }
               
            }

            ObjectId roleid = new ObjectId();
            BsonDocument result2 = new BsonDocument();
            if (RolesBox.SelectedValue != null)
            {
                var filterat = builder.Eq("name", RolesBox.SelectedValue.ToString());
                try
                {
                    result2 = roles.Find(filterat).Single();
                    if (result2.ElementCount > 0) roleid = result2.GetValue(0).AsObjectId;
                }
                catch
                {
                    ;
                }
             
            }

            if (SessionsResultsBox.SelectedItem == null) SessionsResultsBox.SelectedIndex = 0;

            ObjectId sessionid = ObjectId.Empty;
            if (sessionname == null && DatabaseHandler.Sessions.Count > 1)
            {
                DatabaseSession session = SessionsResultsBox.SelectedItems.Count > 0 ? (DatabaseSession)SessionsResultsBox.SelectedItem : DatabaseHandler.Sessions[0];
                sessionid = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Sessions, "name", session.Name);
                sessionname = session.Name;
            }

            else
            {
                sessionid = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Sessions, "name", sessionname);
            }
           


            var filter = builder.Eq("session_id", sessionid);
            var annos = annotations.Find(filter).ToList();

            foreach (var anno in annos)
            {
                var filtera = builder.Eq("_id", anno["role_id"]);
                var roledb = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Roles).Find(filtera).Single();
                string rolename = roledb.GetValue(1).ToString();

                var filterb = builder.Eq("_id", anno["scheme_id"]);
                var annotdb = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes).Find(filterb).Single();
                string annoschemename = annotdb.GetValue(1).ToString();
                string type = annotdb.GetValue(2).ToString();

                var filterc = builder.Eq("_id", anno["annotator_id"]);
                var annotatdb = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotators).Find(filterc).ToList();
                string annotatorname = "Deleted User";
                string annotatornamefull = annotatorname;
                if (annotatdb.Count > 0)
                {
                    annotatorname = annotatdb[0].GetValue(1).ToString();

                    annotatornamefull = DatabaseHandler.Annotators.Find(a => a.Name == annotatorname).FullName;
      

                if (result.ElementCount > 0 && result2.ElementCount > 0 && anno["scheme_id"].AsObjectId == schemeid && anno["role_id"].AsObjectId == roleid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = sessionname });
                }
                else if (result.ElementCount > 0 && result2.ElementCount == 0 && anno["scheme_id"].AsObjectId == schemeid)
                {
                    items.Add(new DatabaseAnnotation() { Role = rolename, Scheme = annoschemename, AnnotatorFullName = annotatornamefull, Annotator = annotatorname, Id = anno["_id"].AsObjectId, Session = sessionname });
                }
                }
            }

            AnnotationResultBox.ItemsSource = items;
        }

        public void GetAnnotationSchemes()
        {
            var annoschemes = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Schemes);
            var annosch = annoschemes.Find(_ => true).ToList();

            if (annosch.Count > 0)
            {
                if (AnnoSchemesBox.Items != null) AnnoSchemesBox.Items.Clear();

                foreach (var c in annosch)
                {
                    if (c["isValid"].AsBoolean == true) AnnoSchemesBox.Items.Add(c["name"]);
                }
                AnnoSchemesBox.SelectedItem = AnnoSchemesBox.Items.GetItemAt(0);
            }
        }

        public void GetRoles()
        {
            if (DatabaseHandler.Roles.Count > 0)
            {
                if (RolesBox.Items != null) RolesBox.Items.Clear();

                foreach (var r in DatabaseHandler.Roles)
                {
                    RolesBox.Items.Add(r.Name);
                }
                if (RolesBox.SelectedIndex == -1) RolesBox.SelectedIndex = 0;
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            List<AnnoList> al = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems);
            if (al.Count == 0) return;

            if (al[0].Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {

           
            string restclass = "REST";
            List<AnnoList> convertedlists = convertAnnoListsToMatrix(al, restclass);
            List<AnnoScheme.Label> schemelabels = convertedlists[0].Scheme.Labels;
            schemelabels.Remove(schemelabels.ElementAt(schemelabels.Count - 1));
            int[] counter = new int[schemelabels.Count+1];

            for(int i=0; i < convertedlists[0].Count; i++)
            {
             
                {
                    AnnoScheme.Label label = schemelabels.Find(s => s.Name == convertedlists[0].ElementAt(i).Label);
                    if(label == null) { counter[schemelabels.Count]++; }
                    else counter[schemelabels.IndexOf(label)]++;
                }
            }

            string result = "Class Distribution in %: \n\n";
            schemelabels.Add(new AnnoScheme.Label(restclass, Colors.Black));
            foreach(var label in schemelabels)
            {
                result += label.Name + ": " + ((float)counter[schemelabels.IndexOf(label)] / (float)convertedlists[0].Count) * 100 + "%" + "\n";
            }

            Statistics.Content = result;
            }

            else
            {
                Statistics.Content = "Only discrete/non empty Schemes are supported for now";
            }

        }

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

       
    

        private List<AnnoList> convertAnnoListsToMatrix(List<AnnoList> annolists, string restclass)
        {
            List<AnnoList> convertedlists = new List<AnnoList>();

            double maxlength = GetAnnoListMinLength(annolists);
            double chunksize = Properties.Settings.Default.DefaultMinSegmentSize; //Todo make option

            foreach (AnnoList al in annolists)
            {
                AnnoList list = ConvertDiscreteAnnoListToContinuousList(al, chunksize, maxlength, restclass);
                convertedlists.Add(list);
            }

            return convertedlists;
        }

        private double GetAnnoListMinLength(List<AnnoList> annolists)
        {
            double length = 0;
            foreach (AnnoList al in annolists)
            {
                if (al.Count > 0 && al.ElementAt(al.Count - 1).Stop > length) length = al.ElementAt(al.Count - 1).Stop;
            }

            return length;
        }

        private AnnoList ConvertDiscreteAnnoListToContinuousList(AnnoList annolist, double chunksize, double end, string restclass = "Rest")
        {
            AnnoList result = new AnnoList();
            result.Scheme = annolist.Scheme;
            result.Meta = annolist.Meta;
            result.Source.StoreToDatabase = true;
            result.Source.Database.Session = annolist.Source.Database.Session;
            double currentpos = 0;

            bool foundlabel = false;
            AnnoListItem lastitem = null;
            while (currentpos < end)
            {
                foundlabel = false;
                if (lastitem != null && lastitem.Stop < currentpos)
                    annolist.Remove(lastitem);

                foreach (AnnoListItem orgitem in annolist)
                {
                    if (orgitem.Start < currentpos && orgitem.Stop > currentpos)
                    {
                        AnnoListItem ali = new AnnoListItem(currentpos, chunksize, orgitem.Label);
                        result.Add(ali);
                        foundlabel = true;

                        lastitem = orgitem;
                        if (orgitem.Stop < currentpos + chunksize) annolist.Remove(orgitem);
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

     
        private void RolesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            GetAnnotations();
        }

        public void GetDatabases(string selectedItem = null)
        {
            DatabasesBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases(DatabaseAuthentication.READWRITE);

            foreach (string db in databases)
            {
                DatabasesBox.Items.Add(db);
            }

            Select(DatabasesBox, selectedItem);
        }

        private void Select(ListBox list, string select)
        {
            if (select != null)
            {
                foreach (string item in list.Items)
                {
                    if (item == select)
                    {
                        list.SelectedItem = item;
                    }
                }
            }
        }

        private void DatabasesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabasesBox.SelectedItem != null)
            {
                string name = DatabasesBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
            }
           
            GetRoles();
            GetAnnotationSchemes();
            GetSessions();
            GetAnnotations();
        }

  
   
    
    }
}
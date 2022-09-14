using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für Window1.xaml
    /// </summary>
    public partial class DatabaseBountiesCreateWindow : Window
    {
        Lightning lightning = new Lightning();
        DatabaseBounty bounty = new DatabaseBounty();
        DatabaseBounty selectedBounty = new DatabaseBounty();
        List<DatabaseBounty> allbounties = new List<DatabaseBounty>();


        private CultureInfo culture = CultureInfo.InvariantCulture;
        private BackgroundWorker backgroundWorker = new BackgroundWorker();

        public DatabaseBountiesCreateWindow()
        {
            InitializeComponent();
            if(!MainHandler.ENABLE_LIGHTNING)
            {
                balance.Visibility = Visibility.Hidden;
                sats.Visibility = Visibility.Hidden;
                satsperannotatorlabel.Visibility = Visibility.Hidden;
            }
        }


        private async void loadbalancewithCurrency()
        {
            if (MainHandler.myWallet != null)
            {
                balance.Content = "Balance: " + (MainHandler.myWallet.balance / 1000) + " Sats (" + (await lightning.PriceinCurrency(MainHandler.myWallet.balance / 1000, "EUR")).ToString("0.00") + "€)";
            }

            else balance.Content = "Lightning wallet not activated";


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
            this.DialogResult = false;
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


        public void GetAnnotators()
        {
            AnnotatorsBox.Items.Clear();

            foreach (DatabaseAnnotator annotator in DatabaseHandler.Annotators)
            {
                AnnotatorsBox.Items.Add(annotator);
            }
           
        }

        public void GetAnnotations(bool onlyme = false, string sessionname = null)

        {
            CreateBounty.IsEnabled = true;
            AnnotationResultBox.ItemsSource = null;
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
                result = annotationschemes.Find(filteras).Single();
                if (result.ElementCount > 0) schemeid = result.GetValue(0).AsObjectId;
                string type = result.GetValue(2).ToString();
            
            }

            ObjectId roleid = new ObjectId();
            BsonDocument result2 = new BsonDocument();
            if (RolesBox.SelectedValue != null)
            {
                var filterat = builder.Eq("name", RolesBox.SelectedValue.ToString());
                result2 = roles.Find(filterat).Single();
                if (result2.ElementCount > 0) roleid = result2.GetValue(0).AsObjectId;
            }

            if (SessionsResultsBox.SelectedItem == null) SessionsResultsBox.SelectedIndex = 0;

            ObjectId sessionid = ObjectId.Empty;
            if (sessionname == null)
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

                if (AnnoSchemesBox.HasItems)
                {
                    AnnoSchemesBox.SelectedIndex = 0;
                   
                }
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

       

        private void AnnoSchemesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
            GetStreams();
            GetAnnotations();
            if ((DatabaseHandler.CheckAuthentication() < DatabaseAuthentication.DBADMIN))
            {
                assignpanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                assignpanel.Visibility = Visibility.Visible;
                GetAnnotators();
            }
               
        }



        private void GetStreams(System.Collections.IList selectedItems = null)
        {

            List<StreamItem> temp = new List<StreamItem>();

            if (selectedItems != null)
            {
                foreach (var item in selectedItems)
                {
                    temp.Add((StreamItem)item);
                }

              
            }


            List<DatabaseStream> streams = DatabaseHandler.Streams;
            List<DatabaseRole> roles = DatabaseHandler.Roles;

            string session = "";
            if (SessionsResultsBox.SelectedItem != null)
            {
                session = SessionsResultsBox.SelectedItem.ToString();
            }

            if (StreamsBox.HasItems)
            {
                StreamsBox.ItemsSource = null;
            }

            List<StreamItem> items = new List<StreamItem>();
            foreach (DatabaseStream stream in streams)
            {
                foreach (DatabaseRole role in roles)
                {
                    if (role.HasStreams)
                    {
                        string filename = role.Name + "." + stream.Name + "." + stream.FileExt;
                        string directory = Properties.Settings.Default.DatabaseDirectory + "\\" + DatabaseHandler.DatabaseName + "\\" + session + "\\";
                        string filepath = directory + filename;
                        items.Add(new StreamItem() { Name = filename, Extension = stream.FileExt, Role = role.Name, Type = stream.Name, Exists = File.Exists(filepath) });
                    }
                }
            }

            StreamsBox.ItemsSource = items;

            if (StreamsBox.HasItems)
            {
                if (selectedItems != null)
                {
                    foreach (StreamItem stream in temp)
                    {
                        StreamsBox.SelectedItems.Add(items.Find(item => item.Name == stream.Name));
                    }

                    if (StreamsBox.HasItems)
                    {
                        StreamsBox.SelectedIndex = 0;

                    }

                    //  StreamsBox.SelectedItems = items.Find(item => item.Name == selectedItem);
                }
            }

        }



        private void Stats_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationResultBox.SelectedItems.Count == 1)
            {

                List<AnnoList> annolists = DatabaseHandler.LoadSession(AnnotationResultBox.SelectedItems, SessionsResultsBox.SelectedItems);



            }
            else if (AnnotationResultBox.SelectedItems.Count > 1)
            {
                foreach (var entry in SessionsResultsBox.SelectedItems)
                {

                }
            }
        }

        public List<StreamItem> SelectedStreams()
        {
            List<StreamItem> selectedStreams = new List<StreamItem>();

            if (StreamsBox.SelectedItems != null)
            {
                foreach (StreamItem stream in StreamsBox.SelectedItems)
                {
                    selectedStreams.Add(stream);
                }
            }

            return selectedStreams;
        }

        public DatabaseBounty getBounty()
        {
            return selectedBounty;
        }

        private void CreateBounty_Click(object sender, RoutedEventArgs e)
        {
           
            bounty.Contractor = DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);
            int nannotators = 0;
            Int32.TryParse(numannotators.Text.ToString(), out nannotators);
            bounty.numOfAnnotations = nannotators;
            bounty.numOfAnnotationsNeededCurrent = bounty.numOfAnnotations;
            int satsperannotator = 0;
            Int32.TryParse(sats.Text.ToString(), out satsperannotator);
            bounty.valueInSats = satsperannotator;
           
            bounty.Session = (SessionsResultsBox.SelectedItems.Count > 0 ? (DatabaseSession)SessionsResultsBox.SelectedItem : DatabaseHandler.Sessions[0]).Name;
            //bounty.Session = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Sessions, "name", session.Name);

            bounty.Role = (string)RolesBox.SelectedItem;
            //bounty.Role = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Roles, "name", role);

            bounty.Scheme = AnnoSchemesBox.SelectedItems.Count > 0 ? AnnoSchemesBox.SelectedItem.ToString() : DatabaseHandler.Schemes[0].Name.ToString();
            //bounty.Scheme = GetObjectID(DatabaseHandler.Database, DatabaseDefinitionCollections.Schemes, "name", scheme);
            bounty.annotatorsJobDone = new List<BountyJob>();
            bounty.annotatorsJobCandidates = new List<BountyJob>();

            if(AnnotatorsBox.SelectedItems != null)
            {
                foreach (DatabaseAnnotator annotator in AnnotatorsBox.SelectedItems)
                {
                    BountyJob job = new BountyJob();
                    job.user = DatabaseHandler.GetUserInfo(annotator.Name);
                    job.status = "open";
                    //job.pickedLNURL = false;
                    //job.LNURLW = "TODO";
                    bounty.annotatorsJobCandidates.Add(job);
                }

            }

            bounty.Database = (string)DatabasesBox.SelectedItem;
            string type = "Trust";
            //if (Interrater.IsChecked == true) type = "Interrater";
            //else 
            if (ManualApproval.IsChecked == true) type = "Manual";

            bounty.streams = new List<StreamItem>();
            foreach(StreamItem item in StreamsBox.SelectedItems)
            {
                bounty.streams.Add(item);
            }
            bounty.Type = type; 
            //DialogResult = true;

            if (bounty.streams.Count == 0)
            {
                MessageBox.Show("Please select at lease one Stream file.");
            }

            else if (DatabaseHandler.SaveBounty(bounty))
            {
                MessageBox.Show("Contract succesfully saved");
            }
        
        }

        private void StreamsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        public class AnnotatorStatus
        {
            public string Name { get; set; }

            public bool isPaid { get; set; }
            public double Rating { get; set; }
        }


        private void updateBounties()
        {

            List<string> databases = DatabaseHandler.GetDatabases();
            allbounties.Clear();
            BountiesOverviewBox.ItemsSource = null;

            foreach (string databaseName in databases)
            {
                List<DatabaseBounty> bounties = new List<DatabaseBounty>();
                bounties = DatabaseHandler.LoadCreatedBounties(databaseName);
                if (bounties != null) allbounties.AddRange(bounties);
            }
            BountiesOverviewBox.ItemsSource = allbounties;
        }

        private void UpdateJobsDoneList()
        {
            if (BountiesOverviewBox.SelectedItem != null)
            {

                BountiesCandidates.ItemsSource = ((DatabaseBounty)BountiesOverviewBox.SelectedItem).annotatorsJobCandidates;

                List<AnnotatorStatus> annostatus = new List<AnnotatorStatus>();
                DatabaseBounty bounty = (DatabaseBounty)BountiesOverviewBox.SelectedItem;
               
                foreach (BountyJob item in bounty.annotatorsJobDone)
                {
                    //item.rating = stars.Value;
                    //item.status = "finished";
                    //item.LNURLW = "TODO";
                    //item.pickedLNURL = false;
                    AnnotatorStatus stat = new AnnotatorStatus();
                    stat.Name = item.user.Name;
                    stat.Rating = item.rating;
                    ObjectId id = DatabaseHandler.GetAnnotationId(bounty.Role, bounty.Scheme, item.user.Name, bounty.Session, bounty.Database);

                    if(id != new ObjectId())
                    {

                        IMongoDatabase db = DatabaseHandler.Client.GetDatabase(bounty.Database);
                        var annos = db.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
                        var builder = Builders<BsonDocument>.Filter;

                        var filter = builder.Eq("_id", id);
                        var annotationDoc = annos.Find(filter).Single();

                        BsonElement value;
                        if (annotationDoc.TryGetElement("bountyIsPaid", out value))
                        {
                            stat.isPaid = annotationDoc["bountyIsPaid"].AsBoolean;
                        }
                        //if (annotationDoc.TryGetElement("rating", out value))
                        //{
                        //    stat.Rating = annotationDoc["rating"].AsDouble;
                        //}

                        annostatus.Add(stat);
                       
                    }

                   
                }

               

                BountiesJobDone.ItemsSource = annostatus;
            }
        }

        private void BountiesOverviewBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateJobsDoneList();
            DatabaseBounty bounty = (DatabaseBounty)BountiesOverviewBox.SelectedItem;

            if (bounty != null && (bounty.numOfAnnotationsNeededCurrent == 0 || bounty.annotatorsJobDone.Count == 0)) //&& make sure they are paid for.
            {
                RemoveButton.IsEnabled = true;
            }
            else RemoveButton.IsEnabled = false;

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (BountiesOverviewBox.SelectedItem != null)
            {
                if (BountiesJobDone.SelectedItem != null)
                {
                    selectedBounty = (DatabaseBounty)BountiesOverviewBox.SelectedItem;
                    DialogResult = true;
                }

            }

        }

        //public DatabaseBounty getBounty()
        //{
        //    return selectedBounty;
        //}

        public string getAnnotator()
        {
            return ((AnnotatorStatus)BountiesJobDone.SelectedItem).Name;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {

            DialogResult = false;
            this.Close();

        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ChangePaidState(ObjectId id, bool state)
        {
            var annos = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("bountyIsPaid", state);
            annos.UpdateOne(filter, update);

            //var updater = Builders<BsonDocument>.Update.Set("rating", rating);
            //annos.UpdateOne(filter, updater);
        }

        private async void Unlock_Click(object sender, RoutedEventArgs e)
        {
            double rating = stars.Value;
            DatabaseBounty bounty = (DatabaseBounty)BountiesOverviewBox.SelectedItem;
            string name = ((AnnotatorStatus)BountiesJobDone.SelectedItem).Name;
            DatabaseUser user = DatabaseHandler.GetUserInfo(name);
            if (bounty.valueInSats > 0 && MainHandler.ENABLE_LIGHTNING && MainHandler.myWallet != null)
            {
                Lightning lightning = new Lightning();
                Lightning.LightningInvoice invoice = await lightning.CreateInvoice(user.ln_invoice_key, (uint)bounty.valueInSats, bounty.Database + "/" + bounty.Session + "/" + bounty.Scheme + "/" + bounty.Role);
                MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);
                if(MainHandler.myWallet.balance >= bounty.valueInSats)
                {
                    string message = await lightning.PayInvoice(MainHandler.myWallet, invoice.payment_request);
                    if (message == "Success")
                    {
                        MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);
                        ObjectId id = DatabaseHandler.GetAnnotationId(bounty.Role, bounty.Scheme, user.Name, bounty.Session, bounty.Database);
                        ChangePaidState(id, true);
                        MessageBox.Show("Congratulations, Annotation Unlocked");
                    }
                    else MessageBox.Show("Error");
                }
                else MessageBox.Show("Unsifficent Balance in Wallet");


            }

            else
            {
                ObjectId id = DatabaseHandler.GetAnnotationId(bounty.Role, bounty.Scheme, user.Name, bounty.Session, bounty.Database);
                ChangePaidState(id, true);
                MessageBox.Show("Zero-Cost Annotation Unlocked");
               
            }

           // DatabaseHandler.ChangeUserCustomData(user);

            foreach (BountyJob job in bounty.annotatorsJobDone)
            {
                if (job.user.Name == user.Name)
                {
                    job.rating = stars.Value;
                    //if (bounty.valueInSats > 0)
                    //{
                    //    //var lnurl = await lightning.getLNURLw(MainHandler.myWallet, bounty.valueInSats, bounty.numOfAnnotations);
                    //    job.pickedLNURL = true;
                    //}

                    break;
                }

            }
            DatabaseHandler.SaveBounty(bounty);

            UpdateJobsDoneList();
        }

        private void BountiesJobDone_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BountiesJobDone.SelectedItem != null)
            {
                stars.Value =  (int)((AnnotatorStatus)BountiesJobDone.SelectedItem).Rating;
                unlockpanellabel.Visibility = Visibility.Visible;
                unlockpanel.Visibility = Visibility.Visible;
                OpenButton.Visibility = Visibility.Visible;
                if (((AnnotatorStatus)BountiesJobDone.SelectedItem).isPaid)
                {
                    OpenButton.Content = "Open";
                    unlockButton.IsEnabled = false;
                    stars.IsEnabled = false;
                    unlockpanellabel.Visibility = Visibility.Hidden;

                }
                else {
                    unlockButton.IsEnabled = true;
                    stars.IsEnabled = true;
                    OpenButton.Content = "Preview";
                }
            }

            else
            {
                unlockpanellabel.Visibility = Visibility.Hidden;
                unlockpanel.Visibility = Visibility.Hidden;
                OpenButton.Visibility = Visibility.Hidden;
            }

        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            DatabaseBounty bounty = (DatabaseBounty)BountiesOverviewBox.SelectedItem;
            DatabaseHandler.DeleteBounty(bounty.OID);
            updateBounties();
        }
   


    private void AcceptedTabItem_Selected(object sender, RoutedEventArgs e)
    {
        //if ((DatabaseHandler.CheckAuthentication() < DatabaseAuthentication.DBADMIN)) Warning.Visibility = Visibility.Visible;
        GetDatabases(DatabaseHandler.DatabaseName);
        loadbalancewithCurrency();
        GetSessions();
    }

    private void FindTabItem_Selected(object sender, RoutedEventArgs e)
    {
        updateBounties();
    }


    }
}



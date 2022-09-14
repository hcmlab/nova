using MongoDB.Bson;
using MongoDB.Driver;
using Octokit;
using ssi;
using System;
using System.Collections.Generic;
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
    /// Interaktionslogik für DatabaseBountiesMainWindow.xaml
    /// </summary>
    public partial class DatabaseBountiesMainWindow : Window
    {
        DatabaseBounty selectedBounty = new DatabaseBounty();
        DatabaseBounty selectedAcceptedBounty = new DatabaseBounty();
        List<DatabaseBounty> findbounties = new List<DatabaseBounty>();
        List<DatabaseBounty> acceptedbounties = new List<DatabaseBounty>();
        public DatabaseBountiesMainWindow()
        {
            InitializeComponent();
            //updateBounties();
            //BountiesOverviewBox.ItemsSource = allbounties;

            //updateAcceptedBounties();
            //AcceptedBountiesOverviewBox.ItemsSource = acceptedbounties;

        }

        private void updateFindBounties()
        {
            FindBountiesOverviewBox.ItemsSource = null;
            findbounties.Clear();
            List<string> databases = DatabaseHandler.GetDatabases();
            foreach (string databaseName in databases)
            {
                IMongoDatabase db = DatabaseHandler.Client.GetDatabase(databaseName);
                List<DatabaseBounty> bounties = new List<DatabaseBounty>();
                bounties = DatabaseHandler.LoadActiveBounties(db);
                if(bounties != null) findbounties.AddRange(bounties);
            }
            FindBountiesOverviewBox.ItemsSource = findbounties;
        }

        private void updateAcceptedBounties(bool onlyfinished)
        {
            AcceptedBountiesOverviewBox.ItemsSource = null;
            acceptedbounties.Clear();
            List<string> databases = DatabaseHandler.GetDatabases();
            foreach (string databaseName in databases)
            {
                IMongoDatabase db = DatabaseHandler.Client.GetDatabase(databaseName);
                List<DatabaseBounty> bounties = new List<DatabaseBounty>();
                bounties = DatabaseHandler.LoadAcceptedBounties(db, onlyfinished);
                if (bounties != null) acceptedbounties.AddRange(bounties);
            }
            if(onlyfinished)
            {
                CompletedBountiesOverviewBox.ItemsSource = acceptedbounties;
                updateUserRatingandStats();
        


                }
            else  AcceptedBountiesOverviewBox.ItemsSource = acceptedbounties;
        }

        private void updateUserRatingandStats()
        {
            DatabaseUser user = DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);
            user.ratingcount = 0;
            user.ratingoverall = 0;
            user.XP = 0;
            foreach (var item in acceptedbounties)
            {
                if (item.RatingTemp != 0)
                {
                    user.ratingcount++;
                    user.ratingoverall = user.ratingoverall + item.RatingTemp;
                    user.XP = user.XP + (int)(item.RatingTemp * 10);

                }

            }
            user.ln_admin_key = MainHandler.Cipher.AES.EncryptText(user.ln_admin_key, MainHandler.Decode((Properties.Settings.Default.MongoDBPass)));  //encrypt
            DatabaseHandler.ChangeUserCustomData(user);
        }
       

private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            selectedBounty = (DatabaseBounty)FindBountiesOverviewBox.SelectedItem;
            bool hasWallet = (MainHandler.myWallet != null);
            if(selectedBounty != null)
            {
                if(selectedBounty.valueInSats > 0 && !hasWallet)
                {
                    MessageBox.Show("This is a paid contract, but it seems you did not create a lightning wallet yet. You can do so in the lower status bar but clicking the \u26a1 symbol");
                }

                else
                {
                    BountyJob job = new BountyJob();
                    job.user = DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);
                    job.rating = 0;
                    job.status = "open";
                    //job.pickedLNURL = false;
                    //job.LNURLW = selectedBounty.LNURLW;
                    

                    selectedBounty.annotatorsJobCandidates.Add(job);
                    if (DatabaseHandler.SaveBounty(selectedBounty))
                    {
                        MessageBox.Show("Contract succesfully accepted. Open Accepted bounties Menu to start working on your bounties.");
                        updateFindBounties();
                    }
                }
            }



        }

        public DatabaseBounty getMyBounty()
        {
            return selectedAcceptedBounty;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();

        }

        private void OpenAcceptedButton_Click(object sender, RoutedEventArgs e)
        {
            if (AcceptedBountiesOverviewBox.SelectedItem != null)
            {
                selectedAcceptedBounty = (DatabaseBounty)AcceptedBountiesOverviewBox.SelectedItem;
                DialogResult = true;
            }
            this.Close();

        }

        private void RemoveAcceptedButton_Click(object sender, RoutedEventArgs e)
        {
            if (AcceptedBountiesOverviewBox.SelectedItem != null)
            {
                selectedAcceptedBounty = (DatabaseBounty)AcceptedBountiesOverviewBox.SelectedItem;
                int index = selectedAcceptedBounty.annotatorsJobCandidates.FindIndex(s => s.user.Name == Properties.Settings.Default.MongoDBUser);
                if (index > -1)
                {
                    selectedAcceptedBounty.annotatorsJobCandidates.RemoveAt(index);
                    selectedAcceptedBounty.numOfAnnotationsNeededCurrent += 1;
                    DatabaseHandler.SaveBounty(selectedAcceptedBounty);
                    updateAcceptedBounties(false);

                }
            }

        }



        private void AcceptedTabItem_Selected(object sender, RoutedEventArgs e)
        {
            updateAcceptedBounties(false);
        }

        private void FindTabItem_Selected(object sender, RoutedEventArgs e)
        {
            updateFindBounties();
        }

        private void TabItem_Selected(object sender, RoutedEventArgs e)
        {
            updateAcceptedBounties(true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}

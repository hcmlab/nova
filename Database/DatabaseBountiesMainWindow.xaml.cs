using MongoDB.Bson;
using MongoDB.Driver;
using NAudio.CoreAudioApi;
using Octokit;
using ssi;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.DataVisualization.Charting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Path = System.IO.Path;

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
        WebClient webClient;
        Stopwatch sw = new Stopwatch();
        string bountyDB;
        string bountySession;
        int remainingfiles = int.MaxValue;
        List<string> streamstoDownload = new List<string>();

        private const Int32 GWL_STYLE = -16;
        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_BYPOSITION = 0x00000400;
        private const uint SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern uint RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);
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
            user.ln_admin_key_locked = MainHandler.Cipher.AES.EncryptText(user.ln_admin_key_locked, MainHandler.Decode((Properties.Settings.Default.MongoDBPass)));  //encrypt

            DatabaseHandler.ChangeUserCustomData(user);
        }
       

private void AcceptButton_Click(object sender, RoutedEventArgs e)
        {
            selectedBounty = (DatabaseBounty)FindBountiesOverviewBox.SelectedItem;
            bool hasWallet = (MainHandler.myWallet != null);
            if(selectedBounty != null)
            {
                ObjectId schemeID = new ObjectId();
                DatabaseHandler.GetObjectID(ref schemeID, DatabaseDefinitionCollections.Schemes, selectedBounty.Scheme);
    
                ObjectId roleID = new ObjectId();
                DatabaseHandler.GetObjectID(ref roleID, DatabaseDefinitionCollections.Roles, selectedBounty.Role);

                ObjectId sessionID = new ObjectId();
                DatabaseHandler.GetObjectID(ref sessionID, DatabaseDefinitionCollections.Sessions, selectedBounty.Session);

                ObjectId annotatorID = new ObjectId();
                DatabaseHandler.GetObjectID(ref annotatorID, DatabaseDefinitionCollections.Annotators, Properties.Settings.Default.MongoDBUser);



                if (!DatabaseHandler.AnnotationExists(annotatorID, sessionID, roleID, schemeID))
                {



                    if (selectedBounty.valueInSats > 0 && !hasWallet)
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

                else MessageBox.Show("An annotation by you already exists");
            }



        }

        public DatabaseBounty getMyBounty()
        {
            return selectedAcceptedBounty;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            sw.Stop();
           
            DialogResult = false;
            this.Close();

        }

        private void OpenAcceptedButton_Click(object sender, RoutedEventArgs e)
        {
            if (AcceptedBountiesOverviewBox.SelectedItem != null)
            {
                selectedAcceptedBounty = (DatabaseBounty)AcceptedBountiesOverviewBox.SelectedItem;
                downloadSelectedFiles();
               // DialogResult = true;
            }
            else this.Close();

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

        private void FinishedTabItem_Selected(object sender, RoutedEventArgs e)
        {
            updateAcceptedBounties(true);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }

   

        private void stars_MouseLeave(object sender, MouseEventArgs e)
        {
            var rating = ((Rating)sender).Value;
            //CompletedBountiesOverviewBox.SelectedItem = CompletedBountiesOverviewBox.Items.
            //CompletedBountiesOverviewBox.SelectedItem = ((StackPanel)((Rating)sender).Parent).Parent;
            DatabaseBounty bounty = ((DatabaseBounty)CompletedBountiesOverviewBox.SelectedItem);
            bounty.annotatorsJobDone.Find(s => s.user.Name == Properties.Settings.Default.MongoDBUser).ratingContractor = rating;
            DatabaseHandler.SaveBounty(bounty);
        }
        private void downloadSelectedFiles()
        {
            OpenButton.IsEnabled = false;
            CancelButton2.IsEnabled = false;
            RemoveButton.IsEnabled = false;
            WindowInteropHelper wih = new WindowInteropHelper(this);
            IntPtr hWnd = wih.Handle;
            IntPtr hMenu = GetSystemMenu(hWnd, false);

            // CloseButton disabled
            RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);

            DatabaseBounty selectedBounty = ((DatabaseBounty)(AcceptedBountiesOverviewBox.SelectedItem));

            //labelDownloaded.Visibility = Visibility.Visible;
            //labelPerc.Visibility = Visibility.Visible;
            //labelSpeed.Visibility = Visibility.Visible;
            //progressBar.Visibility = Visibility.Visible;
            bountyDB = selectedBounty.Database;
            bountySession = selectedBounty.Session;
            string localPath = Defaults.LocalDataLocations().First() + "\\" + bountyDB + "\\" + bountySession + "\\";
            Directory.CreateDirectory(Path.GetDirectoryName(localPath));
            streamstoDownload = new List<string>();

            foreach (StreamItem stream in selectedBounty.streams)
            {

                string existingPath = Defaults.FileExistsinPath(stream.Name, DatabaseHandler.DatabaseName, DatabaseHandler.SessionName);
                string anyPath = existingPath + "\\" + bountyDB + "\\" + bountySession + "\\" + stream.Name;

               // string localfile = localPath + stream.Name;

                if (!File.Exists(anyPath))
                {
                    streamstoDownload.Add(stream.Name);

                }

            }

            if (streamstoDownload.Count == 0)
            {
                DialogResult = true;
                Close();
                return;
            }

            string url = "";
            bool requiresAuth = false;

            DatabaseDBMeta meta = new DatabaseDBMeta()
            {
                Name = DatabaseHandler.DatabaseName
            };
            if (!DatabaseHandler.GetDBMeta(ref meta))
            {
                return;
            }

            if (meta.Server == "" || meta.Server == null)
            {
                return;
            }


            if (meta.UrlFormat == UrlFormat.NEXTCLOUD)
            {
                url = meta.Server + "/download?path=%2F" + selectedBounty.Database + "%2F" + selectedBounty.Session + "&files=";
            }
            else
            {
                url = meta.Server + '/' + DatabaseHandler.SessionName + '/';
                requiresAuth = meta.ServerAuth;
            }





            List<string> urls = new List<string>();

            foreach (string file in streamstoDownload)
            {
                urls.Add(url + file);
                if (file.EndsWith(".stream"))
                {
                    urls.Add(url + file + "~");
                }
            }


            remainingfiles = urls.Count;
            downloadFile(urls);
        }




        private Queue<string> _downloadUrls = new Queue<string>();
  

        private void downloadFile(IEnumerable<string> urls)
        {
            foreach (var url in urls)
            {
                _downloadUrls.Enqueue(url);
                
            }

            DownloadFile();
        }



        public void DownloadFile()
        {
            if (_downloadUrls.Any())
            {

                using (webClient = new WebClient())
                {
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);
                    webClient.DownloadProgressChanged += new DownloadProgressChangedEventHandler(ProgressChanged);

                    string url = _downloadUrls.Dequeue();

                    string localPath = Defaults.LocalDataLocations().First() + "\\" + bountyDB + "\\" + bountySession + "\\";
                    string[] split = System.IO.Path.GetFileName(url).Split('=');

                    string location = localPath + split[split.Length - 1];
                    labelName.Content = split[split.Length - 1];
                    Uri URL = new Uri(url);
                    // Start the stopwatch which we will be using to calculate the download speed
                    sw.Start();

                    try
                    {
                        // Start downloading the file
                        webClient.DownloadFileAsync(URL, location);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }

        }


        // The event that will fire whenever the progress of the WebClient is changed
        private void ProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {


            // Calculate download speed and output it to labelSpeed.
            labelSpeed.Content = string.Format("{0} kb/s", (e.BytesReceived / 1024d / sw.Elapsed.TotalSeconds).ToString("0.00"));

            // Update the progressbar percentage only when the value is not the same.
            progressBar.Value = e.ProgressPercentage;

            // Show the percentage on our label.
            labelPerc.Content = e.ProgressPercentage.ToString() + "%";

            // Update the label with how much data have been downloaded so far and the total size of the file we are currently downloading
            labelDownloaded.Content = string.Format("{0} MB's / {1} MB's",
                (e.BytesReceived / 1024d / 1024d).ToString("0.00"),
                (e.TotalBytesToReceive / 1024d / 1024d).ToString("0.00"));
        }

        // The event that will trigger when the WebClient is completed
        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            remainingfiles--;

            if (e.Error != null)
            {
                // handle error scenario
                //throw e.Error;
            }
            if (e.Cancelled)
            {
                string localPath = Defaults.LocalDataLocations().First() + "\\" + bountyDB + "\\" + bountySession + "\\";
                foreach (string file in streamstoDownload)
                {
                    File.Delete(localPath + file);
                }
                // handle cancelled scenario
            }
            DownloadFile();

            if (remainingfiles == 0)
            {

                //if (closeAfterDownload)
                //{
                    DialogResult = true;
                    Close();
                

                //else
                //{
                //    System.Collections.IList test = StreamsBox.SelectedItems;

                //    GetStreams(test);
                //}
            }
        }



    }
}

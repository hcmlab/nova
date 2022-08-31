using MongoDB.Bson;
using MongoDB.Driver;
using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using System.Security.Cryptography;
using System.Text;

namespace ssi
{
    public partial class MainHandler
    {
        public static Lightning.LightningWallet myWallet = null;
        public void paymentReceived(uint amount)
        {
            updateNavigator();           
        }
        #region DATABASELOGIC

        private async void databaseConnect(bool reconnect = false)
        {
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Connecting to Database...";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
          
            if (reconnect) DatabaseHandler.reconnectClient();

            bool isConnected = DatabaseHandler.Connect();

            MainHandler.myWallet = null;

            if (isConnected)
            {
                if (!DatabaseHandler.ChangeDatabase(Properties.Settings.Default.DatabaseName))
                {
                    Properties.Settings.Default.DatabaseName = null;
                    Properties.Settings.Default.Save();
                }

            }
            else
            {
                MessageTools.Warning("Unable to connect to database, please check your settings\nIf you created a new account, please restart the Software");
                Properties.Settings.Default.DatabaseAutoLogin = false;
                Properties.Settings.Default.Save();                           
            }

            updateNavigator();
           
            control.ShadowBox.Visibility = Visibility.Collapsed;
            control.ShadowBoxText.Text = "Loading Data...";

            if (isConnected && ENABLE_LIGHTNING)
            {

                Lightning lightning = new Lightning();
                try
                {
                    int balance = 0;
                    DatabaseUser user =  DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);
                    if(user.ln_admin_key == "")
                    {
                        MainHandler.myWallet = null;
                    }
                    else 
                    {
                        myWallet = new Lightning.LightningWallet();
                        myWallet.admin_key = user.ln_admin_key;
                        myWallet.invoice_key = user.ln_invoice_key;
                        myWallet.wallet_id = user.ln_wallet_id;
                        myWallet.user_id = user.ln_user_id;
                        myWallet.lnaddressname = user.ln_addressname;
                        myWallet.lnaddresspin = user.ln_addresspin;
                        balance = await lightning.GetWalletBalance(user.ln_admin_key);
                        myWallet.balance = balance;
                        //if error we don't have a wallet, returns -1.
                        //if (balance == -1)
                        // MainHandler.myWallet = null;
                        //else
                        //{



                        //}
                    }

                    updateNavigator();

                    //InitCheckLightningBalanceTimer();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

  



        }

        public void viewonlyMode(bool on)
        {

            control.filemenu.IsEnabled = !on;
            control.databaseCMLMenu.IsEnabled = !on;
            control.XAIMenu.IsEnabled = !on;
            control.annotationmenu.IsEnabled = !on;
            control.databaseCMLMenu.IsEnabled = !on;
            control.databaseLoadSessionMenu.IsEnabled = !on;
            control.navigator.newAnnoButton.IsEnabled = !on;
            ENABLE_VIEWONLY = on;

        }

       
     
        private void DatabaseHuntBounty_Click(object sender, RoutedEventArgs e)
        {
            DatabaseBountiesMainWindow dialog = new DatabaseBountiesMainWindow();
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                DatabaseBounty bounty = dialog.getMyBounty();
                loadFilesForBounty(bounty, Properties.Settings.Default.MongoDBUser);
                wait(1000);
                loadAnnoForBounty(bounty, Properties.Settings.Default.MongoDBUser);

            }
        }

        public void wait(int milliseconds)
        {
            var timer1 = new System.Windows.Forms.Timer();
            if (milliseconds == 0 || milliseconds < 0) return;

            // Console.WriteLine("start wait timer");
            timer1.Interval = milliseconds;
            timer1.Enabled = true;
            timer1.Start();

            timer1.Tick += (s, e) =>
            {
                timer1.Enabled = false;
                timer1.Stop();
                // Console.WriteLine("stop wait timer");
            };

            while (timer1.Enabled)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(DispatcherPriority.Background,
                                                  new Action(delegate { }));
            }
        }


        private void DatabaseCreateBounty_Click(object sender, RoutedEventArgs e)
        {
            DatabaseBountiesCreateWindow dialog = new DatabaseBountiesCreateWindow();
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                DatabaseBounty bounty = dialog.getBounty();
                string annotator = dialog.getAnnotator();
                loadFilesForBounty(bounty, annotator);
                loadAnnoForBounty(bounty, annotator);
                viewonlyMode(true);
            }
            else
            {
                clearWorkspace();
                viewonlyMode(false);
            }
        }
    

        private void DatabaseConnectMenu_Click(object sender, RoutedEventArgs e)
        {
            databaseConnect();
        }

        private async void databaseUpdate()
        {
            DatabaseHandler.UpdateDatabaseLocalLists();
            //Lightning lightning = new Lightning();
            //MainHandler.myWallet.balance = await lightning.GetWalletBalance(MainHandler.myWallet.admin_key);
        }

        private void DatabaseUpdateMenu_Click(object sender, RoutedEventArgs e)
        {
            databaseUpdate();
        }        

        private void DatabasePassMenu_Click(object sender, RoutedEventArgs e)
        {
            DatabaseUser blankuser = new DatabaseUser()
            {
                Name = Properties.Settings.Default.MongoDBUser
            };

            blankuser = DatabaseHandler.GetUserInfo(blankuser);

            DatabaseUserManageWindow dialog = new DatabaseUserManageWindow(blankuser.Name, blankuser.Fullname, blankuser.Email, blankuser.Expertise);
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                DatabaseUser user = DatabaseHandler.GetUserInfo(dialog.GetName());
                user.Name = dialog.GetName();
                user.Fullname = dialog.GetFullName();
                user.Email = dialog.Getemail();
                user.Expertise = dialog.GetExpertise();
                user.ln_admin_key = MainHandler.Cipher.AES.EncryptText(user.ln_admin_key, MainHandler.Decode(Properties.Settings.Default.MongoDBPass));
                string newPassword = dialog.GetPassword();

                //DatabaseUser user = new DatabaseUser()
                //{
                //    Name = dialog.GetName(),
                //    Fullname = dialog.GetFullName(),
                //    Password = dialog.GetPassword(),
                //    Email = dialog.Getemail(),
                //    Expertise = dialog.GetExpertise()
                //};

                DatabaseHandler.ChangeUserCustomData(user);
                if (newPassword != "" && newPassword != null)
                {
                    user.Password = newPassword;
                    if (DatabaseHandler.ChangeUserPassword(user))
                    {
                        Properties.Settings.Default.MongoDBPass = MainHandler.Encode(newPassword);
                        MessageBox.Show("Password Change successful!");
                        if(MainHandler.myWallet != null)
                        {
                            user.ln_admin_key = MainHandler.Cipher.AES.EncryptText(MainHandler.myWallet.admin_key, newPassword);
                            DatabaseHandler.ChangeUserCustomData(user);

                        }
                       
                        databaseConnect();
                    }

                }
               





            }

        }
        



        private void databaseManageDBs()
        {           
            DatabaseAdminManageDBWindow dialog = new DatabaseAdminManageDBWindow();
            showDialogClearWorkspace(dialog);
        }

        private void databaseManageUsers()
        {            
            DatabaseAdminManageUsersWindow dialog = new DatabaseAdminManageUsersWindow();
            showDialogClearWorkspace(dialog);
        }
        private void databaseManageSessions()
        {
            DatabaseAdminManageSessionsWindow dialog = new DatabaseAdminManageSessionsWindow();
            showDialogClearWorkspace(dialog);
        }

        private void databaseManageAnnotations()
        {
            DatabaseAdminManageAnnotationsWindow dialog = new DatabaseAdminManageAnnotationsWindow();
            showDialogClearWorkspace(dialog);
        }
        private void databaseLoadSession()
        {
            

            DatabaseAnnoMainWindow dialog = new DatabaseAnnoMainWindow();



            if (showDialogClearWorkspace(dialog) && (dialog.DialogResult == true))
            {

                Action EmptyDelegate = delegate () { };
                control.ShadowBox.Visibility = Visibility.Visible;
                control.UpdateLayout();
                control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                System.Collections.IList annotations = dialog.Annotations();           
                if (annotations != null && annotations.Count > 0)
                {
                    List<AnnoList> annoLists = DatabaseHandler.LoadSession(annotations);
                    if (annoLists != null)
                    {
                        foreach (AnnoList annoList in annoLists)
                        {
                            addAnnoTierFromList(annoList);
                        }
                    }
                }

                control.ShadowBox.Visibility = Visibility.Collapsed;

                List<StreamItem> streams = dialog.SelectedStreams();
                databaseSessionStreams = new List<string>();

                foreach (StreamItem stream in streams)
                {
                    databaseSessionStreams.Add(stream.Name);
                }

                   

                if (streams != null && streams.Count > 0)
                {
                    List<StreamItem> streamsAll = new List<StreamItem>();
                    foreach (StreamItem stream in streams)
                    {
                        if (stream.Extension == "stream")
                        {
                            StreamItem tilde = new StreamItem();
                            tilde.Name = stream.Name + "~";
                            streamsAll.Add(tilde);
                        }
                        streamsAll.Add(stream);

                    }

                    try
                    {
                        if (filesToDownload != null)
                        {
                            filesToDownload.Clear();
                        }

                        MainHandler.NumberOfAllCurrentDownloads = streamsAll.Count;


                        tokenSource = new CancellationTokenSource();

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
     
                        string localPath = Properties.Settings.Default.DatabaseDirectory + "\\" + DatabaseHandler.DatabaseName + "\\" + DatabaseHandler.SessionName + "\\";

                        if (meta.UrlFormat == UrlFormat.NEXTCLOUD)
                        {
                            url = meta.Server + "/download?path=%2F" + DatabaseHandler.DatabaseName + "%2F" + DatabaseHandler.SessionName + "&files=";
                        }
                        else
                        {
                            url = meta.Server + '/' + DatabaseHandler.SessionName + '/';
                            requiresAuth = meta.ServerAuth;
                        }

                        string connection = "";

                        if (meta.Server == "")
                        {
                            connection = "";
                        }

                        else
                        {
                            string[] split = url.Split(':');
                            connection = split[0];
                        }
                    

                        Directory.CreateDirectory(Path.GetDirectoryName(localPath));

                        List<string> streamstoDownload = new List<string>();
                        foreach (StreamItem stream in streamsAll)
                        {


                            string llocal = localPath + stream.Name;
                            string lurl = url + stream.Name;


                            if (File.Exists(llocal))
                            {
                                DatabaseStream temp = new DatabaseStream();
                                string filename = Path.GetFileNameWithoutExtension(stream.Name);
                                string[] withoutRole = filename.Split('.');
                                string fname = "";
                                //add all strings but the first (role)
                                for(int i = 1; i <withoutRole.Length; i++)
                                {
                                    fname = fname + withoutRole[i] + ".";
                                }

                                temp.Name = fname.Remove(fname.Length - 1, 1);
                                temp.FileExt = stream.Extension;
                                temp.Type = stream.Type;
                                DatabaseHandler.GetStream(ref temp);
                                loadFile(llocal, temp.DimLabels);
                                continue;
                            }

                            streamstoDownload.Add(stream.Name);

                            Thread.Sleep(100);
                           

                            //TODO add more servers...
        

                            if (connection == "sftp")
                            {                            
                                SFTP(lurl, llocal);
                            }
                            else if (connection == "http" || connection == "https" && requiresAuth == false)

                            {
                               int result = httpGetSync(lurl, llocal);
                               if(result == -1) streamstoDownload.RemoveAt(streamstoDownload.Count - 1);


                            }
                            else if (connection == "http" || connection == "https" && requiresAuth == true)
                            {
                                httpPost(lurl, llocal);
                            }

                            else
                            {
                                loadFile(localPath);
                            }
                        }

                      
                        control.ShadowBoxText.UpdateLayout();
                        control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                        control.ShadowBoxText.Text = "Loading Data";
                        control.ShadowBox.Visibility = Visibility.Collapsed;
                        control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                        control.ShadowBox.UpdateLayout();


                        foreach (string stream in streamstoDownload)
                        {
                            string llocal = localPath + stream;

                            try
                            {
                                long length = new System.IO.FileInfo(llocal).Length;
                                if (length == 0)
                                {
                                    Thread.Sleep(100);
                                    if (File.Exists(llocal)) File.Delete(llocal);
                                    continue;
                                }
                            }
                            catch
                            {
                            }

                           
                            loadFile(llocal);
                        }



                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Error: " + e, "Connection to database not possible");
                    }
                }

              
            }

           
        }

        public void ReloadAnnoTierFromDatabase(AnnoTier tier, bool loadBackup)
        {
            if (tier == null || tier.AnnoList == null)
            {
                return;
            }

            if (loadBackup && tier.AnnoList.Source.Database.DataBackupOID == AnnoSource.DatabaseSource.ZERO)
            {                
                MessageTools.Warning("No backup exists");
                return;
            }

            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Reloading Annotation";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            DatabaseAnnotation annotation = new DatabaseAnnotation();
            annotation.Role = tier.AnnoList.Meta.Role;
            annotation.Scheme = tier.AnnoList.Scheme.Name;
            annotation.AnnotatorFullName = tier.AnnoList.Meta.AnnotatorFullName;
            annotation.Annotator = tier.AnnoList.Meta.Annotator;
            annotation.Session = DatabaseHandler.SessionName;

            AnnoList annoList = DatabaseHandler.LoadAnnoList(annotation, loadBackup);
            double maxdur = 0;

            if (annoList != null && annoList.Count > 0 && annoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || annoList.Scheme.Type == AnnoScheme.TYPE.FREE)
            {
                maxdur = annoList[annoList.Count - 1].Stop;

                setAnnoList(annoList);
                tier.Children.Clear();
                tier.AnnoList.Clear();
                tier.segments.Clear();
                tier.AnnoList = annoList;

                foreach (AnnoListItem item in annoList)
                {
                    tier.AddSegment(item);
                }

                tier.TimeRangeChanged(Time);
                updateTimeRange(maxdur);

                tier.AnnoList.HasChanged = false;          
            }

            else if (annoList != null && annoList.Count > 0 && annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                maxdur = annoList[annoList.Count - 1].Stop;

                setAnnoList(annoList);
                tier.AnnoList.Clear();
                tier.AnnoList = annoList;
                tier.TimeRangeChanged(Time);
                updateTimeRange(maxdur);
                AnnoTier.Selected.TimeRangeChanged(MainHandler.Time);

                tier.AnnoList.HasChanged = false;

            }

            control.ShadowBox.Visibility = Visibility.Collapsed;
        }

        private void addNewAnnotationDatabase(DatabaseBounty bounty = null)
        {
            if (Time.TotalDuration > 0)
            {
                string annoScheme = "";
                if (bounty == null)
                {
                    annoScheme = DatabaseHandler.SelectScheme();
                    if (annoScheme == null)
                    {
                        return;
                    }
                }
                else annoScheme = bounty.Scheme;

                string role = "";
                if (bounty == null)
                {
                    role = DatabaseHandler.SelectRole();
                    if (role == null)
                    {
                        return;
                    }
                }
                else role = bounty.Role;
                

                AnnoScheme scheme = DatabaseHandler.GetAnnotationScheme(annoScheme);
                if (scheme == null)
                {
                    return;
                }
                if(scheme.Type != AnnoScheme.TYPE.DISCRETE_POLYGON)
                    scheme.Labels.Add(new AnnoScheme.Label("GARBAGE", Colors.Black));

                ObjectId annotatid = DatabaseHandler.GetObjectID(DatabaseDefinitionCollections.Annotators, "name", Properties.Settings.Default.MongoDBUser);
                string annotator = Properties.Settings.Default.MongoDBUser;

                AnnoList annoList;
                if (DatabaseHandler.AnnotationExists(annotator, DatabaseHandler.SessionName, role, scheme.Name))
                {
                    DatabaseAnnotation annotation = new DatabaseAnnotation()
                    {
                        Annotator = annotator,
                        Session = DatabaseHandler.SessionName,
                        Role = role,
                        Scheme = scheme.Name
                    };
                    annoList = DatabaseHandler.LoadAnnoList(annotation, false);
                    annoList.HasChanged = false;
                }
                else
                {
                    annoList = new AnnoList();
                    annoList.Meta.Role = role;
                    annoList.Meta.Annotator = annotator;
                    DatabaseUser user = DatabaseHandler.GetUserInfo(annotator);
                    string annotatorFullName = user.Fullname;
                    annoList.Meta.AnnotatorFullName = annotatorFullName;
                    annoList.Scheme = scheme;
                    annoList.Source.StoreToDatabase = true;
                    annoList.Source.Database.Session = DatabaseHandler.SessionName;
                    annoList.HasChanged = true;
                    if(bounty != null)
                    {
                        annoList.Source.Database.HasBounty = true;
                        annoList.Source.Database.BountyID = bounty.OID;
                        annoList.Source.Database.BountyIsPaid = false;
                    }
                   
                    
                }

                addAnnoTier(annoList);
                control.annoListControl.editComboBox.SelectedIndex = 0;
            }
            else
            {
                MessageTools.Warning("Nothing to annotate, load some data first.");
            }
        }

        #endregion DATABASELOGIC


        #region EVENTHANDLERS

        private void databaseLoadSession_Click(object sender, RoutedEventArgs e)
        {
            databaseLoadSession();
            
        }

        private void databaseManageDBs_Click(object sender, RoutedEventArgs e)
        {
            databaseManageDBs();
        }

        private void databaseManageUsers_Click(object sender, RoutedEventArgs e)
        {
            databaseManageUsers();
        }

        private void databaseManageSessions_Click(object sender, RoutedEventArgs e)
        {
            databaseManageSessions();
        }

        private void databaseManageAnnotations_Click(object sender, RoutedEventArgs e)
        {
            databaseManageAnnotations();
        }

        private void databaseCMLMergeAnnotations_Click(object sender, RoutedEventArgs e)
        {
            DatabaseAnnoMergeWindow window = new DatabaseAnnoMergeWindow();
            window.ShowDialog();
        }



        #endregion EVENTHANDLERS

        public static class Cipher
        {
            internal static class AES
            {
                private static byte[] AES_Encrypt(byte[] bytesToBeEncrypted, byte[] passwordBytes)
                {
                    byte[] encryptedBytes;
                    byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

                    using (MemoryStream ms = new MemoryStream())
                    {
                        var AES = Aes.Create("AesManaged");
                        AES.KeySize = 256;
                        AES.BlockSize = 128;
                        var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);
                        AES.Mode = CipherMode.CBC;
                        using (var cs = new CryptoStream(ms, AES.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytesToBeEncrypted, 0, bytesToBeEncrypted.Length);
                            cs.Close();
                        }
                        encryptedBytes = ms.ToArray();
                    }
                    return encryptedBytes;
                }

                private static byte[] AES_Decrypt(byte[] bytesToBeDecrypted, byte[] passwordBytes)
                {
                    byte[] decryptedBytes = null;
                    byte[] saltBytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
                    using (MemoryStream ms = new MemoryStream())
                    {
                        var AES = Aes.Create("AesManaged");
                        AES.KeySize = 256;
                        AES.BlockSize = 128;
                        var key = new Rfc2898DeriveBytes(passwordBytes, saltBytes, 1000);
                        AES.Key = key.GetBytes(AES.KeySize / 8);
                        AES.IV = key.GetBytes(AES.BlockSize / 8);
                        AES.Mode = CipherMode.CBC;
                        using (var cs = new CryptoStream(ms, AES.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(bytesToBeDecrypted, 0, bytesToBeDecrypted.Length);
                            cs.Close();
                        }
                        decryptedBytes = ms.ToArray();
                    }
                    return decryptedBytes;
                }

                public static string EncryptText(string password, string salt)
                {
                    byte[] bytesToBeEncrypted = Encoding.UTF8.GetBytes(password);
                    byte[] passwordBytes = Encoding.UTF8.GetBytes(salt);
                    passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
                    byte[] bytesEncrypted = AES_Encrypt(bytesToBeEncrypted, passwordBytes);
                    string result = Convert.ToBase64String(bytesEncrypted);
                    return result;
                }

                public static string DecryptText(string hash, string salt)
                {
                    try
                    {
                        byte[] bytesToBeDecrypted = Convert.FromBase64String(hash);
                        byte[] passwordBytes = Encoding.UTF8.GetBytes(salt);
                        passwordBytes = SHA256.Create().ComputeHash(passwordBytes);
                        byte[] bytesDecrypted = AES_Decrypt(bytesToBeDecrypted, passwordBytes);
                        string result = Encoding.UTF8.GetString(bytesDecrypted);
                        return result;
                    }
                    catch (Exception e)
                    {
                        return e.Message;
                    }
                }

                private const int SALT_BYTE_SIZE = 24;
                private const int HASH_BYTE_SIZE = 24;
                private const int PBKDF2_ITERATIONS = 1000;
                private const int ITERATION_INDEX = 0;
                private const int SALT_INDEX = 1;
                private const int PBKDF2_INDEX = 2;

                public static string PBKDF2_CreateHash(string password)
                {
                    RNGCryptoServiceProvider csprng = new RNGCryptoServiceProvider(); // This reports SYSLIB0023 'RNGCryptoServiceProvider' is obsolete: 'RNGCryptoServiceProvider is obsolete. To generate a random number, use one of the RandomNumberGenerator static methods instead.'
                    byte[] salt = new byte[SALT_BYTE_SIZE];
                    csprng.GetBytes(salt);
                    byte[] hash = PBKDF2(password, salt, PBKDF2_ITERATIONS, HASH_BYTE_SIZE);
                    return PBKDF2_ITERATIONS + ":" + Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
                }

                public static bool PBKDF2_ValidatePassword(string password, string correctHash)
                {
                    char[] delimiter = { ':' };
                    string[] split = correctHash.Split(delimiter);
                    int iterations = Int32.Parse(split[ITERATION_INDEX]);
                    byte[] salt = Convert.FromBase64String(split[SALT_INDEX]);
                    byte[] hash = Convert.FromBase64String(split[PBKDF2_INDEX]);
                    byte[] testHash = PBKDF2(password, salt, iterations, hash.Length);
                    return SlowEquals(hash, testHash);
                }

                private static bool SlowEquals(byte[] a, byte[] b)
                {
                    uint diff = (uint)a.Length ^ (uint)b.Length;
                    for (int i = 0; i < a.Length && i < b.Length; i++)
                        diff |= (uint)(a[i] ^ b[i]);
                    return diff == 0;
                }

                private static byte[] PBKDF2(string password, byte[] salt, int iterations, int outputBytes)
                {
                    Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt)
                    {
                        IterationCount = iterations
                    };
                    return pbkdf2.GetBytes(outputBytes);
                }
            }
        }
    


        public static string Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Decode(string base64EncodedData)
        {
            //try catch is if password is still in old format. deprecated in the future
            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
                return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
            }
            catch
            {
                return base64EncodedData;
            }
        }

    }

}

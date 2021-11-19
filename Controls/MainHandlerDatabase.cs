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
        public static Lightning.LightningWallet myWallet = new Lightning.LightningWallet();
        public void paymentReceived(uint amount)
        {
            updateNavigator();           
        }
        #region DATABASELOGIC

        private async void databaseConnect()
        {
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Connecting to Database...";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            bool isConnected = DatabaseHandler.Connect();

            

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
                MessageTools.Warning("Unable to connect to database, please check your settings");
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
                        DatabaseUser user = DatabaseHandler.GetUserInfo(Properties.Settings.Default.MongoDBUser);
                        int balance = await lightning.GetWalletBalance(user.ln_admin_key);
                        //if error we don't have a wallet, returns -1.
                        if (balance == -1)
                            myWallet = null;
                        else
                        {
                            myWallet.admin_key = user.ln_admin_key;
                            myWallet.invoice_key = user.ln_invoice_key;
                            myWallet.wallet_id = user.ln_wallet_id;
                            myWallet.user_id = user.ln_user_id;
                            myWallet.balance = balance;
                        }
                    updateNavigator();
                    //InitCheckLightningBalanceTimer();
                }
                    catch (Exception e)
                    {

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
                user.Password = dialog.GetPassword();
                user.Email = dialog.Getemail();
                user.Expertise = dialog.GetExpertise();
                //DatabaseUser user = new DatabaseUser()
                //{
                //    Name = dialog.GetName(),
                //    Fullname = dialog.GetFullName(),
                //    Password = dialog.GetPassword(),
                //    Email = dialog.Getemail(),
                //    Expertise = dialog.GetExpertise()
                //};

                DatabaseHandler.ChangeUserCustomData(user);
                if (user.Password != "" && user.Password != null)
                {
                    if (DatabaseHandler.ChangeUserPassword(user))
                    {
                        Properties.Settings.Default.MongoDBPass = MainHandler.Encode(user.Password);
                        MessageBox.Show("Password Change successful!");
                        if(MainHandler.myWallet != null)
                        {
                            user.ln_admin_key = MainHandler.Cipher.EncryptString(MainHandler.myWallet.admin_key, user.Password);
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
                    List<string> streamsAll = new List<string>();
                    foreach (StreamItem stream in streams)
                    {
                        if (stream.Extension == "stream")
                        {
                            streamsAll.Add(stream.Name + "~");
                        }
                        streamsAll.Add(stream.Name);

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
                        foreach (string stream in streamsAll)
                        {


                            string llocal = localPath + stream;
                            string lurl = url + stream;
                            if (File.Exists(llocal))
                            {
                                loadFile(llocal);
                                continue;
                            }

                            streamstoDownload.Add(stream);

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
            private const int Keysize = 256;

            // This constant determines the number of iterations for the password bytes generation function.
            private const int DerivationIterations = 1000;

            public static string EncryptString(string plainText, string passPhrase)
            {
                // Salt and IV is randomly generated each time, but is preprended to encrypted cipher text
                // so that the same Salt and IV values can be used when decrypting.  
                var saltStringBytes = Generate256BitsOfRandomEntropy();
                var ivStringBytes = Generate256BitsOfRandomEntropy();
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var encryptor = symmetricKey.CreateEncryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream())
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                                {
                                    cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);
                                    cryptoStream.FlushFinalBlock();
                                    // Create the final bytes as a concatenation of the random salt bytes, the random iv bytes and the cipher bytes.
                                    var cipherTextBytes = saltStringBytes;
                                    cipherTextBytes = cipherTextBytes.Concat(ivStringBytes).ToArray();
                                    cipherTextBytes = cipherTextBytes.Concat(memoryStream.ToArray()).ToArray();
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Convert.ToBase64String(cipherTextBytes);
                                }
                            }
                        }
                    }
                }
            }

            public static string DecryptString(string cipherText, string passPhrase)
            {
                // Get the complete stream of bytes that represent:
                // [32 bytes of Salt] + [32 bytes of IV] + [n bytes of CipherText]
                var cipherTextBytesWithSaltAndIv = Convert.FromBase64String(cipherText);
                // Get the saltbytes by extracting the first 32 bytes from the supplied cipherText bytes.
                var saltStringBytes = cipherTextBytesWithSaltAndIv.Take(Keysize / 8).ToArray();
                // Get the IV bytes by extracting the next 32 bytes from the supplied cipherText bytes.
                var ivStringBytes = cipherTextBytesWithSaltAndIv.Skip(Keysize / 8).Take(Keysize / 8).ToArray();
                // Get the actual cipher text bytes by removing the first 64 bytes from the cipherText string.
                var cipherTextBytes = cipherTextBytesWithSaltAndIv.Skip((Keysize / 8) * 2).Take(cipherTextBytesWithSaltAndIv.Length - ((Keysize / 8) * 2)).ToArray();

                using (var password = new Rfc2898DeriveBytes(passPhrase, saltStringBytes, DerivationIterations))
                {
                    var keyBytes = password.GetBytes(Keysize / 8);
                    using (var symmetricKey = new RijndaelManaged())
                    {
                        symmetricKey.BlockSize = 256;
                        symmetricKey.Mode = CipherMode.CBC;
                        symmetricKey.Padding = PaddingMode.PKCS7;
                        using (var decryptor = symmetricKey.CreateDecryptor(keyBytes, ivStringBytes))
                        {
                            using (var memoryStream = new MemoryStream(cipherTextBytes))
                            {
                                using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                                {
                                    var plainTextBytes = new byte[cipherTextBytes.Length];
                                    var decryptedByteCount = cryptoStream.Read(plainTextBytes, 0, plainTextBytes.Length);
                                    memoryStream.Close();
                                    cryptoStream.Close();
                                    return Encoding.UTF8.GetString(plainTextBytes, 0, decryptedByteCount);
                                }
                            }
                        }
                    }
                }
            }

            private static byte[] Generate256BitsOfRandomEntropy()
            {
                var randomBytes = new byte[32]; // 32 Bytes will give us 256 bits.
                using (var rngCsp = new RNGCryptoServiceProvider())
                {
                    // Fill the array with cryptographically secure random bytes.
                    rngCsp.GetBytes(randomBytes);
                }
                return randomBytes;
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

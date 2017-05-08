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

namespace ssi
{
    public partial class MainHandler
    {


        #region DATABASELOGIC

        private void databaseConnect()
        {
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
        }

        private void DatabaseConnectMenu_Click(object sender, RoutedEventArgs e)
        {
            showSettings(true);
        }

        private void databaseManage(Window dialog)
        {
            if (DatabaseHandler.IsSession)
            {
                MessageBoxResult result = MessageBox.Show("Before editing a database the workspace has to be cleared. Do you want to continue?", "Question", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
                clearWorkspace();
            }            
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        private void databaseManageDBs()
        {           
            DatabaseAdminManageDBWindow dialog = new DatabaseAdminManageDBWindow();
            databaseManage(dialog);
        }

        private void databaseManageUsers()
        {            
            DatabaseAdminManageUsersWindow dialog = new DatabaseAdminManageUsersWindow();
            databaseManage(dialog);
        }
        private void databaseManageSessions()
        {
            DatabaseAdminManageSessionsWindow dialog = new DatabaseAdminManageSessionsWindow();
            databaseManage(dialog);
        }

        private void databaseStore(bool isfinished = false)
        {
            if (DatabaseHandler.IsConnected)
            {
                string message = "";

                string login = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";

                foreach (AnnoTier track in annoTiers)
                {
                    if (!(track.AnnoList.HasChanged || isfinished))
                    {
                        continue;
                    }

                    if (track.AnnoList.Source.HasDatabase || track.AnnoList.Source.StoreToDatabase)
                    {
                        try
                        {
                   
                            if (DatabaseHandler.SaveAnnoList(track.AnnoList, streams, isfinished))
                            {
                                track.AnnoList.HasChanged = false;
                                message += "\r\n" + track.AnnoList.Scheme.Name;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("not auth"))
                            {
                                MessageBox.Show("Sorry, you don't have write access to the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }

                            else if (ex.Message.Contains("MaxDocumentSize"))
                            {
                                MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                            }
                            else
                            {
                                MessageBox.Show("Could not store tier '" + track.AnnoList.Scheme.Name + "' to database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(message))
                {
                    MessageBox.Show("The following tiers have been stored to the database: " + message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Load a database session first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void databaseLoadSession()
        {
            clearWorkspace();

            if (streams != null) streams.Clear();
            if (filesToDownload != null) filesToDownload.Clear();

            System.Collections.IList annotations = null;
            List<DatabaseMediaInfo> streamsInfo = null;

            DatabaseAnnoMainWindow dialog = new DatabaseAnnoMainWindow();
            try
            {
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    annotations = dialog.Annotations();
                    streams = dialog.Streams();
                    streamsInfo = dialog.StreamsInfo();
                    updateNavigator();
                }

                if (annotations != null)
                {
                    Action EmptyDelegate = delegate () { };
                    control.ShadowBox.Visibility = Visibility.Visible;
                    control.UpdateLayout();
                    control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                    List<AnnoList> annoLists = DatabaseHandler.LoadFromDatabase(annotations, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
                    
                    try
                    {
                        if (annoLists != null)
                        {
                            foreach (AnnoList annoList in annoLists)
                            {
                                //annoList.FilePath = annoList.Role + "." + annoList.Scheme.Name + "." + annoList.AnnotatorFullName;
                                addAnnoTierFromList(annoList);
                            }

                            control.ShadowBox.Visibility = Visibility.Collapsed;

                            //handle media

                            if (streams.Count > 0)
                            {
                                for (int i = 0; i < streams.Count; i++)
                                {
                                    foreach (DatabaseMediaInfo c in streamsInfo)

                                    {
                                        Properties.Settings.Default.DataServerConnectionType = c.connection;

                                        if (c.filename == streams[i].filename.ToString())

                                        {
                                            if (c.connection == "sftp")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "sftp";
                                                SFTPDownloadFiles(c.ip, c.folder, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, c.filename, Properties.Settings.Default.DataServerLogin, Properties.Settings.Default.DataServerPass);
                                            }
                                            else if (streamsInfo[i].connection == "http" || streamsInfo[i].connection == "https" && streamsInfo[i].requiresauth == "false")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "http";
                                                httpGet(c.filepath, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, c.filename);
                                            }
                                            else if (streamsInfo[i].connection == "http" || streamsInfo[i].connection == "https" && streamsInfo[i].requiresauth == "true")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "http";
                                                //This has not been tested and probably needs rework.
                                                httpPost(c.filepath, c.filename, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.DataServerLogin, Properties.Settings.Default.DataServerPass, Properties.Settings.Default.LastSessionId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (TimeoutException e1)
                    {
                        MessageBox.Show("Make sure ip, login and password are correct", "Connection to database not possible");
                    }
                }
            }
            catch (Exception ex)
            {
                dialog.Close();
                MessageTools.Error(ex.ToString());
            }
        }

        private void reloadAnnoTierFromDatabase(AnnoTier tier)
        {
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Reloading Annotation";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            DatabaseAnnotion s = new DatabaseAnnotion();
            s.Role = tier.AnnoList.Meta.Role;
            s.AnnoScheme = tier.AnnoList.Scheme.Name;
            s.AnnotatorFullname = tier.AnnoList.Meta.AnnotatorFullName;
            s.Annotator = tier.AnnoList.Meta.Annotator;

            List<DatabaseAnnotion> list = new List<DatabaseAnnotion>();
            list.Add(s);

            List<AnnoList> annos = DatabaseHandler.LoadFromDatabase(list, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
            double maxdur = 0;

            if (annos[0].Count > 0) maxdur = annos[0][annos[0].Count - 1].Stop;

            if (annos[0] != null && tier != null)
            {
                setAnnoList(annos[0]);
                tier.Children.Clear();
                tier.AnnoList.Clear();
                tier.segments.Clear();
                tier.AnnoList = annos[0];

                foreach (AnnoListItem item in annos[0])
                {
                    tier.AddSegment(item);
                }

                tier.TimeRangeChanged(MainHandler.Time);
            }

            updateTimeRange(maxdur);
            // if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            control.ShadowBox.Visibility = Visibility.Collapsed;
        }

        private void addNewAnnotationDatabase()
        {
            if (Time.TotalDuration > 0)
            {
                AnnoList annoList = new AnnoList();

                string annoScheme = DatabaseHandler.SelectScheme();
                if (annoScheme == null)
                {
                    return;
                }

                annoList.Meta.Role = DatabaseHandler.SelectRole();
                if (annoList.Meta.Role == null)
                {
                    return;
                }

                annoList.Scheme = DatabaseHandler.GetAnnotationScheme(annoScheme);
                annoList.Scheme.Labels.Add(new AnnoScheme.Label("GARBAGE", Colors.Black));

                IMongoDatabase database = DatabaseHandler.Database;

                ObjectId annotatid = DatabaseHandler.GetObjectID(database, DatabaseDefinitionCollections.Annotators, "name", Properties.Settings.Default.MongoDBUser);
                annoList.Meta.Annotator = Properties.Settings.Default.MongoDBUser;
                annoList.Meta.AnnotatorFullName = DatabaseHandler.FetchDBRef(database, DatabaseDefinitionCollections.Annotators, "fullname", annotatid);

                annoList.Source.StoreToDatabase = true;

                addAnnoTier(annoList);
                control.annoListControl.editComboBox.SelectedIndex = 0;
            }
            else
            {
                MessageTools.Warning("Nothing to annotate, load some data first.");
            }
        }

        private void databaseShowDownloadDirectory_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(Properties.Settings.Default.DatabaseDirectory);
            Process.Start(Properties.Settings.Default.DatabaseDirectory);
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

        private void databaseChangeDownloadDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Properties.Settings.Default.DatabaseDirectory;
            dialog.ShowNewFolderButton = true;
            dialog.Description = "Select the folder where you want to store the media of your databases in.";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.DatabaseDirectory = dialog.SelectedPath;
            }
        }

        private void databaseCMLCompleteStep_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLCompleteWindow window = new DatabaseCMLCompleteWindow(this);
            window.Show();
        }

        private void databaseCMLTransferStep_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTransferWindow window = new DatabaseCMLTransferWindow(this);
            window.Show();
        }


        private void databaseCMLMerge_Click(object sender, RoutedEventArgs e)
        {
            DatabaseAnnoMergeWindow window = new DatabaseAnnoMergeWindow();
            window.ShowDialog();

            if(window.DialogResult == true)
            { 

                AnnoList rms = window.RMS();
                AnnoList median = window.Mean();
                AnnoList merge = window.Merge();
                if (rms != null)
                {
                    DatabaseHandler.SaveAnnoList(rms);
                }
                if (median != null)
                {
                    DatabaseHandler.SaveAnnoList(median);
                }

                if (merge != null)
                {
                    DatabaseHandler.SaveAnnoList(merge);
                }

            }
        }

        

        private void databaseCMLExtractFeatures_Click(object sender, RoutedEventArgs e)
        {
            //TODO More logic here in the future

            string arguments = " -overwrite -log cml_extract.log " + "\"" + Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\" " + " expert;novice close";

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            //   startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmltrain.exe";
            startInfo.Arguments = "--extract" + arguments;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }

        #endregion EVENTHANDLERS

    }
}

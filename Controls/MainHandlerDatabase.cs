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

        private void databaseLoadSession()
        {
            clearWorkspace();

            DatabaseAnnoMainWindow dialog = new DatabaseAnnoMainWindow();
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();

            if (dialog.DialogResult == false)
            {
                return;
            }

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

            List<DatabaseStream> streams = dialog.SelectedStreams();
            databaseSessionStreams = streams;
            if (streams != null && streams.Count > 0)
            { 
                try
                {
                    if (filesToDownload != null)
                    {
                        filesToDownload.Clear();
                    }

                    foreach (DatabaseStream stream in streams)
                    {
                        string localPath = Properties.Settings.Default.DatabaseDirectory + "\\" + DatabaseHandler.DatabaseName + "\\" + DatabaseHandler.SessionName + "\\" + stream.Name;
                        if (File.Exists(localPath))
                        {
                            loadFile(localPath);
                            continue;
                        }

                        string url = stream.URL;
                        bool requiresAuth = stream.ServerAuth;

                        if (url == "")
                        {
                            DatabaseDBMeta meta = new DatabaseDBMeta()
                            {
                                Name = DatabaseHandler.DatabaseName
                            };
                            if (!DatabaseHandler.GetDBMeta(ref meta))
                            {
                                continue;
                            }
                            if (meta.Server == "")
                            {
                                continue;
                            }
                            url = meta.Server + '/' + DatabaseHandler.SessionName + '/' + stream.Name;
                            requiresAuth = meta.ServerAuth;
                        }

                        string[] split = url.Split(':');
                        string connection = split[0];                        
                        
                        Directory.CreateDirectory(Path.GetDirectoryName(localPath));

                        if (connection == "sftp")
                        {                            
                            SFTPDownloadFiles(url, localPath);
                        }
                        else if (connection == "http" || connection == "https" && requiresAuth == false)
                        {                            
                            httpGet(url, localPath);
                        }
                        else if (connection == "http" || connection == "https" && requiresAuth == true)
                        {
                            httpPost(url, DatabaseHandler.SessionName, localPath);
                        }
                    }
                }
                catch (TimeoutException e1)
                {
                    MessageBox.Show("Make sure ip, login and password are correct", "Connection to database not possible");
                }
            }
     
        }

        private void reloadAnnoTierFromDatabase(AnnoTier tier)
        {
            if (tier == null)
            {
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

            AnnoList annoList = DatabaseHandler.LoadAnnoList(annotation);
            double maxdur = 0;

            if (annoList != null)
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

                control.ShadowBox.Visibility = Visibility.Collapsed;
            } 
        }

        private void addNewAnnotationDatabase()
        {
            if (Time.TotalDuration > 0)
            {              
                string annoScheme = DatabaseHandler.SelectScheme();
                if (annoScheme == null)
                {
                    return;
                }

                string role = DatabaseHandler.SelectRole();
                if (role == null)
                {
                    return;
                }

                AnnoScheme scheme = DatabaseHandler.GetAnnotationScheme(annoScheme);
                if (scheme == null)
                {
                    return;
                }
                scheme.Labels.Add(new AnnoScheme.Label("GARBAGE", Colors.Black));

                ObjectId annotatid = DatabaseHandler.GetObjectID(DatabaseDefinitionCollections.Annotators, "name", Properties.Settings.Default.MongoDBUser);
                string annotator = Properties.Settings.Default.MongoDBUser;
                string annotatorFullName = DatabaseHandler.FetchDBRef(DatabaseDefinitionCollections.Annotators, "fullname", annotatid);

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
                    annoList = DatabaseHandler.LoadAnnoList(annotation);
                    annoList.HasChanged = false;
                }
                else
                {
                    annoList = new AnnoList();
                    annoList.Meta.Role = role;
                    annoList.Meta.Annotator = annotator;
                    annoList.Meta.AnnotatorFullName = annotatorFullName;
                    annoList.Scheme = scheme;
                    annoList.Source.StoreToDatabase = true;
                    annoList.HasChanged = true;
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

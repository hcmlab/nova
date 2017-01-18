using MongoDB.Bson;
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

        private void databaseReloadAnno(AnnoTier tier)
        {
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Reloading Annotation";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            DatabaseAnno s = new DatabaseAnno();
            s.Role = tier.AnnoList.Role;
            s.AnnoScheme = tier.AnnoList.Scheme.Name;
            s.AnnotatorFullname = tier.AnnoList.AnnotatorFullName;
            s.Annotator = tier.AnnoList.Annotator;

            List<DatabaseAnno> list = new List<DatabaseAnno>();
            list.Add(s);

            string l = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";
            DatabaseHandler db = new DatabaseHandler("mongodb://" + l + Properties.Settings.Default.DatabaseAddress);

            List<AnnoList> annos = db.LoadFromDatabase(list, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
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
                    tier.addSegment(item);
                }

                tier.TimeRangeChanged(MainHandler.Time);
                tier.Name = tier.AnnoList.Name;
                control.annoNameLabel.Content = "#" + tier.AnnoList.Scheme.Name + " #" + tier.AnnoList.Role + " #" + tier.AnnoList.AnnotatorFullName;
            }

            updateTimeRange(maxdur);
            // if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            control.ShadowBox.Visibility = Visibility.Collapsed;
        }

        private void databaseSaveSessionAndMarkAsFinished_Click(object sender, RoutedEventArgs e)
        {
            mongodbStore(true);
        }

        private void databaseSaveSession_Click(object sender, RoutedEventArgs e)
        {
            mongodbStore();
        }

        private void databaseLoadSession_Click(object sender, RoutedEventArgs e)
        {
            mongodbLoad();
        }

        private void databaseManage_Click(object sender, RoutedEventArgs e)
        {
            mongodbAdd();
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

        private void mongodbAdd()

        {
            try
            {
                DatabaseAdminMainWindow daw = new DatabaseAdminMainWindow();
                daw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                daw.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Not authorized to add a new Session");
            }
        }

        private void mongodbStore(bool isfinished = false)
        {
            if (DatabaseLoaded)
            {
                string message = "";

                string login = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";

                foreach (AnnoTier track in annoTiers)
                {
                    if (track.AnnoList.LoadedFromDB && (track.AnnoList.HasChanged || isfinished))
                    {
                        try
                        {
                            DatabaseHandler db = new DatabaseHandler("mongodb://" + login + Properties.Settings.Default.DatabaseAddress);

                            string annotator = db.StoreToDatabase(Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser, track, loadedDBmedia, isfinished);
                            if (annotator != null)
                            {
                                track.AnnoList.HasChanged = false;
                                if (annotator != null) message += "\r\n" + track.AnnoList.Name;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("not auth"))
                            {
                                MessageBox.Show("Sorry, you don't have write access to the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                MessageBox.Show("Could not store tier '" + track.AnnoList.Name + "' to database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void mongodbLoad()
        {
            clearSession();

            if (loadedDBmedia != null) loadedDBmedia.Clear();
            if (filesToDownload != null) filesToDownload.Clear();

            System.Collections.IList annotations = null;
            List<DatabaseMediaInfo> ci = null;

            DatabaseAnnoMainWindow dbhw = new DatabaseAnnoMainWindow();
            try
            {
                dbhw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dbhw.ShowDialog();

                if (dbhw.DialogResult == true)
                {
                    annotations = dbhw.Annotations();
                    loadedDBmedia = dbhw.Media();
                    ci = dbhw.MediaConnectionInfo();
                    control.databaseSaveSessionMenu.IsEnabled = true;
                    control.databaseSaveSessionAndMarkAsFinishedMenu.IsEnabled = true;
                    control.databaseCMLCompleteStepMenu.IsEnabled = true;
                    control.databaseCMLTransferStepMenu.IsEnabled = true;
                    control.databaseCMLExtractFeaturesMenu.IsEnabled = true;

                    //This is just a UI thing. If a user does not have according rights in the mongodb he will not have acess anyway. We just dont want to show the ui here.
                    if (dbhw.Authlevel() > 2)
                    {
                        control.databaseManageMenu.Visibility = Visibility.Visible;
                        control.databaseCMLTransferStepMenu.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        control.databaseManageMenu.Visibility = Visibility.Collapsed;
                        control.databaseCMLTransferStepMenu.Visibility = Visibility.Collapsed;
                    }
                }

                string l = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";
                DatabaseHandler db = new DatabaseHandler("mongodb://" + l + Properties.Settings.Default.DatabaseAddress);

                control.databaseSaveSessionMenu.IsEnabled = true;

                if (annotations != null)
                {
                    Action EmptyDelegate = delegate () { };
                    control.ShadowBox.Visibility = Visibility.Visible;
                    control.UpdateLayout();
                    control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                    List<AnnoList> annoLists = db.LoadFromDatabase(annotations, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
                    control.navigator.Statusbar.Content = "Database Session: " + (Properties.Settings.Default.LastSessionId).Replace('_', '-');
                    try
                    {
                        if (annoLists != null)
                        {
                            foreach (AnnoList annoList in annoLists)

                            {
                                annoList.FilePath = annoList.Role + "." + annoList.Scheme.Name + "." + annoList.AnnotatorFullName;
                                handleAnnotation(annoList, null);
                            }

                            control.ShadowBox.Visibility = Visibility.Collapsed;

                            //handle media

                            if (loadedDBmedia.Count > 0)
                            {
                                for (int i = 0; i < loadedDBmedia.Count; i++)
                                {
                                    foreach (DatabaseMediaInfo c in ci)

                                    {
                                        Properties.Settings.Default.DataServerConnectionType = c.connection;

                                        if (c.filename == loadedDBmedia[i].filename.ToString())

                                        {
                                            if (c.connection == "sftp")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "sftp";
                                                DownloadFileSFTP(c.ip, c.folder, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, c.filename, Properties.Settings.Default.DataServerLogin, Properties.Settings.Default.DataServerPass);
                                            }
                                            else if (ci[i].connection == "http" || ci[i].connection == "https" && ci[i].requiresauth == "false")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "http";
                                                httpGet(c.filepath, Properties.Settings.Default.DatabaseName, Properties.Settings.Default.LastSessionId, c.filename);
                                            }
                                            else if (ci[i].connection == "http" || ci[i].connection == "https" && ci[i].requiresauth == "true")
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
                        DatabaseLoaded = true;
                    }
                    catch (TimeoutException e1)
                    {
                        MessageBox.Show("Make sure ip, login and password are correct", "Connection to database not possible");
                    }
                }
            }
            catch (Exception ex)
            {
                dbhw.Close();
                MessageTools.Error(ex.ToString());
            }
        }

        private void databaseShowDownloadDirectory_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(Properties.Settings.Default.DatabaseDirectory);
            Process.Start(Properties.Settings.Default.DatabaseDirectory);
        }


        public void databaseNewAnno(AnnoScheme.TYPE annoType)
        {
            AnnoList annoList = new AnnoList();
            annoList.LoadedFromDB = true;
            string l = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";
            DatabaseHandler db = new DatabaseHandler("mongodb://" + l + Properties.Settings.Default.DatabaseAddress);

            annoList.Role = db.LoadRoles(Properties.Settings.Default.DatabaseName, null);
            if (annoList.Role == null)
            {
                return;
            }

            string annoScheme = db.SelectAnnotationScheme(Properties.Settings.Default.DatabaseName, null, annoType);
            if (annoScheme == null)
            {
                return;
            }

            annoList.Scheme = db.GetAnnotationScheme(annoScheme, annoType);
            annoList.Scheme.Labels.Add(new AnnoScheme.Label("GARBAGE", Colors.Black));

            ObjectId annotatid = db.GetObjectID(db.GetDatabase(), "Annotators", "name", Properties.Settings.Default.MongoDBUser);
            string AnnotatorFullName = db.FetchDBRef(db.GetDatabase(), "Annotators", "fullname", annotatid);

            annoList.Name = "#" + annoList.Role + " #" + annoList.Scheme.Name + " #" + AnnotatorFullName;

            addAnnoTier(annoList);
            control.annoListControl.editComboBox.SelectedIndex = 0;
        }



    }
}

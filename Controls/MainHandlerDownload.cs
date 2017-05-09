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
using Tamir.SharpSsh;

namespace ssi
{
    public partial class MainHandler
    {

        #region GENERAL

        private int numberOfParallelDownloads = 0;
        private List<long> downloadsReceived = new List<long>();
        private List<long> downloadsTotal = new List<long>();
        private List<string> filesToDownload = new List<string>();
        private List<DownloadStatus> downloads = new List<DownloadStatus>();

        private void CanceledDownload(string localpath)
        {
            foreach (DownloadStatus d in downloads)
            {
                if (d.active == true && d.File == localpath)
                {
                    try
                    {
                        if (localpath.EndsWith("~"))
                        {
                            File.Delete(localpath.Trim('~'));
                        }
                    }
                    catch { }
                    File.Delete(localpath);
                    if (!localpath.EndsWith(".stream~"))
                    {
                        filesToDownload.Remove(localpath);
                    }
                    else
                    {
                        filesToDownload.Remove(localpath.Trim('~'));
                    }

                    numberOfParallelDownloads--;
                    break;
                }
            }

            control.ShadowBox.Visibility = Visibility.Collapsed;
            control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;

            if (numberOfParallelDownloads <= 0)
            {
                string[] files = new string[filesToDownload.Count];
                for (int i = 0; i < filesToDownload.Count; i++)
                {
                    files[i] = filesToDownload[i];
                }

                try
                {
                    if (files.Length > 0) loadMultipleFilesOrDirectory(files);
                }
                catch { }

                filesToDownload.Clear();
                downloads.Clear();
                tokenSource.Dispose();
                tokenSource = new CancellationTokenSource();
            }
        }

        private void updateOnTransfer(string text)
        {
            string[] split = text.Split('#');
            control.ShadowBoxText.Text = "";
            foreach (DownloadStatus d in downloads)
            {
                if (d.File == split[0])
                {
                    d.percent = split[1];
                }

                int pos = d.File.LastIndexOf("\\") + 1;
                if (double.Parse(d.percent) < 99.0) control.ShadowBoxText.Text = control.ShadowBoxText.Text + "Downloading " + d.File.Substring(pos, d.File.Length - pos) + "  (" + d.percent + "%)\n";
                else d.active = false;
            }
        }

        private void clientDownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string id_ = ((System.Net.WebClient)(sender)).QueryString["id"];
            int id = Int32.Parse(id_);

            string filename = ((System.Net.WebClient)(sender)).QueryString["file"];

            numberOfParallelDownloads--;
            if (numberOfParallelDownloads == 0)
            {
                Action EmptyDelegate = delegate () { };
                control.ShadowBoxText.UpdateLayout();
                control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                control.ShadowBoxText.Text = "Loading Data";
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.ShadowBox.UpdateLayout();
                downloadsReceived.Clear();
                downloadsTotal.Clear();
                string[] files = new string[filesToDownload.Count];
                for (int i = 0; i < filesToDownload.Count; i++)
                {
                    files[i] = filesToDownload[i];
                }
                loadMultipleFilesOrDirectory(files);
                filesToDownload.Clear();
            }
        }

        private void clientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                Action EmptyDelegate = delegate () { };

                string filename = ((System.Net.WebClient)(sender)).QueryString["file"];
                string id_ = ((System.Net.WebClient)(sender)).QueryString["id"];
                int id = Int32.Parse(id_);

                downloadsReceived[id] = e.BytesReceived;
                downloadsTotal[id] = e.TotalBytesToReceive;

                double bytesreceived = 0;
                double bytestotal = 0;
                for (int i = 0; i < downloadsTotal.Count; i++)

                {
                    bytesreceived = bytesreceived + downloadsReceived[i];
                    bytestotal = bytestotal + downloadsTotal[i];
                }

                double percent = ((double)bytesreceived / (double)bytestotal) * 100.0;
                
                control.ShadowBox.Visibility = Visibility.Visible;
                control.ShadowBoxText.Text = "Downloading Files... Total progress: " + "  (" + percent.ToString("F2") + "%)";
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }
        }

        #endregion GENERAL

        #region SFTP

        private async Task SFTPDownloadFiles(string url, string localpath)
        {
            bool iscanceled = false;

            string[] split = url.Split(':');
            string[] split2 = split[1].Split(new char[] { '/' }, 4);
            string ftphost = split2[2];
            string fileName = split2[3].Substring(split2[3].LastIndexOf("/") + 1, (split2[3].Length - split2[3].LastIndexOf("/") - 1));
            string folder = split2[3].Remove(split2[3].Length - fileName.Length);

            string login = Properties.Settings.Default.DataServerLogin;
            string password = Properties.Settings.Default.DataServerPass;

            lastDownloadFileName = fileName;

            string ftpfilepath = "/" + folder + fileName;            

            if (!localpath.EndsWith(".stream~")) filesToDownload.Add(localpath);
            numberOfParallelDownloads++;

            if (!File.Exists(localpath))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = "0.00";
                dl.active = true;
                downloads.Add(dl);

                Sftp sftp = new Sftp(ftphost, login, password);
                try
                {
                    sftp.OnTransferProgress += new FileTransferEvent(SFTPonTransferProgress);
                    await control.Dispatcher.BeginInvoke(new Action<string>(SFTPConnect), DispatcherPriority.Normal, "");
                    sftp.Connect();
                  
                    tokenSource = new CancellationTokenSource();
                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        if (sftp.Connected)
                        {
                            token.Register(() => { sftp.Cancel(); iscanceled = true; while (sftp.Connected) Thread.Sleep(100); CanceledDownload(localpath); return; });
                            if (!iscanceled)
                            {
                                try
                                {
                                    sftp.Get(ftpfilepath, localpath);
                                    sftp.Close();
                                }
                                catch
                                {
                                    sftp.Cancel();
                                    sftp.Close();
                                }
                            }
                        }
                    }, token);
                }
                catch (Exception e)
                {                    
                    if (null != sftp && sftp.Connected)
                    {
                        sftp.Cancel();
                    }
                    MessageBox.Show("Can't login to data server, not authorized!");
                }
            }

            if (!iscanceled) await control.Dispatcher.BeginInvoke(new Action<string>(SFTPUpdateOnTransferEnd), DispatcherPriority.Normal, "");
        }

        private void SFTPConnect(string text)
        {
            control.ShadowBoxText.Text = "Connecting to Server...";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.shadowBoxCancelButton.Visibility = Visibility.Visible;
        }

        private void SFTPUpdateOnTransferEnd(string text)
        {
            numberOfParallelDownloads--;
            if (numberOfParallelDownloads <= 0)
            {
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                numberOfParallelDownloads = 0;
                string[] files2 = new string[filesToDownload.Count];
                for (int i = 0; i < filesToDownload.Count; i++)
                {
                    files2[i] = filesToDownload[i];
                }

                if (files2.Length > 0) loadMultipleFilesOrDirectory(files2);
                filesToDownload.Clear();
                downloads.Clear();
            }
        }

        private void SFTPonTransferProgress(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            double percent = ((double)transferredBytes / (double)totalBytes) * 100.0;
            string param = dst + "#" + percent.ToString("F2");
            control.Dispatcher.BeginInvoke(new Action<string>(updateOnTransfer), DispatcherPriority.Normal, param);
        }

        #endregion SFTP

        #region HTTPPOST
        private async Task httpPost(string URL, string session, string localpath)
        {
            string login = Properties.Settings.Default.DataServerLogin;
            string password = Properties.Settings.Default.DataServerPass;

            string fileName = Path.GetFileName(localpath);
            if (fileName.EndsWith(".stream%7E"))
            {
                fileName = fileName.Remove(fileName.Length - 3);
                fileName = fileName + "~";
            }
            
            numberOfParallelDownloads++;

            if (!localpath.EndsWith(".stream~")) filesToDownload.Add(localpath);

            if (!File.Exists(localpath))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = "0.00";
                dl.active = true;
                downloads.Add(dl);

                try
                {
                    Action EmptyDelegate = delegate () { };
                    control.ShadowBoxText.Text = "Downloading '" + fileName + "'";
                    control.ShadowBox.Visibility = Visibility.Visible;
                    control.shadowBoxCancelButton.Visibility = Visibility.Visible;
                    control.UpdateLayout();
                    control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                    // Create a new WebClient instance.

                    WebClient client = new WebClient();

                    client.UploadProgressChanged += (s, e) =>
                    {
                        double percent = ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100.0;
                        string param = localpath + "#" + percent.ToString("F2");
                        control.Dispatcher.BeginInvoke(new Action<string>(updateOnTransfer), DispatcherPriority.Normal, param);
                    };

                    client.UploadValuesCompleted += (s, e) =>
                    {
                        try
                        {
                            byte[] response = e.Result;
                            File.WriteAllBytes(localpath, response);
                            control.Dispatcher.BeginInvoke(new Action<string>(httpPostFinished), DispatcherPriority.Normal, "");
                        }
                        catch (Exception ex)
                        {
                            //Could happen when we cancel the download.
                        }
                    };

                    Console.WriteLine("Downloading File \"{0}\" from \"{1}\" .......\n\n", fileName, URL);
                    
                    tokenSource = new CancellationTokenSource();
                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        token.Register(() => { client.CancelAsync(); CanceledDownload(localpath); return; });
                        string resultString = Regex.Match(session, @"\d+").Value;
                        //Here we assume that the session is stored as simple ID. (as it is done in the Noxi Database). If the SessionID really is a string, this step is not needed.
                        int sid = Int32.Parse(resultString);
                        var values = new NameValueCollection();
                        values.Add("username", login);
                        values.Add("password", password);
                        values.Add("session_id", sid.ToString());
                        values.Add("filename", fileName);

                        Uri url = new Uri(URL);
                        client.UploadValuesAsync(url, values);
                    }, token);
                }
                catch (Exception ex)
                {
                    MessageTools.Error(ex.ToString());
                }
            }
            else await control.Dispatcher.BeginInvoke(new Action<string>(httpPostFinished), DispatcherPriority.Normal, "");
        }

        private void httpPostFinished(string param)
        {
            numberOfParallelDownloads--;
            if (numberOfParallelDownloads <= 0)
            {
                Action EmptyDelegate = delegate () { };
                control.ShadowBoxText.UpdateLayout();
                control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                control.ShadowBoxText.Text = "Loading Data";
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                control.ShadowBox.UpdateLayout();

                string[] files = new string[filesToDownload.Count];
                for (int i = 0; i < filesToDownload.Count; i++)
                {
                    files[i] = filesToDownload[i];
                }

                filesToDownload.Clear();
                downloads.Clear();
                loadMultipleFilesOrDirectory(files);
            }
        }

        #endregion HTTPPOST

        #region HTTPGET

        private void httpGet(string URL, string localPath)
        {
           
            if (!File.Exists(localPath))
            {
                string fileName = Path.GetFileName(localPath);
                if (fileName.EndsWith(".stream%7E"))
                {
                    fileName = fileName.Remove(fileName.Length - 3);
                    fileName = fileName + "~";
                }

                try
                {
                    WebClient client = new WebClient();

                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(clientDownloadProgressChanged);
                    client.DownloadFileCompleted += clientDownloadFileCompleted;
                    client.QueryString.Add("file", localPath);
                    client.QueryString.Add("id", numberOfParallelDownloads.ToString());
                    downloadsTotal.Add(0);
                    downloadsReceived.Add(0);

                    if (!localPath.EndsWith(".stream~"))
                    {
                        filesToDownload.Add(localPath);
                    }

                    numberOfParallelDownloads++;
                    client.DownloadFileAsync(new Uri(URL), localPath);
                }
                catch (Exception ex)
                {
                    MessageTools.Error(ex.ToString());

                }
            }
            else
            {
                loadFile(localPath);
            }
        }

        #endregion HTTPGET
 
    }
}

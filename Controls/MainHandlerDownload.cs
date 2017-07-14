using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Tamir.SharpSsh;

namespace ssi
{
    public partial class MainHandler
    {
        private int numberOfActiveParallelDownloads = 0;
        private List<string> filesToDownload = new List<string>();
        private List<DownloadStatus> statusOfDownloads = new List<DownloadStatus>();
        public static int NumberOfAllCurrentDownloads = 0;

        private void CanceledDownload(string localpath)
        {
            foreach (DownloadStatus d in statusOfDownloads)
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

                    numberOfActiveParallelDownloads--;
                    break;
                }
            }

            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.UpdateLayout();
            control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
            control.ShadowBoxText.Text = "Loading Data";
            control.ShadowBox.Visibility = Visibility.Collapsed;
            control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
            control.ShadowBox.UpdateLayout();

            if (numberOfActiveParallelDownloads <= 0)
            {
                string[] files = new string[filesToDownload.Count];
                int i = 0;
                foreach (string path in filesToDownload)
                {
                    long length = new System.IO.FileInfo(path).Length;
                    if (length == 0)
                    {
                        if (File.Exists(path)) File.Delete(path);
                    }
                    else
                    {
                        files[i] = path;
                        i++;
                    }
                }

                loadMultipleFilesOrDirectory(files);
                filesToDownload.Clear();
                statusOfDownloads.Clear();
                tokenSource.Dispose();
                tokenSource = new CancellationTokenSource();
            }
        }

        private void UpdateOnDownload(DownloadStatus status)
        {
            control.ShadowBoxText.Text = "";
            foreach (DownloadStatus d in statusOfDownloads)
            {
                int pos = d.File.LastIndexOf("\\") + 1;
                if (status.percent < 99.0) control.ShadowBoxText.Text = control.ShadowBoxText.Text + "Downloading " + d.File.Substring(pos, d.File.Length - pos) + "  (" + d.percent.ToString("F2") + "%)\n";
                else d.active = false;
            }
        }

        private void FinishedDownload(string filePath)
        {
            NumberOfAllCurrentDownloads--;
            numberOfActiveParallelDownloads--;
            //remove empty files here, e.g. non existing ones.
            statusOfDownloads.Remove(statusOfDownloads.Find(dl => dl.File == filePath));

            if (numberOfActiveParallelDownloads <= 0 && NumberOfAllCurrentDownloads <= 0)
            {
                Action EmptyDelegate = delegate () { };
                control.ShadowBoxText.UpdateLayout();
                control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                control.ShadowBoxText.Text = "Loading Data";
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                control.ShadowBox.UpdateLayout();

                string[] files = new string[filesToDownload.Count];
                int i = 0;
                foreach (string path in filesToDownload)
                {
                    long length = new System.IO.FileInfo(path).Length;
                    if (length == 0)
                    {
                        if (File.Exists(path)) File.Delete(path);
                    }
                    else
                    {
                        files[i] = path;
                        i++;
                    }
                }

                filesToDownload.Clear();
                statusOfDownloads.Clear();
                loadMultipleFilesOrDirectory(files);
            }
        }

        private async Task SFTP(string url, string localpath)
        {
            bool iscanceled = false;

            string[] split = url.Split(':');
            string[] split2 = split[1].Split(new char[] { '/' }, 4);
            string ftphost = split2[2];
            string fileName = split2[3].Substring(split2[3].LastIndexOf("/") + 1, (split2[3].Length - split2[3].LastIndexOf("/") - 1));
            string folder = split2[3].Remove(split2[3].Length - fileName.Length);

            string login = Properties.Settings.Default.DataServerLogin;
            string password = MainHandler.Decode(Properties.Settings.Default.DataServerPass);

            lastDownloadFileName = fileName;

            string ftpfilepath = "/" + folder + fileName;

            filesToDownload.Add(localpath);
            numberOfActiveParallelDownloads++;

            if (!File.Exists(localpath))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = 0.0;
                dl.active = true;
                statusOfDownloads.Add(dl);

                Sftp sftp = new Sftp(ftphost, login, password);
                try
                {
                    sftp.OnTransferProgress += (src, dst, transferredBytes, totalBytes, message) =>
                    {
                        dl.percent = ((double)transferredBytes / (double)totalBytes) * 100.0;
                        control.Dispatcher.BeginInvoke(new Action<DownloadStatus>(UpdateOnDownload), DispatcherPriority.Normal, dl);
                    };

                    Action EmptyDelegate = delegate () { };
                    control.ShadowBoxText.Text = "Connecting to Server...";
                    control.ShadowBox.Visibility = Visibility.Visible;
                    control.shadowBoxCancelButton.Visibility = Visibility.Visible;
                    control.UpdateLayout();
                    control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
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
                catch
                {
                    if (null != sftp && sftp.Connected)
                    {
                        sftp.Cancel();
                    }
                    MessageBox.Show("Can't login to data server, not authorized!");
                }
            }

            if (!iscanceled) await control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, localpath);
        }

        private async Task httpPost(string URL, string localpath)
        {
            string login = Properties.Settings.Default.DataServerLogin;
            string password = MainHandler.Decode(Properties.Settings.Default.DataServerPass);

            string fileName = Path.GetFileName(localpath);
            if (fileName.EndsWith(".stream%7E"))
            {
                fileName = fileName.Remove(fileName.Length - 3);
                fileName = fileName + "~";
            }

            numberOfActiveParallelDownloads++;
            filesToDownload.Add(localpath);

            if (!File.Exists(localpath))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = 0.0;
                dl.active = true;
                statusOfDownloads.Add(dl);

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
                        control.Dispatcher.BeginInvoke(new Action<DownloadStatus>(UpdateOnDownload), DispatcherPriority.Normal, dl);
                    };

                    client.UploadValuesCompleted += (s, e) =>
                    {
                        try
                        {
                            byte[] response = e.Result;
                            File.WriteAllBytes(localpath, response);
                            control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, localpath);
                        }
                        catch
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

                        //Here we assume that the session is stored as simple ID. (as it is done in the Noxi Database). If the SessionID really is a string, this step is not needed.
                        string resultString = Regex.Match(DatabaseHandler.SessionName, @"\d+").Value;
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
            else await control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, "");
        }

        private async Task httpGet(string URL, string localpath)
        {
            string fileName = Path.GetFileName(localpath);
            if (fileName.EndsWith(".stream%7E"))
            {
                fileName = fileName.Remove(fileName.Length - 3);
                fileName = fileName + "~";
            }

            filesToDownload.Add(localpath);
            numberOfActiveParallelDownloads++;

            if (!File.Exists(localpath))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = 0.0;
                dl.active = true;
                statusOfDownloads.Add(dl);

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

                    client.DownloadProgressChanged += (s, e) =>
                    {
                        dl.percent = ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100.0;
                        control.Dispatcher.BeginInvoke(new Action<DownloadStatus>(UpdateOnDownload), DispatcherPriority.Normal, dl);
                    };

                    client.DownloadFileCompleted += (s, e) =>
                    {
                        try
                        {
                            control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, localpath);
                        }
                        catch
                        {
                            //Could happen when we cancel the download.
                        }
                    };

                    tokenSource = new CancellationTokenSource();
                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        token.Register(() => { client.CancelAsync(); CanceledDownload(localpath); return; });
                        client.DownloadFileAsync(new Uri(URL), localpath);
                    }, token);
                }
                catch (Exception ex)
                {
                    MessageTools.Error(ex.ToString());
                }
            }
            else await control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, "");
        }
    }
}
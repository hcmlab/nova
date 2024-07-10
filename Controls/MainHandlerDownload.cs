using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Tamir.SharpSsh;

namespace ssi
{
    public partial class MainHandler
    {
        private int numberOfActiveParallelDownloads = 0;
        private List<string> filesToDownload = new List<string>();
        private List<string> filesToRemove = new List<string>();
        private List<DownloadStatus> statusOfDownloads = new List<DownloadStatus>();
        public static int NumberOfAllCurrentDownloads = 0;

        public object Errors { get; private set; }

        private void CanceledDownload()
        {
            numberOfActiveParallelDownloads--;

            if (numberOfActiveParallelDownloads == 0)
            {
                foreach (DownloadStatus d in statusOfDownloads)
                {
                    if (d.percent < 100.0)
                    {
                        if (d.File.EndsWith("~"))
                        {
                            if (!filesToRemove.Contains(d.File.Trim('~')))
                            {
                                filesToDownload.Remove(d.File.Trim('~'));
                                filesToRemove.Add(d.File.Trim('~'));
                            }
                        }
                        if (!filesToRemove.Contains(d.File))
                        {
                            filesToDownload.Remove(d.File);
                            filesToRemove.Add(d.File);
                        }
                    }
                }
                statusOfDownloads.Clear();

                string[] files = new string[filesToDownload.Count];
                int i = 0;
                foreach (string path in filesToDownload)
                {
                    files[i] = path;
                    i++;
                }

                foreach (string file in filesToRemove)
                {
                  
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                             //here is still a problem with threading.. (process still accesses the file) needs fix TODO
                            Console.Write("Error");
                        }
                   
                }

                Action EmptyDelegate = delegate () { };
                control.ShadowBoxText.UpdateLayout();
                control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                control.ShadowBoxText.Text = "Loading Data";
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                control.ShadowBox.UpdateLayout();

                loadMultipleFilesOrDirectory(files);
                filesToDownload.Clear();
                filesToRemove.Clear();
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

            if (numberOfActiveParallelDownloads == 0 && NumberOfAllCurrentDownloads == 0)
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

                filesToDownload.Clear();
                statusOfDownloads.Clear();
                loadMultipleFilesOrDirectory(files);

                Action EmptyDelegate = delegate () { };
                control.ShadowBoxText.UpdateLayout();
                control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                control.ShadowBoxText.Text = "Loading Data";
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                control.ShadowBox.UpdateLayout();
            }
        }


        private void FinishedDownloadSync(string filePath)
        {

            //remove empty files here, e.g. non existing ones.
            statusOfDownloads.Remove(statusOfDownloads.Find(dl => dl.File == filePath));

           
                Action EmptyDelegate = delegate () { };
                control.ShadowBoxText.UpdateLayout();
                control.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                control.ShadowBoxText.Text = "Loading Data";
                control.ShadowBox.Visibility = Visibility.Collapsed;
                control.shadowBoxCancelButton.Visibility = Visibility.Collapsed;
                control.ShadowBox.UpdateLayout();

            
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

                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        if (sftp.Connected)
                        {
                            token.Register(() => { sftp.Cancel(); iscanceled = true; while (sftp.Connected) Thread.Sleep(100); CanceledDownload(); return; });
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


        public static BitmapImage DownloadImage(string url)
        {
            BitmapImage bitmap = new BitmapImage();
            using (WebClient client = new WebClient())
            {
                byte[] imageBytes = client.DownloadData(url);
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    bitmap.BeginInit();
                    bitmap.StreamSource = ms;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                }
            }
            return bitmap;
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

                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        token.Register(() => { client.CancelAsync(); CanceledDownload(); return; });

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


        private int httpGetSync(string URL, string localpath)
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
                    //control.shadowBoxCancelButton.Visibility = Visibility.Visible;
                    control.UpdateLayout();
                    control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                    // Create a new WebClient instance.

                    WebClient client = new WebClient();

                    client.DownloadProgressChanged += (s, e) =>
                    {
                        dl.percent = ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100.0;
                        control.ShadowBoxText.Text =  "Downloading " + fileName + "  (" + dl.percent.ToString("F2") + "%)\n";
                    };

                    //client.DownloadFileCompleted += (s, e) =>
                    //{
                      
                    //};

                    tokenSource = new CancellationTokenSource();

                    try
                    {
                       client.DownloadFileAsync(new Uri(URL), localpath, tokenSource);

                       
                    }
                    catch (WebException ex)
                    {

                        return -1;
                    }
                   
                }

                catch (Exception ex)
                {
                    MessageTools.Error(ex.ToString());
                }
            }

            return 1;
           // else control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, "");
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
                            //control.Dispatcher.BeginInvoke(new Action<string>(FinishedDownload), DispatcherPriority.Normal, localpath);

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

                            
                            loadFile(localpath);


                        }
                        catch
                        {
                            //Could happen when we cancel the download.
                        }
                    };

                    //tokenSource = new CancellationTokenSource();

                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        token.Register(() => { client.CancelAsync(); CanceledDownload(); return; });
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

        public static string PYTHON_VERSION = "3.8.10";
        public static string PYTHON_VERSION_FOLDER = "python38";

        private void GetPython()
        {
         

            using (webClient = new WebClient())
            {
                

                try
                {
                    if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "ssi\\python38")) Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "ssi\\"+ PYTHON_VERSION_FOLDER, true);
                    if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Lib")) Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Lib", true);
                    if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Scripts")) Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Scripts", true);
                    if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Share")) Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Share", true);
                    if (Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + "ssi\\tcl")) Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "ssi\\tcl", true);

                }

                catch (Exception e)
                {
                    Console.WriteLine(e);
 
                }


                try
                {
                    

                    webClient.DownloadFile("https://www.python.org/ftp/python/"+ PYTHON_VERSION +"/python-" + PYTHON_VERSION + "-embed-amd64.zip", "python.zip");

                    using (FileStream zipToOpen = new FileStream("python.zip", FileMode.Open))
                    {
                        using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                        {
                            ZipArchiveExtension.ExtractToDirectory(archive, "ssi", true);
                        }
                    }

                    File.Delete("python.zip");

                  

                    using (FileStream zipToOpen = new FileStream("ssi/"+ PYTHON_VERSION_FOLDER + ".zip", FileMode.Open))
                    {
                        using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                        {
                            ZipArchiveExtension.ExtractToDirectory(archive, "ssi/" + PYTHON_VERSION_FOLDER, false);
                        }
                    }
                    File.Delete("ssi/python38.zip");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }

                string path = AppDomain.CurrentDomain.BaseDirectory + "ssi\\"+ PYTHON_VERSION_FOLDER + "._pth";

                using (StreamWriter sw = File.CreateText(path))
                {
                    sw.WriteLine(".");
                    sw.WriteLine(".\\");
                    sw.WriteLine(".\\DLLs");
                    sw.WriteLine(".\\lib");
                    sw.WriteLine(".\\lib\\plat-win");
                    sw.WriteLine(".\\lib\\site-packages");
                    sw.WriteLine(".\\" + PYTHON_VERSION_FOLDER);

                }

                webClient.DownloadFile("https://bootstrap.pypa.io/get-pip.py", "ssi/get-pip.py");



                string cudapath = Environment.GetEnvironmentVariable("CUDA_PATH", EnvironmentVariableTarget.Machine);

                string[] requirements = {
                            "ssi/toolz-0.11.1-py3-none-any.whl",
                            "ssi/termcolor-1.1.0-py2.py3-none-any.whl",
                            "ssi/typing-3.7.4.3-py3-none-any.whl",
                            "ssi/promise-2.3-py3-none-any.whl",
                            "ssi/future-0.18.2-py3-none-any.whl",
                            "resampy",
                            "pickle-mixin",
                            "Flask",
                            "hcai-datasets",
                            //"tensorflow-gpu==2.0.1",
                            "imageio",
                            "h5py",
                            "matplotlib",
                            "tf-explain",
                            //"https://github.com/albermax/innvestigate/archive/1.0.7.tar.gz",
                            "lime",
                            "scipy",
                            "pymongo",
                            "scikit_image",
                            "Pillow",
                            "opencv-python",
                            "numpy",
                            "sklearn",
                            "imblearn",
                            "pandas"

                            };




                //if (cudapath != null)
                //{
                    System.IO.File.WriteAllLines("requirements.txt", requirements);

                //}
                //else
                //{
                //    MessageBoxResult mb = MessageBox.Show("No CUDA installation found, loading tensorflow without GPU support.", "Attention", MessageBoxButton.YesNo);
               

                //    requirements[3] = "tensorflow==2.12.0";

                //    System.IO.File.WriteAllLines("requirements.txt", requirements);
                //}


                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "\"" + AppDomain.CurrentDomain.BaseDirectory + "ssi\\python.exe" + "\"";
                startInfo.Arguments = "\"" + AppDomain.CurrentDomain.BaseDirectory + "ssi\\get-pip.py" + "\"";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                process.Close();


                string sitepackagepath = Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "ssi\\Lib\\site-packages");

                //var current = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.User);
                //var pythonpath = $"{sitepackagepath}";
                //Environment.SetEnvironmentVariable("PYTHONPATH", pythonpath, EnvironmentVariableTarget.User);


                //Install two wheels for broken pip install files on embedded python. Credit to: Christoph Gohlke https://www.lfd.uci.edu/~gohlke/pythonlibs/. Mirrored on NOVA public git.

                string urltoolz = "https://github.com/hcmlab/nova/raw/master/packages/python-fixes/toolz-0.11.1-py3-none-any.whl";
                string urltyping = "https://github.com/hcmlab/nova/raw/master/packages/python-fixes/typing-3.7.4.3-py3-none-any.whl";
                string urlpromise = "https://github.com/hcmlab/nova/raw/master/packages/python-fixes/promise-2.3-py3-none-any.whl";
                string urlfuture = "https://github.com/hcmlab/nova/raw/master/packages/python-fixes/future-0.18.2-py3-none-any.whl";
                string urltermcolor = "https://github.com/hcmlab/nova/raw/master/packages/python-fixes/termcolor-1.1.0-py2.py3-none-any.whl";
                //string urltkinderfix = "https://github.com/hcmlab/nova/raw/master/packages/python-fixes/tkinter-fix-377.zip";

                WebClient Client = new WebClient();
                Client.DownloadFile(urltoolz, Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "toolz-0.11.1-py3-none-any.whl");
                Client.DownloadFile(urltyping, Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "typing-3.7.4.3-py3-none-any.whl");
                Client.DownloadFile(urltermcolor, Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "termcolor-1.1.0-py2.py3-none-any.whl");
                Client.DownloadFile(urlpromise, Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "promise-2.3-py3-none-any.whl");
                Client.DownloadFile(urlfuture, Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "future-0.18.2-py3-none-any.whl");
                //Client.DownloadFile(urltkinderfix, Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "tkinterfix.zip");

                process = new Process();
                startInfo = new ProcessStartInfo();
                startInfo.FileName = "\"" + AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\python.exe" + "\"";
                startInfo.Arguments = "-m pip install -r requirements.txt --no-warn-script-location";
                process.StartInfo = startInfo;
                process.StartInfo.ErrorDialog = true;
                process.Start();
                process.WaitForExit();
                process.Close();

                //File.Delete(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "toolz-0.11.1-py3-none-any.whl");
                //File.Delete(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "typing-3.7.4.3-py3-none-any.whl");
                //File.Delete(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "promise-2.3-py3-none-any.whl");
                //File.Delete(Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\") + "promise-2.3-py3-none-any.whl");




                //if(File.Exists("ssi/_tkinter.pyd")) 
                //{
                //    File.Delete("ssi/_tkinter.pyd");
                //}
                //if (File.Exists("ssi/tcl86t.dll"))
                //{
                //    File.Delete("ssi/tcl86t.dll");
                //}
                //if (File.Exists("ssi/tk86t.dll"))
                //{
                //    File.Delete("ssi/tk86t.dll");
                //}

                //using (FileStream zipToOpen = new FileStream("ssi/tkinterfix.zip", FileMode.Open))
                //{
                //    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                //    {
                //        ZipArchiveExtension.ExtractToDirectory(archive, "ssi", false);
                //   }
                //}
                //File.Delete("ssi/tkinterfix.zip");






                //FileStream zipToOpentkinter = new FileStream("ssi/tkinterfix.zip", FileMode.Open);
                //ZipArchive archivetk = new ZipArchive(zipToOpentkinter, ZipArchiveMode.Update);
                //ZipArchiveExtension.ExtractToDirectory(archivetk, "ssi", true);

                //File.Delete("ssi/tkinterfix.zip");

                //try
                //{
                //    //Temporary FIX for VGG FACE latest Keras version bug
                //    string url = "https://raw.githubusercontent.com/hcmlab/nova/master/packages/python-fixes/vggface-fix.py";
                //    Client = new WebClient();
                //    Client.DownloadFile(url, sitepackagepath + "\\keras_vggface\\models.py");
                //}

                //catch { }

                //DOWNLOAD required python scripts from git.

                try
                {
                    Directory.CreateDirectory("PythonScripts");
                    string url = "https://raw.githubusercontent.com/hcmlab/nova/master/PythonScripts/explanation_backend.py";
                    Client.DownloadFile(url, "PythonScripts\\explanation_backend.py");

                }
                catch
                {
                }

                finally
                {
                    Properties.Settings.Default.forcepythonupdate = false;
                    Properties.Settings.Default.Save();
                }

               
                //File.Delete("requirements.txt");
                //Environment.SetEnvironmentVariable("PYTHONPATH", current, EnvironmentVariableTarget.User);
            }


        }


        public void GetFFMPEG()
        {
            using (webClient = new WebClient())
            {
                webClient.DownloadFile("https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl-shared.zip", "runtimes/FFmpeg.zip");
                using (FileStream zipToOpen = new FileStream("runtimes/FFmpeg.zip", FileMode.Open))
            {
                using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update))
                {
                    ZipArchiveExtension.ExtractToDirectory(archive, "runtimes/", true);
                }
            }
            File.Delete("runtimes/FFmpeg.zip");
            }

            var ffmpegfiles = Directory.GetFiles("runtimes\\ffmpeg-master-latest-win64-gpl-shared\\bin");
            foreach(var file in ffmpegfiles)
            {
                File.Move(file, "runtimes\\win-x64\\native\\" + Path.GetFileName(file));
            }

            Directory.Delete("runtimes\\ffmpeg-master-latest-win64-gpl-shared", true);


        }

    }



}
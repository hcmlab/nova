using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;


namespace ssi
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CMLUpdater : Window
    {

        WebClient webClient;
        Stopwatch sw = new Stopwatch();
        int remainingfiles = int.MaxValue;


        public CMLUpdater(string SSIbinaryGitPath, string cmltrainexe, string cmltrainexePath)
        {
            InitializeComponent();
            update(SSIbinaryGitPath, cmltrainexe, cmltrainexePath);

        }


        private void update(string SSIbinaryGitPath, string cmltrainexe, string cmltrainexePath)
        {


            List<string> urls = new List<string>();

            //Test Internetconnection
            try
            {
                urls.Add(SSIbinaryGitPath + cmltrainexe);
               
                //DownloadFile(SSIbinaryGitPath + cmltrainexe, cmltrainexePath);
            }
            catch
            {
                MessageBox.Show("Can't update tools, check your internet conenction!");
                return;
            }


            //Download xmlchain, if not present yet.
            urls.Add(SSIbinaryGitPath + "xmlchain.exe");
            urls.Add(SSIbinaryGitPath + "xmlpipe.exe");
            urls.Add(SSIbinaryGitPath + "libmongoc-1.0.dll");
            urls.Add(SSIbinaryGitPath + "libbson-1.0.dll");
            urls.Add(SSIbinaryGitPath + "ssiframe.dll");
            urls.Add(SSIbinaryGitPath + "opencv_world310.dll");


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
                    string location = AppDomain.CurrentDomain.BaseDirectory + "\\ssi\\" + System.IO.Path.GetFileName(url);
                    Uri URL = new Uri(url);
                    // Start the stopwatch which we will be using to calculate the download speed
                    sw.Start();

                    try
                    {
                        // Start downloading the file
                        webClient.DownloadFileAsync(URL,location);
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
                throw e.Error;
            }
            if (e.Cancelled)
            {
                // handle cancelled scenario
            }
            DownloadFile();

            if(remainingfiles == 0)
            {
                MessageBox.Show("Cooperative Machine Learning tools are now up to date.");
                this.Close();
            }
        }

    }
}

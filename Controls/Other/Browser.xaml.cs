using Microsoft.Web.WebView2.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Diagnostics;



namespace ssi
{
    /// <summary>
    /// Interaktionslogik für Browser.xaml
    /// </summary>
    public partial class LNBrowser : Window
    {
        string id = "0";

        public LNBrowser(string url)
        {
            InitializeComponent();

            InitializeBrowser(url);


        }

        private async Task InitializeBrowser(string url = null)
        {
            bool performedWebview2Installation = false;
            try
            {

                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                var userDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\nova";
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                await browser.EnsureCoreWebView2Async(env);

            }

            catch (WebView2RuntimeNotFoundException exception)
            {

                try
                {
                    string webview = "webview2.exe";
                    string webviewpath = AppDomain.CurrentDomain.BaseDirectory + webview;
                    MainHandler.DownloadFile("https://go.microsoft.com/fwlink/p/?LinkId=2124703", webviewpath);
                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    startInfo.FileName = "\"" + webviewpath + "\"";
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    process.Close();
                    File.Delete(webviewpath);
                    performedWebview2Installation = true;
                    InitializeBrowser(url);
        
                  
                    }
                catch (Exception e)
                {

                    MessageTools.Warning("You need webview2 in Order to access webtools. Please manually install from https://go.microsoft.com/fwlink/p/?LinkId=2124703");
                   

                }

            }

            catch (Exception ex)
            {
                MessageTools.Warning(ex.Message);
                this.Close();
                //MessageTools.Warning("You need webview2 in Order to access webtools. Please manually install from https://go.microsoft.com/fwlink/p/?LinkId=2124703");
            }
            if (!performedWebview2Installation) browser.Source = new UriBuilder(url).Uri;
   
        }

        void ReceiveLoginData(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            string reply = args.WebMessageAsJson;
            id = reply.Remove(reply.Length - 1).Remove(0, 1);

            DialogResult = true;
            // this.Close();
            // parse the JSON string into an object
            // ...
        }

        public string LNID()
        {
            return (id);
        }

        private void browser_CoreWebView2InitializationCompleted(object sender, CoreWebView2InitializationCompletedEventArgs e)
        {
            browser.CoreWebView2.WebMessageReceived += ReceiveLoginData;
        }
    }
}

﻿using Microsoft.Web.WebView2.Wpf;
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
            try
            {
                var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                //browser.CoreWebView2.Navigate("https://microsoft.com");
                browser.Source = new Uri(url);
             

            }
            catch (WebView2RuntimeNotFoundException exception)
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
                this.Close();
            }
        

            //  this.webView.CoreWebView2.Navigate(url);
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

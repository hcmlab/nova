using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using OxyPlot.Reporting;
using Secp256k1Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;

using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ThirdParty.BouncyCastle.Utilities.IO.Pem;
using WebSocketSharp;

using static ssi.MainHandler;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using Button = System.Windows.Controls.Button;
using Image = System.Windows.Controls.Image;


namespace ssi
{
    /// <summary>
    /// Interaktionslogik für NostrDVM.xaml
    /// </summary>
    public partial class BatchTools : System.Windows.Window
    {
        public BatchTools()
        {
            InitializeComponent();
         
           
        }

        private void PickDownloadDirectory_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Defaults.LocalDataLocations().First();
            dialog.ShowNewFolderButton = true;
            dialog.Description = "Select the folder where you want to store the media of your databases in.";
            System.Windows.Forms.DialogResult result = System.Windows.Forms.DialogResult.None;

            try
            {
                dialog.SelectedPath = Defaults.LocalDataLocations().First();
                result = dialog.ShowDialog();

            }

            catch
            {
                dialog.SelectedPath = "";
                result = dialog.ShowDialog();
            }



            if (result == System.Windows.Forms.DialogResult.OK)
            {
                DownloadDirectory.Text = dialog.SelectedPath;
            }
        }

        private void ViewDownloadDirectory_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(DownloadDirectory.Text))
            {
                Directory.CreateDirectory(DownloadDirectory.Text);
                Process.Start(DownloadDirectory.Text);
            }
        }

        private void batch_send_Click(object sender, RoutedEventArgs e)
        {
            if (elan.IsChecked == true)
            {
                MainHandler.BatchConvertElanAnnotations(DownloadDirectory.Text);
            }
            else if (noldus.IsChecked == true)
            {
                MainHandler.batchConvertNoldus(DownloadDirectory.Text);
            }
           
            System.Windows.MessageBox.Show("Done");

        }
    }
}

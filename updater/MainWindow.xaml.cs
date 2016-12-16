using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
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

namespace updater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow(string version)
        {
            InitializeComponent();


            update(version);

        }


        private void update(string version)
        {

            string url = "https://github.com/hcmlab/nova/releases/download/" + version + "/nova.exe";

            WebClient Client = new WebClient();
            Client.DownloadFile(url,"nova.exe");



         
            System.Diagnostics.Process updateProcess = new System.Diagnostics.Process();
            updateProcess.StartInfo.FileName = "nova.exe";
            updateProcess.Start();


            //System.Diagnostics.Process deleteupdater = new System.Diagnostics.Process();
            //deleteupdater.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\nova.exe";
            //deleteupdater.StartInfo.Arguments = "/ C choice / C Y / N / D Y / T 3 & Del " + AppDomain.CurrentDomain.BaseDirectory + "\\updater.exe";
            //deleteupdater.Start();
           

            ProcessStartInfo Info = new ProcessStartInfo();
            Info.Arguments = "/C choice /C Y /N /D Y /T 3 & Del " + "updater.exe";
            //Info.WindowStyle = ProcessWindowStyle.Hidden;
            //Info.CreateNoWindow = true;
            Info.FileName = "cmd.exe";
            Process.Start(Info);

            this.Close();

        }

    }
}

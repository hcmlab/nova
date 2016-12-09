using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseCMLCompleteTier.xaml
    /// </summary>
    public partial class DatabaseCMLCompleteTierWindow : Window
    {
        ViewHandler handler;

        public DatabaseCMLCompleteTierWindow(ViewHandler handler)
        {
            InitializeComponent();

            this.handler = handler;
            StreamListBox.Items.Add("close.mfccdd[-f 0.04 -d 0]");

            foreach (AnnoTrack tier in handler.AnnoTracks)
            {
                if (tier.AnnoList.FromDB)
                {
                    TierListBox.Items.Add(tier.AnnoList.Name);
                }
            }

            StreamListBox.SelectedIndex = 0;
            TierListBox.SelectedIndex = 0;

        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (TierListBox.SelectedItem != null)
            {
                string tierName = (string)TierListBox.SelectedItem;
                AnnoTrack tier = handler.getAnnoTrackFromName(tierName);
                if (tier != null)
                {
                    if (StreamListBox.SelectedItem != null)
                    {
                        string stream = (string)StreamListBox.SelectedItem;
                        int context = 0;
                        if (int.TryParse(ContextTextBox.Text, out context))
                        {
                            DatabaseHandler db = new DatabaseHandler("mongodb://" + Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@" + Properties.Settings.Default.MongoDBIP);
                            db.StoreToDatabase(Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser, tier, handler.loadedDBmedia, false);
                            completeTier(context, tier, stream);
                        }
                    }
                }
            }
        }

        private void FinishedButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        void completeTier(int context, AnnoTrack tier, string stream)
        {

            Directory.CreateDirectory(Properties.Settings.Default.DataPath + "\\" + Properties.Settings.Default.Database + "\\models");

            string username = Properties.Settings.Default.MongoDBUser;
            string password = Properties.Settings.Default.MongoDBPass;
            string session = Properties.Settings.Default.LastSessionId;
            string datapath = Properties.Settings.Default.DataPath;
            string ipport = Properties.Settings.Default.MongoDBIP;
            string[] split = ipport.Split(':');
            string ip = split[0];
            string port = split[1];
            string database = Properties.Settings.Default.Database;
            string role = tier.AnnoList.Role;
            string scheme = tier.AnnoList.AnnotationScheme.name;
            string annotator = tier.AnnoList.Annotator;

            bool isTrained = false;
            bool isForward = false;

            string arguments = " -cooperative "
                    + "-context " + context +
                    " -username " + username +
                    " -password " + password +
                    " -filter " + session +
                    " -log cml.log " +
                    "\"" + datapath + "\\" + database + "\" " +
                    ip + " " +
                    port + " " +
                    database + " " +
                    role + " " +
                    scheme + " " +
                    annotator + " " +
                    "\"" + stream + "\"";

            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
             //   startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmltrain.exe";
                startInfo.Arguments = "--train" + arguments;
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    isTrained = true;
                }
                else
                {
                    return;
                }
            }

            {
                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
               // startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                startInfo.FileName = "cmltrain.exe";
                startInfo.Arguments = "--forward" + arguments;                
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    isForward = true;
                }
                else
                {
                    return;
                }
            }

            if (isTrained && isForward)
            {
                handler.reloadAnnoDB(tier);
            }
        }

    }
}

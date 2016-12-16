using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseCMLCompleteTier.xaml
    /// </summary>
    public partial class DatabaseCMLCompleteTierWindow : Window
    {
        private ViewHandler handler;

        public DatabaseCMLCompleteTierWindow(ViewHandler handler)
        {
            InitializeComponent();

            this.handler = handler;
            StreamListBox.Items.Add("close.mfccdd[-f 0.04 -d 0]");

            foreach (AnnoTrack tier in handler.AnnoTracks)
            {
                if (tier.AnnoList.FromDB && tier.AnnoList.AnnotationScheme.name == "voiceactivity")
                {

                    TierListBox.Items.Add(tier.AnnoList.Name);
                }
            }

            StreamListBox.SelectedIndex = 0;
            TierListBox.SelectedIndex = 0;

            ContextTextBox.Text = Properties.Settings.Default.CMLContext.ToString();
            TierListBox.SelectedItem = Properties.Settings.Default.CMLDefaultScheme;

            ConfidenceCheckBox.IsChecked = Properties.Settings.Default.CMLSetConf;
            FillGapCheckBox.IsChecked = Properties.Settings.Default.CMLFill;
            RemoveLabelCheckBox.IsChecked = Properties.Settings.Default.CMLRemove;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (TierListBox.SelectedItem != null)
            {
                foreach (var tierName in TierListBox.SelectedItems)
                {
                    AnnoTrack tier = handler.getAnnoTrackFromName(tierName.ToString());
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
        }

        private void FinishedButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void completeTier(int context, AnnoTrack tier, string stream)
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
            double confidence = -1.0;
            if (ConfidenceTextBox.IsEnabled)
            {
                double.TryParse(ConfidenceTextBox.Text, out confidence);
            }
            double minGap = 0.0;
            if (FillGapTextBox.IsEnabled)
            {
                double.TryParse(FillGapTextBox.Text, out minGap);
            }
            double minDur = 0.0;
            if (RemoveLabelTextBox.IsEnabled)
            {
                double.TryParse(RemoveLabelTextBox.Text, out minDur);
            }

            bool isTrained = false;
            bool isForward = false;

            string arguments = " -cooperative "
                    + "-context " + context +
                    " -username " + username +
                    " -password " + password +
                    " -filter " + session +
                    " -confidence " + confidence +
                    " -mingap " + minGap +
                    " -mindur " + minDur +
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

        private void ConfidenceCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLSetConf = true;
            Properties.Settings.Default.Save();
            ConfidenceTextBox.IsEnabled = true;
        }

        private void ConfidenceCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLSetConf = false;
            Properties.Settings.Default.Save();
            ConfidenceTextBox.IsEnabled = false;
        }

        private void FillGapCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLFill = true;
            Properties.Settings.Default.Save();
            FillGapTextBox.IsEnabled = true;
        }

        private void FillGapCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLFill = false;
            Properties.Settings.Default.Save();
            FillGapTextBox.IsEnabled = false;
        }

        private void RemoveLabelCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLRemove = true;
            Properties.Settings.Default.Save();
            RemoveLabelTextBox.IsEnabled = true;
        }

        private void RemoveLabelCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.CMLRemove = false;
            Properties.Settings.Default.Save();
            RemoveLabelTextBox.IsEnabled = false;
        }
    }
}
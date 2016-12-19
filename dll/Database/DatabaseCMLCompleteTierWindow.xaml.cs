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
              //  if (tier.AnnoList.FromDB && tier.AnnoList.AnnotationScheme.name == "voiceactivity")
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

            ConfidenceTextBox.Text = Properties.Settings.Default.CMLDefaultConf.ToString();
            FillGapTextBox.Text = Properties.Settings.Default.CMLDefaultGap.ToString();
            RemoveLabelTextBox.Text = Properties.Settings.Default.CMLDefaultMinDur.ToString();
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
                                Properties.Settings.Default.CMLDefaultGap = minGap;
                                Properties.Settings.Default.CMLDefaultConf = confidence;
                                Properties.Settings.Default.CMLDefaultMinDur = minDur;
                                Properties.Settings.Default.Save();

                                logTextBox.Text = "";
                                logTextBox.AppendText(handler.completeTier(context, tier, stream, confidence, minGap, minDur));
                                //logTextBox.AppendText(File.ReadAllText("cml.log"));
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

        private void StreamListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Properties.Settings.Default.CMLDefaultStream = StreamListBox.SelectedItem.ToString() ;
            Properties.Settings.Default.Save();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(logTextBox.Text);
        }
    }
}
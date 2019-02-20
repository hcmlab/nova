using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace ssi
{
    public partial class MainHandler
    {
        public enum MEDIABACKEND
        {
            MEDIA,
            MEDIAKIT
        }



        private void addMediaBox(IMedia media)
        {
            int mediaListCount = mediaList.Count;
            MediaBox box = new MediaBox(media);
            control.mediaBoxControl.Add(box);

            mediaBoxes.Add(box);

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(50);
            timer.Tick += new EventHandler(delegate (object o, EventArgs a)
            {
                if (media.GetLength() > 0)
                {
                    updateTimeRange(media.GetLength());
                    if (mediaListCount == 1
                        && media.GetLength() > Properties.Settings.Default.DefaultZoomInSeconds
                        && Properties.Settings.Default.DefaultZoomInSeconds != 0)
                    {
                        fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
                    }
                    timer.Stop();
                }
            });
            timer.Start();

            MediaBoxStatic.Select(box);
            updateNavigator();
        }

        public void clearMediaBox()
        {
            control.mediaSettingsButton.Visibility = Visibility.Hidden;
            control.mediaStatusFileNameLabel.Text = "";
            control.mediaStatusFileNameLabel.ToolTip = "";
            control.mediaStatusSampleRateLabel.Text = "";
            control.mediaVolumeControl.Visibility = Visibility.Collapsed;
            control.mediaPositionLabel.Text = "#0";
            control.mediaCloseButton.Visibility = Visibility.Hidden;
        }

        private void updateMediaBox(MediaBox box)
        {
            if (box != null)
            {
                IMedia media = box.Media;
                if (media != null)
                {
                    control.mediaSettingsButton.Visibility = Visibility.Visible;
                    control.mediaStatusFileNameLabel.Text = Path.GetFileName(media.GetFilepath());
                    control.mediaStatusFileNameLabel.ToolTip = media.GetFilepath();
                    control.mediaStatusSampleRateLabel.Text = media.GetSampleRate().ToString() + " Hz";
                    control.mediaVolumeControl.Visibility = Visibility.Collapsed;
                    if (media.HasAudio())
                    {
                        media.SetVolume(0.0);
                        //control.mediaVolumeControl.volumeSlider.Value = media.GetVolume();
                        //control.mediaVolumeControl.Visibility = Visibility.Visible;
                    }                    
                    control.mediaCloseButton.Visibility = playIsPlaying ? Visibility.Hidden : Visibility.Visible;

                    if (AnnoTierStatic.Selected != null)
                    {
                        control.navigator.askforlabels.Visibility = AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE ? Visibility.Visible : Visibility.Collapsed;
                        control.navigator.askforlabels.IsChecked = AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE ? control.navigator.askforlabels.IsChecked : false;
                    }
                }
            }
        }

        private void onMediaBoxChange(MediaBox box, EventArgs e)
        {
            updateMediaBox(box);
        }

        private void mediaVolumeControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MediaBoxStatic.Selected != null && MediaBoxStatic.Selected.Media != null && MediaBoxStatic.Selected.Media.HasAudio())
            {
                MediaBoxStatic.Selected.Media.SetVolume(control.mediaVolumeControl.volumeSlider.Value);
            }
        }

        private void mediaBoxCloseButton_Click(object sender, RoutedEventArgs e)
        {
            removeMediaBox();
        }

        private void removeMediaBox()
        {
            removeMediaBox(MediaBoxStatic.Selected);
        }

        private void removeMediaBox(MediaBox box)
        {           
            if (box != null)
            {
                control.mediaBoxControl.Remove(box);                

                MediaBoxStatic.Unselect();
                mediaBoxes.Remove(box);
                mediaList.Remove(box.Media);

                if (mediaBoxes.Count > 0)
                {
                    MediaBoxStatic.Select(mediaBoxes[0]);
                }
                else
                {
                    clearMediaBox();
                    updateNavigator();
                }
            }
        }

        public static double getMaxVideoSampleRate()
        {
            double rate = 0;
            foreach (IMedia media in mediaList)
            {
                if (media.GetMediaType() != MediaType.AUDIO)
                {
                    rate = Math.Max(rate, media.GetSampleRate());
                }
            }
            return rate == 0 ? Defaults.DefaultSampleRate : rate;
        }
       
    }
}
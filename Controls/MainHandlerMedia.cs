using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ssi
{
    public partial class MainHandler
    {
        private void addMediaBox(IMedia media)
        {
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
                    if (mediaList.Count == 1
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

        private void changeMediaBox(MediaBox box)
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
                    if (media.HasAudio())
                    {
                        control.mediaVolumeControl.volumeSlider.Value = media.GetVolume();
                        control.mediaVolumeControl.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        control.mediaVolumeControl.Visibility = Visibility.Collapsed;
                    }
                    control.mediaCloseButton.Visibility = Visibility.Visible;
                }
            }           
        }

        private void onMediaBoxChange(MediaBox box, EventArgs e)
        {
            changeMediaBox(box);
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
            MediaBox box = MediaBoxStatic.Selected;

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
                }
            }
        }


        #region MEDIAPLAYER

        // Plays Signal (e.g Skeleton, Face) when no audiovisual file is loaded
        private void noMediaPlayHandler(Signal signal = null)
        {
            control.navigator.playButton.IsEnabled = true;
            double fps;
            if (signal != null)
            {
                fps = signal.rate;
                skelfps = fps;
            }
            else
            {
                fps = skelfps;
            }
            EventHandler ev = new EventHandler(delegate (object sender, EventArgs a)
            {
                if (!movemedialock)
                {
                    double time = (MainHandler.Time.CurrentPlayPositionPrecise + (1000.0 / fps) / 1000.0);
                    MainHandler.Time.CurrentPlayPositionPrecise = time;
                    // if (media_list.Medias.Count == 0)
                    if (visualizeskel || visualizepoints)
                    {
                        signalCursor.X = Time.PixelFromTime(Time.CurrentPlayPositionPrecise);

                        if (Time.CurrentPlayPositionPrecise >= Time.SelectionStop && control.navigator.followplaybox.IsChecked == true)
                        {
                            double factor = (((Time.CurrentPlayPositionPrecise - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                            control.timeLineControl.rangeSlider.followmedia = true;
                            control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
                        }
                        else if (control.navigator.followplaybox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;

                        //hm additional syncstep..
                        if (lasttimepos < Time.CurrentPlayPosition)
                        {
                            lasttimepos = Time.CurrentPlayPosition;
                            Time.CurrentPlayPositionPrecise = lasttimepos;
                        }
                        if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
                    }
                }

                if (!inNoMediaPlayMode)
                {
                    if (_timerp != null)
                    {
                        _timerp.Stop();
                        _timerp = null;
                    }
                }
            });

            if (inNoMediaPlayMode)
            {
                // lasttimepos = ViewHandler.Time.CurrentPlayPosition;
                control.navigator.playButton.Content = "II";
                // Play();
                _timerp = new DispatcherTimer();
                _timerp.Interval = TimeSpan.FromMilliseconds(1000.0 / fps);
                _timerp.Tick += ev;
                _timerp.Start();
            }
            else
            {
                if (_timerp != null)
                {
                    _timerp.Stop();
                    _timerp.Tick -= ev;
                }
            }
        }

        private void onMediaPlay(MediaList videos, MediaPlayEventArgs e)
        {
            if (movemedialock == false)
            {
                double time = e.pos;
                double pos = Time.PixelFromTime(time);

                if (Time.SelectionStop - Time.SelectionStart < 1)
                {
                    Time.SelectionStart = Time.SelectionStop - 1;
                }

                Time.CurrentPlayPosition = time;

                if (!visualizeskel && !visualizepoints)
                {
                    signalCursor.X = pos;
                }

                updatePositionLabels(time);
                control.annoTierControl.currentTime = time;

                if (time > timeline.TotalDuration - 0.5)
                {
                    Stop();
                }
            }

            if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.followplaybox.IsChecked == true && !movemedialock)
            {
                double factor = (((Time.CurrentPlayPosition - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                control.timeLineControl.rangeSlider.followmedia = true;
                control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
            }
            else if (control.navigator.followplaybox.IsChecked == false)
            {
                control.timeLineControl.rangeSlider.followmedia = false;
                if (AnnoTierStatic.Label != null)
                {
                    AnnoTierStatic.Label.select(true);
                }
            }
        }

        private void handlePlay()
        {
            if ((string)control.navigator.playButton.Content == "II")
            {
                //   nomediaPlayHandler(null);
                inNoMediaPlayMode = false;
                control.navigator.playButton.Content = ">";
            }
            else
            {
                inNoMediaPlayMode = true;
                noMediaPlayHandler(null);
                control.navigator.playButton.Content = "II";
            }

            infastbackward = false;
            infastforward = false;

            control.navigator.fastForwardButton.Content = ">>";
            control.navigator.fastBackwardButton.Content = "<<";

            if (mediaList.Count > 0)
            {
                if (IsPlaying())
                {
                    Stop();
                }
                else
                {
                    Play();
                }
            }
        }

        public void Play()
        {
            Stop();

            double pos = 0;
            AnnoListItem item = null;
            bool loop = false;

            AnnoTierSegment selected = AnnoTierStatic.Label;
            if (selected != null)
            {
                item = selected.Item;
                signalCursor.X = Time.PixelFromTime(item.Start);
                loop = true;
            }
            else
            {
                pos = signalCursor.X;
                double from = MainHandler.Time.TimeFromPixel(pos);
                double to = MainHandler.timeline.TotalDuration;
                item = new AnnoListItem(from, to, "");
                signalCursor.X = pos;
            }

            try
            {
                mediaList.Play(item, loop);
                control.navigator.playButton.Content = "II";
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }

            //
        }

        public bool IsPlaying()
        {
            return mediaList.IsPlaying;
        }

        public void Stop()
        {
            if (IsPlaying())
            {
                mediaList.Stop();
                control.navigator.playButton.Content = ">";
            }
        }

        #endregion MEDIAPLAYER


    }
}

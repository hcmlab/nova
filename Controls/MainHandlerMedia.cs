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
      

        private void loadMedia(string filename, bool is_video, string url = null)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Media file not found '" + filename + "'");
                return;
            }

            double pos = MainHandler.Time.TimeFromPixel(signalCursor.X);
            IMedia media = mediaList.addMedia(filename, pos, url);
            control.mediaVideoControl.addMedia(media, is_video);
            control.navigator.playButton.IsEnabled = true;
            innomediaplaymode = false;
            noMediaPlayHandler(null);

            ColumnDefinition columvideo = control.videoskel.ColumnDefinitions[0];
            columvideo.Width = new GridLength(1, GridUnitType.Star);

            DispatcherTimer _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += new EventHandler(delegate (object s, EventArgs a)
            {
                if (media.GetLength() > 0)
                {
                    updateTimeRange(media.GetLength());
                    if (this.mediaList.Medias.Count == 1 && media.GetLength() > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
                    _timer.Stop();
                }
            });
            _timer.Start();
        }
        private void removeMedia(object sender, MediaRemoveEventArgs e)
        {
            mediaList.Medias.Remove(e.media);
        }


        #region MEDIAPLAYER

        // Plays Signal (e.g Skeleton, Face) when no audiovisual file is loaded
        private void noMediaPlayHandler(Signal s = null)
        {
            control.navigator.playButton.IsEnabled = true;
            double fps;
            if (s != null)
            {
                fps = s.rate;
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
                        signalCursor.X = MainHandler.Time.PixelFromTime(MainHandler.Time.CurrentPlayPositionPrecise);

                        if (Time.CurrentPlayPositionPrecise >= Time.SelectionStop && control.navigator.followplaybox.IsChecked == true)
                        {
                            double factor = (((Time.CurrentPlayPositionPrecise - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                            control.timeLineControl.rangeSlider.followmedia = true;
                            control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
                        }
                        else if (control.navigator.followplaybox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;

                        //hm additional syncstep..
                        if (lasttimepos < MainHandler.Time.CurrentPlayPosition)
                        {
                            lasttimepos = MainHandler.Time.CurrentPlayPosition;
                            MainHandler.Time.CurrentPlayPositionPrecise = lasttimepos;
                        }
                        if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
                    }
                }

                if (!innomediaplaymode)
                {
                    if (_timerp != null)
                    {
                        _timerp.Stop();
                        _timerp = null;
                    }
                }
            });

            if (innomediaplaymode)
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

        private void mediaPlayHandler(MediaList videos, MediaPlayEventArgs e)
        {
            if (movemedialock == false)
            {
                double pos = MainHandler.Time.PixelFromTime(e.pos);

                if (Time.SelectionStop - Time.SelectionStart < 1) Time.SelectionStart = Time.SelectionStop - 1;

                Time.CurrentPlayPosition = e.pos;

                if (!visualizeskel && !visualizepoints) signalCursor.X = pos;
                //   Console.WriteLine("5 " + signalCursor.X);
                //if (ViewHandler.Time.TimeFromPixel(signalCursor.X) > Time.SelectionStop || signalCursor.X <= 1 ) signalCursor.X = ViewHandler.Time.PixelFromTime(Time.SelectionStart);
                // Console.WriteLine(signalCursor.X + "_____" + Time.SelectionStart);

                double time = Time.TimeFromPixel(pos);
                control.signalStatusPositionLabel.Text = FileTools.FormatSeconds(e.pos);
                control.annoTierControl.currentTime = Time.TimeFromPixel(pos);

                if (e.pos > MainHandler.timeline.TotalDuration - 0.5)
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
            else if (control.navigator.followplaybox.IsChecked == false) control.timeLineControl.rangeSlider.followmedia = false;
            if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
        }

        private void handlePlay()
        {
            if ((string)control.navigator.playButton.Content == "II")
            {
                //   nomediaPlayHandler(null);
                innomediaplaymode = false;
                control.navigator.playButton.Content = ">";
            }
            else
            {
                innomediaplaymode = true;
                noMediaPlayHandler(null);
                control.navigator.playButton.Content = "II";
            }

            infastbackward = false;
            infastforward = false;

            control.navigator.fastForwardButton.Content = ">>";
            control.navigator.fastBackwardButton.Content = "<<";

            if (mediaList.Medias.Count > 0)
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
                mediaList.play(item, loop);
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
                mediaList.stop();
                control.navigator.playButton.Content = ">";
            }
        }

        #endregion MEDIAPLAYER


    }
}

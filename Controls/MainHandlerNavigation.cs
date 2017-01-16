using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace ssi
{
    public partial class MainHandler
    {
        private void initCursor()
        {
            control.trackGrid.MouseDown += signalTrackGrid_MouseDown;
            control.annoTrackControl.MouseDown += annoTrackGrid_MouseDown;
            control.annoTrackControl.MouseMove += annoTrackGrid_MouseMove;
            control.annoTrackControl.MouseRightButtonUp += annoTrackGrid_MouseUp;

            AdornerLayer cursorLayer = control.signalAndAnnoControl.AdornerLayer;
            signalCursor = new Cursor(control.trackGrid, Brushes.Red, 1.5);
            cursorLayer.Add(signalCursor);
            annoCursor = new Cursor(control.trackGrid, Brushes.Green, 1.5);
            cursorLayer.Add(annoCursor);

            signalCursor.OnCursorChange += onCursorChange;
            signalCursor.MouseDown += annoTrackGrid_MouseDown;
            annoCursor.MouseDown += annoTrackGrid_MouseDown;
        }

        private void onCursorChange(double pos)
        {
            if (SignalTrackStatic.Selected != null)
            {
                Signal signal = SignalTrackStatic.Selected.Signal;
                if (signal != null)
                {
                    double time = Time.TimeFromPixel(pos);
                    control.signalValueLabel.Text = signal.Value(time).ToString();
                    control.signalValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                    control.signalValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString();
                }
            }
        }

        private void moveCursorTo(double seconds)
        {
            double pos = MainHandler.Time.PixelFromTime(seconds);

            signalCursor.X = pos;

            //this.view.scrollViewer.ScrollToHorizontalOffset(Math.Max(0, pos - this.view.scrollViewer.ActualWidth / 2));
            double time = Time.TimeFromPixel(pos);
            control.signalPositionLabel.Text = FileTools.FormatSeconds(time);
        }

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

                            control.timeTrackControl.rangeSlider.followmedia = true;
                            control.timeTrackControl.rangeSlider.MoveAndUpdate(true, factor);
                        }
                        else if (control.navigator.followplaybox.IsChecked == false) control.timeTrackControl.rangeSlider.followmedia = false;

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
                control.signalPositionLabel.Text = FileTools.FormatSeconds(e.pos);
                control.annoTrackControl.currenttime = Time.TimeFromPixel(pos);

                if (e.pos > MainHandler.timeline.TotalDuration - 0.5)
                {
                    Stop();
                }
            }

            if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.followplaybox.IsChecked == true && !movemedialock)
            {
                double factor = (((Time.CurrentPlayPosition - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                control.timeTrackControl.rangeSlider.followmedia = true;
                control.timeTrackControl.rangeSlider.MoveAndUpdate(true, factor);
            }
            else if (control.navigator.followplaybox.IsChecked == false) control.timeTrackControl.rangeSlider.followmedia = false;
            if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(true);
        }


        private void navigatorFollowAnno_Unchecked(object sender, RoutedEventArgs e)
        {
            AnnoTier.UnselectLabel();

            bool is_playing = IsPlaying();
            Stop();
            if (is_playing)
            {
                Play();
            }
        }

        private void navigatorJumpFront_Click(object sender, RoutedEventArgs e)
        {
            bool is_playing = IsPlaying();
            Stop();
            moveCursorTo(0);
            if (is_playing)
            {
                Play();
            }
        }

        private void navigatorJumpEnd_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            moveCursorTo(MainHandler.Time.TotalDuration);
        }

        private void navigatorPlay_Click(object sender, RoutedEventArgs e)
        {
            handlePlay();
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

        private void navigatorFastForward_Click(object sender, RoutedEventArgs e)
        {
            int updateinms = 300;
            double updatestep = 1;

            infastbackward = false;
            if (infastforward) infastforward = false;
            else infastforward = true;

            if (infastforward)
            {
                control.navigator.fastForwardButton.Content = ">";
                _timerff.Interval = TimeSpan.FromMilliseconds(updateinms);
                _timerff.Tick += new EventHandler(delegate (object s, EventArgs a)
                {
                    if (mediaList.Medias.Count > 0)
                    {
                        mediaList.move(MainHandler.Time.TimeFromPixel(signalCursor.X) + updatestep);
                        //  media_list.move(Time.CurrentPlayPosition+1);
                        if (!infastforward) _timerff.Stop();
                    }
                });
                _timerff.Start();
            }
            else
            {
                _timerff.Stop();
                control.navigator.fastForwardButton.Content = ">>";
            }
        }

        private void navigatorFastBackward_Click(object sender, RoutedEventArgs e)
        {
            int updateinms = 300;
            double updatestep = 1;

            infastforward = false;

            if (infastbackward) infastbackward = false;
            else infastbackward = true;

            if (infastbackward)
            {
                control.navigator.fastBackwardButton.Content = ">";
                _timerfb.Interval = TimeSpan.FromMilliseconds(updateinms);
                _timerfb.Tick += new EventHandler(delegate (object s, EventArgs a)
                {
                    if (mediaList.Medias.Count > 0)
                    {
                        mediaList.move(MainHandler.Time.TimeFromPixel(signalCursor.X) - updatestep);
                        if (!infastbackward) _timerfb.Stop();
                    }
                });
                _timerfb.Start();
            }
            else
            {
                _timerfb.Stop();
                control.navigator.fastBackwardButton.Content = "<<";
            }
        }

        public void Stop()
        {
            if (IsPlaying())
            {
                mediaList.stop();
                control.navigator.playButton.Content = ">";
            }
        }

        public bool IsPlaying()
        {
            return mediaList.IsPlaying;
        }

        public void Play()
        {
            Stop();

            double pos = 0;
            AnnoListItem item = null;
            bool loop = false;

            AnnoTierLabel selected = AnnoTierStatic.Label;
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

        private void navigatorCorrectionMode_Click(object sender, RoutedEventArgs e)
        {
            if (control.navigator.correctionModeCheckBox.IsChecked == true) AnnoTier.CorrectMode = true;
            else AnnoTier.CorrectMode = false;

            foreach (AnnoTier a in annoTiers)
            {
                a.TimeRangeChanged(timeline);
            }
        }


        private void navigatorNewAnno_Click(object sender, RoutedEventArgs e)
        {
            if (Time.TotalDuration > 0)
            {
                AnnoTierNewWindow dialog = new AnnoTierNewWindow();
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();

                if (dialog.DialogResult == true)
                {
                    AnnoScheme.TYPE annoType = dialog.Result();

                    if (DatabaseLoaded)
                    {
                        databaseNewAnno(annoType);
                    }
                    else
                    {
                        if (annoType == AnnoScheme.TYPE.FREE)
                        {
                            AnnoScheme annoScheme = new AnnoScheme() { Type = AnnoScheme.TYPE.FREE, MinOrBackColor = Colors.LightBlue, MaxOrForeColor = Colors.Orange };
                            AnnoList annoList = new AnnoList() { Scheme = annoScheme };
                            addAnnoTier(annoList);
                        }
                        else if (annoType == AnnoScheme.TYPE.DISCRETE)
                        {
                            AnnoSchemeEditor ase = new AnnoSchemeEditor();
                            ase.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            ase.ShowDialog();

                            if (ase.DialogResult == true)
                            {
                                AnnoList al = ase.GetAnnoList();
                                al.FilePath = al.Name;
                                addAnnoTier(al);
                            }
                        }
                        else if (annoType == AnnoScheme.TYPE.CONTINUOUS)
                        {
                            double defaultSr = 25.0;

                            foreach (IMedia m in mediaList.Medias)
                            {
                                if (m.IsVideo())
                                {
                                    defaultSr = m.GetSampleRate();
                                    break;
                                }
                            }

                            AnnoTierNewContinuousSchemeWindow.Input input = new AnnoTierNewContinuousSchemeWindow.Input() { SampleRate = defaultSr, MinScore = 0.0, MaxScore = 1.0, MinColor = Colors.LightBlue, MaxColor = Colors.Orange };
                            AnnoTierNewContinuousSchemeWindow dialog2 = new AnnoTierNewContinuousSchemeWindow(input);
                            dialog2.ShowDialog();
                            if (dialog2.DialogResult == true)
                            {
                                AnnoScheme annoScheme = dialog2.Result;
                                AnnoList annoList = new AnnoList() { Scheme = annoScheme };
                                addAnnoTier(annoList);
                            }
                        }
                    }
                }
            }
            else
            {
                MessageTools.Warning("Nothing to annotate, load some data first.");
            }
        }

        private void navigatorClearSession_Click(object sender, RoutedEventArgs e)
        {
            clearSession();
        }

    }
}

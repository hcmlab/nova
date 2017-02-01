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
        #region CURSOR
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
            if (SignalTrackStatic.Selected != null && SignalTrackStatic.Selected.Signal != null)
            {
                Signal signal = SignalTrackStatic.Selected.Signal;
                control.signalValueLabel.Text = signal.Value(Time.TimeFromPixel(pos)).ToString();
                control.signalValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                control.signalValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString(); 
            }
        }

        private void moveSignalCursorToSecond(double seconds)
        {
            double pos = MainHandler.Time.PixelFromTime(seconds);
            signalCursor.X = pos;
            control.signalPositionLabel.Text = FileTools.FormatSeconds(Time.TimeFromPixel(pos));
        }

        #endregion CURSOR

        #region EVENTHANDLER


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
                        databaseAddNewAnnotation(annoType);
                    }
                    else
                    {
                        if (annoType == AnnoScheme.TYPE.FREE)
                        {
                            AnnoScheme annoScheme = new AnnoScheme() { Type = AnnoScheme.TYPE.FREE, MinOrBackColor = Colors.Transparent, MaxOrForeColor = Colors.Black };
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
                                al.Source.File.Path = al.Scheme.Name;
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

                            AnnoTierNewContinuousSchemeWindow.Input input = new AnnoTierNewContinuousSchemeWindow.Input() { SampleRate = defaultSr, MinScore = 0.0, MaxScore = 1.0, MinColor = Colors.LightBlue, MaxColor = Colors.Red };
                            AnnoTierNewContinuousSchemeWindow dialog2 = new AnnoTierNewContinuousSchemeWindow(input);
                            dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
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
            moveSignalCursorToSecond(0);
            if (is_playing)
            {
                Play();
            }
        }

        private void navigatorJumpEnd_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            moveSignalCursorToSecond(MainHandler.Time.TotalDuration);
        }

        private void navigatorPlay_Click(object sender, RoutedEventArgs e)
        {
            handlePlay();
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

        #endregion EVENTHANDLER
    }
}

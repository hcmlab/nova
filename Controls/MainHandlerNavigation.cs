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
            control.annoTierControl.MouseDown += annoTrackGrid_MouseDown;
            control.annoTierControl.MouseMove += annoTrackGrid_MouseMove;
            control.annoTierControl.MouseRightButtonUp += annoTrackGrid_MouseUp;

            control.mediaVideoControl.MouseDown += geometricOverlay_MouseDown;
            control.mediaVideoControl.MouseUp += geometricOverlay_MouseUp;
            control.mediaVideoControl.MouseMove += geometricOverlay_MouseMove;

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
                control.signalStatusValueLabel.Text = signal.Value(Time.TimeFromPixel(pos)).ToString();
                control.signalStatusValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                control.signalStatusValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString(); 
            }
        }

        private void moveSignalCursorToSecond(double seconds)
        {
            double pos = MainHandler.Time.PixelFromTime(seconds);
            signalCursor.X = pos;
            control.signalStatusPositionLabel.Text = FileTools.FormatSeconds(Time.TimeFromPixel(pos));
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
                AnnoTierNewSchemeWindow dialog = new AnnoTierNewSchemeWindow();
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
                        AnnoList annoList = null;

                        if (annoType == AnnoScheme.TYPE.FREE)
                        {
                            AnnoTierNewFreeSchemeWindow dialog2 = new AnnoTierNewFreeSchemeWindow();
                            dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            dialog2.ShowDialog();

                            if (dialog2.DialogResult == true)
                            {
                                AnnoScheme annoScheme = dialog2.Result;
                                annoList = new AnnoList() { Scheme = annoScheme };
                            }
                        }
                        else if (annoType == AnnoScheme.TYPE.DISCRETE)
                        {
                            AnnoTierNewDiscreteSchemeWindow dialog2 = new AnnoTierNewDiscreteSchemeWindow();
                            dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            dialog2.ShowDialog();

                            if (dialog2.DialogResult == true)
                            {
                                annoList = dialog2.GetAnnoList();
                            }
                        }
                        else if (annoType == AnnoScheme.TYPE.CONTINUOUS || annoType == AnnoScheme.TYPE.POINT || annoType == AnnoScheme.TYPE.POLYGON ||
                                  annoType == AnnoScheme.TYPE.GRPAH || annoType == AnnoScheme.TYPE.SEGMENTATION)
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

                            if (annoType == AnnoScheme.TYPE.CONTINUOUS)
                            {
                                AnnoTierNewContinuousSchemeWindow.Input input = new AnnoTierNewContinuousSchemeWindow.Input() { SampleRate = defaultSr, MinScore = 0.0, MaxScore = 1.0, MinColor = Defaults.Colors.GradientMin, MaxColor = Defaults.Colors.GradientMax };
                                AnnoTierNewContinuousSchemeWindow dialog2 = new AnnoTierNewContinuousSchemeWindow(input);
                                dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                dialog2.ShowDialog();
                                if (dialog2.DialogResult == true)
                                {
                                    AnnoScheme annoScheme = dialog2.Result;
                                    annoList = new AnnoList() { Scheme = annoScheme };
                                }
                            }
                            else
                            {


                                if (annoType == AnnoScheme.TYPE.POINT)
                                {
                                    AnnoTierNewPointSchemeWindow.Input input = new AnnoTierNewPointSchemeWindow.Input() { SampleRate = defaultSr, NumPoints = 1.0, Color = Colors.Green };
                                    AnnoTierNewPointSchemeWindow dialog2 = new AnnoTierNewPointSchemeWindow(input);
                                    dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    dialog2.ShowDialog();
                                    if (dialog2.DialogResult == true)
                                    {
                                        AnnoScheme annoScheme = dialog2.Result;
                                        annoList = new AnnoList() { Scheme = annoScheme };
                                    }
                                }
                                else if (annoType == AnnoScheme.TYPE.POLYGON)
                                {
                                    AnnoTierNewPolygonSchemeWindow.Input input = new AnnoTierNewPolygonSchemeWindow.Input() { SampleRate = defaultSr, NumNodes = 1.0, NodeColour = Colors.Green, LineColour = Colors.Red };
                                    AnnoTierNewPolygonSchemeWindow dialog2 = new AnnoTierNewPolygonSchemeWindow(input);
                                    dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    dialog2.ShowDialog();
                                    if (dialog2.DialogResult == true)
                                    {
                                        AnnoScheme annoScheme = dialog2.Result;
                                        annoList = new AnnoList() { Scheme = annoScheme };
                                    }
                                }
                                else if (annoType == AnnoScheme.TYPE.GRPAH)
                                {
                                    AnnoTierNewGraphSchemeWindow.Input input = new AnnoTierNewGraphSchemeWindow.Input() { SampleRate = defaultSr, NumNodes = 1.0, NodeColour = Colors.Green, LineColour = Colors.Red };
                                    AnnoTierNewGraphSchemeWindow dialog2 = new AnnoTierNewGraphSchemeWindow(input);
                                    dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    dialog2.ShowDialog();
                                    if (dialog2.DialogResult == true)
                                    {
                                        AnnoScheme annoScheme = dialog2.Result;
                                        annoList = new AnnoList() { Scheme = annoScheme };
                                    }
                                }
                                else if (annoType == AnnoScheme.TYPE.SEGMENTATION)
                                {
                                    AnnoTierNewSegmentationSchemeWindow.Input input = new AnnoTierNewSegmentationSchemeWindow.Input() { SampleRate = defaultSr, MinScore = 0.0, MaxScore = 1.0, MinColor = Colors.LightBlue, MaxColor = Colors.Red };
                                    AnnoTierNewSegmentationSchemeWindow dialog2 = new AnnoTierNewSegmentationSchemeWindow(input);
                                    dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                                    dialog2.ShowDialog();
                                    if (dialog2.DialogResult == true)
                                    {
                                        AnnoScheme annoScheme = dialog2.Result;
                                        annoList = new AnnoList() { Scheme = annoScheme };
                                    }
                                }
                            }
                        }
                        if (annoList != null)
                        {
                            annoList.Meta.Annotator = Properties.Settings.Default.Annotator;                          
                            addAnnoTier(annoList);
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

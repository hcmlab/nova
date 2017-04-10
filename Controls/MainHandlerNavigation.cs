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
            AdornerLayer cursorLayer = control.signalAndAnnoAdorner.AdornerLayer;
            signalCursor = new Cursor(control.signalAndAnnoGrid, Brushes.Red, 1.5);
            cursorLayer.Add(signalCursor);
            annoCursor = new Cursor(control.signalAndAnnoGrid, Brushes.Green, 1.5);
            cursorLayer.Add(annoCursor);

            signalCursor.MouseDown += annoTierControl_MouseDown;
            annoCursor.MouseDown += annoTierControl_MouseDown;
        }

        private void moveSignalCursor(double time)
        {
            signalCursor.X = Time.PixelFromTime(time);
            updatePositionLabels(time);
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

        private void annoContinuousMode_Changed(object sender, RoutedEventArgs e)
        {
         
            if (AnnoTier.Selected != null && AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            AnnoTier.Selected.ContinuousAnnoMode();
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

                        double defaultSr = 25.0;

                        foreach (IMedia m in mediaList)
                        {
                            if (m.GetMediaType() == MediaType.VIDEO)
                            {
                                defaultSr = m.GetSampleRate();
                                break;
                            }
                        }

                        if (annoType == AnnoScheme.TYPE.FREE)
                        {
                            AnnoTierNewFreeSchemeWindow dialog2 = new AnnoTierNewFreeSchemeWindow(annoLists.Count);
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
                        else if (annoType == AnnoScheme.TYPE.CONTINUOUS)
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
                        } else if (annoType == AnnoScheme.TYPE.POINT)
                        {
                            AnnoTierNewPointSchemeWindow.Input input = new AnnoTierNewPointSchemeWindow.Input() { SampleRate = defaultSr, NumPoints = 1, Color = Colors.Green };
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
                            AnnoTierNewPolygonSchemeWindow.Input input = new AnnoTierNewPolygonSchemeWindow.Input() { SampleRate = defaultSr, NumNodes = 1, NodeColour = Colors.Green, LineColour = Colors.Red };
                            AnnoTierNewPolygonSchemeWindow dialog2 = new AnnoTierNewPolygonSchemeWindow(input);
                            dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            dialog2.ShowDialog();
                            if (dialog2.DialogResult == true)
                            {
                                AnnoScheme annoScheme = dialog2.Result;
                                annoList = new AnnoList() { Scheme = annoScheme };
                            }
                        }
                        else if (annoType == AnnoScheme.TYPE.GRAPH)
                        {
                            AnnoTierNewGraphSchemeWindow.Input input = new AnnoTierNewGraphSchemeWindow.Input() { SampleRate = defaultSr, NumNodes = 1, NodeColour = Colors.Green, LineColour = Colors.Red };
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
                            int width = (int) mediaList.GetFirstVideo().GetOverlay().Width;
                            int height = (int)mediaList.GetFirstVideo().GetOverlay().Height;
                            AnnoTierNewSegmentationSchemeWindow.Input input = new AnnoTierNewSegmentationSchemeWindow.Input() { SampleRate = defaultSr, Width = width, Height = height};
                            AnnoTierNewSegmentationSchemeWindow dialog2 = new AnnoTierNewSegmentationSchemeWindow(input);
                            dialog2.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            dialog2.ShowDialog();
                            if (dialog2.DialogResult == true)
                            {
                                AnnoScheme annoScheme = dialog2.Result;
                                annoList = new AnnoList() { Scheme = annoScheme };
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
        
        private void navigatorPlay_Click(object sender, RoutedEventArgs e)
        {
            if (playIsPlaying)
            {
                Stop();
            }
            else
            {
                Play();
            }
        }     

        private void updateNavigator()
        {
            if (mediaList.Count > 0
                || signalTracks.Count > 0
                || annoTiers.Count > 0)
            {
                control.navigator.playButton.IsEnabled = true;                
            }
            else
            {
                control.navigator.playButton.IsEnabled = false;
                control.navigator.Statusbar.Content = "";
            }

            if (!playIsPlaying)
            {
                control.navigator.playButton.Content = ">";
            }
            else
            {
                control.navigator.playButton.Content = "II";
            }
        }

        public void updateTimeRange(double duration, SignalTrack track)
        {
            if (duration > Time.TotalDuration)
            {
                Time.TotalDuration = duration;
                control.timeLineControl.rangeSlider.Update();
            }
            else if(track != null)
            {
                track.TimeRangeChanged(Time);
            }
            if (signalTracks.Count == 1
                && duration > Properties.Settings.Default.DefaultZoomInSeconds
                && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
            }

        }

        private void navigatorFollowAnno_Unchecked(object sender, RoutedEventArgs e)
        {
            AnnoTierStatic.UnselectLabel();

            bool is_playing = IsPlaying();
            Stop();
            if (is_playing)
            {
                Play();
            }
        }

        private void navigatorJumpFront_Click(object sender, RoutedEventArgs e)
        {
            jumpToGeometric(0);
            bool is_playing = IsPlaying();
            Stop();
            moveSignalCursor(0);
            if (is_playing)
            {
                Play();
            }
            jumpToGeometric(0);

        }

        private void navigatorJumpEnd_Click(object sender, RoutedEventArgs e)
        {
            Stop();

            moveSignalCursor(Time.TotalDuration);
            int end = control.annoListControl.annoDataGrid.Items.Count - 1;
            jumpToGeometric(end);
        }

        #endregion EVENTHANDLER
    }
}

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
               if (control.annoContinuousMode.IsChecked == true)
                {
                    AnnoTierStatic.Selected.ContinuousAnnoMode(false);
                }
                else
                {
                    AnnoTierStatic.Selected.ContinuousAnnoMode(true);
                }
        }

        private void navigatorNewAnnoFromDatabase_Click(object sender, RoutedEventArgs e)
        {
            addNewAnnotationDatabase();
        }

        private void navigatorNewAnnoFromFile_Click(object sender, RoutedEventArgs e)
        {
            addNewAnnotationFile();
        }

        private void navigatorClearSession_Click(object sender, RoutedEventArgs e)
        {
            clearWorkspace();
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
            }            

            if (!playIsPlaying)
            {
                control.navigator.playButton.Content = ">";
            }
            else
            {
                control.navigator.playButton.Content = "II";
            }

            bool isConnected = DatabaseHandler.IsConnected;
            bool isConnectedAndHasSession = DatabaseHandler.IsConnected && DatabaseHandler.IsSession;

            control.navigator.newAnnoFromDatabaseButton.IsEnabled = isConnectedAndHasSession;
            control.navigator.statusBarSessionInfo.Foreground = isConnectedAndHasSession ? Brushes.Green : Brushes.DarkGray;
            control.navigator.statusBarSessionInfo.Content = DatabaseHandler.SessionInfo;
            control.navigator.statusBarServer.Content = DatabaseHandler.ServerInfo;
            control.navigator.statusBarServer.Foreground = isConnected ? Brushes.Green : Brushes.DarkGray;
            control.navigator.statusBarDatabase.Content = DatabaseHandler.DatabaseInfo;
            control.navigator.statusBarDatabase.Foreground = isConnected ? Brushes.Green : Brushes.DarkGray;
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

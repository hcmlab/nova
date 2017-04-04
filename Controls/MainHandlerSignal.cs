using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    public partial class MainHandler
    {
        private void addSignalTrack(Signal signal, Color signalColor, Color backgroundColor)
        {
            SignalTrack track = new SignalTrack(signal);

            control.signalTrackControl.Add(track, signalColor, backgroundColor);
            control.timeLineControl.rangeSlider.OnTimeRangeChanged += track.TimeRangeChanged;

            signals.Add(signal);
            signalTracks.Add(track);

            double duration = signal.number / signal.rate;
            UpdateTimeRange(duration, track);

            SignalTrackStatic.Select(track);
        }

        private void signalTrackCloseButton_Click(object sender, RoutedEventArgs e)
        {
            removeSignalTrack();          
        }

        private void removeSignalTrack()
        {
            SignalTrack track = SignalTrackStatic.Selected;

            if (track != null)
            {
                control.signalTrackControl.Remove(track);

                SignalTrackStatic.Unselect();
                track.Children.Clear();
                signalTracks.Remove(track);

                if (signalTracks.Count > 0)
                {
                    SignalTrackStatic.Select(signalTracks[0]);
                }
                else
                {
                    clearSignalTrack();
                }
            }
        }

        public void clearSignalTrack()
        {
            control.signalSettingsButton.Visibility = Visibility.Hidden;
            control.signalStatusFileNameLabel.Text = "";
            control.signalStatusFileNameLabel.ToolTip = "";
            control.signalStatusBytesLabel.Text = "";
            control.signalStatusDimLabel.Text = "";
            control.signalStatusDimComboBox.Items.Clear();
            control.signalStatusSrLabel.Text = "";
            control.signalStatusTypeLabel.Text = "";
            control.signalStatusValueLabel.Text = "";
            control.signalStatusValueMinLabel.Text = "";
            control.signalStatusValueMaxLabel.Text = "";
            control.signalVolumeControl.Visibility = Visibility.Collapsed; 
            control.signalPositionLabel.Text = "00:00:00.00";
            control.signalCloseButton.Visibility = Visibility.Hidden;
        }

        private void changeSignalTrack(SignalTrack track)
        {
            Signal signal = track.Signal;
            if (signal != null)
            {
                control.signalSettingsButton.Visibility = Visibility.Visible;
                control.signalStatusFileNameLabel.Text = signal.FileName;
                control.signalStatusFileNameLabel.ToolTip = signal.FilePath;
                control.signalStatusBytesLabel.Text = signal.bytes + " bytes";
                control.signalStatusDimComboBox.Items.Clear();
                for (int i = 0; i < signal.dim; i++)
                {
                    control.signalStatusDimComboBox.Items.Add(i);
                }
                control.signalStatusDimComboBox.SelectedIndex = signal.ShowDim;
                control.signalStatusDimLabel.Text = signal.dim.ToString();
                control.signalStatusSrLabel.Text = signal.rate + " Hz";
                control.signalStatusTypeLabel.Text = Signal.TypeName[(int)signal.type];
                control.signalStatusValueLabel.Text = signal.Value(timeline.SelectionStart).ToString();
                control.signalStatusValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                control.signalStatusValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString();
                if (signal.IsAudio && signal.Media != null && signal.Media.HasAudio())
                {
                    control.signalVolumeControl.volumeSlider.Value = signal.Media.GetVolume();                    
                    control.signalVolumeControl.Visibility = Visibility.Visible;
                }
                else
                {
                    control.signalVolumeControl.Visibility = Visibility.Collapsed;
                }
                control.signalCloseButton.Visibility = Visibility.Visible;
            }
        }

        private void onSignalTrackChange(SignalTrack track, EventArgs e)
        {
            changeSignalTrack(track);
        }

        private void signalDimComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SignalTrackStatic.Selected != null
                && control.signalStatusDimComboBox.SelectedIndex != -1 
                && control.signalStatusDimComboBox.SelectedIndex != SignalTrackStatic.Selected.Signal.ShowDim)
            {
                SignalTrackStatic.Selected.Signal.ShowDim = control.signalStatusDimComboBox.SelectedIndex;
                SignalTrackStatic.Selected.InvalidateVisual();
            }
        }

        private void signalSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrackStatic.Selected != null)
            {
                SignalTrackSettingsWindow window = new SignalTrackSettingsWindow();
                window.DataContext = SignalTrackStatic.Selected;
                window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;                
                window.ShowDialog();
            }
        }       

        private void signalVolumeControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SignalTrackStatic.Selected != null && SignalTrackStatic.Selected.Signal.Media != null && MediaBoxStatic.Selected.Media.HasAudio())
            {
                SignalTrackStatic.Selected.Signal.Media.SetVolume(control.signalVolumeControl.volumeSlider.Value);
            }
        }

        #region EVENTHANDLER

        private void signalTrackGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                 if (AnnoTierStatic.Label != null && Mouse.DirectlyOver.GetType() != AnnoTierStatic.Label.GetType() || AnnoTierStatic.Label == null)
                {
                     AnnoTierStatic.UnselectLabel();
                    bool is_playing = IsPlaying();
                    if (is_playing)
                    {
                        Stop();
                    }

                    double pos = e.GetPosition(control.signalAndAnnoGrid).X;
                    signalCursor.X = pos;
                    Time.CurrentPlayPosition = Time.TimeFromPixel(signalCursor.X);
                    Time.CurrentPlayPositionPrecise = Time.TimeFromPixel(signalCursor.X);
                    mediaList.Move(Time.TimeFromPixel(pos));                    
                    updatePositionLabels(Time.TimeFromPixel(pos));

                    if (AnnoTierStatic.Selected != null)
                    {
                        if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.GRAPH ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                        {
                            if (control.annoListControl.annoDataGrid.Items.Count > 0)
                            {
                                AnnoListItem ali = (AnnoListItem)control.annoListControl.annoDataGrid.Items[0];
                                double deltaTime = ali.Duration;
                                double roughtPosition = Time.CurrentPlayPosition / deltaTime;
                                jumpToGeometric((int)roughtPosition);
                            }
                        }
                    }

                    if (is_playing)
                    {
                        Play();
                    }
                }
            }
        }

        #endregion EVENTHANDLER


    }
}

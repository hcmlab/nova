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
        public void clearSignalInfo()
        {
            control.signalStatusSettingsButton.IsEnabled = false;
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
            control.signalStatusPositionLabel.Text = "00:00:00.00";
            control.signalStatusCloseButton.IsEnabled = false;
        }


        private void addSignal(Signal signal, Color signalColor, Color backgroundColor)
        {
            SignalTrack track = new SignalTrack(signal);

            control.signalTrackControl.Add(track, signalColor, backgroundColor);
            control.timeLineControl.rangeSlider.OnTimeRangeChanged += track.TimeRangeChanged;

            signals.Add(signal);
            signalTracks.Add(track);

            double duration = signal.number / signal.rate;
            if (duration > Time.TotalDuration)
            {
                Time.TotalDuration = duration;
                control.timeLineControl.rangeSlider.Update();
            }
            else
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
                    clearSignalInfo();
                }
            }
        }

        private void changeSignalTrackHandler(SignalTrack track, EventArgs e)
        {
            Signal signal = track.Signal;
            if (signal != null)
            {
                control.signalStatusSettingsButton.IsEnabled = true;
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
                control.signalStatusCloseButton.IsEnabled = true;
            }
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

        #region EVENTHANDLER

        private void signalTrackGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (AnnoTierStatic.Label != null && Mouse.DirectlyOver.GetType() != AnnoTierStatic.Label.GetType() || AnnoTierStatic.Label == null)
                {
                    AnnoTier.UnselectLabel();
                    bool is_playing = IsPlaying();
                    if (is_playing)
                    {
                        Stop();
                    }

                    double pos = e.GetPosition(control.trackGrid).X;
                    signalCursor.X = pos;
                    Time.CurrentPlayPosition = MainHandler.Time.TimeFromPixel(signalCursor.X);
                    Time.CurrentPlayPositionPrecise = MainHandler.Time.TimeFromPixel(signalCursor.X);
                    mediaList.move(MainHandler.Time.TimeFromPixel(pos));
                    double time = Time.TimeFromPixel(pos);
                    control.signalStatusPositionLabel.Text = FileTools.FormatSeconds(time);


                    if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.GRPAH ||
                        AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                    {
                        while (control.annoListControl.annoDataGrid.SelectedItems.Count > 0)
                        {
                          control.annoListControl.annoDataGrid.SelectedItems.RemoveAt(0);
                        }
                        int i = 0;
                        foreach (AnnoListItem ali in control.annoListControl.annoDataGrid.Items)
                        {
                            if (ali.Start <= time)
                            {
                                ++i;
                            }
                            else
                            {
                                break;
                            }
                        }
                        control.annoListControl.annoDataGrid.SelectedItems.Add(control.annoListControl.annoDataGrid.Items[i]);
                        control.geometricListControl.geometricDataGrid.Items.Refresh();
                        control.geometricListControl.geometricDataGrid.ScrollIntoView(control.geometricListControl.geometricDataGrid.Items[0]);
                        control.annoListControl.annoDataGrid.Items.Refresh();
                        control.annoListControl.annoDataGrid.ScrollIntoView(control.annoListControl.annoDataGrid.Items[i]);

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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
            updateTimeRange(duration, track);

            SignalTrackStatic.Select(track);
            updateNavigator();
        }

        private void signalTrackCloseButton_Click(object sender, RoutedEventArgs e)
        {
            removeSignalTrack();          
        }

        private void removeSignalTrack()
        {
            removeSignalTrack(SignalTrackStatic.Selected);
        }

        private void removeSignalTrack(SignalTrack track)
        {          
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
                    updateNavigator();
                }
            }
        }

        public void clearSignalInfo()
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
            control.signalStatsButton.Visibility = Visibility.Hidden;
            control.signalCloseButton.Visibility = Visibility.Hidden;
            this.control.signalbar.Height = new GridLength(30);
            this.control.signalstatusbar.Visibility = Visibility.Hidden;
        }

        private void updateSignalTrack(SignalTrack track)
        {
            if (track == null)
            {
                return;
            }

            Signal signal = track.Signal;
            if (signal != null)
            {
                control.signalSettingsButton.Visibility = Visibility.Visible;
                control.signalStatsButton.Visibility = Visibility.Visible;
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
                control.signalCloseButton.Visibility = playIsPlaying ? Visibility.Hidden : Visibility.Visible;
            }
        }

        private void onSignalTrackChange(SignalTrack track, EventArgs e)
        {
            updateSignalTrack(track);
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

        private void signalStatsButton_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrackStatic.Selected != null)
            {
                SignalStatsWindow ssw = new SignalStatsWindow(SignalTrackStatic.Selected.Signal, SignalTrackStatic.Selected.Signal.ShowDim);
                Time.OnTimelineChanged += ssw.timeRangeChanged;
                ssw.Topmost = true;
                ssw.WindowStartupLocation = WindowStartupLocation.Manual;
                ssw.Show();

                if (ssw.DialogResult == false)
                {
                    Time.OnTimelineChanged -= ssw.timeRangeChanged;
                }

            }
        }

        private void annoStatsButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTier.Selected != null && AnnoTier.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {


                Signal temp = new Signal();

                AnnoTier annoTier = AnnoTierStatic.Selected;
             
                    double sr = 1 / annoTier.AnnoList[0].Duration;
                    double from = 0.0;
                    double to = annoTier.AnnoList[annoTier.AnnoList.Count - 1].Stop;
                    int num = annoTier.AnnoList.Count;
                    string ftype = "ASCII";
                    string type = "FLOAT";
                    int by = sizeof(float);
                    int dim = 1;
                    int ms = Environment.TickCount;

                   var filename = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), annoTier.AnnoList.Scheme.Name + ".stream");


                try
                {

               

                    StreamWriter swheader = new StreamWriter(filename, false, System.Text.Encoding.Default);
                    swheader.WriteLine("<?xml version=\"1.0\" ?>");
                    swheader.WriteLine("<stream ssi-v=\"2\">");
                    swheader.WriteLine("\t<info ftype=\"" + ftype + "\" sr=\"" + sr.ToString("0.000000", CultureInfo.InvariantCulture) + "\" dim=\"" + dim.ToString() + "\" byte=\"" + by.ToString() + "\" type=\"" + type + "\" />");
                    swheader.WriteLine("\t<time ms=\"" + ms + "\" local=\"" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "\" system=\"" + DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + "\"/>");
                    swheader.WriteLine("\t<chunk from=\"" + from.ToString("0.000000", CultureInfo.InvariantCulture) + "\" to=\"" + to.ToString("0.000000", CultureInfo.InvariantCulture) + "\" byte=\"" + "0" + "\" num=\"" + num + "\"/>");

                    swheader.WriteLine("</stream>");
                    swheader.Close();

                    StreamWriter swdata = new StreamWriter(filename + "~", false, System.Text.Encoding.Default);
                    foreach (AnnoListItem i in annoTier.AnnoList)
                    {
                        swdata.WriteLine(i.Score);
                    }
                    swdata.Close();

                    temp = Signal.LoadStreamFile(filename);
                    try
                    {
                        File.Delete(filename);
                        File.Delete(filename + "~");
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }

                    //temp.

                    SignalStatsWindow ssw = new SignalStatsWindow(temp, 0, true);
                Time.OnTimelineChanged += ssw.timeRangeChanged;
                ssw.Topmost = true;
                ssw.WindowStartupLocation = WindowStartupLocation.Manual;
                ssw.Show();

                    if (ssw.DialogResult == false)
                    {
                        Time.OnTimelineChanged -= ssw.timeRangeChanged;
                    }

                }

                catch(Exception x)
                {
                    MessageBox.Show(x.ToString());
                }

            }
        }

        




        private void signalVolumeControl_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (SignalTrackStatic.Selected != null && (SignalTrackStatic.Selected.Signal.Media != null || MediaBoxStatic.Selected != null && MediaBoxStatic.Selected.Media.HasAudio()))
            {
                SignalTrackStatic.Selected.Signal.Media.SetVolume(control.signalVolumeControl.volumeSlider.Value);
            }
        }

        #region EVENTHANDLER


        private void signalAndAnnoGrid_Move(double mouseX)
        {
            if (AnnoTierStatic.Label != null && Mouse.DirectlyOver.GetType() != AnnoTierStatic.Label.GetType() || AnnoTierStatic.Label == null)
            {
                AnnoTierStatic.UnselectLabel();

                signalCursor.X = mouseX;
                double time = Time.TimeFromPixel(mouseX);

                Move(time);

                if (AnnoTierStatic.Selected != null)
                {
                    if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.GRAPH ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                    {
                        if (control.annoListControl.annoDataGrid.Items.Count > 0)
                        {
                            int position = (int)(Time.CurrentPlayPosition * AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate);
                            if (position < control.annoListControl.annoDataGrid.Items.Count)
                            {
                                jumpToGeometric(position);
                            }
                        }
                    }
                }

               
            }

        }


        private void signalAndAnnoGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !Keyboard.IsKeyDown(Key.LeftShift))
            {
               
                signalAndAnnoGrid_Move(e.GetPosition(control.signalAndAnnoGrid).X);
            }
        }

        #endregion EVENTHANDLER


    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ssi
{
    public partial class MainHandler
    {
        private void addSignal(Signal signal, string foreground, string background)
        {
            ISignalTrack track = control.signalTrackControl.AddSignalTrack(signal, foreground, background);
            control.timeTrackControl.rangeSlider.OnTimeRangeChanged += track.TimeRangeChanged;

            this.signals.Add(signal);
            this.signalTracks.Add(track);

            double duration = signal.number / signal.rate;
            if (duration > MainHandler.Time.TotalDuration)
            {
                MainHandler.Time.TotalDuration = duration;
                control.timeTrackControl.rangeSlider.Update();
            }
            else
            {
                track.TimeRangeChanged(MainHandler.Time);
            }
            if (this.signalTracks.Count == 1 && duration > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
        }

        private void removeSignal(object sender, SignalRemoveEventArgs e)
        {
            signalTracks.Remove(e.SignalTrack);
        }
     
        private void changeSignalTrackHandler(ISignalTrack track, EventArgs e)
        {
            Signal signal = track.getSignal();
            if (signal != null)
            {
                control.signalNameLabel.Text = signal.FileName;
                control.signalNameLabel.ToolTip = signal.FilePath;
                control.signalBytesLabel.Text = signal.bytes + " bytes";
                control.signalDimLabel.Text = signal.dim.ToString();
                control.signalSrLabel.Text = signal.rate + " Hz";
                control.signalTypeLabel.Text = Signal.TypeName[(int)signal.type];
                control.signalValueLabel.Text = signal.Value(timeline.SelectionStart).ToString();
                control.signalValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                control.signalValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString();
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
                    control.signalPositionLabel.Text = FileTools.FormatSeconds(time);

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

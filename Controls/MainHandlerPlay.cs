using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ssi
{
    partial class MainHandler
    {
        private bool IsPlaying()
        {
            return playIsPlaying;
        }

        private void PlayTimer_Tick(object sender, EventArgs e)
        {
            int elapsed = (int) ((Environment.TickCount - playLastTick) * PlayerSpeed);
            Time.CurrentPlayPosition += elapsed / 1000.0;

            playLastTick = Environment.TickCount;

            if (Time.CurrentPlayPosition >= timeline.TotalDuration)
            {
                Stop();
            }
            else
            {
                if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.autoScrollCheckBox.IsChecked == true)
                {
                    double factor = (((Time.CurrentPlayPosition - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                    control.timeLineControl.rangeSlider.followmedia = true;
                    control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
                }
                else if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.autoScrollCheckBox.IsChecked == false)
                {
                    control.timeLineControl.rangeSlider.followmedia = false;
                    if (AnnoTierStatic.Label != null)
                    {
                        AnnoTierStatic.Label.select(true);
                    }
                    Stop();
                }
            }

            double samplerate = MainHandler.getMaxVideoSampleRate();
            double offset = 1.0f / samplerate;

            foreach (IMedia media in mediaList)
            {
                if (media.GetMediaType() != MediaType.AUDIO
                    && media.GetMediaType() != MediaType.VIDEO)
                {
                    media.Move(Time.CurrentPlayPosition);
                }
            }

            updatePositionLabels(Time.CurrentPlayPosition);
            signalCursor.X = Time.PixelFromTime(Time.CurrentPlayPosition + (offset*2.5));

            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsGeometric)
            {
                control.annoListControl.annoDataGrid.SelectedItems.Clear();
                int position = (int)((Time.CurrentPlayPosition + 0.04) * AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate);
                if (position < control.annoListControl.annoDataGrid.Items.Count)
                {
                    if(AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON || AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                    {
                        AnnoListItem annoListItem = (AnnoListItem)control.annoListControl.annoDataGrid.Items[position];
                        if (annoListItem.PolygonList.Polygons.Count > 0)
                        {
                            polygonDrawUnit.polygonOverlayUpdate(annoListItem);
                        }
                        else
                        {
                            polygonDrawUnit.clearOverlay();
                        }
                    }
                    else
                    {
                        AnnoListItem ali = (AnnoListItem)control.annoListControl.annoDataGrid.Items[position];
                        if (ali.Points.Count > 0)
                        {
                            geometricOverlayUpdate(ali, AnnoScheme.TYPE.POINT);
                        }
                    }
                }
            }
        }

        private void Play()
        {

            if (IsPlaying())
            {
                return;
            }

            playSampleRate = getMaxVideoSampleRate();

            playTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / playSampleRate);
            playLastTick = Environment.TickCount;

           
            {
                playTimer.Start();
                mediaList.Play();

                playIsPlaying = true;
                updateControl();

            }
        }

        private void Move(double time)
        {
            bool is_playing = IsPlaying();
            if (is_playing)
            {
                Stop();
            }
            
            Time.CurrentPlayPosition = time;
            mediaList.Move(time);
            updatePositionLabels(time);

            if (is_playing)
            {
                Play();
            }
        }

        private void Stop()
        {
            if (!IsPlaying())
            {
                return;
            }

            playTimer.Stop();
            mediaList.Stop();

            playIsPlaying = false;

            updateControl();
        }
    }
}

﻿using System;
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
            int elapsed = (Environment.TickCount - playLastTick);
            Time.CurrentPlayPosition += elapsed / 1000.0;
            playLastTick = Environment.TickCount;

            if (Time.CurrentPlayPosition > timeline.TotalDuration - 0.5)
            {
                Stop();
            }
            else
            {
                if (Time.CurrentPlayPosition >= Time.SelectionStop && control.navigator.followplaybox.IsChecked == true)
                {
                    double factor = (((Time.CurrentPlayPosition - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                    control.timeLineControl.rangeSlider.followmedia = true;
                    control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
                }
                else if (control.navigator.followplaybox.IsChecked == false)
                {
                    control.timeLineControl.rangeSlider.followmedia = false;
                    if (AnnoTierStatic.Label != null)
                    {
                        AnnoTierStatic.Label.select(true);
                    }
                }
            }

            foreach (IMedia media in mediaList)
            {
                if (media.GetMediaType() != MediaType.AUDIO
                    && media.GetMediaType() != MediaType.VIDEO)
                {
                    media.Move(Time.CurrentPlayPosition);
                }
            }
            updatePositionLabels(Time.CurrentPlayPosition);
            signalCursor.X = Time.PixelFromTime(Time.CurrentPlayPosition);

            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.IsGeometric)
            {
                control.annoListControl.annoDataGrid.SelectedItems.Clear();
                int position = (int)(Time.CurrentPlayPosition * AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate);
                if (position < control.annoListControl.annoDataGrid.Items.Count)
                {
                    AnnoListItem ali = (AnnoListItem)control.annoListControl.annoDataGrid.Items[position];
                    if (ali.Points.Count > 0)
                    {
                        geometricOverlayUpdate(position);
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
            playTimer.Start();
            mediaList.Play();

            playIsPlaying = true;

            updateControl();
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

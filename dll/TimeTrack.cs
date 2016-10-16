using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    public partial class TimeTrack : Canvas, ITrack
    {
        public const uint TICKGAP = 100;

        private bool view_selection = false;
        private double seconds_from = 0;
        private double seconds_to = 0;
        private uint n_ticks = 0;
        private List<TimeTrackSegment> segments = new List<TimeTrackSegment>();
        private int unitCount = 0;

        public TimeTrack()
        {
            this.MouseDown += new MouseButtonEventHandler(OnMouseDown);
        }

        public void setViewSelection(bool selection)
        {
            view_selection = selection;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            TimeTrackSegment.Unit[] values = (TimeTrackSegment.Unit[])Enum.GetValues(typeof(TimeTrackSegment.Unit));
            TimeTrackSegment.Unit unit = values[++unitCount % values.Length];
            foreach (TimeTrackSegment segment in segments)
            {
                segment.setUnit(unit); //Little optical workaround to avoid the first and last label to be shown
                if (segments[0] == segment) segment.Text = "";
                if (segments[segments.Count - 1] == segment) segment.Text = "";
            }
        }

        public double SecondsFrom
        {
            get { return seconds_from; }
        }

        public double SecondsTo
        {
            get { return seconds_to; }
        }

        public void timeRangeChanged(ViewTime time)
        {
            this.seconds_from = view_selection ? time.SelectionStart : 0.0;
            this.seconds_to = view_selection ? time.SelectionStop : time.TotalDuration;
            this.Width = time.SelectionInPixel;

            n_ticks = (uint)(this.Width / TICKGAP + 0.5);
            for (int i = segments.Count; i < n_ticks; i++)
            {
                segments.Add(new TimeTrackSegment(this, view_selection));
            }

            double pos = 0;
            for (int i = 0; i < n_ticks; i++)
            {
                segments[i].setPos(pos, n_ticks);

                pos += 1.0 / n_ticks;
            }
        }
    }
}
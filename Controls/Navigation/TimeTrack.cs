using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    public partial class TimeTrack : Canvas, ITrack
    {
        public const int MIN_SEGMENT_GAP_IN_PIXELS = 100;

        private double segmentGapRelative;
        private bool isSelection = false;
        private double secondsFrom = 0;
        private double secondsTo = 0;
        private int nSegments = 0;
        private List<TimeTrackSegment> segments = new List<TimeTrackSegment>();
        private int unitCount = 0;

        public TimeTrack()
        {
            MouseDown += new MouseButtonEventHandler(OnMouseDown);
        }

        public void IsSelection(bool flag)
        {
            isSelection = flag;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            TimeTrackSegment.Unit[] values = (TimeTrackSegment.Unit[])Enum.GetValues(typeof(TimeTrackSegment.Unit));
            TimeTrackSegment.Unit unit = values[++unitCount % values.Length];
            foreach (TimeTrackSegment segment in segments)
            {
                segment.setUnit(unit);
            }
        }

        public double SecondsFrom
        {
            get { return secondsFrom; }
        }

        public double SecondsTo
        {
            get { return secondsTo; }
        }

        public void TimeRangeChanged(Timeline time)
        {
            secondsFrom = isSelection ? time.SelectionStart : 0.0;
            secondsTo = isSelection ? time.SelectionStop : time.TotalDuration;
            Width = time.SelectionInPixel;

            nSegments = (int) Math.Floor(Width / MIN_SEGMENT_GAP_IN_PIXELS);
            segmentGapRelative = 1.0 / nSegments;

            int nAvailable = segments.Count;
            for (int i = nAvailable; i < nSegments; i++)
            {
                TimeTrackSegment segment = new TimeTrackSegment(this, isSelection);
                segments.Add(segment);
            }

            double pos = 0;
            for (int i = 0; i < nSegments; i++)
            {
                segments[i].SetPosition(pos, nSegments);
                segments[i].SetVisibility(System.Windows.Visibility.Visible);
                pos += segmentGapRelative;
            }
            for (int i = nSegments; i < nAvailable; i++)
            {
                segments[i].SetVisibility(System.Windows.Visibility.Hidden);
            }
        }
    }
}
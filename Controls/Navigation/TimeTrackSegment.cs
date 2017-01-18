using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ssi
{
    public partial class TimeTrackSegment : TextBlock
    {
        public enum Unit
        {
            SECONDS = 0,
            CLOCK = 1
        }

        private TimeTrack track = null;

        private bool isSelection = false;
        private List<Line> markers = new List<Line>();
        private double time = 0;
        private int markersPerSegment = 10;
        private Unit unit = TimeTrackSegment.Unit.CLOCK;

        public void setUnit(Unit unit)
        {
            this.unit = unit;
            switch (unit)
            {
                case Unit.CLOCK:
                    this.Text = FileTools.FormatSeconds(time);
                    break;

                case Unit.SECONDS:
                    this.Text = time.ToString("F5");
                    break;
            }
        }

        public void SetVisibility(System.Windows.Visibility visibility)
        {
            for (int i = 0; i < markers.Count; i++)
            {
                markers[i].Visibility = visibility;
            }
            Visibility = visibility;
        }

        public TimeTrackSegment(TimeTrack track, bool isSelection)
        {
            this.track = track;
            this.isSelection = isSelection;
            track.Children.Add(this);

            if (isSelection)
            {
                for (int i = 0; i < markersPerSegment; i++)
                {
                    Line l = new Line();
                    markers.Add(l);
                    track.Children.Add(l);
                }
            }
        }

        private void setMarkers(double pos, double labelwidth)
        {
            for (int i = 0; i < markersPerSegment; i++)
            {
                markers[i].X1 = pos * track.Width + (i * labelwidth / markersPerSegment);
                markers[i].X2 = markers[i].X1;
                markers[i].Y1 = i == 0 ? ActualHeight : ActualHeight / 6;
                markers[i].Y2 = 0;
                markers[i].Stroke = i == 0 ? new SolidColorBrush(Colors.Black) : new SolidColorBrush(Colors.DarkGray);
                markers[i].StrokeThickness = 1;                
            }
        }

        public void SetPosition(double pos, int nSegments)
        {
            double length = track.Width / nSegments;

            Canvas.SetLeft(this, pos * track.Width + 3);
            time = track.SecondsFrom + (track.SecondsTo - track.SecondsFrom) * pos;
            setUnit(unit);

            if (isSelection)
            {
                setMarkers(pos, length);
            }
        }
    }
}
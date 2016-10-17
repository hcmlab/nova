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

        private bool view_selection = false;
        private List<Line> markers = new List<Line>();
        private double time = 0;
        private double drawingoffset = 5;
        private int markerspersegment = 10;
        private Unit unit = TimeTrackSegment.Unit.CLOCK;

        public void setUnit(Unit unit)
        {
            this.unit = unit;
            switch (unit)
            {
                case Unit.CLOCK:
                    this.Text = ViewTools.FormatSeconds(time);
                    break;

                case Unit.SECONDS:
                    this.Text = time.ToString("F5");
                    break;
            }
        }

        public TimeTrackSegment(TimeTrack track, bool view_selection)
        {
            this.track = track;
            this.view_selection = view_selection;
            track.Children.Add(this);

            if (view_selection)
            {
                for (int i = 0; i < markerspersegment; i++)
                {
                    Line l = new Line();
                    markers.Add(l);
                    track.Children.Add(l);
                }
            }
        }

        private void setMarkers(double pos, double labelwidth)
        {
            for (int i = 0; i < markerspersegment; i++)
            {
                markers[i].X1 = pos * track.Width + (i * labelwidth / markerspersegment) - drawingoffset;
                markers[i].X2 = markers[i].X1;
                markers[i].Y1 = this.ActualHeight / 3;
                markers[i].Y2 = 0;
                markers[i].Stroke = new SolidColorBrush(Colors.DarkGray);
                markers[i].StrokeThickness = 1;

                //The Middle marker is a bit longer to be better seen
                if (i == 0) markers[i].Y1 = markers[i].Y1 + 10;
            }
        }

        public void setPos(double pos, uint n_ticks)
        {
            double length = track.Width / n_ticks;

            Canvas.SetLeft(this, pos * track.Width - ActualWidth / 2 - drawingoffset);
            time = track.SecondsFrom + (track.SecondsTo - track.SecondsFrom) * pos;
            setUnit(unit);

            if (pos == 0) this.Text = "";
            if (track.Width - pos * track.Width < 100)
                this.Text = "";

            if (view_selection)
            {
                setMarkers(pos, length);
            }
        }
    }
}
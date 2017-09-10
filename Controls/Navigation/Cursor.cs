using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ssi
{
    public delegate void OnCursorChangeEvent(double pos);

    public class Cursor : Adorner
    {
        private double x_pos = 0;
        private double minVisibleTime = 0;
        private double maxVisibleTime = 0;
        private double time = 0;
        private bool signal_loaded = false;

        public OnCursorChangeEvent OnCursorChange;
         

        public double X
        {
            get { return x_pos; }
            set
            {
                x_pos = value;
                double currentTime = MainHandler.Time.TimeFromPixel(x_pos);
                if (currentTime > minVisibleTime && currentTime < maxVisibleTime) 
                {
                    time = MainHandler.Time.TimeFromPixel(x_pos);
                }
                this.InvalidateVisual();
                if (OnCursorChange != null)
                {
                    OnCursorChange(x_pos);
                }
            }
        }


        public bool signalLoaded
        {
            get { return signal_loaded; }
            set
            {
                signal_loaded = value;
            }
        }

        public void setCurserToTime(Timeline currentTimeLine)
        {
            minVisibleTime = currentTimeLine.SelectionStart;
            maxVisibleTime = currentTimeLine.SelectionStop;
            X = MainHandler.Time.PixelFromTime(time);
        }

        private Pen pen = null;

        public Cursor(FrameworkElement adornedElement, SolidColorBrush color, double size)
            : base(adornedElement)
        {
            pen = new Pen(color, size);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int toppixel = 20;
            if (signal_loaded) toppixel = 40;
            drawingContext.DrawLine(pen, new Point(x_pos, toppixel), new Point(x_pos, ((FrameworkElement)this.AdornedElement).ActualHeight - 40));
        }
    }
}
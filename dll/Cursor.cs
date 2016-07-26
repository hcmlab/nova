using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace ssi
{
    public delegate void OnCursorChangeEvent(double pos);

    public class Cursor : Adorner
    {
        private double x_pos = 0;

        public OnCursorChangeEvent OnCursorChange;

        public double X
        {
            get { return x_pos; }
            set
            {
                x_pos = value;
                this.InvalidateVisual();
                if (OnCursorChange != null)
                {
                    OnCursorChange(x_pos);
                }
            }
        }

        private Pen pen = null;

        public Cursor(FrameworkElement adornedElement, SolidColorBrush color, double size)
            : base(adornedElement)
        {
            pen = new Pen(color, size);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(pen, new Point(x_pos, 0), new Point(x_pos, ((FrameworkElement)this.AdornedElement).ActualHeight));
        }
    }
}
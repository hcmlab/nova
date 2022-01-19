using ssi.Types.Polygon;
using System;
using System.Windows;
using System.Linq;


namespace ssi.Tools.Polygon_Helper
{
    class MousePositionInformation
    {
        private MainControl control;
        private PolygonInformations polygonInformations;

        public MousePositionInformation(MainControl control, PolygonInformations polygonInformations)
        {
            this.control = control;
            this.polygonInformations = polygonInformations;
        }

        public bool isMouseAbovePoint(double mouseX, double mouseY, double pointX, double pointY)
        {
            const int POINT_THICKNESS = 5;

            if ((mouseX < (pointX + POINT_THICKNESS) && mouseX > (pointX - POINT_THICKNESS) && mouseY < (pointY + POINT_THICKNESS) && mouseY > (pointY - POINT_THICKNESS)))
            {
                return true;
            }

            return false;
        }

        public bool mouseIsOnLine()
        {
            const double EPSILON_PER_SIDE = 2;
            Point currentPoint = polygonInformations.LastKnownPoint;

            if (control.polygonListControl.polygonDataGrid.SelectedItems.Count == 0)
                return false;
            
            foreach (PolygonLabel polygonLabel in control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>())
            {
                Point p1 = new Point();
                Point p2 = new Point();

                for (int i = 0; i < polygonLabel.Polygon.Count; i++)
                {
                    if (i + 1 < polygonLabel.Polygon.Count)
                    {
                        p1 = new Point(polygonLabel.Polygon[i].X, polygonLabel.Polygon[i].Y);
                        p2 = new Point(polygonLabel.Polygon[i + 1].X, polygonLabel.Polygon[i + 1].Y);
                    }
                    else
                    {
                        p1 = new Point(polygonLabel.Polygon[i].X, polygonLabel.Polygon[i].Y);
                        p2 = new Point(polygonLabel.Polygon[0].X, polygonLabel.Polygon[0].Y);
                    }

                    Point p3 = new Point(currentPoint.X - EPSILON_PER_SIDE, currentPoint.Y);
                    Point p4 = new Point(currentPoint.X + EPSILON_PER_SIDE, currentPoint.Y);
                    Point p5 = new Point(currentPoint.X, currentPoint.Y - EPSILON_PER_SIDE);
                    Point p6 = new Point(currentPoint.X, currentPoint.Y + EPSILON_PER_SIDE);

                    if (doLineIntersect(p1, p2, p3, p4) || doLineIntersect(p1, p2, p5, p6))
                    {
                        polygonInformations.LastPolygonPoint = polygonLabel.Polygon[i];
                        polygonInformations.OverLinePolygon = polygonLabel;
                        return true;
                    }
                }
            }
            
            return false;
        }

        bool doLineIntersect(Point p1, Point p2, Point p3, Point p4)
        {
            double s1_x = p2.X - p1.X;
            double s1_y = p2.Y - p1.Y;
            double s2_x = p4.X - p3.X;
            double s2_y = p4.Y - p3.Y;

            double s, t;
            s = (-s1_y * (p1.X - p3.X) + s1_x * (p1.Y - p3.Y)) / (-s2_x * s1_y + s1_x * s2_y);
            t = (s2_x * (p1.Y - p3.Y) - s2_y * (p1.X - p3.X)) / (-s2_x * s1_y + s1_x * s2_y);

            if (s >= 0 && s <= 1 && t >= 0 && t <= 1)
                return true;

            return false; 
        }

    }
}

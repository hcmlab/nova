using ssi.Types.Polygon;
using System;
using System.Windows;

namespace ssi.Tools.Polygon_Helper
{
    class MousePositionInformation
    {
        private MainControl control;
        private EditInformations editInfos;
        private CreationInformation creationInfos;

        public MousePositionInformation(MainControl control, CreationInformation creationInfos, EditInformations editInfos)
        {
            this.control = control;
            this.editInfos = editInfos;
            this.creationInfos = creationInfos;
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

        //TODO überarbeiten -> Mause ist bei Linien die im winkel von 90 grad verlaufen nie auf der Linie
        public bool mouseIsOnLine()
        {
            const double EPSILON = 3;
            Point currentPoint = creationInfos.LastKnownPoint;
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;

            if (polygonLabel == null)
            {
                return false;
            }

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

                double a = (p2.Y - p1.Y) / (p2.X - p1.X);
                double b = p1.Y - a * p1.X;

                if (Math.Abs(currentPoint.Y - (a * currentPoint.X + b)) < EPSILON || Math.Abs(currentPoint.X - ((currentPoint.Y - b) / a)) < EPSILON)
                {
                    // Set the defintion area
                    double largeX;
                    double largeY;
                    double smallX;
                    double smallY;

                    if (p1.X >= p2.X)
                    {
                        largeX = p1.X;
                        smallX = p2.X;
                    }
                    else
                    {
                        largeX = p2.X;
                        smallX = p1.X;
                    }

                    if (p1.Y >= p2.Y)
                    {
                        largeY = p1.Y;
                        smallY = p2.Y;
                    }
                    else
                    {
                        largeY = p2.Y;
                        smallY = p1.Y;
                    }

                    // Check whether we are in the definition area
                    if (currentPoint.X > (smallX - EPSILON) && currentPoint.X < (largeX + EPSILON) && currentPoint.Y > (smallY - EPSILON) && currentPoint.Y < (largeY + EPSILON))
                    {
                        editInfos.LastPolygonPoint = polygonLabel.Polygon[i];
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

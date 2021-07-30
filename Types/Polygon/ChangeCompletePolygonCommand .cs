using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;

namespace ssi.Types.Polygon
{
    class ChangeCompletePolygonCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private List<Point> oldPoints;
        private List<Point> newPoints;

        public ChangeCompletePolygonCommand(List<Point> oldPoints, List<Point> newPoints, PolygonLabel polygonToChange)
        {
            this.polygonToChange = polygonToChange;
            this.oldPoints = oldPoints;
            this.newPoints = newPoints;
        }

        public PolygonLabel Do()
        {
            int counter = 0;
            foreach(PolygonPoint point in polygonToChange.Polygon)
            {
                point.X = newPoints[counter].X;
                point.Y = newPoints[counter].Y;
                counter++;
            }
                
            return polygonToChange;
        }

        public PolygonLabel Undo()
        {
            int counter = 0;
            foreach (PolygonPoint point in polygonToChange.Polygon)
            {
                point.X = oldPoints[counter].X;
                point.Y = oldPoints[counter].Y;
                counter++;
            }

            return polygonToChange;
        }
    }
}

using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    class ChangeCompletePolygonCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private List<Point> oldPoints;
        private List<Point> newPoints;
        private TYPE type;

        public ChangeCompletePolygonCommand(List<Point> oldPoints, List<Point> newPoints, PolygonLabel polygonToChange, TYPE type)
        {
            this.polygonToChange = polygonToChange;
            this.oldPoints = oldPoints;
            this.newPoints = newPoints;
            this.type = type;
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

            polygonToChange.Informations = new LabelInformations(this.type);
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

            polygonToChange.Informations = new LabelInformations(this.type);
            return polygonToChange;
        }
    }
}

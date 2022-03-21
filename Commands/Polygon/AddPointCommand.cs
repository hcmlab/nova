using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    // Adds a point to label non finished label
    class AddPointCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private PolygonPoint point;
        private readonly TYPE type = TYPE.CREATION;

        public AddPointCommand(PolygonPoint point, PolygonLabel polygonToChange)
        {
            this.polygonToChange = polygonToChange;
            this.point = point;
        }

        public PolygonLabel[] Do()
        {
            if (point.X != -1 && point.Y != -1)
                polygonToChange.addPoint(point);

            polygonToChange.Informations = new LabelInformations(this.type);
            return new PolygonLabel[] { polygonToChange };
        }

        public PolygonLabel[] Undo()
        {
            if(point.X != -1 && point.Y != -1)
                polygonToChange.removeLastPoint();

            polygonToChange.Informations = new LabelInformations(this.type);
            return new PolygonLabel[] { polygonToChange };
        }
    }
}

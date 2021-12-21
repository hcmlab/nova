using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    class AddOrRemovePolygonPointCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private PolygonPoint point;
        private TYPE type;

        public AddOrRemovePolygonPointCommand(PolygonPoint point, PolygonLabel polygonToChange, TYPE type)
        {
            this.polygonToChange = polygonToChange;
            this.point = point;
            this.type = type;
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

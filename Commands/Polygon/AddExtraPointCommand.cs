using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    // Adds a point to a finished label
    class AddExtraPointCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private double lastID;
        private PolygonPoint newPoint;
        private readonly TYPE type = TYPE.EXTRA_POINT;

        public AddExtraPointCommand(double ID, PolygonPoint point, PolygonLabel polygonToChange)
        {
            this.polygonToChange = polygonToChange;
            this.lastID = ID;
            this.newPoint = point;
        }

        public PolygonLabel[] Do()
        {
            polygonToChange.addPointAfterSpecificID(this.lastID, this.newPoint);
            polygonToChange.Informations = new LabelInformations(this.type);
            return new PolygonLabel[]{polygonToChange};
        }

        public PolygonLabel[] Undo()
        {
            polygonToChange.removePointAfterSpecificID(this.lastID);
            polygonToChange.Informations = new LabelInformations(this.type);
            return new PolygonLabel[] { polygonToChange };
        }
    }
}

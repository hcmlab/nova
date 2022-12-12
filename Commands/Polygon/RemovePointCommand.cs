using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    // Adds a point to a finished label
    class RemovePointCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private double lastID;
        private PolygonPoint newPoint;
        private readonly TYPE type = TYPE.REMOVE_POINT;

        public RemovePointCommand(double ID, PolygonPoint point, PolygonLabel polygonToChange)
        {
            this.polygonToChange = polygonToChange;
            this.lastID = ID;
            this.newPoint = point;
        }

        public PolygonLabel[] Do()
        {
            polygonToChange.removePointAfterSpecificID(this.lastID);
            polygonToChange.Informations = new LabelInformations(this.type);
            return new PolygonLabel[] { polygonToChange };
        }


        public PolygonLabel[] Undo()
        {
            polygonToChange.addPointAfterSpecificID(this.lastID, this.newPoint);
            polygonToChange.Informations = new LabelInformations(this.type);
            return new PolygonLabel[]{polygonToChange};
        }

    }
}

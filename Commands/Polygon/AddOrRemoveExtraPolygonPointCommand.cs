using ssi.Interfaces;
using System.Collections.Generic;
using System.Windows;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    class AddOrRemoveExtraPolygonPointCommand : ICommand
    {
        private PolygonLabel polygonToChange;
        private double lastID;
        private PolygonPoint newPoint;
        private TYPE type;

        public AddOrRemoveExtraPolygonPointCommand(double ID, PolygonPoint point, PolygonLabel polygonToChange, TYPE type)
        {
            this.polygonToChange = polygonToChange;
            this.lastID = ID;
            this.newPoint = point;
            this.type = type;
        }

        public PolygonLabel Do()
        {
            polygonToChange.addPointAfterSpecificID(this.lastID, this.newPoint);
            polygonToChange.Informations = new LabelInformations(this.type);
            return polygonToChange;
        }

        public PolygonLabel Undo()
        {
            polygonToChange.removePointAfterSpecificID(this.lastID);
            polygonToChange.Informations = new LabelInformations(this.type);
            return polygonToChange;
        }
    }
}

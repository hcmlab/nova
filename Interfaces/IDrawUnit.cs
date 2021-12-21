using ssi.Types.Polygon;
using System.Collections.Generic;

namespace ssi.Interfaces
{
    interface IDrawUnit
    {
        Utilities PolygonUtilities { get; set; }
        void drawLineToMousePosition(double x, double y);
        void polygonOverlayUpdate(AnnoListItem item, List<int> selectionRectPoints = null);
        List<int> getRectanglePointsAsList(double xStart, double yStart, double xDestiny, double yDestiny);
        void drawNew(PolygonLabel currentPolygonLabel, AnnoListItem item);
        void clearOverlay();
    }
}

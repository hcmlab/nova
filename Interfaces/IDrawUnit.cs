using ssi.Tools.Polygon_Helper;
using ssi.Types.Polygon;
using System.Collections.Generic;

namespace ssi.Interfaces
{
    interface IDrawUnit
    {
        void drawLineToMousePosition(double x, double y);
        void polygonOverlayUpdate(AnnoListItem item, List<int> selectionRectPoints = null);
        void drawNew(PolygonLabel currentPolygonLabel, AnnoListItem item);
        void clearOverlay();
        DrawUtilities getDrawUtilities();
        void setObjects(MainControl control, PolygonInformations polygonInformations, PolygonUtilities polygonUtilities);
    }
}

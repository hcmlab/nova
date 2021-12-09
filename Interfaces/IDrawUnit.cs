using ssi.Types.Polygon;

namespace ssi.Interfaces
{
    interface IDrawUnit
    {
        Utilities PolygonUtilities { get; set; }
        void drawLineToMousePosition(double x, double y);
        void polygonOverlayUpdate(AnnoListItem item);
        void drawNew(PolygonLabel currentPolygonLabel, AnnoListItem item);
        void clearOverlay();
    }
}

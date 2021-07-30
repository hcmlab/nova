using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi.Interfaces
{
    interface IPolygonDrawUnit
    {
        IPolygonUtilities PolygonUtilities { get; set; }
        void drawLineToMousePosition(double x, double y);
        void polygonOverlayUpdate(AnnoListItem item);
        void removeLastPoint(PolygonLabel currentPolygonLabel, AnnoListItem item);
        void clearOverlay();
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ssi.Types.Polygon
{
    class CreationInformation
    {
        private bool isCreateModeOn = false;
        private bool isPolylineToDraw = false;
        private System.Windows.Point lastKnownPoint;
        private PolygonLabel lastSelectedLabel;
        private System.Windows.Point lastPrintedPoint;

        public bool IsCreateModeOn { get => isCreateModeOn; set => isCreateModeOn = value; }
        public bool IsPolylineToDraw { get => isPolylineToDraw; set => isPolylineToDraw = value; }
        public Point LastKnownPoint { get => lastKnownPoint; set => lastKnownPoint = value; }
        public Point LastPrintedPoint { get => lastPrintedPoint; set => lastPrintedPoint = value; }
        public PolygonLabel LastSelectedLabel { get => lastSelectedLabel; set => lastSelectedLabel = value; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace ssi.Types.Polygon
{
    class PolygonInformations
    {
        private bool mouseOnMedia = false;
        private bool showingSelectionRect = false;
        private PolygonPoint selectedPoint = null;
        private PolygonLabel selectedPolygon = null;
        private bool isAboveSelectedPolygonLineAndCtrlPressed = false;
        private Point startPosition = new Point(-1, -1);
        private List<Point> polygonStartPosition;
        private List<Tuple<double, double>> polygonPointsXandYDistances = new List<Tuple<double, double>>();
        private PolygonPoint lastPolygonPoint;
        private double imageWidth = 0;
        private double imageHeight = 0;
        private bool isCreateModeOn = false;
        private bool isPolylineToDraw = false;
        private System.Windows.Point lastKnownPoint;
        private PolygonLabel lastSelectedLabel;
        private System.Windows.Point lastPrintedPoint;
        private PolygonLabel overLinePolygon = null;

        public bool MouseOnMedia { get => mouseOnMedia; set => mouseOnMedia = value; }
        public bool ShowingSelectionRect { get => showingSelectionRect; set => showingSelectionRect = value; }

        public PolygonLabel SelectedPolygon { get => selectedPolygon; set => selectedPolygon = value; }
        public PolygonPoint SelectedPoint { get => selectedPoint; set => selectedPoint = value; }
        public bool IsAboveSelectedPolygonLineAndCtrlPressed { get => isAboveSelectedPolygonLineAndCtrlPressed; set => isAboveSelectedPolygonLineAndCtrlPressed = value; }

        public Point StartPosition { get => startPosition; set => startPosition = value; }
        public List<Point> PolygonStartPosition { get => polygonStartPosition; set => polygonStartPosition = value; }
        public PolygonPoint LastPolygonPoint { get => lastPolygonPoint; set => lastPolygonPoint = value; }
        public double ImageWidth { get => imageWidth; set => imageWidth = value; }
        public double ImageHeight { get => imageHeight; set => imageHeight = value; }
        public List<Tuple<double, double>> PolygonPointsXandYDistances { get => polygonPointsXandYDistances; set => polygonPointsXandYDistances = value; }
        public bool IsCreateModeOn { get => isCreateModeOn; set => isCreateModeOn = value; }
        public bool IsPolylineToDraw { get => isPolylineToDraw; set => isPolylineToDraw = value; }
        public Point LastKnownPoint { get => lastKnownPoint; set => lastKnownPoint = value; }
        public Point LastPrintedPoint { get => lastPrintedPoint; set => lastPrintedPoint = value; }
        public PolygonLabel LastSelectedLabel { get => lastSelectedLabel; set => lastSelectedLabel = value; }
        public PolygonLabel OverLinePolygon { get => overLinePolygon; set => overLinePolygon = value; }

        public PolygonInformations()
        {

        }

        public void restetInfos()
        {
            startPosition = new Point(-1, -1);
            isAboveSelectedPolygonLineAndCtrlPressed = false;
            SelectedPoint = null;
            selectedPolygon = null;
        }

        public void setSelectedPolygonPointsAsDistanceToMouse(List<Point> polygon, double xMouse, double yMouse)
        {
            polygonPointsXandYDistances = new List<Tuple<double, double>>();
            polygon.ForEach(point => polygonPointsXandYDistances.Add(new Tuple<double, double>(point.X - xMouse, point.Y - yMouse)));
        }

        public void resetSelectedElements()
        {
            SelectedPoint = null;
            selectedPolygon = null;
        }
    }
}

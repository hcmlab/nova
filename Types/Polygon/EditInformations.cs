using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace ssi.Types.Polygon
{
    class EditInformations
    {
        private bool isEditModeOn = false;
        private bool mouseOnMedia = false;
        private bool showingSelectionRect = false;
        private bool isAboveSelectedPolygonPoint = false;
        private bool isWithinSelectedPolygonArea = false;
        private bool isAboveSelectedPolygonLineAndCtrlPressed = false;
        private PolygonPoint selectedPolygonPoint;
        private Point startPosition = new Point(-1, -1);
        private List<Point> polygonStartPosition;
        private PolygonPoint lastPolygonPoint;
        private double imageWidth = 0;
        private double imageHeight = 0;


        public bool IsEditModeOn { get => isEditModeOn; set => isEditModeOn = value; }
        public bool MouseOnMedia { get => mouseOnMedia; set => mouseOnMedia = value; }
        public bool ShowingSelectionRect { get => showingSelectionRect; set => showingSelectionRect = value; }

        public bool IsWithinSelectedPolygonArea { get => isWithinSelectedPolygonArea; set => isWithinSelectedPolygonArea = value; }
        public bool IsAboveSelectedPolygonPoint { get => isAboveSelectedPolygonPoint; set => isAboveSelectedPolygonPoint = value; }
        public bool IsAboveSelectedPolygonLineAndCtrlPressed { get => isAboveSelectedPolygonLineAndCtrlPressed; set => isAboveSelectedPolygonLineAndCtrlPressed = value; }

        public Point StartPosition { get => startPosition; set => startPosition = value; }
        public List<Point> PolygonStartPosition { get => polygonStartPosition; set => polygonStartPosition = value; }
        public PolygonPoint SelectedPolygonPoint { get => selectedPolygonPoint; set => selectedPolygonPoint = value; }
        public PolygonPoint LastPolygonPoint { get => lastPolygonPoint; set => lastPolygonPoint = value; }
        public double ImageWidth { get => imageWidth; set => imageWidth = value; }
        public double ImageHeight { get => imageHeight; set => imageHeight = value; }

        public EditInformations()
        {

        }

        public void restetInfos()
        {
            startPosition = new Point(-1, -1);
            isAboveSelectedPolygonLineAndCtrlPressed = false;
            isAboveSelectedPolygonPoint = false;
            isWithinSelectedPolygonArea = false;
            selectedPolygonPoint = null;
        }
    }
}

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
        private bool isAboveSelectedPolygonPoint = false;
        private PolygonPoint selectedPolygonPoint;
        private bool isWithinSelectedPolygonArea = false;
        private bool isLeftMouseDown = false;
        private Point startPosition;
        private List<Point> polygonStartPosition;
        private PolygonPoint lastPolygonPoint;


        public bool IsEditModeOn { get => isEditModeOn; set => isEditModeOn = value; }
        public bool IsAboveSelectedPolygonPoint { get => isAboveSelectedPolygonPoint; set => isAboveSelectedPolygonPoint = value; }
        public PolygonPoint SelectedPolygonPoint { get => selectedPolygonPoint; set => selectedPolygonPoint = value; }

        public bool IsWithinSelectedPolygonArea { get => isWithinSelectedPolygonArea; set => isWithinSelectedPolygonArea = value; }
        public bool IsLeftMouseDown { get => isLeftMouseDown; set => isLeftMouseDown = value; }
        public Point StartPosition { get => startPosition; set => startPosition = value; }
        public List<Point> PolygonStartPosition { get => polygonStartPosition; set => polygonStartPosition = value; }
        public PolygonPoint LastPolygonPoint { get => lastPolygonPoint; set => lastPolygonPoint = value; }

        public EditInformations()
        {

        }

        public void restetInfos()
        {
            isAboveSelectedPolygonPoint = false;
            isWithinSelectedPolygonArea = false;
            selectedPolygonPoint = null;
        }
    }
}

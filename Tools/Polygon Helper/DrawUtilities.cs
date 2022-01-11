using ssi.Types.Polygon;
using System;
using System.Collections.Generic;
using System.Windows;

namespace ssi.Tools.Polygon_Helper
{
    class DrawUtilities
    {
        private EditInformations editInfos;
        private PolygonUtilities polygonUtilities;
        private bool isNextToStartPoint = false;

        public bool IsNextToStartPoint
        {
            get => isNextToStartPoint;
            set => isNextToStartPoint = value;
        }

        public DrawUtilities(EditInformations editInfos, PolygonUtilities polygonUtilities)
        {
            this.editInfos = editInfos;
            this.polygonUtilities = polygonUtilities;
        }

        public bool isNewPointNextToStartPoint(PolygonPoint startPoint, Point newPoint)
        {
            const int MIN_DISTANCE = 10;

            double currentDistance = Math.Sqrt(Math.Pow(startPoint.X - newPoint.X, 2) + Math.Pow(startPoint.Y - newPoint.Y, 2));
            if (currentDistance < MIN_DISTANCE)
            {
                isNextToStartPoint = true;
                return true;
            }
            else
            {
                isNextToStartPoint = false;
                return false;
            }
        }

        public List<int> getRectanglePointsAsList(double xStart, double yStart, double xDestiny, double yDestiny)
        {
            if (!polygonUtilities.isPos5pxFromBottomAway(yStart))
                yStart = editInfos.ImageHeight;
            if (!polygonUtilities.isPos5pxFromBottomAway(yDestiny))
                yDestiny = editInfos.ImageHeight;

            List<int> allPoints = new List<int>
            {
                (int)xStart,
                (int)yStart,
                (int)xStart,
                (int)yDestiny,
                (int)xDestiny,
                (int)yDestiny,
                (int)xDestiny,
                (int)yStart,
                (int)xStart,
                (int)yStart
            };

            return allPoints;
        }
    }
}

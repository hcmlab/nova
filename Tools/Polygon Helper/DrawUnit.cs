using ssi.Interfaces;
using ssi.Tools.Polygon_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;


namespace ssi.Types.Polygon
{
    class DrawUnit : IDrawUnit
    {
        private MainControl control;
        private PolygonInformations polygonInformations;
        private DrawUtilities drawUtilities;
        private PolygonUtilities polygonUtilities;

        public DrawUnit()
        {

        }

        public void setObjects(MainControl control, PolygonInformations polygonInformations, PolygonUtilities polygonUtilities)
        {
            this.control = control;
            this.polygonInformations = polygonInformations;
            this.polygonUtilities = polygonUtilities;
            this.drawUtilities = new DrawUtilities(this.polygonInformations, this.polygonUtilities);
        }

        public DrawUtilities getDrawUtilities()
        {
            return drawUtilities;
        }

        public void polygonOverlayUpdate(AnnoListItem item, List<int> selectionRectPoints = null)
        {
            if (!(item is object))
                return;

            PolygonList polygonList = item.PolygonList;

            if (!(polygonList is object) || !(polygonList.Polygons is object))
                return;

            WriteableBitmap overlay = null;

            IMedia video = MainHandler.mediaList.GetFirstVideo();

            if (video != null)
                overlay = video.GetOverlay();
            else
                return;

            Tuple<int, int> size = video.GetImageSize();
            overlay.Lock();
            overlay.Clear();

            drawLabels(polygonList, overlay, size, selectionRectPoints);

            overlay.Unlock();
        }

        private void drawLabels(PolygonList polygonList, WriteableBitmap overlay, Tuple<int, int> size, List<int> selectionRectPoints = null)
        {
            int pixelCount = size.Item1 * size.Item2;

            int standard_thickness = (int)Math.Ceiling((double)pixelCount / 1000000) + 1;
            int thickness = 1;
            Color lastColor = new Color();

            if (selectionRectPoints != null)
            {
                polygonInformations.ShowingSelectionRect = true;
                overlay.DrawPolyline(selectionRectPoints.ToArray(), Color.FromRgb(0, 120, 215));
                overlay.FillPolygon(selectionRectPoints.ToArray(), Color.FromArgb(100, 0, 102, 204));
            }

            // we dont want to draw the area over the points -> so we have to draw the area first
            foreach (PolygonLabel polygonLabel in polygonList.Polygons)
            {
                bool currentPolygonLabelEqualSelectedOne = false;
                for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                {
                    if (polygonLabel.Equals(control.polygonListControl.polygonDataGrid.SelectedItems[i]))
                    {
                        currentPolygonLabelEqualSelectedOne = true;
                        break;
                    }
                }

                if (currentPolygonLabelEqualSelectedOne && !polygonInformations.IsCreateModeOn)
                {
                    List<int> allPoints = new List<int>();

                    lastColor.R = polygonLabel.Color.R;
                    lastColor.G = polygonLabel.Color.G;
                    lastColor.B = polygonLabel.Color.B;
                    lastColor.A = polygonLabel.Color.A;

                    foreach (PolygonPoint pp in polygonLabel.Polygon)
                    {
                        allPoints.Add((int)pp.X);
                        allPoints.Add((int)pp.Y);
                    }

                    allPoints.Add((int)polygonLabel.Polygon.First().X);
                    allPoints.Add((int)polygonLabel.Polygon.First().Y);
                    Color contentColor = polygonLabel.Color;

                    if (polygonInformations.SelectedPolygon != null)
                        contentColor.A = 160;
                    else
                        contentColor.A = 120;

                    overlay.FillPolygon(allPoints.ToArray(), contentColor);
                }
            }

            foreach (PolygonLabel polygonLabel in polygonList.Polygons)
            {
                if (polygonLabel.Polygon.Count > 0)
                {
                    bool currentPolygonLabelEqualSelectedOnes = false;
                    for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                    {
                        if (polygonLabel.Equals(control.polygonListControl.polygonDataGrid.SelectedItems[i]))
                        {
                            currentPolygonLabelEqualSelectedOnes = true;
                            break;
                        }
                    }
                    if (currentPolygonLabelEqualSelectedOnes)
                        thickness = standard_thickness * 2;
                    else
                        thickness = standard_thickness;

                    Color color = polygonLabel.Color;
                    Color secondColor = Color.FromRgb(255, 255, 255);

                    if (color.R + color.G + color.B > 600)
                        secondColor = Color.FromRgb(0, 0, 0);

                    drawPolygon(overlay, polygonLabel, thickness, currentPolygonLabelEqualSelectedOnes, color, secondColor);
                }
            }

            if (polygonInformations.SelectedPoint != null)
            {
                fillRect(overlay, lastColor);
            }
        }

        private void drawPolygon(WriteableBitmap overlay, PolygonLabel polygonLabel, int thickness, bool isSelectedOne, Color color, Color secondColor)
        {
            PolygonPoint point, nextPoint = null;

            foreach (PolygonPoint pp in polygonLabel.Polygon)
            {
                point = nextPoint;
                nextPoint = pp;

                if (isSelectedOne && drawUtilities.IsNextToStartPoint)
                {
                    if (!(point is object))
                    {
                        overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 7, thickness + 7, color);
                        overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 6, thickness + 6, secondColor);
                        continue;
                    }

                    overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 2, thickness + 2, color);
                    overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 1, thickness + 1, secondColor);
                    overlay.DrawLineAa((int)point.X, (int)point.Y, (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
                }
                else
                {
                    overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 2, thickness + 2, color);

                    if(!(point is object))
                    {
                        continue;
                    }

                    overlay.DrawLineAa((int)point.X, (int)point.Y, (Int32)nextPoint.X, (Int32)nextPoint.Y, color, thickness);
                }
            }

            // we finish the polygon when we are not in the creation mode or when we dont draw the polygon of the selected label
            if (!isSelectedOne || !polygonInformations.IsCreateModeOn)
            {
                overlay.DrawLineAa((int)(polygonLabel.Polygon.First().X), (int)(polygonLabel.Polygon.First().Y), (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
            }
        }

        private void fillRect(WriteableBitmap overlay, Color lastColor)
        {
            const int LINE_THICKNESS = 7;
            overlay.FillRectangle((int)polygonInformations.SelectedPoint.X - LINE_THICKNESS, (int)polygonInformations.SelectedPoint.Y - LINE_THICKNESS, (int)polygonInformations.SelectedPoint.X + LINE_THICKNESS, (int)polygonInformations.SelectedPoint.Y + LINE_THICKNESS, lastColor);
            overlay.FillRectangle((int)polygonInformations.SelectedPoint.X - LINE_THICKNESS + 2, (int)polygonInformations.SelectedPoint.Y - LINE_THICKNESS + 2, (int)polygonInformations.SelectedPoint.X + LINE_THICKNESS - 2, (int)polygonInformations.SelectedPoint.Y + LINE_THICKNESS - 2, Colors.White);
        }

        public void drawLineToMousePosition(double x, double y)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            WriteableBitmap overlay = null;
            IMedia video = MainHandler.mediaList.GetFirstVideo();

            if (video != null)
                overlay = video.GetOverlay();
            else
                return;
            
            int thickness = 3;
            Color color = polygonLabel.Color;
            overlay.Lock();

            if (!polygonUtilities.isPos5pxFromBottomAway(y))
                y = polygonInformations.ImageHeight;

            polygonOverlayUpdate(item);
            if (drawUtilities.isNewPointNextToStartPoint(polygonLabel.Polygon.First(), new Point(x, y)))
            {
                overlay.DrawLineAa((int)polygonLabel.Polygon.Last().X, (int)polygonLabel.Polygon.Last().Y, (int)x, (int)y, color, thickness);
            }
            else
            {
                overlay.FillEllipseCentered((int)x, (int)y, thickness + 2, thickness + 2, color);
                overlay.DrawLineAa((int)polygonLabel.Polygon.Last().X, (int)polygonLabel.Polygon.Last().Y, (int)x, (int)y, color, thickness);
            }
            overlay.Unlock();
        }

        public void drawNew(PolygonLabel currentPolygonLabel, AnnoListItem item)
        {
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object)
            {
                polygonOverlayUpdate(item);
                if(currentPolygonLabel.Polygon.Count > 0)
                    drawLineToMousePosition(polygonInformations.LastKnownPoint.X, polygonInformations.LastKnownPoint.Y);
            }
        }

        public void clearOverlay()
        {
            WriteableBitmap overlay = null;

            IMedia video = MainHandler.mediaList.GetFirstVideo();

            if (video != null)
            {
                overlay = video.GetOverlay();
            }
            else
            {
                return;
            }

            overlay.Lock();
            overlay.Clear();
            overlay.Unlock();
        }
    }
}

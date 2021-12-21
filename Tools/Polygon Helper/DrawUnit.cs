using ssi.Interfaces;
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
        private EditInformations editInfos;
        private CreationInformation creationInfos;
        private Utilities polygonUtilities;

        public Utilities PolygonUtilities { get => polygonUtilities; set => polygonUtilities = value; }

        public DrawUnit(MainControl control, CreationInformation creationInfos, EditInformations editInfos)
        {
            this.control = control;
            this.editInfos = editInfos;
            this.creationInfos = creationInfos;
            this.PolygonUtilities = polygonUtilities;
        }

        public void polygonOverlayUpdate(AnnoListItem item, List<int> selectionRectPoints = null)
        {
            if (item == null)
                return;

            PolygonList polygonList = item.PolygonList;

            if (polygonList == null || polygonList.Polygons == null)
                return;

            WriteableBitmap overlay = null;

            IMedia video = MainHandler.mediaList.GetFirstVideo();

            if (video != null)
                overlay = video.GetOverlay();
            else
                return;

            overlay.Lock();
            overlay.Clear();

            drawLabels(polygonList, overlay, selectionRectPoints);

            overlay.Unlock();
        }

        private void drawLabels(PolygonList polygonList, WriteableBitmap overlay, List<int> selectionRectPoints = null)
        {
            List<int> allPoints = new List<int>();
            int thickness = 1;
            Color lastColor = new Color();

            if (selectionRectPoints != null)
            {
                editInfos.ShowingSelectionRect = true;
                overlay.DrawPolyline(selectionRectPoints.ToArray(), Color.FromRgb(0, 120, 215));
                overlay.FillPolygon(selectionRectPoints.ToArray(), Color.FromArgb(100, 0, 102, 204));
            }

            foreach (PolygonLabel polygonLabel in polygonList.Polygons)
            {
                if (polygonLabel.Polygon.Count > 0)
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

                    if (currentPolygonLabelEqualSelectedOne)
                        thickness = 3;
                    else
                        thickness = 1;

                    Color color = polygonLabel.Color;
                    Color secondColor = Color.FromRgb(255, 255, 255);

                    if (color.R + color.G + color.B > 600)
                        secondColor = Color.FromRgb(0, 0, 0);

                    if (currentPolygonLabelEqualSelectedOne && !creationInfos.IsCreateModeOn && (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea))
                    {
                        lastColor.R = polygonLabel.Color.R;
                        lastColor.G = polygonLabel.Color.G;
                        lastColor.B = polygonLabel.Color.B;
                        lastColor.A = polygonLabel.Color.A;

                        // we dont want to draw the area over the points -> so we have to draw the area first
                        foreach (PolygonPoint pp in polygonLabel.Polygon)
                        {
                            if (currentPolygonLabelEqualSelectedOne)
                            {
                                allPoints.Add((int)pp.X);
                                allPoints.Add((int)pp.Y);
                            }
                        }

                        allPoints.Add((int)polygonLabel.Polygon.First().X);
                        allPoints.Add((int)polygonLabel.Polygon.First().Y);
                        Color contentColor = polygonLabel.Color;
                        contentColor.A = 126;
                        overlay.FillPolygon(allPoints.ToArray(), contentColor);
                    }

                    drawPolygons(overlay, polygonLabel, thickness, currentPolygonLabelEqualSelectedOne, color, secondColor);
                }
            }

            if (editInfos.IsAboveSelectedPolygonPoint)
            {
                fillRect(overlay, lastColor);
            }
        }

        private void drawPolygons(WriteableBitmap overlay, PolygonLabel polygonLabel, int thickness, bool isSelectedOne, Color color, Color secondColor)
        {
            PolygonPoint point, nextPoint = null;

            foreach (PolygonPoint pp in polygonLabel.Polygon)
            {
                point = nextPoint;
                nextPoint = pp;

                if (isSelectedOne && PolygonUtilities.IsNextToStartPoint)
                {
                    if (point == null)
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

                    if (point == null)
                    {
                        continue;
                    }

                    overlay.DrawLineAa((int)point.X, (int)point.Y, (Int32)nextPoint.X, (Int32)nextPoint.Y, color, thickness);
                }
            }

            // we finish the polygon when we are not in the creation mode or when we dont draw the polygon of the selected label
            if (!isSelectedOne || !creationInfos.IsCreateModeOn)
            {
                overlay.DrawLineAa((int)(polygonLabel.Polygon.First().X), (int)(polygonLabel.Polygon.First().Y), (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
            }
        }

        private void fillRect(WriteableBitmap overlay, Color lastColor)
        {
            const int LINE_THICKNESS = 7;
            overlay.FillRectangle((int)editInfos.SelectedPolygonPoint.X - LINE_THICKNESS, (int)editInfos.SelectedPolygonPoint.Y - LINE_THICKNESS, (int)editInfos.SelectedPolygonPoint.X + LINE_THICKNESS, (int)editInfos.SelectedPolygonPoint.Y + LINE_THICKNESS, lastColor);
            overlay.FillRectangle((int)editInfos.SelectedPolygonPoint.X - LINE_THICKNESS + 2, (int)editInfos.SelectedPolygonPoint.Y - LINE_THICKNESS + 2, (int)editInfos.SelectedPolygonPoint.X + LINE_THICKNESS - 2, (int)editInfos.SelectedPolygonPoint.Y + LINE_THICKNESS - 2, Colors.White);
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

            if (!polygonUtilities.posIs5pxFromBottomAway(y))
                y = editInfos.ImageHeight;

            polygonOverlayUpdate(item);
            if (PolygonUtilities.isNewPointNextToStartPoint(polygonLabel.Polygon.First(), new Point(x, y)))
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


        public List<int> getRectanglePointsAsList(double xStart, double yStart, double xDestiny, double yDestiny)
        {
            if (!polygonUtilities.posIs5pxFromBottomAway(yStart))
                yStart = editInfos.ImageHeight;
            if (!polygonUtilities.posIs5pxFromBottomAway(yDestiny))
                yDestiny = editInfos.ImageHeight;

            List<int> allPoints = new List<int>();
            allPoints.Add((int)xStart);
            allPoints.Add((int)yStart);
            allPoints.Add((int)xStart);
            allPoints.Add((int)yDestiny);
            allPoints.Add((int)xDestiny);
            allPoints.Add((int)yDestiny);
            allPoints.Add((int)xDestiny);
            allPoints.Add((int)yStart);
            allPoints.Add((int)xStart);
            allPoints.Add((int)yStart);

            return allPoints;
        }

        public void drawNew(PolygonLabel currentPolygonLabel, AnnoListItem item)
        {
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object)
            {
                polygonOverlayUpdate(item);
                if(currentPolygonLabel.Polygon.Count > 0)
                    drawLineToMousePosition(creationInfos.LastKnownPoint.X, creationInfos.LastKnownPoint.Y);
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

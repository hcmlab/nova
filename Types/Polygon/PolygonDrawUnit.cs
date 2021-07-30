using ssi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Windows.Media.Color;


namespace ssi.Types.Polygon
{
    class PolygonDrawUnit : IPolygonDrawUnit
    {
        private MainControl control;
        private EditInformations editInfos;
        private CreationInformation creationInfos;
        private IPolygonUtilities polygonUtilities;

        public IPolygonUtilities PolygonUtilities { get => polygonUtilities; set => polygonUtilities = value; }

        public PolygonDrawUnit(MainControl control, CreationInformation creationInfos, EditInformations editInfos)
        {
            this.control = control;
            this.editInfos = editInfos;
            this.creationInfos = creationInfos;
            this.PolygonUtilities = polygonUtilities;
        }

        public void polygonOverlayUpdate(AnnoListItem item)
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

            

            drawLabels(polygonList, overlay);

            overlay.Unlock();
        }

        private void drawLabels(PolygonList polygonList, WriteableBitmap overlay)
        {
            List<int> allPoints = new List<int>();
            int thickness = 1;
            Color lastColor = new Color();


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

                    if (currentPolygonLabelEqualSelectedOne && editInfos.IsEditModeOn && (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea))
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

        public void drawPolygons(WriteableBitmap overlay, PolygonLabel polygonLabel, int thickness, bool isSelectedOne, Color color, Color secondColor)
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

                    overlay.DrawLineAa((int)point.X, (int)point.Y, (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
                }
            }

            // we finish the polygon when we are not in the creation mode or when we dont draw the polygon of the selected label
            if (!isSelectedOne || !creationInfos.IsCreateModeOn)
            {
                overlay.DrawLineAa((int)(polygonLabel.Polygon.First().X), (int)(polygonLabel.Polygon.First().Y), (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
            }
        }

        public void fillRect(WriteableBitmap overlay, Color lastColor)
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

            if (PolygonUtilities.isNewPointNextToStartPoint(polygonLabel.Polygon.First(), new Point(x, y)))
            {
                polygonOverlayUpdate(item);
                overlay.DrawLineAa((int)polygonLabel.Polygon.Last().X, (int)polygonLabel.Polygon.Last().Y, (int)x, (int)y, color, thickness);
            }
            else
            {
                polygonOverlayUpdate(item);
                overlay.FillEllipseCentered((int)x, (int)y, thickness + 2, thickness + 2, color);
                overlay.DrawLineAa((int)polygonLabel.Polygon.Last().X, (int)polygonLabel.Polygon.Last().Y, (int)x, (int)y, color, thickness);
            }
            overlay.Unlock();
        }

        

        public void removeLastPoint(PolygonLabel currentPolygonLabel, AnnoListItem item)
        {
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object)
            {
                if (currentPolygonLabel.Polygon.Count > 1)
                {
                    List<PolygonLabel> polygonLabels = item.PolygonList.getRealList();
                    foreach (PolygonLabel polygonLabel in polygonLabels)
                    {
                        if (polygonLabel.Equals(currentPolygonLabel))
                        {
                            List<PolygonPoint> polygon = polygonLabel.Polygon;
                            polygon.RemoveAt(polygon.Count - 1);
                        }
                    }

                    polygonOverlayUpdate(item);

                    drawLineToMousePosition(creationInfos.LastKnownPoint.X, creationInfos.LastKnownPoint.Y);
                }
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

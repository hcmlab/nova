using ssi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;


namespace ssi.Types.Polygon
{
    class PolygonUtilities : IPolygonUtilities
    {
        private MainControl control;
        private EditInformations editInfos;
        private CreationInformation creationInfos;
        private IPolygonDrawUnit polygonDrawUnit;
        private DataGridChecker dataGridChecker;
        private UndoRedoStack polygonUndoRedoStack;
        private bool isNextToStartPoint;

        static private double idCounter = 0;

        public static double IDcounter { get => idCounter; set => idCounter = value; }
        
        public bool IsNextToStartPoint
        { 
            get => isNextToStartPoint; 
            set => isNextToStartPoint = value; 
        }
        public UndoRedoStack PolygonUndoRedoStack { get => polygonUndoRedoStack; set => polygonUndoRedoStack = value; }

        public PolygonUtilities(MainControl control, CreationInformation creationInfos, EditInformations editInfos, DataGridChecker dataGridChecker, IPolygonDrawUnit polygonDrawUnit)
        {
            this.control = control;
            this.editInfos = editInfos;
            this.creationInfos = creationInfos;
            this.dataGridChecker = dataGridChecker;
            this.polygonDrawUnit = polygonDrawUnit;
            this.isNextToStartPoint = false;
        }

        public void enableOrDisableControls(bool enable)
        {
            control.polygonListControl.IsEnabled = enable;
            control.annoListControl.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
        }

        public void endCreationMode(AnnoListItem item, PolygonLabel currentPolygonLabel = null)
        {
            if (currentPolygonLabel != null)
                item.PolygonList.removeExplicitPolygon(currentPolygonLabel);

            creationInfos.IsPolylineToDraw = false;
            creationInfos.IsCreateModeOn = false;
            creationInfos.AddMoreLabels = false;

            this.isNextToStartPoint = false;
            this.enableOrDisableControls(true);
            this.polygonSelectItem(item);
            this.polygonUndoRedoStack = null;
            this.control.Cursor = Cursors.Arrow;

            polygonSelectItem(item);
            if (creationInfos.LastSelectedLabel != null)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = creationInfos.LastSelectedLabel;
                creationInfos.LastSelectedLabel = null;
            }

            if (dataGridChecker.polygonDGIsNotNullAndCountsOne())
                editInfos.IsEditModeOn = true;
        }

        public bool isMouseWithinPolygonArea(double x, double y)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;

            var contains = false;
            using (var gp = new System.Drawing.Drawing2D.GraphicsPath())
            {
                System.Drawing.Point[] points = new System.Drawing.Point[polygonLabel.Polygon.Count];

                for (int i = 0; i < polygonLabel.Polygon.Count; i++)
                {
                    PolygonPoint polygonPoint = polygonLabel.Polygon.ElementAt(i);
                    points[i] = new System.Drawing.Point((int)polygonPoint.X, (int)polygonPoint.Y);
                }

                if (points.Length < 3)
                    return false;

                gp.AddPolygon(points);
                contains = gp.IsVisible(new System.Drawing.Point((int)x, (int)y));

                if(contains)
                    editInfos.StartPosition = new Point(x, y);
            }
            return contains;

        }

        public bool isMouseAbovePoint(double x, double y)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;

            foreach (PolygonPoint polygonPoint in polygonLabel.Polygon)
            {
                if (mouseIsAbovePointCheck(x, y, polygonPoint.X, polygonPoint.Y))
                {
                    editInfos.StartPosition = new Point(polygonPoint.X, polygonPoint.Y);
                    editInfos.IsAboveSelectedPolygonPoint = true;
                    editInfos.SelectedPolygonPoint = polygonPoint;
                    return true;
                }
            }
            return false;
        }

        private bool mouseIsAbovePointCheck(double mouseX, double mouseY, double pointX, double pointY)
        {
            const int POINT_THICKNESS = 5;

            if ((mouseX < (pointX + POINT_THICKNESS) && mouseX > (pointX - POINT_THICKNESS) && mouseY < (pointY + POINT_THICKNESS) && mouseY > (pointY - POINT_THICKNESS)))
            {
                return true;
            }

            return false;
        }

        public void updatePoint(double x, double y, AnnoListItem item)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;
            foreach (PolygonPoint polygonPoint in polygonLabel.Polygon)
            {
                if (polygonPoint.Equals(editInfos.SelectedPolygonPoint))
                {
                    polygonPoint.X = x;
                    polygonPoint.Y = y;
                    editInfos.SelectedPolygonPoint = polygonPoint;
                }
            }

            foreach (PolygonLabel polygon in item.PolygonList.getRealList())
            {
                foreach (PolygonPoint polygonPoint in polygon.Polygon)
                {
                    if (polygonPoint.Equals(editInfos.SelectedPolygonPoint))
                    {
                        polygonPoint.X = x;
                        polygonPoint.Y = y;
                    }
                }
            }
        }

        public void updatePolygon(double x, double y, AnnoListItem item)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;
            foreach (PolygonPoint polygonPoint in polygonLabel.Polygon)
            {
                polygonPoint.X += x - editInfos.StartPosition.X;
                polygonPoint.Y += y - editInfos.StartPosition.Y;
            }

            editInfos.StartPosition = new Point(x, y);
        }

        public void editPolygon(double x, double y)
        {
            if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && mouseIsOnLine())
            {
                PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                List<PolygonPoint> polygonPoints = new List<PolygonPoint>();
                foreach (PolygonPoint point in polygonLabel.Polygon)
                {
                    polygonPoints.Add(new PolygonPoint(point.X, point.Y, point.PointID, false));
                    if (point.PointID == editInfos.LastPolygonPoint.PointID)
                        polygonPoints.Add(new PolygonPoint(x, y));
                }

                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                List<PolygonLabel> polygonLabels = item.PolygonList.getRealList();
                PolygonLabel selectedItem = null;

                foreach (PolygonLabel label in polygonLabels)
                {
                    if (label.ID == polygonLabel.ID)
                    {
                        label.Polygon = polygonPoints;
                        selectedItem = label;
                        break;
                    }
                }

                item.PolygonList.Polygons = polygonLabels;
                polygonSelectItem(item);
                refreshAnnoDataGrid();
                control.polygonListControl.polygonDataGrid.SelectedItem = selectedItem;
            }
            else
            {
                if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                {
                    PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                    List<Point> polygonPoints = new List<Point>();

                    foreach (PolygonPoint point in polygonLabel.Polygon)
                    {
                        polygonPoints.Add(new Point(point.X, point.Y));
                    }

                    editInfos.PolygonStartPosition = polygonPoints;
                    editInfos.IsLeftMouseDown = true;
                    control.Cursor = Cursors.SizeAll;
                }
            }
        }

        public bool addNewPoint(double x, double y)
        {
            creationInfos.IsPolylineToDraw = true;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;

            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
            if (polygonLabel.Polygon.Count > 0 && this.IsNextToStartPoint)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                polygonTableUpdate();
                item.updateLabelCount();
                refreshAnnoDataGrid();
                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                return true;
            }

            double id = idCounter;
            idCounter++;
            PolygonPoint polygonPoint = new PolygonPoint(x, y, id);
            PolygonLabel selectedItem = null;
            List<PolygonLabel> polygonLabels = item.PolygonList.getRealList();
            creationInfos.LastPrintedPoint = new Point(x, y);
            foreach (PolygonLabel label in polygonLabels)
            {
                if (label.ID == polygonLabel.ID)
                {
                    label.addPoint(polygonPoint);
                    selectedItem = label;
                    break;
                }
            }

            item.PolygonList.Polygons = polygonLabels;

            polygonSelectItem(item);
            refreshAnnoDataGrid();
            control.polygonListControl.polygonDataGrid.SelectedItem = selectedItem;

            return false;
        }

        public void addPolygonLabelToPolygonList(PolygonLabel pl)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsZero() && dataGridChecker.polygonDGIsNotNullAndCountsZero())
                control.annoListControl.annoDataGrid.SelectedItem = control.annoListControl.annoDataGrid.Items[0];

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            item.PolygonList.addPolygonLabel(pl);
            polygonSelectItem(item);
            control.polygonListControl.polygonDataGrid.SelectedItem = pl;
        }

        public void polygonSelectItem(AnnoListItem item)
        {
            if (!dataGridChecker.annonDGIsNotNullAndCountsOne())
                item = (AnnoListItem)control.annoListControl.annoDataGrid.Items[0];
            control.polygonListControl.polygonDataGrid.ItemsSource = item.PolygonList.getRealList();
            this.polygonTableUpdate();
            polygonDrawUnit.polygonOverlayUpdate(item);
        }

        public void activateEditMode()
        {
            editInfos.IsEditModeOn = true;
            polygonUndoRedoStack = new UndoRedoStack();
        }

        public void polygonTableUpdate()
        {
            control.polygonListControl.polygonDataGrid.Items.Refresh();
        }

        public void refreshAnnoDataGrid()
        {
            control.annoListControl.annoDataGrid.Items.Refresh();
        }

        public bool labelIsNotSelected(PolygonLabel pl)
        {
            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                if (pl.Equals(control.polygonListControl.polygonDataGrid.SelectedItems[i]))
                {
                    return false;
                }
            }

            return true;
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

        public void escClickedCWhileProbablyCreatingPolygon()
        {
            PolygonLabel currentPolygonLabel = control.polygonListControl.polygonDataGrid.SelectedItem as PolygonLabel;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object && currentPolygonLabel.Polygon.Count > 0)
            {
                List<PolygonLabel> polygonLabels = item.PolygonList.Polygons;

                for (int j = 0; j < polygonLabels.Count; j++)
                {
                    if (polygonLabels[j].ID == currentPolygonLabel.ID)
                    {
                        polygonLabels[j].removeAll();
                        currentPolygonLabel = polygonLabels[j];
                        break;
                    }
                }
                item.PolygonList.Polygons = polygonLabels;

                polygonSelectItem(item);
                polygonTableUpdate();

                control.polygonListControl.polygonDataGrid.SelectedItem = currentPolygonLabel;
                creationInfos.IsPolylineToDraw = false;
            }
            else
            {
                endCreationMode(item, currentPolygonLabel);
            }
        }

        public bool mouseIsOnLine()
        {
            const double EPSILON = 4;
            Point currentPoint = creationInfos.LastKnownPoint;
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
            Point p1 = new Point();
            Point p2 = new Point();
            for (int i = 0; i < polygonLabel.Polygon.Count; i++)
            {
                if (i + 1 < polygonLabel.Polygon.Count)
                {
                    p1 = new Point(polygonLabel.Polygon[i].X, polygonLabel.Polygon[i].Y);
                    p2 = new Point(polygonLabel.Polygon[i + 1].X, polygonLabel.Polygon[i + 1].Y);
                }
                else
                {
                    p1 = new Point(polygonLabel.Polygon[i].X, polygonLabel.Polygon[i].Y);
                    p2 = new Point(polygonLabel.Polygon[0].X, polygonLabel.Polygon[0].Y);
                }

                double a = (p2.Y - p1.Y) / (p2.X - p1.X);
                double b = p1.Y - a * p1.X;

                if (Math.Abs(currentPoint.Y - (a * currentPoint.X + b)) < EPSILON || Math.Abs(currentPoint.X - ((currentPoint.Y - b) / a)) < EPSILON)
                {
                    // Set the defintion area
                    double largeX;
                    double largeY;
                    double smallX;
                    double smallY;

                    if(p1.X >= p2.X)
                    {
                        largeX = p1.X;
                        smallX = p2.X;
                    }
                    else
                    {
                        largeX = p2.X;
                        smallX = p1.X;
                    }

                    if (p1.Y >= p2.Y)
                    {
                        largeY = p1.Y;
                        smallY = p2.Y;
                    }
                    else
                    {
                        largeY = p2.Y;
                        smallY = p1.Y;
                    }
                    
                    // Check whether we are in the definition area
                    if(currentPoint.X > (smallX - EPSILON) && currentPoint.X < (largeX + EPSILON) && currentPoint.Y > (smallY - EPSILON) && currentPoint.Y < (largeY + EPSILON))
                    {
                        editInfos.LastPolygonPoint = polygonLabel.Polygon[i];
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

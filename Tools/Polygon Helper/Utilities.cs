using ssi.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    internal class Utilities
    {
        private MainHandler handler;
        private MainControl control;
        private EditInformations editInfos;
        private CreationInformation creationInfos;
        private IDrawUnit polygonDrawUnit;
        private DataGridChecker dataGridChecker;
        private UndoRedoStack undoRedoStack;
        private Window interpolationWindow = null;
        private bool isNextToStartPoint;

        static private double idCounter = 0;

        public static double IDcounter { get => idCounter; set => idCounter = value; }
        
        public bool IsNextToStartPoint
        { 
            get => isNextToStartPoint; 
            set => isNextToStartPoint = value; 
        }
        public Window InterpolationWindow { get => interpolationWindow; set => interpolationWindow = value; }
        public UndoRedoStack UndoRedoStack { get => undoRedoStack; set => undoRedoStack = value; }

        public Utilities(MainControl control, MainHandler handler, CreationInformation creationInfos, EditInformations editInfos, DataGridChecker dataGridChecker, IDrawUnit polygonDrawUnit)
        {
            this.handler = handler;
            this.control = control;
            this.editInfos = editInfos;
            this.creationInfos = creationInfos;
            this.dataGridChecker = dataGridChecker;
            this.polygonDrawUnit = polygonDrawUnit;
            this.isNextToStartPoint = false;
        }

        public void enableOrDisableControls(bool enable)
        {
            enableOrDisablePolygonListControl(enable);
            control.annoListControl.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
        }

        public void enableOrDisableAllControls(bool enable)
        {
            control.annoListControl.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
            control.polygonListControl.IsEnabled = enable;
        }

        public void enableOrDisablePolygonListControl(bool enable)
        {
            control.polygonListControl.polygonSelectAllButton.IsEnabled = enable;
            control.polygonListControl.polygonSetDefaultLabelButton.IsEnabled = enable;
            control.polygonListControl.polygonCopyButton.IsEnabled = enable;
            control.polygonListControl.polygonRelabelButton.IsEnabled = enable;
            control.polygonListControl.editTextBox.IsEnabled = enable;
            control.polygonListControl.editComboBox.IsEnabled = enable;
            control.polygonListControl.polygonDataGrid.IsEnabled = enable;
            control.polygonListControl.interpolateLabels.IsEnabled = enable;

            if(enable)
            {
                control.polygonListControl.addLabels.Visibility = Visibility.Visible;
                control.polygonListControl.stopInsertion.Visibility = Visibility.Hidden;
            }
            else
            {
                control.polygonListControl.addLabels.Visibility = Visibility.Hidden;
                control.polygonListControl.stopInsertion.Visibility = Visibility.Visible;
            }
        }
        

        public void enableOrDisableControlButtons(bool enable, bool withInterpolationButton = false)
        {
            control.polygonListControl.polygonSelectAllButton.IsEnabled = enable;
            control.polygonListControl.polygonSetDefaultLabelButton.IsEnabled = enable;
            control.polygonListControl.polygonCopyButton.IsEnabled = enable;
            control.polygonListControl.polygonRelabelButton.IsEnabled = enable;
            control.polygonListControl.editTextBox.IsEnabled = enable;
            control.polygonListControl.editComboBox.IsEnabled = enable;
            control.polygonListControl.addLabels.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
            if (withInterpolationButton)
                control.polygonListControl.interpolateLabels.IsEnabled = enable;
        }



        public void endCreationMode(AnnoListItem item, PolygonLabel currentPolygonLabel = null)
        {
            creationInfos.IsPolylineToDraw = false;
            creationInfos.IsCreateModeOn = false;

            this.isNextToStartPoint = false;
            this.enableOrDisableControls(true);
            this.polygonSelectItem(item);
            this.control.Cursor = Cursors.Arrow;

            if (creationInfos.LastSelectedLabel != null)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = creationInfos.LastSelectedLabel;
                creationInfos.LastSelectedLabel = null;
            }
        }

        public bool isMouseWithinPolygonArea(double x, double y)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;

            var contains = false;
            if (polygonLabel != null)
            {
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

                    if (contains)
                        editInfos.StartPosition = new Point(x, y);
                }
            }
            
            return contains;
        }

        public bool isMouseAbovePoint(double x, double y)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;

            if (polygonLabel != null)
            {
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
            
            foreach (PolygonLabel polygon in item.PolygonList.getRealList())
            {
                foreach (PolygonPoint polygonPoint in polygon.Polygon)
                {
                    if (polygonPoint.Equals(editInfos.SelectedPolygonPoint))
                    {
                        if (x > editInfos.ImageWidth)
                            polygonPoint.X = editInfos.ImageWidth;
                        else if (x < 0)
                            polygonPoint.X = 0;
                        else
                            polygonPoint.X = x;

                        if (y > editInfos.ImageHeight)
                            polygonPoint.Y = editInfos.ImageHeight;
                        else if (y < 0)
                            polygonPoint.Y = 0;
                        else
                            polygonPoint.Y = y;
                    }
                }
            }
        }

        public void updatePolygon(double x, double y, AnnoListItem item)
        {
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;

            Tuple<bool, bool> rightRangeTuple = isPolygonInTheRightRange(polygonLabel.Polygon, x, y);
            foreach (PolygonPoint polygonPoint in polygonLabel.Polygon)
            {
                if(rightRangeTuple.Item1)
                    polygonPoint.X += x - editInfos.StartPosition.X;
                if(rightRangeTuple.Item2)
                    polygonPoint.Y += y - editInfos.StartPosition.Y;
            }

            editInfos.StartPosition = new Point(x, y);
        }

        private Tuple<bool, bool> isPolygonInTheRightRange(List<PolygonPoint> polygon, double x, double y)
        {
            bool x_okay = true;
            bool y_okay = true;
            foreach (PolygonPoint point in polygon)
            {
                double newX = point.X + x - editInfos.StartPosition.X;
                double newY = point.Y + y - editInfos.StartPosition.Y;

                double MIN_WIDTH = 0;
                double MAX_WIDTH = editInfos.ImageWidth;
                double MIN_HEIGHT = 0;
                double MAX_HEIGHT = editInfos.ImageHeight;

                if (newX < MIN_WIDTH || newX > MAX_WIDTH)
                    x_okay = false;
                if (newY < MIN_HEIGHT || newY > MAX_HEIGHT)
                    y_okay = false;
            }


            return new Tuple<bool, bool>(x_okay, y_okay);
        }

        public void updateImageSize()
        {
            IMedia video = MainHandler.mediaList.GetFirstVideo();

            if (video != null)
            {
                Tuple<int, int> imageSize = video.GetImageSize();
                editInfos.ImageWidth = (double)imageSize.Item1;
                editInfos.ImageHeight = (double)imageSize.Item2 - 5;
            }
        }

        public bool posIs5pxFromBottomAway(double y_val)
        {
            return editInfos.ImageHeight > y_val;
        }

        public void editPolygon(double x, double y)
        {
            if (editInfos.IsAboveSelectedPolygonLineAndCtrlPressed)
            {
                PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                this.UndoRedoStack.Do(new AddOrRemoveExtraPolygonPointCommand(editInfos.LastPolygonPoint.PointID, new PolygonPoint(x, y), polygonLabel, TYPE.EXTRA_POINT));
                polygonDrawUnit.polygonOverlayUpdate(((AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem));
            }
            else
            {
                if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                {
                    enableOrDisableAllControls(false);
                    PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                    List<Point> polygonPoints = new List<Point>();

                    foreach (PolygonPoint point in polygonLabel.Polygon)
                    {
                        polygonPoints.Add(new Point(point.X, point.Y));
                    }

                    editInfos.PolygonStartPosition = polygonPoints;
                    control.Cursor = Cursors.SizeAll;
                }
            }
        }

        public bool addNewPoint(double x, double y)
        {
            if (control.polygonListControl.polygonDataGrid.SelectedItem == null)
                addNewLabel();

            creationInfos.IsPolylineToDraw = true;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;

            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;

            if (polygonLabel.Polygon == null)
                return false;

            if (polygonLabel.Polygon.Count > 2 && this.IsNextToStartPoint)
            {
                // We cannot creat complex polygons
                if (isPolygonComplex(polygonLabel.Polygon))
                {
                    MessageBox.Show("You are trying to create a complex polygon. Complex polygons cannot be used for image segmentation in Nova. Edit the current polygon (\"ctrl + z\" for undo) or create a new one (with \"ESC\"). ", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                polygonTableUpdate();
                item.updateLabelCount();
                refreshAnnoDataGrid();
                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                this.UndoRedoStack.Do(new AddOrRemovePolygonPointCommand(new PolygonPoint(-1, -1), polygonLabel, TYPE.CREATION));

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
                    this.UndoRedoStack.Do(new AddOrRemovePolygonPointCommand(polygonPoint, polygonLabel, TYPE.CREATION));
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

        private void addNewLabel()
        {
            AnnoScheme scheme = AnnoTierStatic.Selected.AnnoList.Scheme;

            String name = scheme.DefaultLabel;
            Color color = scheme.DefaultColor;
            if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                name = handler.CurrentLabelName;
                color = handler.CurrentLabelColor;
            }

            PolygonLabel polygonLabel = new PolygonLabel(null, name, color);
            this.addPolygonLabelToPolygonList(polygonLabel);
        }

        private bool isPolygonComplex(List<PolygonPoint> polygon)
        {
            PolygonPoint interval1Point1 = new PolygonPoint(0,0);
            PolygonPoint interval1Point2 = new PolygonPoint(0, 0);
            PolygonPoint interval2Point1 = new PolygonPoint(0, 0);
            PolygonPoint interval2Point2 = new PolygonPoint(0, 0);

            PolygonPoint interval1veryFirst = new PolygonPoint(0, 0);
            PolygonPoint interval1veryLast = new PolygonPoint(0, 0);
            PolygonPoint interval2veryFirst = new PolygonPoint(0, 0);
            PolygonPoint interval2veryLast = new PolygonPoint(0, 0);

            bool interval1firstSet = false;
            bool interval2firstSet = false;

            foreach (PolygonPoint point1 in polygon)
            {
                if (!interval1firstSet)
                {
                    interval1firstSet = true;
                    interval1Point1 = point1;
                    interval1veryFirst = point1;
                    continue;
                }

                interval1veryLast = point1;
                interval1Point2 = interval1Point1;
                interval1Point1 = point1;

                foreach (PolygonPoint point2 in polygon)
                {
                    if (!interval2firstSet)
                    {
                        interval2firstSet = true;
                        interval2Point1 = point2;
                        interval2veryFirst = point2;
                        continue;
                    }

                    interval2veryLast = point2;
                    interval2Point2 = interval2Point1;
                    interval2Point1 = point2;
                    if(!tupleAreEqual(interval1Point1, interval1Point2, interval2Point1, interval2Point2))
                        if(intervalsAreCutting(interval1Point1, interval1Point2, interval2Point1, interval2Point2))
                        {
                            return true;
                        }
                }

                if (!tupleAreEqual(interval1Point1, interval1Point2, interval2veryFirst, interval2veryLast))
                    if (intervalsAreCutting(interval1Point1, interval1Point2, interval2veryFirst, interval2veryLast))
                    {
                        return true;
                    }

                interval2firstSet = false;
            }

            foreach (PolygonPoint point2 in polygon)
            {
                if (!interval2firstSet)
                {
                    interval2firstSet = true;
                    interval2Point1 = point2;
                    interval2veryFirst = point2;
                    continue;
                }

                interval2veryLast = point2;
                interval2Point2 = interval1Point1;
                interval2Point1 = point2;
                if (!tupleAreEqual(interval1veryFirst, interval1veryLast, interval2Point1, interval2Point2))
                    if (intervalsAreCutting(interval1veryFirst, interval1veryLast, interval2Point1, interval2Point2))
                    {
                        return true;
                    }
            }

            if (!tupleAreEqual(interval1veryFirst, interval1veryLast, interval2veryFirst, interval2veryLast))
                if (intervalsAreCutting(interval1veryFirst, interval1veryLast, interval2veryFirst, interval2veryLast))
                {
                    return true;
                }

            return false;
        }

        private bool intervalsAreCutting(PolygonPoint A, PolygonPoint B, PolygonPoint C, PolygonPoint D)
        {
            if (ccw(A, C, D) != ccw(B, C, D) && 
                ccw(A, B, C) != ccw(A, B, D))
            {
                if(A != C && A != D && 
                   B != C && B != D)
                {
                    return true;
                }
                else if(intervalsHaveSameAngle(A, B, C, D))
                {
                    return true;
                }
            }

            return false; 
        }

        private bool intervalsHaveSameAngle(PolygonPoint interval1Point1, PolygonPoint interval1Point2, PolygonPoint interval2Point1, PolygonPoint interval2Point2)
        {
            double xDiff = interval1Point2.X - interval1Point1.X;
            double yDiff = interval1Point2.Y - interval1Point1.Y;
            double a1 = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;
            xDiff = interval2Point2.X - interval2Point1.X;
            yDiff = interval2Point2.Y - interval2Point1.Y;
            double a2 = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;

            return a2 == a1;
        }

        private bool ccw(PolygonPoint A, PolygonPoint B, PolygonPoint C)
        {
            return (C.Y - A.Y) * (B.X - A.X) > (B.Y - A.Y) * (C.X - A.X);
        }


        private bool tupleAreEqual(PolygonPoint interval1Point1, PolygonPoint interval1Point2, PolygonPoint interval2Point1, PolygonPoint interval2Point2)
        {
            if (interval1Point1 == interval2Point1 && interval1Point2 == interval2Point2)
                return true;

            if (interval1Point1 == interval2Point2 && interval1Point2 == interval2Point1)
                return true;

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
            if(control.polygonListControl.polygonDataGrid.Items.Count > 0)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = control.polygonListControl.polygonDataGrid.Items[control.polygonListControl.polygonDataGrid.Items.Count - 1];
            }
            polygonDrawUnit.polygonOverlayUpdate(item);
        }

        public void activateEditMode()
        {
            editInfos.IsEditModeOn = true;
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
                PolygonLabel label = this.UndoRedoStack.Undo();

                while (label.Polygon.Count > 0)
                {
                    label = this.UndoRedoStack.Undo();
                }

                item.PolygonList.removeExplicitPolygon(label);
                item.updateLabelCount();
                refreshAnnoDataGrid();
                polygonSelectItem(item);
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                creationInfos.IsPolylineToDraw = false;
            }
            else
            {
                endCreationMode(item, currentPolygonLabel);
            }
        }

        public void changeSelection(int newSelectedAnnoListItemIndex, AnnoListItem newSelectedAnnoListItem, PolygonLabel newSelectedPolygonLabel)
        {
            control.annoListControl.annoDataGrid.SelectedItem = newSelectedAnnoListItem;
            handler.jumpToGeometric(newSelectedAnnoListItemIndex);
            control.polygonListControl.polygonDataGrid.SelectedItem = newSelectedPolygonLabel;
        }

        public int getCurrentAnnoListIndex()
        {
            return control.annoListControl.annoDataGrid.SelectedIndex;
        }

        public int getCurrentLabelIndex()
        {
            return control.polygonListControl.polygonDataGrid.SelectedIndex;
        }

        public AnnoList getCurrentAnnoList()
        {
            return (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
        }

        public void setInterpolationWindowClosed()
        {
            this.interpolationWindow = null;
        }

        public bool updateFrameData(PolygonLabel pl)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsOne())
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
                List<PolygonLabel> polygonLabels = item.PolygonList.Polygons;

                for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                {
                    PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i];

                    for (int j = 0; j < polygonLabels.Count; j++)
                    {
                        if (polygonLabels[j].ID == polygonLabel.ID)
                        {
                            polygonLabels[j].Color = pl.Color;
                            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                            break;
                        }
                    }
                }
                item.PolygonList.Polygons = polygonLabels;
                this.polygonSelectItem(item);
                return true;
            }

            MessageBox.Show("It was not possible to update the color\n Make sure you have selected a Frame", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        public void selectSpecificLabel(PolygonLabel pl)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsNotZero() && this.labelIsNotSelected(pl))
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = pl;
            }
        }

        public bool targetPositionEqualsSource(List<Point> start, List<Point> target)
        {
            if (start is null || target is null)
                return true;

            for (int i = 0; i < start.Count; i++)
            {
                if (start[i].X != target[i].X || start[i].Y != target[i].Y)
                    return false;
            }
            return true;
        }

        public void undoColorChanges(PolygonLabel updatedLabel, System.Windows.Media.Color oldColor)
        {
            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                PolygonLabel polygonLabel = control.polygonListControl.polygonDataGrid.SelectedItems[i] as PolygonLabel;
                if (polygonLabel != null && polygonLabel.ID == updatedLabel.ID)
                {
                    polygonLabel.Color = oldColor;
                    control.polygonListControl.polygonDataGrid.UpdateLayout();
                    control.polygonListControl.polygonDataGrid.Items.Refresh();
                    return;
                }
            }
        }


        //TODO überarbeiten -> Mause ist bei Linien die im winkel von 90 grad verlaufen nie auf der Linie
        public bool mouseIsOnLine()
        {
            const double EPSILON = 3;
            Point currentPoint = creationInfos.LastKnownPoint;
            PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;

            if(polygonLabel == null)
            {
                return false;
            }

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

        public void handle2DInterpolation(ComboboxItem targetFrame, ComboboxItem sourceFrame, PolygonLabel sourceLabel, PolygonLabel targetLabel, double xStepPerFrame, double yStepPerFrame, int framesBetween)
        {
            AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
            list[sourceFrame.FrameIndex].PolygonList.removeExplicitPolygon(sourceLabel);
            list[targetFrame.FrameIndex].PolygonList.removeExplicitPolygon(targetLabel);
            list[sourceFrame.FrameIndex].PolygonList.removeExplicitPolygon(targetLabel);
            list[targetFrame.FrameIndex].PolygonList.removeExplicitPolygon(sourceLabel);


            int newLabelCounter = -1;
            foreach (AnnoListItem listItem in list)
            {
                if (listItem.Equals(list[sourceFrame.FrameIndex]))
                {
                    newLabelCounter++;
                }

                if (newLabelCounter >= 0)
                {
                    List<PolygonPoint> newPolygon = new List<PolygonPoint>();

                    foreach (PolygonPoint point in sourceLabel.Polygon)
                    {
                        newPolygon.Add(new PolygonPoint(point.X + (newLabelCounter * xStepPerFrame), point.Y + (newLabelCounter * yStepPerFrame)));
                    }

                    PolygonLabel newLabel = new PolygonLabel(newPolygon, sourceLabel.Label, sourceLabel.Color);
                    listItem.PolygonList.addPolygonLabel(newLabel);
                    listItem.updateLabelCount();
                    newLabelCounter++;

                    if (newLabelCounter == (framesBetween + 1))
                    {
                        listItem.PolygonList.removeExplicitPolygon(targetLabel);
                        this.refreshAnnoDataGrid();
                        listItem.updateLabelCount();
                        break;
                    }
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }

        public void handle3DInterpolation(List<double> xStepsPerFrame, List<double> yStepsPerFrame, ComboboxItem sourceFrame, PolygonLabel sourceLabel,
                                          PolygonLabel targetLabel, List<PointAngleTuple> sourcePointsAnglesTuple, List<PointAngleTuple> targetPointsAnglesTuple,
                                          int stepsFromSourceToTarget)
        {

            AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
            int newLabelCounter = 0;

            foreach (AnnoListItem listItem in list)
            {
                // We don't change the selected frame
                if (listItem.Equals(list[sourceFrame.FrameIndex]))
                {
                    newLabelCounter++;
                    continue;
                }

                if (newLabelCounter > 0)
                {
                    List<PolygonPoint> newPolygon = new List<PolygonPoint>();

                    int pointCounter = 0;
                    foreach (PointAngleTuple tuple in sourcePointsAnglesTuple)
                    {
                        newPolygon.Add(new PolygonPoint(tuple.point.X + (newLabelCounter * xStepsPerFrame[pointCounter]),
                                                        tuple.point.Y + (newLabelCounter * yStepsPerFrame[pointCounter])));
                        pointCounter++;
                    }

                    PolygonLabel newLabel = new PolygonLabel(newPolygon, sourceLabel.Label, sourceLabel.Color);
                    listItem.PolygonList.addPolygonLabel(newLabel);
                    listItem.updateLabelCount();
                    newLabelCounter++;

                    if (newLabelCounter == (stepsFromSourceToTarget))
                    {
                        this.refreshAnnoDataGrid();
                        break;
                    }
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }
    }
}

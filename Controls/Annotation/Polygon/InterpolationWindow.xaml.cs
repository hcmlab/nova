using ssi.Tools.Polygon_Helper;
using ssi.Types.Polygon;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    partial class InterpolationWindow : Window
    {
        private AnnoList list;
        private PolygonUtilities polygonUtilities;
        private UIElementsController uIElementsController;
        private PolygonHandlerPerformer polygonHandlerPerformer;
        private int lastInterpolatedSourceFrameIndex = -1;
        private PolygonLabel lastInterpolatedSourcePolygon = null;

        internal InterpolationWindow(PolygonUtilities polygonUtilities, UIElementsController uIElementsController, PolygonHandlerPerformer polygonHandlerPerformer)
        {   
            InitializeComponent();
            this.polygonUtilities = polygonUtilities;
            this.uIElementsController = uIElementsController;
            this.polygonHandlerPerformer = polygonHandlerPerformer;
            this.uIElementsController.enableOrDisableControlsBesidesPolygon(false);
            this.uIElementsController.enableOrDisablePolygonControlElements(false, false);
            this.updateItmes();

            this.setStatus();

            int currentIndex = polygonUtilities.getCurrentAnnoListIndex();
            TargetFrames.SelectedIndex = currentIndex;
            SourceFrames.SelectedIndex = currentIndex;
            
            if (SourceFrames.SelectedItem != null)
                SourceLabelsListBox.SelectedIndex = polygonUtilities.getCurrentLabelIndex();

        }

        public void updateItmes()
        {
            this.list = polygonUtilities.getCurrentAnnoList();
            int sourceFrameIndex = SourceFrames.SelectedIndex;
            int targetFrameIndex = TargetFrames.SelectedIndex;
            int sourceLabelIndex = SourceLabelsListBox.SelectedIndex;
            int targetLabelIndex = TargetLabelsListBox.SelectedIndex;
            SourceFrames.Items.Clear();
            TargetFrames.Items.Clear();
            int counter = 0;
            foreach (AnnoListItem item in this.list)
            {
                SourceFrames.Items.Add(new ComboboxItem(item.Label + " | Labels in frame: " + item.LabelCount, counter));
                TargetFrames.Items.Add(new ComboboxItem(item.Label + " | Labels in frame: " + item.LabelCount, counter));
                counter++;
            }
            SourceFrames.SelectedIndex = sourceFrameIndex;
            TargetFrames.SelectedIndex = targetFrameIndex;
            SourceLabelsListBox.SelectedIndex = sourceLabelIndex;
            TargetLabelsListBox.SelectedIndex = targetLabelIndex;

        }

        private void Interpolate_2D_Click(object sender, RoutedEventArgs e)
        {
            if(selectedLabelsCorrect())
            {
                PolygonLabel selectedSourcePolygon = (PolygonLabel)SourceLabelsListBox.SelectedValue;
                PolygonLabel selectedTargetPolygon = (PolygonLabel)TargetLabelsListBox.SelectedValue;

                if (selectedSourcePolygon.Polygon.Count < selectedTargetPolygon.Polygon.Count)
                {
                    PolygonLabel tmp = selectedSourcePolygon;
                    selectedSourcePolygon = selectedTargetPolygon;
                    selectedTargetPolygon = tmp;
                }

                double selectedSourcePolygonArea = calculatePolygonArea(selectedSourcePolygon.Polygon);
                double selectedTargetPolygonArea = calculatePolygonArea(selectedTargetPolygon.Polygon);

                Point midPointSource = calculatePolygonMidPoint(selectedSourcePolygon.Polygon, selectedSourcePolygonArea);
                Point midPointTarget = calculatePolygonMidPoint(selectedTargetPolygon.Polygon, selectedTargetPolygonArea);

                double xDif = midPointTarget.X - midPointSource.X;
                double yDif = midPointTarget.Y - midPointSource.Y;

                int framesBetween = TargetFrames.SelectedIndex - SourceFrames.SelectedIndex;

                double xStepPerFrame = xDif / framesBetween;
                double yStepPerFrame = yDif / framesBetween;

                polygonHandlerPerformer.handle2DInterpolation((ComboboxItem)TargetFrames.SelectedItem, (ComboboxItem)SourceFrames.SelectedItem, selectedSourcePolygon, selectedTargetPolygon, xStepPerFrame, yStepPerFrame, framesBetween);
                updateItmes();
                this.lastInterpolatedSourceFrameIndex = SourceFrames.SelectedIndex;
                this.lastInterpolatedSourcePolygon = (PolygonLabel)SourceLabelsListBox.SelectedValue;
                this.setStatus();
            }
        }

        private void Interpolate_3D_Click(object sender, RoutedEventArgs e)
        {
            if (selectedLabelsCorrect())
            {
                PolygonLabel selectedSourcePolygon = (PolygonLabel)SourceLabelsListBox.SelectedValue;
                PolygonLabel selectedTargetPolygon = (PolygonLabel)TargetLabelsListBox.SelectedValue;

                // Step 1: Calculation for the midpoints
                double selectedSourcePolygonArea = calculatePolygonArea(selectedSourcePolygon.Polygon);
                double selectedTargetPolygonArea = calculatePolygonArea(selectedTargetPolygon.Polygon);
                Point midPointSource = calculatePolygonMidPoint(selectedSourcePolygon.Polygon, selectedSourcePolygonArea);
                Point midPointTarget = calculatePolygonMidPoint(selectedTargetPolygon.Polygon, selectedTargetPolygonArea);

                // Step 2: Change coordination system from image to math 
                Point negativeMidPointSource = new Point(midPointSource.X, -midPointSource.Y);
                Point negativeMidPointTarget = new Point(midPointTarget.X, -midPointTarget.Y);

                List<PolygonPoint> newNegativeSourcePolygon = selectedSourcePolygon.getPolygonAsCopy();
                List<PolygonPoint> newNegativeTargetPolygon = selectedTargetPolygon.getPolygonAsCopy();

                for (int i = 0; i < newNegativeSourcePolygon.Count; i++)
                {
                    newNegativeSourcePolygon[i].Y *= -1;
                }

                for (int i = 0; i < newNegativeTargetPolygon.Count; i++)
                {
                    newNegativeTargetPolygon[i].Y *= -1;
                }

                // Step 3: Add points of the source polygon, to the target polygon and and vice versa 
                double angleOfPointToAdd;
                Point pointToAdd = new Point();
                foreach (PolygonPoint point in selectedSourcePolygon.Polygon)
                {
                    pointToAdd = new Point(point.X, -point.Y);
                    angleOfPointToAdd = calculateAngle(negativeMidPointSource, pointToAdd);
                    newNegativeTargetPolygon = addPoint(angleOfPointToAdd, negativeMidPointTarget, newNegativeTargetPolygon);
                }

                foreach (PolygonPoint point in selectedTargetPolygon.Polygon)
                {
                    pointToAdd = new Point(point.X, -point.Y);
                    angleOfPointToAdd = calculateAngle(negativeMidPointTarget, pointToAdd);
                    newNegativeSourcePolygon = addPoint(angleOfPointToAdd, negativeMidPointSource, newNegativeSourcePolygon);
                }

                // 4. Check the results: we must have two polygons with the same amount of points and each angle of a point must be in both polygons 
                List<PointAngleTuple> sourcePointsAnglesTuple = new List<PointAngleTuple>();
                List<PointAngleTuple> targetPointsAnglesTuple = new List<PointAngleTuple>();

                foreach (PolygonPoint point in newNegativeSourcePolygon)
                {
                    sourcePointsAnglesTuple.Add(new PointAngleTuple(new PolygonPoint(point.X, -point.Y), Math.Round(calculateAngle(negativeMidPointSource, getMathPoint(point)), 2)));
                }

                foreach (PolygonPoint point in newNegativeTargetPolygon)
                {
                    targetPointsAnglesTuple.Add(new PointAngleTuple(new PolygonPoint(point.X, -point.Y), Math.Round(calculateAngle(negativeMidPointTarget, getMathPoint(point)), 2)));
                }

                sourcePointsAnglesTuple.Sort(delegate (PointAngleTuple first, PointAngleTuple second) { return first.angle.CompareTo(second.angle); });
                targetPointsAnglesTuple.Sort(delegate (PointAngleTuple first, PointAngleTuple second) { return first.angle.CompareTo(second.angle); });

                if (sourcePointsAnglesTuple.Count != targetPointsAnglesTuple.Count)
                {
                    MessageBox.Show("Not able to interpolate with the selected values!", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                for (int i = 0; i < sourcePointsAnglesTuple.Count; i++)
                {
                    if (Math.Truncate(sourcePointsAnglesTuple[i].angle) != Math.Truncate(targetPointsAnglesTuple[i].angle))
                    {
                        sourcePointsAnglesTuple.RemoveAt(i);
                        targetPointsAnglesTuple.RemoveAt(i);
                    }
                }

                int stepsFromSourceToTarget = TargetFrames.SelectedIndex - SourceFrames.SelectedIndex;
                List<double> xStepsPerFrame = new List<double>();
                List<double> yStepsPerFrame = new List<double>();

                for (int i = 0; i < sourcePointsAnglesTuple.Count; i++)
                {
                    xStepsPerFrame.Add((targetPointsAnglesTuple[i].point.X - sourcePointsAnglesTuple[i].point.X) / stepsFromSourceToTarget);
                    yStepsPerFrame.Add((targetPointsAnglesTuple[i].point.Y - sourcePointsAnglesTuple[i].point.Y) / stepsFromSourceToTarget);
                }

                polygonHandlerPerformer.handle3DInterpolation(xStepsPerFrame, yStepsPerFrame, (ComboboxItem)SourceFrames.SelectedItem, (PolygonLabel)SourceLabelsListBox.SelectedValue, (PolygonLabel)TargetLabelsListBox.SelectedValue, sourcePointsAnglesTuple, targetPointsAnglesTuple, stepsFromSourceToTarget);
                updateItmes();
                this.lastInterpolatedSourceFrameIndex = SourceFrames.SelectedIndex;
                this.lastInterpolatedSourcePolygon = (PolygonLabel)SourceLabelsListBox.SelectedValue;
                this.setStatus();
            }
        }

        private bool selectedLabelsCorrect()
        {
            if(!(SourceFrames.SelectedIndex + 1 < TargetFrames.SelectedIndex && SourceLabelsListBox.SelectedValue != null && TargetLabelsListBox.SelectedValue != null))
            {
                MessageBox.Show("Not able to interpolate with the selected values! \nMake sure your source frame is smaller than your target frame \nand it is at least one frame between them. ", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            
            if(((PolygonLabel)SourceLabelsListBox.SelectedValue).Label != ((PolygonLabel)TargetLabelsListBox.SelectedValue).Label)
            {
                MessageBox.Show("You can just interpolate between same labels.", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                return false;
            }
            
            if(this.lastInterpolatedSourceFrameIndex == SourceFrames.SelectedIndex && this.lastInterpolatedSourcePolygon == (PolygonLabel)SourceLabelsListBox.SelectedValue)
            {
                MessageBoxResult answer = MessageBox.Show("You try to interpolate with the same values the second time.  You possibly add \nsimilar labels. Are you sure you want to do this? ", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Information);
                if (answer == MessageBoxResult.No)
                {
                    return false;
                }
            }

            return true;
        }


        private List<PolygonPoint> addPointToPolygonAfterSpecificPosition(PolygonPoint newPoint, PolygonPoint posPoint, List<PolygonPoint> polygon)
        {
            List<PolygonPoint> newPolygon = new List<PolygonPoint>();
            bool pointSet = false; 
            foreach (PolygonPoint polygonPoint in polygon)
            {
                newPolygon.Add(polygonPoint);
                
                if (posPoint.isOnTheSameSpot(polygonPoint) && pointSet == false)
                {
                    pointSet = true;
                    newPolygon.Add(newPoint);
                }
            }

            return newPolygon;
        }

        private List<PolygonPoint> addPoint(double angleOfPointToAdd, Point midPointNew, List<PolygonPoint> listToBeAttached)
        {
            // The new point is needed to create a line (which cut the given mid point and the new point).
            // This line will cut another line which is spanned by two points from the given polygon.
            // If the intersection point is within the section of the polygon points, we will add it to the polygon.
            // We must always find a place in the given polyogn where we add a new point.
            Point newPoint = new Point(Math.Round(midPointNew.X + Math.Cos(AngleToRadians(angleOfPointToAdd)), 6),
                                       Math.Round(midPointNew.Y + Math.Sin(AngleToRadians(angleOfPointToAdd)), 6));
            
            //Change the type of the points
            List<Point> listToBeAttachedWithMathPoints = listToBeAttached.ConvertAll(point => getMathPoint(point));
            Point first = listToBeAttachedWithMathPoints[0];
            Point second = new Point(0, 0);
            Point veryFirst = listToBeAttachedWithMathPoints[0];
            Point veryLast = listToBeAttachedWithMathPoints[listToBeAttached.Count - 1];
            Point intersectionPoint = new Point(0, 0);

            foreach (Point currentPoint in listToBeAttachedWithMathPoints)
            {
                if (currentPoint.Equals(first))
                {
                    continue;
                }

                second = first;
                first = currentPoint;
                
                intersectionPoint = getIntersectionPoint(newPoint, midPointNew, first, second);
                if (isPointWithinSection(midPointNew, angleOfPointToAdd, intersectionPoint, first, second))
                {
                    return addPointToPolygonAfterSpecificPosition(getPolygonPoint(intersectionPoint), getPolygonPoint(second), listToBeAttached);
                }
            }

            // Check the first and the last point too
            intersectionPoint = getIntersectionPoint(newPoint, midPointNew, veryLast, veryFirst);
            if (isPointWithinSection(midPointNew, angleOfPointToAdd, intersectionPoint, veryLast, veryFirst))
            {
                return addPointToPolygonAfterSpecificPosition(getPolygonPoint(intersectionPoint), getPolygonPoint(veryLast), listToBeAttached);
            }

            return listToBeAttached;
        }

        private double AngleToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        private Point getMathPoint(PolygonPoint point)
        {
            return new Point(point.X, point.Y);
        }

        private PolygonPoint getPolygonPoint(Point point)
        {
            return new PolygonPoint(point.X, point.Y);
        }

        // Checks whether the point is between A and B and also whether the angle of the point is between the angles of A and B
        private bool isPointWithinSection(Point midPoint, double angleOfPointToAdd, Point intersectionPoint, Point p1, Point p2)
        {
            intersectionPoint = truncPoint(intersectionPoint);
            p1 = truncPoint(p1);
            p2 = truncPoint(p2);

            double x_start = p1.X <= p2.X ? p1.X : p2.X;
            double y_start = p1.Y <= p2.Y ? p1.Y : p2.Y;
            double x_end = p1.X > p2.X ? p1.X : p2.X;
            double y_end = p1.Y > p2.Y ? p1.Y : p2.Y;

            if (intersectionPoint.X < x_start || intersectionPoint.X > x_end)
                return false;
            if (intersectionPoint.Y < y_start || intersectionPoint.Y > y_end)
                return false;

            // We look down, the y coordinate must be equal or lower then the point, from where we look (midpoint)
            if(angleOfPointToAdd >= 180 && !(intersectionPoint.Y <= midPoint.Y))
            {
                 return false;
            }

            // We look up, so the y coordinate must be higher then the point, from where we look (midpoint)
            if (angleOfPointToAdd < 180 && !(intersectionPoint.Y >= midPoint.Y))
            {
                return false;
            }

            // We look to the right, so the x coordinate must be higher then the point, from where we look (midpoint)
            if ((angleOfPointToAdd <= 90 || angleOfPointToAdd >= 270) && !(intersectionPoint.X >= midPoint.X))
            {
                return false;
            }

            // We look to the left, so the x coordinate must be lower then the point, from where we look (midpoint)
            if (angleOfPointToAdd > 90 && angleOfPointToAdd < 270 && !(intersectionPoint.X <= midPoint.X))
            {
                return false;
            }

            return true;
        }

        private Point truncPoint(Point input)
        {
            return new Point(Math.Truncate(100 * input.X) / 100, Math.Truncate(100 * input.Y) / 100);
        }

        private Point getIntersectionPoint(Point A, Point B, Point C, Point D)
        {
            double a1 = B.Y - A.Y;
            double b1 = A.X - B.X;
            double c1 = a1 * (A.X) + b1 * (A.Y);

            double a2 = D.Y - C.Y;
            double b2 = C.X - D.X;
            double c2 = a2 * (C.X) + b2 * (C.Y);

            double determinant = a1 * b2 - a2 * b1;

            if (determinant == 0)
            {
                // The lines are parallel. This is simplified 
                return new Point(double.MaxValue, double.MaxValue);
            }
            else
            {
                double x = (b2 * c1 - b1 * c2) / determinant;
                double y = (a1 * c2 - a2 * c1) / determinant;
                return new Point(Math.Round(x, 6), Math.Round(y, 6));
            }
        }

        private double calculateAngle(Point midPoint, Point point)
        {
            midPoint.Y = -1 * midPoint.Y;
            point.Y = -1 * point.Y;

            point.X -= midPoint.X;
            point.Y -= midPoint.Y;
            point.Y *= -1;

            point.X = Math.Round(point.X, 6);
            point.Y = Math.Round(point.Y, 6);

            double angle = (180 / Math.PI) * (Math.Atan2(point.Y, point.X));

            if (angle < 0)
            {
                angle += 360;
            }

            return Math.Round(angle, 6);
        }


        private double calculatePolygonArea(List<PolygonPoint> vertices)
        {
            double sum = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (i == vertices.Count - 1)
                {
                    sum += vertices[i].X * vertices[0].Y - vertices[0].X * vertices[i].Y;
                }
                else
                {
                    sum += vertices[i].X * vertices[i + 1].Y - vertices[i + 1].X * vertices[i].Y;
                }
            }

            double area = 0.5 * Math.Abs(sum);
            return area;
        }

        private Point calculatePolygonMidPoint(List<PolygonPoint> vertices, double area)
        {
            double Cx = 0;
            double Cy = 0;
            double tmp = 0;
            int k;

            for (int i = 0; i < vertices.Count; i++)
            {
                k = (i + 1) % (vertices.Count);
                tmp = vertices[i].X * vertices[k].Y - vertices[k].X * vertices[i].Y;
                Cx += (vertices[i].X + vertices[k].X) * tmp;
                Cy += (vertices[i].Y + vertices[k].Y) * tmp;
            }
            Cx /= (6.0 * area);
            Cy /= (6.0 * area);

            if (Cx < 0)
            {
                Cx *= -1;
                Cy *= -1;
            }

            return new Point(Math.Round(Cx, 6), Math.Round(Cy, 6));
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void sourceFrameChanged(object sender, RoutedEventArgs e)
        {
            if(SourceFrames.SelectedItem != null)
            {
                this.setStatus();

                ObservableCollection<PolygonLabel> items = new ObservableCollection<PolygonLabel>();
                PolygonList polygonList = this.list[((ComboboxItem)SourceFrames.SelectedValue).FrameIndex].PolygonList;
                foreach (PolygonLabel label in polygonList.Polygons)
                {
                    items.Add(label);
                }
                SourceLabelsListBox.ItemsSource = items;
                this.SourceLabelsListBox.SelectedIndex = 0;
            }
        }

        private void SourceLabelsListBox_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            sourceLabelChanged(sender, null);
        }

        private void SourceLabelsListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            sourceLabelChanged(sender, null);
        }

        private void sourceLabelChanged(object sender, RoutedEventArgs e)
        {
            if(SourceLabelsListBox.SelectedValue != null)
            {
                this.setStatus();

                AnnoListItem newSelectedAnnoListItem = this.list[((ComboboxItem)SourceFrames.SelectedValue).FrameIndex];
                int newSelectedAnnoListItemIndex = SourceFrames.SelectedIndex;
                PolygonLabel newSelectedPolygonLabel = (PolygonLabel)SourceLabelsListBox.SelectedItem;

                this.polygonUtilities.changeSelection(newSelectedAnnoListItemIndex, newSelectedAnnoListItem, newSelectedPolygonLabel);
            }
        }

        private void targetFrameChanged(object sender, RoutedEventArgs e)
        {
            if(TargetFrames.SelectedItem != null)
            {
                this.setStatus();

                ObservableCollection<PolygonLabel> items = new ObservableCollection<PolygonLabel>();
                PolygonList polygonList = this.list[((ComboboxItem)TargetFrames.SelectedValue).FrameIndex].PolygonList;
                foreach (PolygonLabel label in polygonList.Polygons)
                {
                    items.Add(label);
                }
                TargetLabelsListBox.ItemsSource = items;

                this.TargetLabelsListBox.SelectedIndex = 0;
            }
        }

        private void TargetLabelsListBox_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            targetLabelChanged(sender, null);
        }

        private void TargetLabelsListBox_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            targetLabelChanged(sender, null);
        }

        private void targetLabelChanged(object sender, RoutedEventArgs e)
        {
            if(TargetLabelsListBox.SelectedItem != null)
            {
                this.setStatus();

                AnnoListItem newSelectedAnnoListItem = this.list[((ComboboxItem)TargetFrames.SelectedValue).FrameIndex];
                int newSelectedAnnoListItemIndex = TargetFrames.SelectedIndex;
                PolygonLabel newSelectedPolygonLabel = (PolygonLabel)TargetLabelsListBox.SelectedItem;

                this.polygonUtilities.changeSelection(newSelectedAnnoListItemIndex, newSelectedAnnoListItem, newSelectedPolygonLabel);
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            this.uIElementsController.enableOrDisablePolygonControlElements(true, true);
            uIElementsController.enableOrDisableControlsBesidesPolygon(true);
            this.polygonUtilities.InterpolationWindow = null;
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Escape))
            {
                this.Close();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
           
            int indexSource = SourceFrames.SelectedIndex;
            int indexTarget = TargetFrames.SelectedIndex;
            
            if (indexSource >= indexTarget)
                return;

            // Just in case we start with the first frame, we have to add an extra value to the target frame
            int plus = indexSource == 0 ? 1 : 0;
            int dif = indexTarget - indexSource;
            SourceFrames.SelectedIndex += dif;
            TargetFrames.SelectedIndex += dif + plus;
        }

        private void setStatus()
        {
            if (this.lastInterpolatedSourceFrameIndex == SourceFrames.SelectedIndex)
            {
                if(this.lastInterpolatedSourcePolygon != null)
                    if(this.lastInterpolatedSourcePolygon.Equals((PolygonLabel)SourceLabelsListBox.SelectedValue))
                    {
                        this.Status_Label.Foreground = Brushes.Green;
                        this.Status_Label.Content = "Interpolation done!";
                    }
            }
            else
            {
                this.Status_Label.Foreground = Brushes.Red;
                this.Status_Label.Content = "Interpolation not done...";
            }
        }
    }

    public class ComboboxItem
    {
        public string Text { get; set; }
        public int FrameIndex { get; set; }

        public ComboboxItem(string text, int frameIndex)
        {
            Text = text;
            FrameIndex = frameIndex;
        }

        public override string ToString()
        {
            return Text;
        }
    }

    public class PointAngleTuple
    {
        public PolygonPoint point { get; set; }
        public double angle { get; set; }

        public PointAngleTuple(PolygonPoint point, double angle)
        {
            this.point = point;
            this.angle = angle;
        }

        public PointAngleTuple()
        {

        }
    }
}
using ssi.Interfaces;
using ssi.Types.Polygon;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace ssi.Tools.Polygon_Helper
{
    class PolygonEditor
    {
        private IDrawUnit drawUnit;
        private MainControl control;
        private PolygonInformations polygonInformations;
        private DataGridChecker dataGridChecker;
        private PolygonUtilities polygonUtilities;
        private UIElementsController uIElementsController;


        public PolygonEditor()
        {

        }

        public void setObjects(IDrawUnit drawUnit, MainControl control, PolygonInformations polygonInformations, PolygonUtilities polygonUtilities)
        {
            this.control = control;
            this.drawUnit = drawUnit;
            this.polygonInformations = polygonInformations;
            this.polygonUtilities = polygonUtilities;
            this.dataGridChecker = new DataGridChecker(control);
            this.uIElementsController = new UIElementsController(control);
        }

        public void addNewPointToDonePolygon(double x, double y)
        {
            PolygonLabel polygonLabel = polygonInformations.OverLinePolygon;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            item.UndoRedoStack.Do(new AddExtraPointCommand(polygonInformations.LastPolygonPoint.PointID, new PolygonPoint(x, y), polygonLabel));
            drawUnit.polygonOverlayUpdate(((AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem));
        }

        public void editPolygon(double x, double y)
        {
            uIElementsController.enableOrDisableControls(false);

            List<Point> polygonPoints = new List<Point>();
            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i];
                foreach (PolygonPoint point in polygonLabel.Polygon)
                {
                    polygonPoints.Add(new Point(point.X, point.Y));
                }
            }

            polygonInformations.setSelectedPolygonPointsAsDistanceToMouse(polygonPoints, x, y);
            polygonInformations.PolygonStartPosition = polygonPoints;
            control.Cursor = Cursors.SizeAll;
        }

        public bool updateLabelColor(PolygonLabel pl)
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
                polygonUtilities.polygonSelectItem(item);
                return true;
            }

            MessageBox.Show("It was not possible to update the color\n Make sure you have selected a Frame", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
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

        public void updatePolygon(double x, double y)
        {
            List<PolygonPoint> polygon = new List<PolygonPoint>();

            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i];
                polygon.AddRange(polygonLabel.Polygon);
            }

            if (polygon.Count == 0)
                return;

            int counter = 0;

            polygonInformations.PolygonPointsXandYDistances.ForEach(distances =>
            {
                polygon[counter].X = x + distances.Item1;
                polygon[counter].Y = y + distances.Item2;
                counter++;
            });

            int minXIndex = 0;
            int minYIndex = 0;
            int maxXIndex = 0;
            int maxYIndex = 0;

            for (int i = 1; i < polygon.Count; i++)
            {
                if (polygon[minXIndex].X > polygon[i].X)
                    minXIndex = i;
                if (polygon[minYIndex].Y > polygon[i].Y)
                    minYIndex = i;
                if (polygon[maxXIndex].X < polygon[i].X)
                    maxXIndex = i;
                if (polygon[maxYIndex].Y < polygon[i].Y)
                    maxYIndex = i;
            }

            double MIN_IMAGE_X = 0;
            double MAX_IMAGE_X = polygonInformations.ImageWidth;
            double MIN_IMAGE_Y = 0;
            double MAX_IMAGE_Y = polygonInformations.ImageHeight;

            if (MIN_IMAGE_X > polygon[minXIndex].X)
            {
                double x_dif = MIN_IMAGE_X - polygon[minXIndex].X;
                foreach(PolygonLabel label in control.polygonListControl.polygonDataGrid.SelectedItems)
                {
                    label.Polygon.ForEach(point => point.X += x_dif);
                }
            }

            if (MAX_IMAGE_X < polygon[maxXIndex].X)
            {
                double x_dif = MAX_IMAGE_X - polygon[maxXIndex].X;
                foreach (PolygonLabel label in control.polygonListControl.polygonDataGrid.SelectedItems)
                {
                    label.Polygon.ForEach(point => point.X += x_dif);
                }
            }

            if (MIN_IMAGE_Y > polygon[minYIndex].Y)
            {
                double y_dif = MIN_IMAGE_Y - polygon[minYIndex].Y;
                foreach (PolygonLabel label in control.polygonListControl.polygonDataGrid.SelectedItems)
                {
                    label.Polygon.ForEach(point => point.Y += y_dif);
                }
            }

            if (MAX_IMAGE_Y < polygon[maxYIndex].Y)
            {
                double y_dif = MAX_IMAGE_Y - polygon[maxYIndex].Y;
                foreach (PolygonLabel label in control.polygonListControl.polygonDataGrid.SelectedItems)
                {
                    label.Polygon.ForEach(point => point.Y += y_dif);
                }
            }

            polygonInformations.StartPosition = new Point(x, y);
            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
        }
    }
}
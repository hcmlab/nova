using ssi.Types.Polygon;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi.Tools.Polygon_Helper
{
    class LabelCreator
    {
        private MainHandler handler;
        private MainControl control;
        private DrawUtilities drawUtilities;
        private PolygonRevisor polygonRevisor;
        private DataGridChecker dataGridChecker;
        private CreationInformation creationInfos;
        private PolygonUtilities polygonUtilities;
        private UIElementsController uIElementsController;

        public LabelCreator()
        {

        }

        public void setObjects(MainControl control, MainHandler handler, DrawUtilities drawUtilities, CreationInformation creationInfos,
                            UIElementsController uIElementsController, DataGridChecker dataGridChecker, PolygonUtilities polygonUtilities)
        {
            this.control = control;
            this.handler = handler;
            this.drawUtilities = drawUtilities;
            this.creationInfos = creationInfos;
            this.dataGridChecker = dataGridChecker;
            this.polygonUtilities = polygonUtilities;
            this.polygonRevisor = new PolygonRevisor();
            this.uIElementsController = uIElementsController;
        }

        public void startCreationMode()
        {
            creationInfos.IsCreateModeOn = true;
            uIElementsController.enableOrDisableControlsBesidesPolygon(false);
            uIElementsController.enableOrDisablePolygonControlElements(false);
            uIElementsController.switchAddLabelButton();
            control.polygonListControl.stopInsertion.IsEnabled = true;
            control.polygonListControl.polygonDataGrid.SelectedItem = null;
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

        public void addPolygonLabelToPolygonList(PolygonLabel pl)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsZero() && dataGridChecker.polygonDGIsNotNullAndCountsZero())
                control.annoListControl.annoDataGrid.SelectedItem = control.annoListControl.annoDataGrid.Items[0];

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            item.PolygonList.addPolygonLabel(pl);
            polygonUtilities.polygonSelectItem(item);
            control.polygonListControl.polygonDataGrid.SelectedItem = pl;
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

            if (polygonLabel.Polygon.Count > 2 && drawUtilities.IsNextToStartPoint)
            {
                // We cannot creat complex polygons
                if (polygonRevisor.isPolygonComplex(polygonLabel.Polygon))
                {
                    MessageBox.Show("You are trying to create a complex polygon. Complex polygons cannot be used for image segmentation in Nova. Edit the current polygon (\"ctrl + z\" for undo) or create a new one (with \"ESC\"). ", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                    return false;
                }

                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                polygonUtilities.polygonTableUpdate();
                item.updateLabelCount();
                polygonUtilities.refreshAnnoDataGrid();
                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                item.UndoRedoStack.Do(new AddPointCommand(new PolygonPoint(-1, -1), polygonLabel));

                return true;
            }

            PolygonPoint polygonPoint = new PolygonPoint(x, y);
            PolygonLabel selectedItem = null;
            List<PolygonLabel> polygonLabels = item.PolygonList.getRealList();
            creationInfos.LastPrintedPoint = new Point(x, y);
            foreach (PolygonLabel label in polygonLabels)
            {
                if (label.ID == polygonLabel.ID)
                {
                    item.UndoRedoStack.Do(new AddPointCommand(polygonPoint, polygonLabel));
                    selectedItem = label;
                    break;
                }
            }

            item.PolygonList.Polygons = polygonLabels;

            polygonUtilities.polygonSelectItem(item);
            polygonUtilities.refreshAnnoDataGrid();
            control.polygonListControl.polygonDataGrid.SelectedItem = selectedItem;

            return false;
        }

        public void endCreationMode(AnnoListItem item, PolygonLabel currentPolygonLabel = null)
        {
            creationInfos.IsPolylineToDraw = false;
            creationInfos.IsCreateModeOn = false;

            drawUtilities.IsNextToStartPoint = false;
            uIElementsController.enableOrDisableControlsBesidesPolygon(true);
            uIElementsController.enableOrDisablePolygonControlElements(true);
            uIElementsController.switchAddLabelButton();
            polygonUtilities.polygonSelectItem(item);
            this.control.Cursor = Cursors.Arrow;

            if (creationInfos.LastSelectedLabel != null)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = creationInfos.LastSelectedLabel;
                creationInfos.LastSelectedLabel = null;
            }
        }

    }
}

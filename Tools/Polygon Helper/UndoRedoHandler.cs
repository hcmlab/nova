using ssi.Interfaces;
using ssi.Tools.Polygon_Helper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using static ssi.Types.Polygon.LabelInformations;

namespace ssi.Types.Polygon
{
    internal class UndoRedoHandler
    {
        private MainControl control;
        private PolygonInformations polygonInformations;
        private PolygonUtilities generalPolygonUtilities;
        private IDrawUnit polygonDrawUnit;
        private UIElementsController uIElementController;


        public UndoRedoHandler(MainControl control, PolygonInformations polygonInformations, PolygonUtilities generalPolygonUtilities, IDrawUnit polygonDrawUnit, UIElementsController uIElementController)
        {
            this.control = control;
            this.polygonInformations = polygonInformations;
            this.generalPolygonUtilities = generalPolygonUtilities;
            this.polygonDrawUnit = polygonDrawUnit;
            this.uIElementController = uIElementController;
        }

        public void handleUndo()
        {
            AnnoListItem item = null;
            if (control.annoListControl.annoDataGrid.SelectedItems.Count > 0)
            {
                item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
            }

            PolygonLabel label = null;

            PolygonLabel[] allLabels = item.UndoRedoStack.Undo();
            generalPolygonUtilities.polygonTableUpdate();

            if (allLabels != null && allLabels.Length > 0)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                label = allLabels[0];
                control.polygonListControl.polygonDataGrid.SelectedItems.Add(label);

                if (label.Informations.Type == TYPE.CREATION)
                {
                    // We draw the line to the mouse and select the label
                    if (label.Polygon.Count > 0)
                    {
                        polygonInformations.IsPolylineToDraw = true;
                    }
                    else
                    {
                        item.PolygonList.removeExplicitPolygon(label);
                        generalPolygonUtilities.refreshAnnoDataGrid();
                        generalPolygonUtilities.polygonSelectItem(item);
                        control.polygonListControl.polygonDataGrid.SelectedItem = null;
                        polygonInformations.IsPolylineToDraw = false;
                    }
                }
                else if (label.Informations.Type == TYPE.REMOVE)
                {
                    selectLabels(allLabels);
                }
                else if (label.Informations.Type == TYPE.COPY)
                {
                    selectNoLabel();
                }
            }
            setPossiblyCreationMode(label);
            updateOverlayAndLabelCount(item);
        }

        public void handleRedo()
        {
            AnnoListItem item = null;
            if (control.annoListControl.annoDataGrid.SelectedItems.Count > 0)
            {
                item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
            }

            PolygonLabel label = null;
            int pointsBeforeRedo = 0;
            if (control.polygonListControl.polygonDataGrid.SelectedItem != null)
            {
                pointsBeforeRedo = ((PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem).Polygon.Count;
            }

            PolygonLabel[] allLabels = item.UndoRedoStack.Redo();
            generalPolygonUtilities.polygonTableUpdate();

            if (allLabels != null && allLabels.Length > 0)
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                label = allLabels[0];
                control.polygonListControl.polygonDataGrid.SelectedItems.Add(label);

                if (label.Informations.Type == TYPE.CREATION)
                {
                    int pointsAfterRedo = -1;
                    if (control.polygonListControl.polygonDataGrid.SelectedItem != null)
                    {
                        pointsAfterRedo = ((PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem).Polygon.Count;
                    }
                    if (control.polygonListControl.polygonDataGrid.SelectedItem == null)
                    {
                        item.PolygonList.addPolygonLabel(label);
                        generalPolygonUtilities.refreshAnnoDataGrid();
                        generalPolygonUtilities.polygonSelectItem(item);
                        polygonInformations.IsPolylineToDraw = true;
                    }
                    if (pointsBeforeRedo == pointsAfterRedo)
                    {
                        control.polygonListControl.polygonDataGrid.SelectedItem = null;
                        polygonInformations.IsPolylineToDraw = false;
                    }
                }
                else if (label.Informations.Type == TYPE.REMOVE)
                {
                    selectNoLabel();
                }
                else if (label.Informations.Type == TYPE.COPY)
                {
                    selectLabels(allLabels);
                }
            }

            setPossiblyCreationMode(label);
            updateOverlayAndLabelCount(item);
        }

        public void setPossiblyCreationMode(PolygonLabel label)
        {
            if (label != null)
            {
                if (label.Informations.Type == TYPE.CREATION)
                {
                    if (!polygonInformations.IsCreateModeOn)
                    {
                        polygonInformations.IsCreateModeOn = true;
                        uIElementController.enableOrDisableControlsBesidesPolygon(false);
                        uIElementController.enableOrDisablePolygonControlElements(false);
                        uIElementController.switchAddLabelButton();
                        control.polygonListControl.stopInsertion.IsEnabled = true;
                    }
                }
                else
                {
                    if (polygonInformations.IsCreateModeOn)
                    {
                        polygonInformations.IsCreateModeOn = false;
                        uIElementController.enableOrDisableControls(true);
                        uIElementController.enableOrDisablePolygonControlElements(true);
                        uIElementController.switchAddLabelButton();
                    }
                }
            }
        }

        public void updateOverlayAndLabelCount(AnnoListItem item)
        {
            item.updateLabelCount();
            polygonDrawUnit.polygonOverlayUpdate(item);
            if (polygonInformations.IsPolylineToDraw)
                polygonDrawUnit.drawLineToMousePosition(polygonInformations.LastKnownPoint.X, polygonInformations.LastKnownPoint.Y);
        }

        public void selectLabels(PolygonLabel[] allLabels)
        {
            control.polygonListControl.polygonDataGrid.SelectedItem = null;

            foreach (PolygonLabel currentLabel in allLabels)
            {
                control.polygonListControl.polygonDataGrid.SelectedItems.Add(currentLabel);
            }

            generalPolygonUtilities.refreshAnnoDataGrid();
            generalPolygonUtilities.polygonTableUpdate();
        }

        public void selectNoLabel()
        {
            control.polygonListControl.polygonDataGrid.SelectedItem = null;
            generalPolygonUtilities.refreshAnnoDataGrid();
            generalPolygonUtilities.polygonTableUpdate();
        }
    }
}

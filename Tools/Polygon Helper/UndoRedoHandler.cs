using ssi.Interfaces;
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
        private CreationInformation creationInfos;
        private Utilities polygonUtilities;
        private IDrawUnit polygonDrawUnit;


        public UndoRedoHandler(MainControl control, CreationInformation creationInfos, Utilities polygonUtilities, IDrawUnit polygonDrawUnit)
        {
            this.control = control;
            this.creationInfos = creationInfos;
            this.polygonUtilities = polygonUtilities;
            this.polygonDrawUnit = polygonDrawUnit;
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
                        creationInfos.IsPolylineToDraw = true;
                    }
                    else
                    {
                        item.PolygonList.removeExplicitPolygon(label);
                        polygonUtilities.refreshAnnoDataGrid();
                        polygonUtilities.polygonSelectItem(item);
                        control.polygonListControl.polygonDataGrid.SelectedItem = null;
                        creationInfos.IsPolylineToDraw = false;
                    }
                }
                else if (label.Informations.Type == TYPE.REMOVE)
                {
                    control.polygonListControl.polygonDataGrid.SelectedItem = null;
                    polygonUtilities.polygonSelectItem(item);
                    polygonUtilities.refreshAnnoDataGrid();
                    polygonUtilities.polygonTableUpdate();
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
                        polygonUtilities.refreshAnnoDataGrid();
                        polygonUtilities.polygonSelectItem(item);
                        creationInfos.IsPolylineToDraw = true;
                    }
                    if (pointsBeforeRedo == pointsAfterRedo)
                    {
                        control.polygonListControl.polygonDataGrid.SelectedItem = null;
                        creationInfos.IsPolylineToDraw = false;
                    }
                }
                else if (label.Informations.Type == TYPE.REMOVE)
                {
                    control.polygonListControl.polygonDataGrid.SelectedItem = null;

                    foreach (PolygonLabel currentLabel in allLabels)
                    {
                        control.polygonListControl.polygonDataGrid.SelectedItems.Add(currentLabel);
                    }

                    polygonUtilities.refreshAnnoDataGrid();
                    polygonUtilities.polygonSelectItem(item);
                    polygonUtilities.polygonTableUpdate();
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
                    if (!creationInfos.IsCreateModeOn)
                    {
                        creationInfos.IsCreateModeOn = true;
                        polygonUtilities.enableOrDisableControls(false);
                    }
                }
                else
                {
                    if (creationInfos.IsCreateModeOn)
                    {
                        creationInfos.IsCreateModeOn = false;
                        polygonUtilities.enableOrDisableControls(true);
                    }
                }
            }
        }

        public void updateOverlayAndLabelCount(AnnoListItem item)
        {
            item.updateLabelCount();
            polygonDrawUnit.polygonOverlayUpdate(item);
            if (creationInfos.IsPolylineToDraw)
                polygonDrawUnit.drawLineToMousePosition(creationInfos.LastKnownPoint.X, creationInfos.LastKnownPoint.Y);
        }
    }
}

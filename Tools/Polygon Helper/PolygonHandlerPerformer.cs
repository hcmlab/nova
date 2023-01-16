using ssi.Interfaces;
using ssi.Tools.Polygon_Helper;
using System.Collections.Generic;
using System.Linq;
using CMS = System.Windows.Forms.ContextMenuStrip;
using CMSItem = System.Windows.Forms.ToolStripMenuItem;

namespace ssi.Types.Polygon
{
    internal class PolygonHandlerPerformer
    {
        private MainHandler handler;
        private MainControl control;
        private PolygonLabel[] copyLabels;
        private PolygonInformations polygonInformations;
        private PolygonUtilities polygonUtilities;
        private UndoRedoHandler undoRedoHandler;
        private IDrawUnit polygonDrawUnit;
        private InterpolationHandler interpolationHandler;

        private CMS contextMenu;
        private CMSItem copy;
        private CMSItem paste;
        private CMSItem cut;
        private CMSItem delete;

        private bool contextMenuOpen;
        
        public CMS ContextMenu { get => contextMenu; set => contextMenu = value; }
        public bool ContextMenuOpen { get => contextMenuOpen; set => contextMenuOpen = value; }

        public PolygonHandlerPerformer(MainControl control, MainHandler handler, PolygonInformations polygonInformations)
        {
            this.control = control;
            this.handler = handler;
            this.polygonInformations = polygonInformations;
            initContextMenu();
        }

        public void setObjects(PolygonInformations polygonInformations, IDrawUnit polygonDrawUnit, UIElementsController uIElementController, PolygonUtilities polygonUtilities)
        {
            this.polygonDrawUnit = polygonDrawUnit;
            this.polygonUtilities = polygonUtilities;
            this.interpolationHandler = new InterpolationHandler(this.control);
            this.undoRedoHandler = new UndoRedoHandler(this.control, polygonInformations, polygonUtilities, polygonDrawUnit, uIElementController);
        }

        public CMSItem[] getItemsToChange()
        {
            return new CMSItem[] { copy, cut, delete };
        }

        private void initContextMenu()
        {
            this.ContextMenu = new System.Windows.Forms.ContextMenuStrip();
            this.copy = new CMSItem("Copy", null, handler.polygonCopyContextMenue_Click);
            this.copy.ShortcutKeyDisplayString = "Ctrl+C";
            this.paste = new CMSItem("Paste", null, handler.polygonPasteContextMenue_Click);
            this.paste.ShortcutKeyDisplayString = "Ctrl+V";
            this.paste.Enabled = false;
            this.cut = new CMSItem("Cut", null, handler.polygonCutContextMenue_Click);
            this.cut.ShortcutKeyDisplayString = "Ctrl+X";
            this.delete = new CMSItem("Delete", null, handler.polygonDeleteContextMenue_Click);
            this.delete.ShortcutKeyDisplayString = "Ctrl+Back, Del";
            this.ContextMenu.Items.Add(copy);
            this.ContextMenu.Items.Add(paste);
            this.ContextMenu.Items.Add(cut);
            this.ContextMenu.Items.Add(delete);
        }


        public void handleCopy()
        {
            List<PolygonLabel> newCopyList = new List<PolygonLabel>();
            foreach (PolygonLabel label in control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>())
            {
                PolygonLabel newLabel = new PolygonLabel(label.getPolygonAsCopy(), label.Label, label.Color, label.Confidence);
                newCopyList.Add(newLabel);
            }

            copyLabels = newCopyList.ToArray();
            this.ContextMenuOpen = false;

            control.paste.IsEnabled = true;
            control.polygonListControl.paste.IsEnabled = true;
            this.paste.Enabled = true;
        }

        public void handlePaste()
        {
            if(control.annoListControl.annoDataGrid.SelectedItems.Count > 0 && this.copyLabels.Length > 0)
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];

                PolygonLabel[] labelsToSelect = item.UndoRedoStack.Do(new AddCopiedLabelsCommand(this.copyLabels, item));

                undoRedoHandler.selectLabels(labelsToSelect);
                undoRedoHandler.updateOverlayAndLabelCount(item);
                this.ContextMenuOpen = false;
            }
        }

        public void handleCut()
        {
            handleCopy();
            handleRemove();
        }

        public void handleRemove()
        {
            PolygonLabel[] labelsToDelete = control.polygonListControl.polygonDataGrid.SelectedItems.Cast<PolygonLabel>().ToArray();
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];

            item.UndoRedoStack.Do(new RemoveLabelsCommand(labelsToDelete, item));

            undoRedoHandler.selectLabels(labelsToDelete);
            undoRedoHandler.updateOverlayAndLabelCount(item);
            this.ContextMenuOpen = false;
        }

        public void handleSelectAll()
        {
            control.polygonListControl.polygonDataGrid.SelectAll();
        }

        public void handleRightMouseDown(IMedia media, double x, double y)
        {
            if(this.contextMenuOpen)
            {
                polygonInformations.resetSelectedElements();
                polygonUtilities.setPossiblySelectedPolygonAndSelectedPoint(x, y);
            }

            if (polygonInformations.SelectedPolygon != null)
            {
                this.ContextMenu.Show(System.Windows.Forms.Cursor.Position);
            }
            else
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                polygonUtilities.polygonTableUpdate();
                if (this.paste.Enabled)
                    this.ContextMenu.Show(System.Windows.Forms.Cursor.Position);
                else
                    return;
            }

            this.ContextMenuOpen = true;
        }

        public void handleUndo()
        {
            this.undoRedoHandler.handleUndo();
        }

        public void handleRedo()
        {
            this.undoRedoHandler.handleRedo();
        }

        public void handle2DInterpolation(ComboboxItem targetFrame, ComboboxItem sourceFrame, PolygonLabel sourceLabel, PolygonLabel targetLabel, double xStepPerFrame, double yStepPerFrame, int framesBetween)
        {
            this.interpolationHandler.handle2DInterpolation(targetFrame, sourceFrame, sourceLabel, targetLabel, xStepPerFrame, yStepPerFrame, framesBetween);
            polygonUtilities.refreshAnnoDataGrid();
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }

        public void handle3DInterpolation(List<double> xStepsPerFrame, List<double> yStepsPerFrame, ComboboxItem sourceFrame, PolygonLabel sourceLabel,
                                          PolygonLabel targetLabel, List<PointAngleTuple> sourcePointsAnglesTuple, List<PointAngleTuple> targetPointsAnglesTuple,
                                          int stepsFromSourceToTarget)
        {
            this.interpolationHandler.handle3DInterpolation(xStepsPerFrame, yStepsPerFrame, sourceFrame, sourceLabel, targetLabel, sourcePointsAnglesTuple, targetPointsAnglesTuple, stepsFromSourceToTarget);
            polygonUtilities.refreshAnnoDataGrid();
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }
    }
}

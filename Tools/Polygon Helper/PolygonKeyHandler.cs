using ssi.Types.Polygon;


namespace ssi.Tools.Polygon_Helper
{
    class PolygonKeyHandler
    {
        private MainControl control;
        private PolygonCreator labelCreator;
        private PolygonInformations polygonInformations;
        private PolygonUtilities polygonUtilities;

        public PolygonKeyHandler()
        {
        }

        public void setObjects(MainControl control, PolygonCreator labelCreator, PolygonInformations polygonInformations, PolygonUtilities polygonUtilities)
        {
            this.control = control;
            this.labelCreator = labelCreator;
            this.polygonInformations = polygonInformations;
            this.polygonUtilities = polygonUtilities;
        }

        public void escClickedCWhileProbablyCreatingPolygon()
        {
            PolygonLabel currentPolygonLabel = control.polygonListControl.polygonDataGrid.SelectedItem as PolygonLabel;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object && currentPolygonLabel.Polygon.Count > 0)
            {
                PolygonLabel label = item.UndoRedoStack.Undo()[0];

                while (label.Polygon.Count > 0)
                {
                    PolygonLabel[] tmp = item.UndoRedoStack.Undo();
                    if (label != null)
                        label = tmp[0];
                    else
                        return;
                }

                item.PolygonList.removeExplicitPolygon(label);
                item.updateLabelCount();
                polygonUtilities.refreshAnnoDataGrid();
                polygonUtilities.polygonSelectItem(item);
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                polygonInformations.IsPolylineToDraw = false;
            }
            else
            {
                labelCreator.endCreationMode(item, currentPolygonLabel);
            }
        }

    }
}

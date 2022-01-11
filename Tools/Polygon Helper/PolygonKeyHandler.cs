using ssi.Types.Polygon;


namespace ssi.Tools.Polygon_Helper
{
    class PolygonKeyHandler
    {
        private MainControl control;
        private LabelCreator labelCreator;
        private CreationInformation creationInfos;
        private PolygonUtilities polygonUtilities;

        public PolygonKeyHandler()
        {
        }

        public void setObjects(MainControl control, LabelCreator labelCreator, CreationInformation creationInfos, PolygonUtilities polygonUtilities)
        {
            this.control = control;
            this.labelCreator = labelCreator;
            this.creationInfos = creationInfos;
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
                    label = item.UndoRedoStack.Undo()[0];
                }

                item.PolygonList.removeExplicitPolygon(label);
                item.updateLabelCount();
                polygonUtilities.refreshAnnoDataGrid();
                polygonUtilities.polygonSelectItem(item);
                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                creationInfos.IsPolylineToDraw = false;
            }
            else
            {
                labelCreator.endCreationMode(item, currentPolygonLabel);
            }
        }

    }
}

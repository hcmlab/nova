using ssi.Types;
using System.Windows;

namespace ssi.Interfaces
{
    interface IPolygonUtilities
    {
        bool isMouseWithinPolygonArea(double x, double y);
        void updatePoint(double x, double y, AnnoListItem item);
        void updatePolygon(double x, double y, AnnoListItem item);
        void polygonTableUpdate();
        void endCreationMode(AnnoListItem item, PolygonLabel currentPolygonLabel = null);
        bool isMouseAbovePoint(double x, double y);
        void editPolygon(double x, double y);
        bool addNewPoint(double x, double y);
        void activateEditMode();
        void polygonSelectItem(AnnoListItem item);
        void escClickedCWhileProbablyCreatingPolygon();
        void refreshAnnoDataGrid();
        bool labelIsNotSelected(PolygonLabel pl);
        void enableOrDisableControls(bool enable);
        void addPolygonLabelToPolygonList(PolygonLabel pl);
        bool isNewPointNextToStartPoint(PolygonPoint startPoint, Point newPoint);
        bool IsNextToStartPoint { get; set; }
        UndoRedoStack PolygonUndoRedoStack { get; set; }
        bool mouseIsOnLine();
    }
}

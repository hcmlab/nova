using System.Windows;
using CMSItem = System.Windows.Forms.ToolStripMenuItem;


namespace ssi.Tools.Polygon_Helper
{
    // This class only changes UI-Elements (like buttons, listviews and so on)
    class UIElementsController
    {
        private MainControl control = null;

        public UIElementsController(MainControl control)
        {
            this.control = control;
        }

        public void enableOrDisableMenuControls(bool enable, CMSItem[] itemsToChange)
        {
            control.copy.IsEnabled = enable;
            control.cut.IsEnabled = enable;
            control.delete.IsEnabled = enable;

            control.polygonListControl.copy.IsEnabled = enable;
            control.polygonListControl.cut.IsEnabled = enable;
            control.polygonListControl.delete.IsEnabled = enable;

            foreach (CMSItem item in itemsToChange)
                item.Enabled = enable;
        }


        public void enableOrDisableControls(bool enable)
        {
            control.annoListControl.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
            control.polygonListControl.IsEnabled = enable;
        }

        public void enableOrDisablePolygonControlElements(bool enable, bool withInterpolationButton = true)
        {
            control.polygonListControl.polygonSelectAllButton.IsEnabled = enable;
            control.polygonListControl.polygonSetDefaultLabelButton.IsEnabled = enable;
            control.polygonListControl.polygonCopyButton.IsEnabled = enable;
            control.polygonListControl.polygonRelabelButton.IsEnabled = enable;
            control.polygonListControl.editTextBox.IsEnabled = enable;
            control.polygonListControl.editComboBox.IsEnabled = enable;
            control.polygonListControl.polygonDataGrid.IsEnabled = enable;
            control.polygonListControl.addLabels.IsEnabled = enable;
            control.polygonListControl.delete.IsEnabled = enable;
            control.polygonListControl.stopInsertion.IsEnabled = enable;

            if (withInterpolationButton)
                control.polygonListControl.interpolateLabels.IsEnabled = enable;
        }

        public void enableOrDisableControlsBesidesPolygon(bool enable)
        {
            control.annoListControl.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
        }

        public void switchAddLabelButton()
        {
            if (control.polygonListControl.addLabels.Visibility != Visibility.Visible)
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
    }
}

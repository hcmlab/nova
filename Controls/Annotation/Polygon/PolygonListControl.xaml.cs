using System;
using System.Windows.Controls;
using System.Windows.Media;
using PropertyTools.Wpf;
using ssi.Tools.Polygon_Helper;
using ssi.Types.Polygon;

namespace ssi
{
    /// <summary>
    /// Interaction logic for PolygonListControl.xaml
    /// </summary>
    public partial class PolygonListControl : UserControl
    {
        private ListViewSortAdorner listViewSortAdorner;
        private PolygonEditor polygonUtilitiesManager;
        private GridViewColumnHeader listViewSortCol;
        private PolygonUtilities PolygonUtilities;
        private Color color;

        public PolygonListControl()
        {
            InitializeComponent();
        }

        internal void setPolygonUtilitiesManagerAndEditor(PolygonEditor polygonEditor, PolygonUtilities utilities)
        {
            this.polygonUtilitiesManager = polygonEditor;
            this.PolygonUtilities = utilities;
        }

        public void setDefaultLabel(String defaultLabel)
        {
            this.editTextBox.Text = defaultLabel;
        }

        private void ColorPicker_DropDownClosed(object sender, EventArgs e)
        {
            PolygonLabel pl = (PolygonLabel)((ColorPicker)sender).DataContext;

            if (!this.polygonUtilitiesManager.updateLabelColor(pl))
            {
                this.polygonUtilitiesManager.undoColorChanges(pl, this.color);
            }
        }

        private void ColorPicker_DropDownOpened(object sender, EventArgs e)
        {
            if(AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                ((ColorPicker)sender).IsEnabled = false;
            }
            else
            {
                PolygonLabel pl = (PolygonLabel)((ColorPicker)sender).DataContext;
                color = pl.Color;
                PolygonUtilities.selectSpecificLabel(pl);
            }
        }
    }
}
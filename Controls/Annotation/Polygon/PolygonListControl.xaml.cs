using System;
using System.Windows.Controls;
using System.Windows.Media;
using PropertyTools.Wpf;
using ssi.Types.Polygon;

namespace ssi
{
    /// <summary>
    /// Interaction logic for PolygonListControl.xaml
    /// </summary>
    public partial class PolygonListControl : UserControl
    {
        private GridViewColumnHeader listViewSortCol = null;
        private ListViewSortAdorner listViewSortAdorner = null;
        private Utilities polygonUtilitiesManager;
        private Color color;

        public PolygonListControl()
        {
            InitializeComponent();
        }

        internal void setPolygonUtilitiesManager(Utilities manager)
        {
            this.polygonUtilitiesManager = manager;
        }

        public void setDefaultLabel(String defaultLabel)
        {
            this.editTextBox.Text = defaultLabel;
        }

        private void ColorPicker_DropDownClosed(object sender, EventArgs e)
        {
            PolygonLabel pl = (PolygonLabel)((ColorPicker)sender).DataContext;

            if (!this.polygonUtilitiesManager.updateFrameData(pl))
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
                this.color = pl.Color;
                this.polygonUtilitiesManager.selectSpecificLabel(pl);
            }
        }
    }
}
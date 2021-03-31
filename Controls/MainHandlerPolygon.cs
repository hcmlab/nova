using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ssi
{
    public partial class MainHandler
    {
        private void polygonAddNewLabelButton_Click(object sender, RoutedEventArgs e)
        {
            Window dialog = null;
            AnnoScheme scheme = AnnoTierStatic.Selected.AnnoList.Scheme;
            MainHandler mainHandler = this;
            dialog = new AddPolygonLabelWindow(ref scheme, ref mainHandler);

            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        private void polygonSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (control.polygonListControl.polygonDataGrid.Items.Count > 0)
            {
                control.polygonListControl.polygonDataGrid.SelectAll();
            }
        }

        private void polygonSetDefaultLabelButton_Click(object sender, RoutedEventArgs e)
        {
            Window dialog = null;
            AnnoScheme scheme;

            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON)
            {
                scheme = AnnoTierStatic.Selected.AnnoList.Scheme;

                dialog = new DefaultLabelWindow(ref scheme);

                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();
            }
            // TODO MARCO was passiert wenn man kein Anno ausgewählt hat, oder es kein Anno vom Typ Polygon ist?
        }

        private void polygonCopyButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO MARCO
        }

        private void polygonRelabelButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO MARCO schauen ob man hier das hier braucht: 
            //string name = control.polygonListControl.polygonDataGrid.SelectedItems[0].GetType().Name;
            //if (name == "PointListItem")

            foreach (PointListItem item in control.polygonListControl.polygonDataGrid.SelectedItems)
            {
                item.Label = control.polygonListControl.editTextBox.Text;
            } 
        }

        private void polygonListElementDelete_Click(object sender, RoutedEventArgs e)
        {
            if (control.polygonListControl.polygonDataGrid.SelectedItem != null && control.polygonListControl.polygonDataGrid.SelectedItems.Count != 0)
            {
                // TODO MARCO 1. Auch mehrere Elemente löschen wenn sie Ausgewählt wurde 2. Vermutlich neu zeichnen, updaten oder so ähnlich..
                control.polygonListControl.polygonDataGrid.Items.Remove(control.polygonListControl.polygonDataGrid.SelectedItem);
                this.polygonTableUpdate();
            }
        }

        public void addPolygonLabelToDataGrid(PolygonLabel pl)
        {
            control.polygonListControl.polygonDataGrid.Items.Add(pl);
        }

        private void polygonTableUpdate()
        {
            control.polygonListControl.polygonDataGrid.Items.Refresh();
        }
    }
}

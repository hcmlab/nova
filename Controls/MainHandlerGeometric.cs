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

        private void setPointList(PointList pl)
        {
            control.geometricListControl.geometricDataGrid.ItemsSource = pl;
        }

        private void geometricTableUpdate()
        {
            control.geometricListControl.geometricDataGrid.Items.Refresh();
        }

        private void geometricSelectItem(AnnoListItem item)
        {
            if (item.Points != null && item.Points.Count > 0)
            {
                setPointList(item.Points);              
                geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
            }
        }

        public void geometricOverlayUpdate(AnnoListItem item, AnnoScheme.TYPE type)
        {
            WriteableBitmap overlay = null;

            foreach (IMedia m in mediaList)
            {
                if (m.GetMediaType() == MediaType.VIDEO)
                {
                    overlay = m.GetOverlay();
                    break;
                }
            }

            if (overlay == null)
            {
                return;
            }

            overlay.Clear();

            switch (type)
            {
                case AnnoScheme.TYPE.POINT:                            
                    foreach (PointListItem p in item.Points)
                    {
                        if (p.XCoord != -1 && p.YCoord != -1)
                        {
                            Color color = item.Color;
                            //color.A = 128;
                            overlay.FillEllipseCentered((int)p.XCoord, (int)p.YCoord, 4, 4, color);
                        }
                    }
                    break;
                case AnnoScheme.TYPE.POLYGON:
                    break;
                case AnnoScheme.TYPE.GRPAH:
                    break;
                case AnnoScheme.TYPE.SEGMENTATION:
                    break;
            }
        }

        void OnMediaMouseDown(MediaKit media, double x, double y)
        {
            if (AnnoTierStatic.Selected != null &&
                AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT &&
                control.annoListControl.annoDataGrid.SelectedItem != null &&
                control.geometricListControl.geometricDataGrid.SelectedItem != null)
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                PointListItem point = (PointListItem)control.geometricListControl.geometricDataGrid.SelectedItem;
                point.XCoord = x;
                point.YCoord = y;
                geometricTableUpdate();
                geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
            }
        }

        private void geometricListEdit_Click(object sender, RoutedEventArgs e)
        {
            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                string name = control.geometricListControl.geometricDataGrid.SelectedItems[0].GetType().Name;
                if (name == "PointListItem")
                {
                    foreach (PointListItem item in control.geometricListControl.geometricDataGrid.SelectedItems)
                    {
                        item.Label = control.geometricListControl.editTextBox.Text;
                    }
                }
            }
        }

        private void geometricListEdit_Focused(object sender, MouseEventArgs e)
        {
            control.geometricListControl.editTextBox.SelectAll();
        }

        private void geometricListSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (control.geometricListControl.geometricDataGrid.Items.Count > 0)
            {
                control.geometricListControl.geometricDataGrid.SelectAll();
            }
        }

        private void geometricListCopy_Click(object sender, RoutedEventArgs e)
        {
            if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1)
            {
                if (control.geometricListControl.geometricDataGrid.Items[0].GetType().Name == "PointListItem")
                {
                    AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
                    AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;

                    for (int i = 0; i < list.Count; ++i)
                    {
                        if (Math.Round(list[i].Start, 2) == Math.Round(item.Stop, 2))
                        {
                            for (int j = 0; j < list[i].Points.Count; ++j)
                            {
                                list[i].Points[j].Label = item.Points[j].Label;
                                list[i].Points[j].XCoord = item.Points[j].XCoord;
                                list[i].Points[j].YCoord = item.Points[j].YCoord;
                            }
                            break;
                        }
                    }
                }
            }
            else
            {
                MessageBoxResult mb = MessageBoxResult.OK;
                mb = MessageBox.Show("Select one frame to copy", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void geometricList_Selection(object sender, SelectionChangedEventArgs e)
        {            
            if (control.annoListControl.annoDataGrid.SelectedItem != null)
            {
                AnnoListItem item = (AnnoListItem) control.annoListControl.annoDataGrid.SelectedItem;
                geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
            }

            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                PointListItem item = (PointListItem)control.geometricListControl.geometricDataGrid.SelectedItems[0];
                control.geometricListControl.editTextBox.Text = item.Label;
            }
        }

        private void geometricListDelete(object sender, RoutedEventArgs e)
        {
            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count != 0 
                && control.annoListControl.annoDataGrid.SelectedItem != null)
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                foreach (PointListItem point in control.geometricListControl.geometricDataGrid.SelectedItems)
                {
                    point.XCoord = -1;
                    point.YCoord = -1;
                }
                geometricTableUpdate();
                geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
            }
        }
    }
}

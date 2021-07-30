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

            IMedia video = mediaList.GetFirstVideo();

            if (video != null)
            {
                overlay = video.GetOverlay();
            }
            else
            { 
                return;
            }

            overlay.Lock();
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
                            overlay.FillEllipseCentered((int)p.XCoord, (int)p.YCoord, 1, 1, color);
                        }
                    }
                    break;
                case AnnoScheme.TYPE.GRAPH:
                    break;
                case AnnoScheme.TYPE.SEGMENTATION:
                    break;
            }            

            overlay.Unlock();
        }

        private static bool rightHeld;
        private static bool RightHeld
        {
            get { return rightHeld; }
            set
            {
                rightHeld = value;
                if (!rightHeld)
                {
                    RightHeldPos = new double[2] { 0, 0 };
                }
            }
        }

        private static double[] rightHeldPos;
        private static double[] RightHeldPos
        {
            get
            {
                if (rightHeldPos == null)
                {
                    rightHeldPos = new double[2] { 0, 0 };
                }
                return rightHeldPos;
            }
            set
            {
                if (value.Length == 2)
                {
                    rightHeldPos = value;
                }
            }
        }

        void OnMediaMouseMove(IMedia media, double x, double y)
        {
            if (RightHeld)
            {
                if (AnnoTierStatic.Selected != null &&
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT &&
                    control.annoListControl.annoDataGrid.SelectedItem != null &&
                    control.geometricListControl.geometricDataGrid.SelectedItem != null)
                {
                    double deltaX = x - RightHeldPos[0];
                    double deltaY = y - RightHeldPos[1];

                    RightHeldPos = new double[] { x, y };
                    AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;

                    foreach (PointListItem pli in control.geometricListControl.geometricDataGrid.SelectedItems)
                    {
                        pli.XCoord += deltaX;
                        pli.YCoord += deltaY;
                    }
                    geometricTableUpdate();
                    int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                    geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
                }
            }
        }

        void OnMediaMouseUp(IMedia media, double x, double y)
        {
            if (Mouse.RightButton == MouseButtonState.Released && RightHeld)
            {
                RightHeld = false;
            }
        }

        void OnMediaMouseDown(IMedia media, double x, double y)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (AnnoTierStatic.Selected != null &&
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT &&
                    control.annoListControl.annoDataGrid.SelectedItem != null &&
                    control.geometricListControl.geometricDataGrid.SelectedItem != null)
                {
                    AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                    if (control.geometricListControl.geometricDataGrid.SelectedItems.Count > 1) return;
                    PointListItem point = (PointListItem)control.geometricListControl.geometricDataGrid.SelectedItem;
                    point.XCoord = x;
                    point.YCoord = y;
                    geometricTableUpdate();
                    int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                    geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
                }
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                RightHeldPos = new double[] { x, y };
                RightHeld = true;
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
                int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                geometricOverlayUpdate(item, AnnoScheme.TYPE.POINT);
            }
        }

        private void geometricKeyDown(object sender, KeyEventArgs e)
        {
            if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1 && control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                int index = control.geometricListControl.geometricDataGrid.SelectedIndex;
                if (e.Key == Key.OemPeriod)
                {
                    if (index + 1 < control.geometricListControl.geometricDataGrid.Items.Count)
                    {
                        while (control.geometricListControl.geometricDataGrid.SelectedItems.Count > 0)
                        {
                            control.geometricListControl.geometricDataGrid.SelectedItems.RemoveAt(0);
                        }
                        control.geometricListControl.geometricDataGrid.SelectedItems.Add(control.geometricListControl.geometricDataGrid.Items[index + 1]);

                    }
                }
                else if (e.Key == Key.OemComma)
                {
                    if (index - 1 >= 0)
                    {
                        while (control.geometricListControl.geometricDataGrid.SelectedItems.Count > 0)
                        {
                            control.geometricListControl.geometricDataGrid.SelectedItems.RemoveAt(0);
                        }
                        control.geometricListControl.geometricDataGrid.SelectedItems.Add(control.geometricListControl.geometricDataGrid.Items[index - 1]);
                        geometricTableUpdate();
                    }
                }
            }
        }

        public void jumpToGeometric(int pos)
        {
            if (control.annoListControl.annoDataGrid.Items.Count == 0) return;

            while (control.annoListControl.annoDataGrid.SelectedItems.Count > 0)
            {
                control.annoListControl.annoDataGrid.SelectedItems.RemoveAt(0);
            }

            control.annoListControl.annoDataGrid.SelectedItems.Add(control.annoListControl.annoDataGrid.Items[pos]);
            if (control.geometricListControl.geometricDataGrid.Items.Count != 0)
            {
                control.geometricListControl.geometricDataGrid.Items.Refresh();
                control.geometricListControl.geometricDataGrid.ScrollIntoView(control.geometricListControl.geometricDataGrid.Items[0]);
            }
            control.annoListControl.annoDataGrid.Items.Refresh();
            control.annoListControl.annoDataGrid.ScrollIntoView(control.annoListControl.annoDataGrid.Items[pos]);

        }

        private List<AnnoList> geometricCompare = new List<AnnoList>(0);
    }
}

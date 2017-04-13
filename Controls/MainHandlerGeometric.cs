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
        private int pointSize = 1;
        private static bool rightHeld;
        private static double[] rightHeldPos;
        private Dictionary<byte, double> grey2ColorMap = null;
        private List<int[]> lasso = new List<int[]>();
        private int[] minLasso = new int[] { int.MaxValue, int.MaxValue };
        private int[] maxLasso = new int[] { 0, 0 };

        private void createGrey2ColorMap()
        {
            grey2ColorMap = new Dictionary<byte, double>();
            int numPoints = 255;
            double min = 360.0;
            double current = 0.0;
            double offset = 180;

            List<double> usedList =  new List<double>(0);

            for (int i = 0; i <= numPoints; ++i)
            {
                if (i == 0)
                {
                    grey2ColorMap[(byte)i] = 0.0;
                    usedList.Add(0.0);
                    continue;
                }


                double next = current + offset;
                bool used = false;
                if (next < 360)
                {
                    foreach (KeyValuePair<byte, double> entry in grey2ColorMap)
                    {
                        if (entry.Value == next)
                        {
                            used = true;
                            break;
                        }
                    }
                }
                else
                {
                    next = min;
                    while (true)
                    {
                        next += min;
                        if (next >= 360)
                        {
                            used = true;
                            break;
                        }
                        used = false;
                        foreach (KeyValuePair<byte, double> entry in grey2ColorMap)
                        {
                            if (entry.Value == next)
                            {
                                used = true;
                            }
                        }
                        if (!used) break;
                    }
                }

                if (used)
                {
                    min /= 2;
                    current = min;
                    grey2ColorMap[(byte)i] = current;
                    usedList.Add(current);
                }
                else
                {
                    current = next;
                    if (current < min) min = current;
                    grey2ColorMap[(byte)i] = current;
                    usedList.Add(current);
                }
            }
        }

        private byte[] hue2RGB(double _hue)
        {
            ////////////////////////////////////////////////////////////////////////
            /// http://www.splinter.com.au/converting-hsv-to-rgb-colour-using-c/ ///
            ////////////////////////////////////////////////////////////////////////

            double hue = _hue;
            while (hue < 0) { hue += 360; };
            while (hue >= 360) { hue -= 360; };

            double R, G, B;
            double V = 1.0;
            double S = 1.0;

            double hf = hue / 60.0;
            int i = (int)Math.Floor(hf);
            double f = hf - i;
            double pv = V * (1 - S);
            double qv = V * (1 - S * f);
            double tv = V * (1 - S * (1 - f));
            switch (i)
            {
                case 0:
                    R = V;
                    G = tv;
                    B = pv;
                    break;
                case 1:
                    R = qv;
                    G = V;
                    B = pv;
                    break;
                case 2:
                    R = pv;
                    G = V;
                    B = tv;
                    break;
                case 3:
                    R = pv;
                    G = qv;
                    B = V;
                    break;
                case 4:
                    R = tv;
                    G = pv;
                    B = V;
                    break;
                case 5:
                    R = V;
                    G = pv;
                    B = qv;
                    break;
                case 6:
                    R = V;
                    G = tv;
                    B = pv;
                    break;
                case -1:
                    R = V;
                    G = pv;
                    B = qv;
                    break;
                default:
                    R = G = B = V; 
                    break;
            }

            byte red = (byte)(R * 255.0);
            red = (red >(byte)255) ? (byte)255 : red;
            red = (red < (byte)0)  ? (byte)0   : red;

            byte green = (byte)(G * 255.0);
            green = (green > (byte)255) ? (byte)255 : green;
            green = (green < (byte)0)   ? (byte)0   : green;

            byte blue = (byte)(B * 255.0);
            blue = (blue > (byte)255) ? (byte)255 : blue;
            blue = (blue < (byte)0)   ? (byte)0   : blue;

            return new byte[] { red, green, blue };
        }

        private void clearGeometricList()
        {
            control.geometricListControl.geometricDataGrid.ItemsSource = null;
            geometricTableUpdate();
        }
        private void setGeometricList(PointList pl)
        {
            control.geometricListControl.geometricDataGrid.ItemsSource = pl;
            geometricTableUpdate();
        }

        //private void setGeometricList(PolygonList pl)
        //{
        //    control.geometricListControl.geometricDataGrid.ItemsSource = pl;
        //    geometricTableUpdate();
        //}
        //private void setGeometricList(GraphList gl)
        //{
        //    control.geometricListControl.geometricDataGrid.ItemsSource = gl;
        //    geometricTableUpdate();
        //}

        private void setGeometricList(SegmentationList sl)
        {
            control.geometricListControl.geometricDataGrid.ItemsSource = sl;
            if (grey2ColorMap == null) createGrey2ColorMap();
            geometricTableUpdate();
        }

        private void geometricTableUpdate()
        {
            control.geometricListControl.geometricDataGrid.Items.Refresh();
        }

        private void geometricSelectItem(AnnoListItem item, int pos)
        {
            AnnoScheme.TYPE type = ((AnnoList)control.annoListControl.annoDataGrid.ItemsSource).Scheme.Type;
            switch (type)
            {
                case AnnoScheme.TYPE.POINT:
                    if (item.Points != null && item.Points.Count > 0) setGeometricList(item.Points);
                    break;
                //case AnnoScheme.TYPE.POLYGON:
                //    if (item.Polygons != null && item.Polygons.Count > 0) setPolygonList(item.Polygons);
                //    break;
                //case AnnoScheme.TYPE.GRAPH:
                //    if (item.Garphs != null && item.Graphs.Count > 0) setGraphList(item.Graphs);
                //    break;
                case AnnoScheme.TYPE.SEGMENTATION:
                    if (item.Segments != null && item.Segments.Count > 0) setGeometricList(item.Segments);
                    break;
            }
            geometricOverlayUpdate(pos);
        }

        public void geometricOverlayUpdate(int pos)
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
            for (int i =0; i < control.annoTierControl.grid.Children.Count; i+=4)
            {
                AnnoTier at = (AnnoTier)control.annoTierControl.grid.Children[i];
                if (at.AnnoList.Show)
                {
                    AnnoListItem item = at.AnnoList[pos];
                    switch (at.AnnoList.Scheme.Type)
                    {
                        case AnnoScheme.TYPE.POINT:
                            foreach (PointListItem p in item.Points)
                            {
                                if (p.XCoord != -1 && p.YCoord != -1)
                                {
                                    Color color = item.Color;
                                    overlay.FillEllipseCentered((int)p.XCoord, (int)p.YCoord, pointSize, pointSize, color);
                                }
                            }
                            break;
                        case AnnoScheme.TYPE.POLYGON:
                            break;
                        case AnnoScheme.TYPE.GRAPH:
                            break;
                        case AnnoScheme.TYPE.SEGMENTATION:
                            int[,] mask = item.Segments[0].getMask();
                            int width = item.Segments[0].getWidth();
                            int height = item.Segments[0].getHeight();
                            byte a = 255;
                            double alpha = 255.0 - 255.0 * (0.1 * (pointSize -1) );
                            a = (byte)alpha;

                            for (int y = 0; y < height; ++y)
                            {
                                for (int x = 0; x < width; ++x)
                                {
                                    Color currentPixel = overlay.GetPixel(x, y);
                                    int mv = mask[x, y];
                                    byte b = (byte)mv;
                                    double hue = grey2ColorMap[b];
                                    byte[] rgb = hue2RGB(hue);
                                    currentPixel = Color.FromArgb(a, rgb[0], rgb[1], rgb[2]);
                                    overlay.SetPixel(x, y, currentPixel);
                                }
                            }
                            break;
                    }
                }

            }
            overlay.Unlock();
        }

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

        void OnMediaMouseMove(MediaKit media, double x, double y)
        {
            if (RightHeld)
            {
                if (AnnoTierStatic.Selected != null &&
                    control.annoListControl.annoDataGrid.SelectedItem != null &&
                    control.geometricListControl.geometricDataGrid.SelectedItem != null)
                {
                    double deltaX = x - RightHeldPos[0];
                    double deltaY = y - RightHeldPos[1];

                    RightHeldPos = new double[] { x, y };
                    switch (AnnoTierStatic.Selected.AnnoList.Scheme.Type)
                    {
                        case AnnoScheme.TYPE.POINT:
                            foreach (PointListItem pli in control.geometricListControl.geometricDataGrid.SelectedItems)
                            {
                                pli.XCoord += deltaX;
                                pli.YCoord += deltaY;
                            }
                            int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                            geometricOverlayUpdate(pos);
                            geometricTableUpdate();
                            break;
                        case AnnoScheme.TYPE.POLYGON:
                            break;
                        case AnnoScheme.TYPE.GRAPH:
                            break;
                        case AnnoScheme.TYPE.SEGMENTATION:
                            int ix = (int)x;
                            int iy = (int)y;
                            lasso.Add(new int[2] { ix, iy });
                            if (minLasso[0] > ix) minLasso[0] = ix;
                            if (minLasso[1] > iy) minLasso[1] = iy;
                            if (minLasso[0] > ix) minLasso[0] = ix;
                            if (minLasso[1] > iy) minLasso[1] = iy;
                            if (maxLasso[0] < ix) maxLasso[0] = ix;
                            if (maxLasso[1] < iy) maxLasso[1] = iy;
                            if (maxLasso[0] < ix) maxLasso[0] = ix;
                            if (maxLasso[1] < iy) maxLasso[1] = iy;
                            break;
                    }
                }
            }
        }

        void OnMediaMouseUp(MediaKit media, double x, double y)
        {
            if (Mouse.RightButton == MouseButtonState.Released && RightHeld)
            {
                RightHeld = false;
                if (lasso.Count > 0)
                {
                    SegmentationListItem segment = ((AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem).Segments[0];

                    //List<int[]> points = new List<int[]>();
                    //for (int i = 0; i < lasso.Count - 2; ++i)
                    //{
                    //    List<int[]> cp = interpolate(lasso[i], lasso[i + 1]);
                    //    foreach (int[] p in cp)
                    //    {
                    //        points.Add(p);
                    //    }
                    //}

                    //List<int[]> cps = interpolate(lasso[0], lasso[lasso.Count-1]);
                    //foreach (int[] p in cps)
                    //{
                    //    points.Add(p);
                    //}

                    foreach (int[] point in lasso)
                    {
                        segment.setPixel(point[0], point[1], control.geometricListControl.geometricDataGrid.SelectedIndex);
                    }
                    int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                    geometricOverlayUpdate(pos);
                }
            }
        }

        List<int[]> interpolate(int[] p1, int[] p2)
        {
            List<int[]> allPoints = new List<int[]>();

            if (p1[0] > p2[0])
            {
                int[] temp = p1;
                p1 = p2;
                p2 = temp;
            }

            double dy = p2[1] - p1[1];
            double dx = p2[0] - p1[0];
            double m = dy / dx;
            double c = p1[1] - m * p1[0];

            for (int x = p1[0]; x < p2[0]; ++x)
            {
                double newY = m * x + c;

                //if (newY % 1 == 0)
                {
                    allPoints.Add(new int[] { x, (int)newY });
                }

            }

            return allPoints;
        }

        void OnMediaMouseDown(MediaKit media, double x, double y)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (AnnoTierStatic.Selected != null &&
                    control.annoListControl.annoDataGrid.SelectedItem != null &&
                    control.geometricListControl.geometricDataGrid.SelectedItem != null)
                {
                    if (control.geometricListControl.geometricDataGrid.SelectedItems.Count > 1) return;
                    switch (AnnoTierStatic.Selected.AnnoList.Scheme.Type)
                    {
                        case AnnoScheme.TYPE.POINT:
                            PointListItem point = (PointListItem)control.geometricListControl.geometricDataGrid.SelectedItem;
                            point.XCoord = x;
                            point.YCoord = y;
                            break;
                        case AnnoScheme.TYPE.POLYGON:
                            //PolygonListItem polygon = (PolygonListItem)control.geometricListControl.geometricDataGrid.SelectedItem;
                            break;
                        case AnnoScheme.TYPE.GRAPH:
                            //GraphListItem graph = (GraphListItem)control.geometricListControl.geometricDataGrid.SelectedItem;
                            break;
                        case AnnoScheme.TYPE.SEGMENTATION:
                            SegmentationListItem segment = ((AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem).Segments[0];
                            segment.setPixel((int) x, (int) y, control.geometricListControl.geometricDataGrid.SelectedIndex);
                            break;
                    }
                    geometricTableUpdate();
                    int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                    geometricOverlayUpdate(pos);
                }
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                RightHeldPos = new double[] { x, y };
                RightHeld = true;
                lasso.Clear();
                minLasso[0] = int.MaxValue;
                minLasso[1] = int.MaxValue;
                maxLasso[0] = 0;
                maxLasso[1] = 0;
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
                int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                geometricOverlayUpdate(pos);
            }

            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                string name = control.geometricListControl.geometricDataGrid.SelectedItems[0].GetType().Name;
                if (name == "PointListItem")
                {
                        PointListItem item = (PointListItem)control.geometricListControl.geometricDataGrid.SelectedItems[0];
                        control.geometricListControl.editTextBox.Text = item.Label;
                }
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
                geometricOverlayUpdate(pos);
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
                else if (e.Key == Key.OemPlus || e.Key == Key.OemMinus)
                {
                    AnnoScheme.TYPE type = ((AnnoList)control.annoListControl.annoDataGrid.ItemsSource).Scheme.Type;
                    //if (type == AnnoScheme.TYPE.POINT)
                    {
                        if (e.Key == Key.OemPlus) ++pointSize;
                        if (pointSize > 5) pointSize = 5;
                        if (e.Key == Key.OemMinus) --pointSize;
                        if (pointSize < 1) pointSize = 1;
                        AnnoListItem ali = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                        int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                        geometricOverlayUpdate(pos);
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
    }
}

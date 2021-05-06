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

        private static bool isCreateModeOn = false;
        private static bool isPolylineToDraw = false;
        private static bool isNextToStartPoint = false;
        private static PolygonPoint lastKnownPoint = null;

        public static bool IsCreateModeOn { get => isCreateModeOn; set => isCreateModeOn = value; }

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
            if (isAnnoDataGridItemsNotNullAndNotZero())
            {
                control.polygonListControl.polygonDataGrid.SelectAll();
            }
        }

        public void selectSpecificLabel(PolygonLabel pl)
        {
            if(isAnnoDataGridItemsNotNullAndNotZero() && labelIsNotSelected(pl))
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = pl;
            }
        }

        private bool labelIsNotSelected(PolygonLabel pl)
        {
            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                if (pl.Equals(control.polygonListControl.polygonDataGrid.SelectedItems[i]))
                {
                    return false;
                }
            }

            return true;
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
        }

        private void polygonCopyButton_Click(object sender, RoutedEventArgs e) 
        {
            if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1) 
            {
                if (control.polygonListControl.polygonDataGrid.Items.Count > 0)
                {
                    if (control.polygonListControl.polygonDataGrid.Items[0].GetType().Name == "PolygonLabel")
                    {
                        AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
                        AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;

                        for (int i = 0; i < list.Count; ++i)
                        {
                            if (Math.Round(list[i].Start, 2) == Math.Round(item.Stop, 2))
                            {
                                for (int j = 0; j < control.polygonListControl.polygonDataGrid.SelectedItems.Count; j++)
                                {
                                    PolygonLabel pl = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[j];
                                    list[i].PolygonList.addPolygonLabel(new PolygonLabel(pl.Polygon, pl.Label, pl.Color));
                                }

                                break;
                            }
                        }
                    }
                }
                else
                {
                    MessageBoxResult mb = MessageBoxResult.OK;
                    mb = MessageBox.Show("There are no entries to copy in the selected frame", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBoxResult mb = MessageBoxResult.OK;
                mb = MessageBox.Show("Select one frame to copy", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void polygonRelabelButton_Click(object sender, RoutedEventArgs e)
        {
            if (isAnnoDataGridValueNotNullAndCountEqualOne() && control.polygonListControl.polygonDataGrid.SelectedValue is object)
            {
                if(control.polygonListControl.editTextBox.Text.Length > 0)
                {
                    AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
                    List<PolygonLabel> polygonLabels = item.PolygonList.Polygons;

                    for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                    {
                        PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i];

                        for (int j = 0; j < polygonLabels.Count; j++)
                        {
                            if (polygonLabels[j].ID == polygonLabel.ID)
                            {
                                polygonLabels[j].Label = control.polygonListControl.editTextBox.Text;
                                break;
                            }
                        }
                    }
                    item.PolygonList.Polygons = polygonLabels;
                    polygonSelectItem(item);
                    control.polygonListControl.editTextBox.Text = "";
                    return;
                }
                else
                {
                    MessageBox.Show("You can not replace your label name with an empty string", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }

            MessageBox.Show("It was not possible to update the label name\nMake sure you have selected a frame and a label", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);            
        }

        private void polygonTableUpdate()
        {
            control.polygonListControl.polygonDataGrid.Items.Refresh();
        }

        private void polygonListElementDelete_Click(object sender, RoutedEventArgs e)
        {
            if (isPolygonDataGridValueNotNullAndCountNotZero())
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];

                for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                {
                    item.PolygonList.removeExplicitPolygon((PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i]);
                }
                
                polygonSelectItem(item);
            }
        }

        private void editPolygon(object sender, RoutedEventArgs e)
        {
            if (isPolygonDataGridValueNotNullAndCountEqualOne() && isAnnoDataGridValueNotNullAndCountEqualOne())
            {
                // TODO MARCO Ändern eines bestehenden Polygons
            }
            else
            {
                MessageBox.Show("Exactly only one label and one frame must be selected", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void polygonSelectItem(AnnoListItem item)
        {
            control.polygonListControl.polygonDataGrid.ItemsSource = item.PolygonList.Polygons;
            polygonOverlayUpdate(item);
        }

        

        public void polygonOverlayUpdate(AnnoListItem item)
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
            int thickness = 1;

            PolygonList polygonList = item.PolygonList;
            if (polygonList == null || polygonList.Polygons == null)
                return;

            foreach (PolygonLabel pl in polygonList.Polygons)
            {
                if(pl.Polygon.Count > 0)
                {
                    if (pl.Equals(control.polygonListControl.polygonDataGrid.SelectedValue))
                        thickness = 3;
                    else
                        thickness = 1;

                    Color color = pl.Color;
                    Color secondColor = Color.FromRgb(255, 255, 255);

                    if (color.R + color.G + color.B > 600)
                        secondColor = Color.FromRgb(0, 0, 0);

                    PolygonPoint point, nextPoint = null;

                    bool currentPolygonLabelEqualSelectedOne = pl.Equals(control.polygonListControl.polygonDataGrid.SelectedValue);

                    foreach (PolygonPoint pp in pl.Polygon)
                    {
                        point = nextPoint;
                        nextPoint = pp;

                        if(isNextToStartPoint && currentPolygonLabelEqualSelectedOne)
                        {
                            if (point == null)
                            {
                                
                                overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 7, thickness + 7, color);
                                overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 6, thickness + 6, secondColor);
                                continue;
                            }

                            overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 2, thickness + 2, color);
                            overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 1, thickness + 1, secondColor);
                            overlay.DrawLineAa((int)point.X, (int)point.Y, (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
                        }
                        else
                        {
                            overlay.FillEllipseCentered((int)nextPoint.X, (int)nextPoint.Y, thickness + 2, thickness + 2, color);

                            if (point == null)
                            {
                                continue;
                            }

                            overlay.DrawLineAa((int)point.X, (int)point.Y, (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
                        }
                    }
                    
                    // we finish the polygon when we are not in the creation mode or when we dont draw the polygon of the selected label
                    if (!currentPolygonLabelEqualSelectedOne || !isCreateModeOn)
                        overlay.DrawLineAa((int)(pl.Polygon.First().X), (int)(pl.Polygon.First().Y), (int)nextPoint.X, (int)nextPoint.Y, color, thickness);
                }
            }
                    
            overlay.Unlock();
        }

        private void clearOverlay()
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
            overlay.Unlock();
        }

        public void addPolygonLabelToPolygonList(PolygonLabel pl)
        {

            if (isAnnoDataGridItemsNotNullAndNotZero() && control.annoListControl.annoDataGrid.SelectedValue == null)
                control.annoListControl.annoDataGrid.SelectedValue = control.annoListControl.annoDataGrid.Items[0];

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            item.PolygonList.addPolygonLabel(pl);
            polygonSelectItem(item);
            control.polygonListControl.polygonDataGrid.SelectedItem = pl;
        }

        public bool updateFrameData(PolygonLabel pl)
        {
            if(isAnnoDataGridValueNotNullAndCountEqualOne())
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];
                List<PolygonLabel> polygonLabels = item.PolygonList.Polygons;

                for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                {
                    PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i];

                    for (int j = 0; j < polygonLabels.Count; j++)
                    {
                        if (polygonLabels[j].ID == polygonLabel.ID)
                        {
                            polygonLabels[j].Color = pl.Color;
                            break;
                        }
                    }
                }
                item.PolygonList.Polygons = polygonLabels;
                polygonSelectItem(item);
                return true;
            }

            MessageBox.Show("It was not possible to update the color\n Make sure you have selected a Frame", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
            
        }

        private void polygonList_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (isAnnoDataGridItemsNotNullAndNotZero())
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                polygonOverlayUpdate(item);
            }

            if (isPolygonDataGridValueNotNullAndCountEqualOne())
            {
                PolygonLabel item = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[0];
                control.polygonListControl.editTextBox.Text = item.Label;
            }
        }
 

        private void OnPolygonMediaMouseDown(IMedia media, double x, double y)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (isSchemeTypePolygon() && isSlectedItemInAnnoDataGridAndPolygonDataGridNotNull())
                {
                    if(isCreateModeOn)
                    {
                        isPolylineToDraw = true;
                        AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                        PolygonPoint pp = new PolygonPoint(x, y);
                        PolygonLabel pl = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;
                        if (pl.Polygon.Count > 0 && isNextToStartPoint)
                        {
                            control.polygonListControl.polygonDataGrid.SelectedItem = null;
                            endCreationMode(item);
                            polygonTableUpdate();
                            return;
                        }

                        List<PolygonLabel> polygonLabels = item.PolygonList.Polygons;

                        for (int j = 0; j < polygonLabels.Count; j++)
                        {
                            if (polygonLabels[j].ID == pl.ID)
                            {
                                polygonLabels[j].addPoint(pp);
                                pl = polygonLabels[j];
                                break;
                            }
                        }
                        
                        item.PolygonList.Polygons = polygonLabels;

                        polygonSelectItem(item);
                        polygonTableUpdate();

                        control.polygonListControl.polygonDataGrid.SelectedItem = pl;

                    }
                }
            }
            if (Mouse.RightButton == MouseButtonState.Pressed)
            {
                RightHeldPos = new double[] { x, y };
                RightHeld = true;
            }
        }

        public void OnPolygonMediaBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (isCreateModeOn)
            {
                control.Cursor = Cursors.Cross;
                if (isPolylineToDraw)
                    control.Cursor = Cursors.Cross;
            }
        }

        public void handleKeyEvent(object sender, KeyEventArgs e)
        {
            if (isCreateModeOn)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.Escape))
                {
                    this.escClickedCWhileProbablyCreatingPolygon();
                }

                if (isPolylineToDraw)
                {
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Z))
                    {
                        removeLastPoint();
                    }
                }
            }
        }

        public void removeLastPoint()
        {
            PolygonLabel currentPolygonLabel = control.polygonListControl.polygonDataGrid.SelectedItem as PolygonLabel;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object)
            {
                if(currentPolygonLabel.Polygon.Count > 1)
                {
                    List<PolygonLabel> polygonLabels = item.PolygonList.getRealList();
                    foreach (PolygonLabel pl in polygonLabels)
                    {
                        if (pl.Equals(currentPolygonLabel))
                        {
                            List<PolygonPoint> polygon = pl.getRealPolygon();
                            polygon.RemoveAt(polygon.Count - 1);
                        }
                    }

                    polygonOverlayUpdate(item);

                    drawLineToMousePosition(lastKnownPoint.X, lastKnownPoint.Y);
                }
                else
                {
                    endCreationMode(item, currentPolygonLabel);
                }
            }
        }

        void OnPolygonMediaMouseMove(IMedia media, double x, double y)
        {
            if (isCreateModeOn && isPolylineToDraw)
            {
                lastKnownPoint = new PolygonPoint(x, y);
                drawLineToMousePosition(x, y);
            }
        }

        private void drawLineToMousePosition(double x, double y)
        {
            
            PolygonLabel pl = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedValue;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            WriteableBitmap overlay = null;
            IMedia video = mediaList.GetFirstVideo();

            if (video != null)
                overlay = video.GetOverlay();
            else
                return;

            int thickness = 3;
            Color color = pl.Color;
            overlay.Lock();

            if (isNewPointNextToStartPoint(pl.Polygon.First(), new PolygonPoint(x, y)))
            {
                isNextToStartPoint = true;
                polygonOverlayUpdate(item);
                overlay.DrawLineAa((int)pl.Polygon.Last().X, (int)pl.Polygon.Last().Y, (int)x, (int)y, color, thickness);
            }
            else
            {
                isNextToStartPoint = false;
                polygonOverlayUpdate(item);
                overlay.FillEllipseCentered((int)x, (int)y, thickness + 2, thickness + 2, color);
                overlay.DrawLineAa((int)pl.Polygon.Last().X, (int)pl.Polygon.Last().Y, (int)x, (int)y, color, thickness);
            }
            overlay.Unlock();
            
        }



        private void onPolygonMediaMouseLeave(object sender, MouseEventArgs e)
        {
            control.Cursor = Cursors.Arrow;
        }


        private bool isNewPointNextToStartPoint(PolygonPoint startPoint, PolygonPoint newPoint)
        {
            const int MIN_DISTANCE = 10;

            double currentDistance = Math.Sqrt(Math.Pow(startPoint.X - newPoint.X, 2) + Math.Pow(startPoint.Y - newPoint.Y, 2));
            if (currentDistance < MIN_DISTANCE)
                return true;
            else
                return false;
        }

        public void escClickedCWhileProbablyCreatingPolygon()
        {
            PolygonLabel currentPolygonLabel = control.polygonListControl.polygonDataGrid.SelectedItem as PolygonLabel;
            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            if (currentPolygonLabel is object && currentPolygonLabel.Polygon is object && currentPolygonLabel.Polygon.Count > 0)
            {

                List<PolygonLabel> polygonLabels = item.PolygonList.Polygons;

                for (int j = 0; j < polygonLabels.Count; j++)
                {
                    if (polygonLabels[j].ID == currentPolygonLabel.ID)
                    {
                        polygonLabels[j].removeAll();
                        currentPolygonLabel = polygonLabels[j];
                        break;
                    }
                }
                item.PolygonList.Polygons = polygonLabels;

                polygonSelectItem(item);
                polygonTableUpdate();

                control.polygonListControl.polygonDataGrid.SelectedItem = currentPolygonLabel;
                isPolylineToDraw = false;
            }
            else
            {
                endCreationMode(item, currentPolygonLabel);
            }
        }

        private void endCreationMode(AnnoListItem item, PolygonLabel currentPolygonLabel = null)
        {
            if(currentPolygonLabel != null)
                item.PolygonList.removeExplicitPolygon(currentPolygonLabel);

            isPolylineToDraw = false;
            IsCreateModeOn = false;
            isNextToStartPoint = false;
            enableOrDisableControls(true);
            polygonSelectItem(item);
            control.Cursor = Cursors.Arrow;
        }

        public void drawPolygon(PolygonLabel pl)
        {
            enableOrDisableControls(false);
        }

        private void enableOrDisableControls(bool enable)
        {
            control.polygonListControl.IsEnabled = enable;
            control.annoListControl.IsEnabled = enable;
            control.navigator.IsEnabled = enable;
            control.signalAndAnnoGrid.IsEnabled = enable;
            control.mediaCloseButton.IsEnabled = enable;
        }


        public void undoColorChanges(PolygonLabel updatedLabel, Color oldColor)
        {
            for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
            {
                PolygonLabel polygonLabel = control.polygonListControl.polygonDataGrid.SelectedItems[i] as PolygonLabel;
                if (polygonLabel != null && polygonLabel.ID == updatedLabel.ID)
                {
                    polygonLabel.Color = oldColor;
                    control.polygonListControl.polygonDataGrid.UpdateLayout();
                    control.polygonListControl.polygonDataGrid.Items.Refresh();
                    return;
                }
            }
        }

        private bool isPolygonDataGridValueNotNullAndCountNotZero()
        {
            return control.polygonListControl.polygonDataGrid.SelectedItem != null && control.polygonListControl.polygonDataGrid.SelectedItems.Count != 0;
        }
        private bool isPolygonDataGridValueNotNullAndCountEqualOne()
        {
            return control.polygonListControl.polygonDataGrid.SelectedItem != null && control.polygonListControl.polygonDataGrid.SelectedItems.Count == 1;
        }

        private bool isAnnoDataGridValueNotNullAndCountEqualOne()
        {
            return control.annoListControl.annoDataGrid.SelectedValue != null && control.annoListControl.annoDataGrid.SelectedItems.Count == 1;
        }

        private bool isAnnoDataGridItemsNotNullAndNotZero()
        {
            return control.annoListControl.annoDataGrid.Items != null && control.annoListControl.annoDataGrid.Items.Count > 0;
        }

        private bool isSchemeTypePolygon()
        {
            return AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON;
        }

        private bool isSlectedItemInAnnoDataGridAndPolygonDataGridNotNull()
        {
            return control.annoListControl.annoDataGrid.SelectedItem != null && control.polygonListControl.polygonDataGrid.SelectedItem != null;
        }
    }
}

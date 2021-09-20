using ssi.Types;
using ssi.Types.Polygon;
using ssi.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Label = ssi.Types.Label;

namespace ssi
{
    public partial class MainHandler
    {
        private CreationInformation creationInfos;
        private EditInformations editInfos;
        private DataGridChecker dataGridChecker;
        private IPolygonDrawUnit polygonDrawUnit;
        private IPolygonUtilities polygonUtilities;
        private String currentLabelName = "";
        private Color currentLabelColor;

        public void polygonHandlerInit()
        {
            creationInfos = new CreationInformation();
            editInfos = new EditInformations();
            dataGridChecker = new DataGridChecker(this.control);
            polygonDrawUnit = new PolygonDrawUnit(this.control, creationInfos, editInfos);
            polygonUtilities = new PolygonUtilities(this.control, creationInfos, editInfos, dataGridChecker, polygonDrawUnit);
            polygonDrawUnit.PolygonUtilities = polygonUtilities;
            control.annoListControl.PolygonUtilities = polygonUtilities;
        }

        private void discretePolygonSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(control.polygonListControl.editComboBox.SelectedItem is object)
            {
                currentLabelName = ((Label)control.polygonListControl.editComboBox.SelectedItem).Name;
                currentLabelColor = ((Label)control.polygonListControl.editComboBox.SelectedItem).Color;
            }
            else
            {
                currentLabelName = "";
                currentLabelColor = Colors.Black;
            }
            
        }

        private void polygonAddNewLabelButton_Click(object sender, RoutedEventArgs e)
        {
            AnnoScheme scheme = AnnoTierStatic.Selected.AnnoList.Scheme;

            String name = scheme.DefaultLabel;
            Color color = scheme.DefaultColor;
            if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                name = currentLabelName;
                color = currentLabelColor;
            }

            PolygonLabel polygonLabel = new PolygonLabel(null, name, color);
            editInfos.IsEditModeOn = false;
            creationInfos.IsCreateModeOn = true;
            polygonUtilities.addPolygonLabelToPolygonList(polygonLabel);
            polygonUtilities.enableOrDisableControls(false);
        }

        private void polygonAddmoreLabels_Click(object sender, RoutedEventArgs e)
        {
            creationInfos.AddMoreLabels = true;
            polygonAddNewLabelButton_Click(sender, e);
        }

        private void polygonSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsNotZero())
            {
                control.polygonListControl.polygonDataGrid.SelectAll();
            }
        }

        public void selectSpecificLabel(PolygonLabel pl)
        {
            if(dataGridChecker.annonDGIsNotNullAndCountsNotZero() && polygonUtilities.labelIsNotSelected(pl))
            {
                control.polygonListControl.polygonDataGrid.SelectedItem = pl;
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
                                    list[i].updateLabelCount();
                                }
                                polygonUtilities.refreshAnnoDataGrid();
                                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
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

        private void polygonInterpolateLabels_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsZero() && dataGridChecker.polygonDGIsNotNullAndCountsZero())
                control.annoListControl.annoDataGrid.SelectedItem = control.annoListControl.annoDataGrid.Items[0];

            if (mediaBoxes.Count == 1)
            {
                Window dialog = null;
                AnnoScheme scheme;

                scheme = AnnoTierStatic.Selected.AnnoList.Scheme;
                AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
                dialog = new InterpolationWindow(list, this);

                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();
            }
            else
            {
                MessageBoxResult mb = MessageBoxResult.OK;
                mb = MessageBox.Show("Interpolation only possible with one opened media.", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void handle2DInterpolation(ComboboxItem sourceFrame, PolygonLabel sourceLabel, PolygonLabel targetLabel, double xStepPerFrame, double yStepPerFrame, int framesBetween)
        {
            AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
            int newLabelCounter = 0;
            bool toStart = false;
            foreach (AnnoListItem listItem in list)
            {
                if(listItem.Equals(sourceFrame.Value))
                {
                    toStart = true;
                    newLabelCounter++;
                    continue;
                }

                if(toStart)
                {
                    List<PolygonPoint> newPolygon = new List<PolygonPoint>();

                    foreach(PolygonPoint point in sourceLabel.Polygon)
                    {
                        newPolygon.Add(new PolygonPoint(point.X + (newLabelCounter * xStepPerFrame), point.Y + (newLabelCounter * yStepPerFrame)));
                    }

                    PolygonLabel newLabel = new PolygonLabel(newPolygon, sourceLabel.Label, sourceLabel.Color);
                    listItem.PolygonList.addPolygonLabel(newLabel);
                    listItem.updateLabelCount();
                    newLabelCounter++;
                    
                    if (newLabelCounter == (framesBetween + 1))
                    {
                        listItem.PolygonList.removeExplicitPolygon(targetLabel);
                        polygonUtilities.refreshAnnoDataGrid();
                        listItem.updateLabelCount();
                        break;
                    }
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }

        public void handle3DInterpolation(PolygonLabel souceLabel, PolygonLabel targetLabel, List<PointAngleTuple> sourcePointsAnglesTuple, List<PointAngleTuple> targetPointsAnglesTuple, int framesBetween)
        {
            
        }

        private void polygonRelabelButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsOne() && dataGridChecker.polygonDGIsNotNullAndCountsNotZero())
            {
                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON)
                {
                    if (control.polygonListControl.editTextBox.Text.Length > 0)
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
                                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                                    break;
                                }
                            }
                        }
                        item.PolygonList.Polygons = polygonLabels;
                        polygonUtilities.polygonSelectItem(item);
                        control.polygonListControl.editTextBox.Text = "";
                        return;
                    }
                    else
                    {
                        MessageBox.Show("You can not replace your label name with an empty string", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }
                }
                else
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
                                polygonLabels[j].Label = ((Label)control.polygonListControl.editComboBox.SelectedItem).Name;
                                polygonLabels[j].Color = ((Label)control.polygonListControl.editComboBox.SelectedItem).Color;
                                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                                break;
                            }
                        }
                    }
                    item.PolygonList.Polygons = polygonLabels;
                    polygonUtilities.polygonSelectItem(item);
                    return;
                }
            }

            MessageBox.Show("It was not possible to update the label name\nMake sure you have selected a frame and a label", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);            
        }

        

        private void polygonContextMenueOpened(object sender, RoutedEventArgs e)
        {
            int amountOfSelectedPolygons = control.polygonListControl.polygonDataGrid.SelectedItems.Count;
            int amountOfSelectedFrames = control.annoListControl.annoDataGrid.SelectedItems.Count;
            if(amountOfSelectedPolygons > 1 || amountOfSelectedFrames > 1 || amountOfSelectedPolygons == 0 || amountOfSelectedFrames == 0)
            {
                foreach (MenuItem item in control.polygonListControl.InvoiceDetailsList.Items)
                {
                    if (item.Name == "editPolygon")
                    {
                        item.IsEnabled = false;
                    }
                }
            }
            else
            {
                foreach (MenuItem item in control.polygonListControl.InvoiceDetailsList.Items)
                {
                    if (item.Name == "editPolygon")
                    {
                        item.IsEnabled = true;
                    }
                }
            }
        }

        private void polygonListElementDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.polygonDGIsNotNullAndCountsNotZero())
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];

                for (int i = 0; i < control.polygonListControl.polygonDataGrid.SelectedItems.Count; i++)
                {
                    item.PolygonList.removeExplicitPolygon((PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[i]);
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                }
              
                item.updateLabelCount();
                polygonUtilities.refreshAnnoDataGrid();
                polygonUtilities.polygonSelectItem(item);
            }
        }

        private void polygonList_Selection(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridChecker.polygonDGIsNotNullAndCountsOne())
            {
                creationInfos.LastSelectedLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[0];

                if (!creationInfos.IsCreateModeOn)
                    polygonUtilities.activateEditMode();

                PolygonLabel polygon = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[0];

                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON)
                {
                    control.polygonListControl.editTextBox.Text = polygon.Label;
                }
                else
                {
                    control.polygonListControl.editComboBox.SelectedItem = new Label(polygon.Label, polygon.Color);
                }
            }
            else
            {
                editInfos.IsEditModeOn = false;
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }
 

        private void OnPolygonMediaMouseDown(IMedia media, double x, double y)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (dataGridChecker.isSchemeTypePolygon() && dataGridChecker.annonDGIsNotNull() && dataGridChecker.polygonDGIsNotNull())
                {
                    if(creationInfos.IsCreateModeOn)
                    {
                        bool lastPoint = polygonUtilities.addNewPoint(x, y);

                        if (lastPoint && creationInfos.AddMoreLabels)
                        {
                            creationInfos.IsPolylineToDraw = false;
                            this.polygonAddNewLabelButton_Click(null, null);
                        }

                        if (lastPoint && !creationInfos.AddMoreLabels)
                        {
                            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                            polygonUtilities.endCreationMode(item);
                        }

                    }
                    else if (editInfos.IsEditModeOn)
                    {
                        polygonUtilities.editPolygon(x, y);
                    }
                }
            }
        }           

        private void OnPolygonMediaMouseUp(IMedia media, double x, double y)
        {
            if (dataGridChecker.isSchemeTypePolygon() && dataGridChecker.annonDGIsNotNull() && dataGridChecker.polygonDGIsNotNull())
            {
                if (editInfos.IsEditModeOn)
                {
                    if(editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                    {
                        PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                        List<Point> polygonPoints = new List<Point>();

                        foreach (PolygonPoint point in polygonLabel.Polygon)
                        {
                            polygonPoints.Add(new Point(point.X, point.Y));
                        }
                        if(!targetPositionEqualsSource(editInfos.PolygonStartPosition, polygonPoints))
                            polygonUtilities.PolygonUndoRedoStack.Do(new ChangeCompletePolygonCommand(editInfos.PolygonStartPosition, polygonPoints, polygonLabel));
                        editInfos.IsLeftMouseDown = false;
                        control.Cursor = Cursors.Hand;
                    }
                }
            }
        }

        private bool targetPositionEqualsSource(List<Point> start, List<Point> target)
        {
            if (start is null || target is null)
                return true;
            
            for(int i = 0; i < start.Count; i++)
            {
                if (start[i].X != target[i].X || start[i].Y != target[i].Y)
                    return false;
            }
            return true;
        }

        public void OnPolygonMediaBoxMouseMove(object sender, MouseEventArgs e)
        {
            if (creationInfos.IsCreateModeOn)
             {
                control.Cursor = Cursors.Cross;
            }
        }

        public void handleKeyDownEvent(object sender, KeyEventArgs e)
        {
            if (creationInfos.IsCreateModeOn)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.Escape))
                {
                    polygonUtilities.escClickedCWhileProbablyCreatingPolygon();
                }

                if (creationInfos.IsPolylineToDraw)
                {
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Z))
                    {
                        PolygonLabel currentPolygonLabel = control.polygonListControl.polygonDataGrid.SelectedItem as PolygonLabel;
                        AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
                        polygonDrawUnit.removeLastPoint(currentPolygonLabel, item);
                        if(currentPolygonLabel.Polygon.Count == 0)
                        {
                            polygonUtilities.endCreationMode(item, currentPolygonLabel);
                        }
                    }
                }
            }
            else if (editInfos.IsEditModeOn)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Z))
                {
                    AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;

                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        PolygonLabel label = polygonUtilities.PolygonUndoRedoStack.Redo();
                        polygonDrawUnit.polygonOverlayUpdate(item);
                    }
                    else
                    {
                        PolygonLabel label = polygonUtilities.PolygonUndoRedoStack.Undo();
                        polygonDrawUnit.polygonOverlayUpdate(item);
                    }
                }
                else if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && polygonUtilities.mouseIsOnLine())
                {
                    control.Cursor = Cursors.Cross;
                }
            }
        }


        public void handleKeyUpEvent(object sender, KeyEventArgs e)
        {
           if (editInfos.IsEditModeOn)
            {
                if (e.Key == Key.LeftCtrl && !e.KeyboardDevice.IsKeyDown(Key.RightCtrl) || e.Key == Key.RightCtrl && !e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    control.Cursor = Cursors.Arrow;
                }
            }
        }

        void OnPolygonMediaMouseMove(IMedia media, double x, double y)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed && editInfos.IsLeftMouseDown)
            {
                editInfos.IsLeftMouseDown = false;

                if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                {
                    PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                    List<Point> polygonPoints = new List<Point>();

                    foreach (PolygonPoint point in polygonLabel.Polygon)
                    {
                        polygonPoints.Add(new Point(point.X, point.Y));
                    }

                    polygonUtilities.PolygonUndoRedoStack.Do(new ChangeCompletePolygonCommand(editInfos.PolygonStartPosition, polygonPoints, polygonLabel));
                }
            }

            if (creationInfos.IsCreateModeOn && creationInfos.IsPolylineToDraw)
            {
                creationInfos.LastKnownPoint = new System.Windows.Point(x, y);
                polygonDrawUnit.drawLineToMousePosition(x, y);

                if (Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    int MAX_DISTANCE = Convert.ToInt32(Properties.Settings.Default.DefaultPolygonPointDistance);
                    double currentDistance = Math.Sqrt(Math.Pow(creationInfos.LastPrintedPoint.X - x, 2) + Math.Pow(creationInfos.LastPrintedPoint.Y - y, 2));

                    if (currentDistance > MAX_DISTANCE)
                    {
                        OnPolygonMediaMouseDown(null, x, y);
                    }
                }
            }
            else if (editInfos.IsEditModeOn)
            {
                creationInfos.LastKnownPoint = new System.Windows.Point(x, y);
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;

                if (editInfos.IsLeftMouseDown)
                {
                    if (editInfos.IsAboveSelectedPolygonPoint)
                    {
                        editInfos.IsWithinSelectedPolygonArea = false;
                        polygonUtilities.updatePoint(x, y, item);
                        polygonDrawUnit.polygonOverlayUpdate(item);
                    }
                    else if (editInfos.IsWithinSelectedPolygonArea)
                    {
                        editInfos.IsAboveSelectedPolygonPoint = false;
                        polygonUtilities.updatePolygon(x, y, item);
                        polygonDrawUnit.polygonOverlayUpdate(item);
                    }
                }
                else
                {
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                    {
                        if(polygonUtilities.mouseIsOnLine())
                        {
                            control.Cursor = Cursors.Cross;
                            editInfos.restetInfos();
                        }
                        else
                        {
                            control.Cursor = Cursors.Arrow;
                            if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                            {
                                editInfos.restetInfos();
                                polygonDrawUnit.polygonOverlayUpdate(item);
                            }
                        }
                    }
                    else
                    {
                        if (polygonUtilities.isMouseAbovePoint(x, y))
                        {
                            control.Cursor = Cursors.Hand;
                            polygonDrawUnit.polygonOverlayUpdate(item);
                        }
                        else if (polygonUtilities.isMouseWithinPolygonArea(x, y))
                        {
                            editInfos.IsAboveSelectedPolygonPoint = false;
                            editInfos.IsWithinSelectedPolygonArea = true;
                            control.Cursor = Cursors.Hand;
                            polygonDrawUnit.polygonOverlayUpdate(item);
                        }
                        else
                        {
                            if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                            {
                                control.Cursor = Cursors.Arrow;
                                editInfos.restetInfos();
                                polygonDrawUnit.polygonOverlayUpdate(item);
                            }
                        }
                    }
                    
                }


            }
        }
         
        private void onPolygonMediaMouseLeave(object sender, MouseEventArgs e)
        {
            control.Cursor = Cursors.Arrow;
        }

        public bool updateFrameData(PolygonLabel pl)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsOne())
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
                            AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                            break;
                        }
                    }
                }
                item.PolygonList.Polygons = polygonLabels;
                polygonUtilities.polygonSelectItem(item);
                return true;
            }

            MessageBox.Show("It was not possible to update the color\n Make sure you have selected a Frame", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            return false;
        }

        public void undoColorChanges(PolygonLabel updatedLabel, System.Windows.Media.Color oldColor)
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
    }
}

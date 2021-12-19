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
using static ssi.Types.Polygon.LabelInformations;

namespace ssi
{
    public partial class MainHandler
    {
        #region INIT

        private CreationInformation creationInfos;
        private EditInformations editInfos;
        private DataGridChecker dataGridChecker;
        private IDrawUnit polygonDrawUnit;
        private Utilities polygonUtilities;
        private String currentLabelName = "";
        private Color currentLabelColor;

        public string CurrentLabelName { get => currentLabelName; set => currentLabelName = value; }
        public Color CurrentLabelColor { get => currentLabelColor; set => currentLabelColor = value; }

        public void polygonHandlerInit()
        {
            creationInfos = new CreationInformation();
            editInfos = new EditInformations();
            dataGridChecker = new DataGridChecker(this.control);
            polygonDrawUnit = new DrawUnit(this.control, creationInfos, editInfos);
            polygonUtilities = new Utilities(this.control, this, creationInfos, editInfos, dataGridChecker, polygonDrawUnit);
            polygonDrawUnit.PolygonUtilities = polygonUtilities;
            control.annoListControl.PolygonUtilities = polygonUtilities;
        }

        #endregion INIT

        #region CLICKS

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

        private void polygonSelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsNotZero())
            {
                control.polygonListControl.polygonDataGrid.SelectAll();
            }
        }

        private void polygonInterpolateLabels_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsZero() && dataGridChecker.polygonDGIsNotNullAndCountsZero())
                control.annoListControl.annoDataGrid.SelectedItem = control.annoListControl.annoDataGrid.Items[0];

            if (mediaBoxes.Count == 1)
            {
                if (this.polygonUtilities.InterpolationWindow == null)
                {
                    AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
                    int i = control.annoListControl.annoDataGrid.SelectedIndex;
                    this.polygonUtilities.InterpolationWindow = new InterpolationWindow(polygonUtilities);
                    this.polygonUtilities.InterpolationWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    this.polygonUtilities.InterpolationWindow.Show();
                }
                else
                {
                    this.polygonUtilities.InterpolationWindow.Activate();
                }
            }
            else
            {
                MessageBoxResult mb = MessageBoxResult.OK;
                mb = MessageBox.Show("Interpolation only possible with one opened media.", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
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

        private void polygonStopInsertion_Click(object sender, RoutedEventArgs e)
        {
            // We have to end the CreateMode -> simulate one or two esc-clicks (one if we didnt start a polygon 
            polygonUtilities.escClickedCWhileProbablyCreatingPolygon();
            if (creationInfos.IsCreateModeOn)
                polygonUtilities.escClickedCWhileProbablyCreatingPolygon();
        }

        private void OnPolygonMedia_MouseUp(IMedia media, double x, double y)
        {
            x = Math.Round(x);
            y = Math.Round(y);

            if (dataGridChecker.isSchemeTypePolygon() && dataGridChecker.annonDGIsNotNull() && dataGridChecker.polygonDGIsNotNull())
            {
                if (!creationInfos.IsCreateModeOn)
                {
                    if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                    {
                        PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                        List<Point> polygonPoints = new List<Point>();

                        foreach (PolygonPoint point in polygonLabel.Polygon)
                        {
                            polygonPoints.Add(new Point(point.X, point.Y));
                        }
                        if (!polygonUtilities.targetPositionEqualsSource(editInfos.PolygonStartPosition, polygonPoints))
                            polygonUtilities.UndoRedoStack.Do(new ChangeCompletePolygonCommand(editInfos.PolygonStartPosition, polygonPoints, polygonLabel, TYPE.EDIT));
                    }
                }
            }
        }

        private void polygonAddLabels_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsZero() && dataGridChecker.polygonDGIsNotNullAndCountsZero())
                control.annoListControl.annoDataGrid.SelectedItem = control.annoListControl.annoDataGrid.Items[0];

            // TODO MARCO: nur machen wenn video geladen und wenn z.B. zwei videos vorhanden und das erste gelöscht wird
            polygonUtilities.updateImageSize();

            creationInfos.IsCreateModeOn = true;
            polygonUtilities.enableOrDisableControls(false);
            control.polygonListControl.polygonDataGrid.SelectedItem = null;
        }
        #endregion CLICKS 

        #region SELECTION_CHANGED

        private void discretePolygon_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

        private void polygonList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dataGridChecker.polygonDGIsNotNullAndCountsOne())
            {
                creationInfos.LastSelectedLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[0];

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

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }
        #endregion SELECTION_CHANGED

        #region MOUSE

        private void OnPolygonMainControlMouseUp(object sender, MouseButtonEventArgs e)
        {
            polygonUtilities.enableOrDisableAllControls(true);
        }

        private void OnPolygonMedia_MouseDown(IMedia media, double x, double y)
        {
            x = Math.Round(x);
            y = Math.Round(y);

            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                if (dataGridChecker.isSchemeTypePolygon() && dataGridChecker.annonDGIsNotNull() && dataGridChecker.polygonDGIsNotNull())
                {
                    // TODO MARCO: nur machen wenn video geladen und wenn z.B. zwei videos vorhanden und das erste gelöscht wird
                    polygonUtilities.updateImageSize();
                    if (creationInfos.IsCreateModeOn)
                    {
                        if(polygonUtilities.posIs5pxFromBottomAway(y))
                        {
                            bool lastPoint = polygonUtilities.addNewPoint(x, y);

                            if (lastPoint)
                            {
                                creationInfos.IsPolylineToDraw = false;
                                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                            }
                        }
                    }
                    else
                    {
                        if (editInfos.IsAboveSelectedPolygonLineAndCtrlPressed)
                        {
                            polygonUtilities.addNewPointToDonePolygon(x, y);
                        }
                        else if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                        {
                            polygonUtilities.editPolygon(x, y);
                        }
                        else
                        {

                        }
                    }
                }
            }
        }

        void OnPolygonMedia_MouseMove(IMedia media, double x, double y)
        {
            this.editInfos.MouseOnMedia = true;
            x = Math.Round(x);
            y = Math.Round(y);
            creationInfos.LastKnownPoint = new System.Windows.Point(x, y);

            if (creationInfos.IsCreateModeOn)
            {
                if(creationInfos.IsPolylineToDraw)
                {
                    polygonDrawUnit.drawLineToMousePosition(x, y);

                    if (Mouse.LeftButton == MouseButtonState.Pressed)
                    {
                        int MAX_DISTANCE = Convert.ToInt32(Properties.Settings.Default.DefaultPolygonPointDistance);
                        double currentDistance = Math.Sqrt(Math.Pow(creationInfos.LastPrintedPoint.X - x, 2) + Math.Pow(creationInfos.LastPrintedPoint.Y - y, 2));

                        if (currentDistance > MAX_DISTANCE)
                        {
                            OnPolygonMedia_MouseDown(null, x, y);
                        }
                    }
                }
            }
            else 
            {
                AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;

                if (Mouse.LeftButton == MouseButtonState.Pressed && (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea))
                {
                    if (editInfos.IsAboveSelectedPolygonPoint)
                    {
                        polygonUtilities.updatePoint(x, y, item);
                    }
                    else if (editInfos.IsWithinSelectedPolygonArea)
                    {
                        polygonUtilities.updatePolygon(x, y, item);
                    }

                    polygonDrawUnit.polygonOverlayUpdate(item);
                }
                else
                {
                    editInfos.restetInfos();
                    if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && polygonUtilities.mouseIsOnLine())
                    {
                        editInfos.IsAboveSelectedPolygonLineAndCtrlPressed = true;
                    }
                    else
                    {
                        if (polygonUtilities.isMouseAbovePoint(x, y))
                        {
                            editInfos.IsAboveSelectedPolygonPoint = true;
                        }
                        else if (polygonUtilities.isMouseWithinPolygonArea(x, y))
                        {
                            editInfos.IsWithinSelectedPolygonArea = true;
                        }

                        polygonDrawUnit.polygonOverlayUpdate(item);
                    }
                }
            }
        }

        // We handel here all the cursor stuff
        private void OnPolygonMainControlMouseMove(object sender, MouseEventArgs e)
        {
            if (this.editInfos.MouseOnMedia)
            {
                if (creationInfos.IsCreateModeOn && polygonUtilities.posIs5pxFromBottomAway(creationInfos.LastKnownPoint.Y)) 
                {
                    control.Cursor = Cursors.Cross;
                }
                else
                {
                    if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && editInfos.IsAboveSelectedPolygonLineAndCtrlPressed)
                    {
                        control.Cursor = Cursors.Cross;
                    }
                    else if (editInfos.IsAboveSelectedPolygonPoint || editInfos.IsWithinSelectedPolygonArea)
                    {
                        control.Cursor = Cursors.Hand;
                    }
                    else
                    {
                        control.Cursor = Cursors.Arrow;
                    }
                }
                this.editInfos.MouseOnMedia = false;
            }
            else
            {
                control.Cursor = Cursors.Arrow;
            }
        }
        #endregion MOUSE

        #region KEY

        public void handleKeyDownEvent(object sender, KeyEventArgs e)
        {
            if (creationInfos.IsCreateModeOn)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.Escape))
                {
                    polygonUtilities.escClickedCWhileProbablyCreatingPolygon();
                }
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            if (e.KeyboardDevice.IsKeyDown(Key.Z))
            {
                if (e.KeyboardDevice.IsKeyDown(Key.LeftShift) || e.KeyboardDevice.IsKeyDown(Key.RightShift))
                {
                    int pointsBeforeRedo = 0;
                    if (control.polygonListControl.polygonDataGrid.SelectedItem != null)
                    {
                        pointsBeforeRedo = ((PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem).Polygon.Count;
                    }

                    PolygonLabel label = polygonUtilities.UndoRedoStack.Redo();

                    if (label != null)
                    {
                        if (label.Informations.Type == TYPE.CREATION)
                        {
                            if (!creationInfos.IsCreateModeOn)
                            {
                                creationInfos.IsCreateModeOn = true;
                                polygonUtilities.enableOrDisableControls(false);
                            }

                            int pointsAfterRedo = -1;
                            if (control.polygonListControl.polygonDataGrid.SelectedItem != null)
                            {
                                pointsAfterRedo = ((PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem).Polygon.Count;
                            }
                            if (control.polygonListControl.polygonDataGrid.SelectedItem == null)
                            {
                                item.PolygonList.addPolygonLabel(label);
                                polygonUtilities.refreshAnnoDataGrid();
                                polygonUtilities.polygonSelectItem(item);
                                creationInfos.IsPolylineToDraw = true;
                            }
                            if (pointsBeforeRedo == pointsAfterRedo)
                            {
                                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                                creationInfos.IsPolylineToDraw = false;
                            }
                        }
                        else if (label.Informations.Type == TYPE.EDIT)
                        {
                            if (creationInfos.IsCreateModeOn)
                            {
                                creationInfos.IsCreateModeOn = false;
                                polygonUtilities.enableOrDisableControls(true);
                            }
                        }
                        else if (label.Informations.Type == TYPE.EXTRA_POINT)
                        {
                            if (creationInfos.IsCreateModeOn)
                            {
                                creationInfos.IsCreateModeOn = false;
                                polygonUtilities.enableOrDisableControls(true);
                            }
                        }
                    }
                }
                else
                {
                    PolygonLabel label = polygonUtilities.UndoRedoStack.Undo();
                    if(label != null)
                    {
                        if(label.Informations.Type == TYPE.CREATION)
                        {
                            
                            if(!creationInfos.IsCreateModeOn)
                            {
                                creationInfos.IsCreateModeOn = true;
                                polygonUtilities.enableOrDisableControls(false);
                            }

                            // We draw the line to the mouse and select the label
                            if (label.Polygon.Count > 0)
                            {
                                creationInfos.IsPolylineToDraw = true;
                                control.polygonListControl.polygonDataGrid.SelectedItem =
                                    control.polygonListControl.polygonDataGrid.Items[control.polygonListControl.polygonDataGrid.Items.Count - 1];
                            }
                            else
                            {
                                item.PolygonList.removeExplicitPolygon(label);
                                item.updateLabelCount();
                                polygonUtilities.refreshAnnoDataGrid();
                                polygonUtilities.polygonSelectItem(item);
                                control.polygonListControl.polygonDataGrid.SelectedItem = null;
                                creationInfos.IsPolylineToDraw = false;
                            }

                            
                        }
                        else if(label.Informations.Type == TYPE.EDIT)
                        {
                            if (creationInfos.IsCreateModeOn)
                            {
                                creationInfos.IsCreateModeOn = false;
                                polygonUtilities.enableOrDisableControls(true);
                            }
                        }
                        else if(label.Informations.Type == TYPE.EXTRA_POINT)
                        {
                            if (creationInfos.IsCreateModeOn)
                            {
                                creationInfos.IsCreateModeOn = false;
                                polygonUtilities.enableOrDisableControls(true);
                            }
                        }
                    }
                }

                polygonDrawUnit.polygonOverlayUpdate(item);
                if (creationInfos.IsPolylineToDraw)
                    polygonDrawUnit.drawLineToMousePosition(creationInfos.LastKnownPoint.X, creationInfos.LastKnownPoint.Y);

            }
        }


        public void handleKeyUpEvent(object sender, KeyEventArgs e)
        {
            if (!creationInfos.IsCreateModeOn)
            {
                if (e.Key == Key.LeftCtrl && !e.KeyboardDevice.IsKeyDown(Key.RightCtrl) || e.Key == Key.RightCtrl && !e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    control.Cursor = Cursors.Arrow;
                }
            }
        }

        #endregion KEY

        #region REST

        private void polygonContextMenue_Open(object sender, RoutedEventArgs e)
        {
            int amountOfSelectedPolygons = control.polygonListControl.polygonDataGrid.SelectedItems.Count;
            int amountOfSelectedFrames = control.annoListControl.annoDataGrid.SelectedItems.Count;
            if (amountOfSelectedPolygons > 1 || amountOfSelectedFrames > 1 || amountOfSelectedPolygons == 0 || amountOfSelectedFrames == 0)
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
    
        #endregion REST
        
    }
}

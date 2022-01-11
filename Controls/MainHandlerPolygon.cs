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
using System.Linq;
using ssi.Tools.Polygon_Helper;

namespace ssi
{
    public partial class MainHandler
    {
        #region INIT

        private Color currentLabelColor;
        private IDrawUnit polygonDrawUnit;
        private LabelCreator labelCreator;
        private EditInformations editInfos;
        private PolygonEditor polygonEditor;
        private String currentLabelName = "";
        private DataGridChecker dataGridChecker;
        private CreationInformation creationInfos;
        private PolygonUtilities polygonUtilities;
        private PolygonKeyHandler polygonKeyHandler;
        private UIElementsController uIElementsController;
        private PolygonHandlerPerformer polygonHandlerPerformer;
        private MousePositionInformation mousePositionInformation;

        public string CurrentLabelName { get => currentLabelName; set => currentLabelName = value; }
        public Color CurrentLabelColor { get => currentLabelColor; set => currentLabelColor = value; }

        public void polygonHandlerInit()
        {
            editInfos = new EditInformations();
            creationInfos = new CreationInformation();
            dataGridChecker = new DataGridChecker(control);
            uIElementsController = new UIElementsController(control);
            control.annoListControl.PolygonUtilities = polygonUtilities;
            mousePositionInformation = new MousePositionInformation(control, creationInfos, editInfos);

            polygonDrawUnit = new DrawUnit();
            labelCreator = new LabelCreator();
            polygonEditor = new PolygonEditor();
            polygonUtilities = new PolygonUtilities();
            polygonKeyHandler = new PolygonKeyHandler();
            polygonHandlerPerformer = new PolygonHandlerPerformer(control, this, editInfos);

            polygonUtilities.setObjects(control, this, creationInfos, editInfos, dataGridChecker, polygonDrawUnit);
            polygonDrawUnit.setObjects(control, creationInfos, editInfos, polygonUtilities);
            polygonEditor.setObjects(polygonDrawUnit, control, editInfos, polygonUtilities);
            polygonKeyHandler.setObjects(control, labelCreator, creationInfos, polygonUtilities);
            polygonHandlerPerformer.setObjects(creationInfos, polygonDrawUnit, uIElementsController, polygonUtilities);
            labelCreator.setObjects(control, this, polygonDrawUnit.getDrawUtilities(), creationInfos, uIElementsController,
                dataGridChecker, polygonUtilities);
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

        private void polygonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.polygonDGIsNotNullAndCountsNotZero())
            {
                polygonHandlerPerformer.handleRemove();
            }
        }

        public void polygonPasteContextMenue_Click(object sender, EventArgs e)
        {
            polygonHandlerPerformer.handlePaste();
        }

        public void polygonCopyContextMenue_Click(object sender, EventArgs e)
        {
            polygonHandlerPerformer.handleCopy();
        }

        public void polygonCutContextMenue_Click(object sender, EventArgs e)
        {
            polygonHandlerPerformer.handleCut();
        }

        public void polygonDeleteContextMenue_Click(object sender, EventArgs e)
        {
            if (dataGridChecker.polygonDGIsNotNullAndCountsNotZero())
            {
                polygonHandlerPerformer.handleRemove();
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
                    this.polygonUtilities.InterpolationWindow = new InterpolationWindow(polygonUtilities, uIElementsController, polygonHandlerPerformer);
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
            polygonKeyHandler.escClickedCWhileProbablyCreatingPolygon();
            if (creationInfos.IsCreateModeOn)
                polygonKeyHandler.escClickedCWhileProbablyCreatingPolygon();
        }

        private void polygonAddLabels_Click(object sender, RoutedEventArgs e)
        {
            if (dataGridChecker.annonDGIsNotNullAndCountsZero() && dataGridChecker.polygonDGIsNotNullAndCountsZero())
                control.annoListControl.annoDataGrid.SelectedItem = control.annoListControl.annoDataGrid.Items[0];
            
            labelCreator.startCreationMode();
        }
        #endregion CLICKS 

        #region MENU_CLICK

        private void polygonPaste_Click(object sender, RoutedEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            polygonHandlerPerformer.handlePaste();
        }

        private void polygonCopy_Click(object sender, RoutedEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            polygonHandlerPerformer.handleCopy();
        }

        private void polygonCut_Click(object sender, RoutedEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            polygonHandlerPerformer.handleCut();
        }

        private void polygonRedo_Click(object sender, RoutedEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            polygonHandlerPerformer.handleRedo();
        }

        private void polygonUndo_Click(object sender, RoutedEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            polygonHandlerPerformer.handleUndo();
        }

        #endregion MENU_CLICK

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
            if (dataGridChecker.polygonDGIsNotNullAndCountsNotZero())
            {
                uIElementsController.enableOrDisableMenuControls(true, polygonHandlerPerformer.getItemsToChange());
                creationInfos.LastSelectedLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItems[0];

                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON)
                {
                    control.polygonListControl.editTextBox.Text = creationInfos.LastSelectedLabel.Label;
                }
                else
                {
                    control.polygonListControl.editComboBox.SelectedItem = new Label(creationInfos.LastSelectedLabel.Label, creationInfos.LastSelectedLabel.Color);
                }
            }
            else
            {
                uIElementsController.enableOrDisableMenuControls(false, polygonHandlerPerformer.getItemsToChange());
            }

            AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem;
            polygonDrawUnit.polygonOverlayUpdate(item);
        }
        #endregion SELECTION_CHANGED

        #region MOUSE
        void OnPolygonMedia_RightMouseDown(IMedia media, double x, double y)
        {
            if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON || AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                polygonHandlerPerformer.handleRightMouseDown(media, x, y);
            }
        }

        private void OnPolygonMainControlMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            if (!creationInfos.IsCreateModeOn)
                uIElementsController.enableOrDisableControls(true);

            if ((editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null) && !polygonHandlerPerformer.ContextMenuOpen)
            {
                 control.Cursor = Cursors.Hand;
            }

            if(editInfos.ShowingSelectionRect && !(editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null || editInfos.IsAboveSelectedPolygonLineAndCtrlPressed))
            {
                editInfos.ShowingSelectionRect = false;
                polygonDrawUnit.polygonOverlayUpdate((AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem);
                editInfos.restetInfos();
            }
        }

        private void OnPolygonMedia_MouseUp(IMedia media, double x, double y)
        {
            x = Math.Round(x);
            y = Math.Round(y);

            if (dataGridChecker.isSchemeTypePolygon() && dataGridChecker.annonDGIsNotNull() && dataGridChecker.polygonDGIsNotNull())
            {
                if (!creationInfos.IsCreateModeOn)
                {
                    if (editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null)
                    {
                        PolygonLabel polygonLabel = (PolygonLabel)control.polygonListControl.polygonDataGrid.SelectedItem;
                        List<Point> polygonPoints = new List<Point>();
                        if(polygonLabel != null)
                            foreach (PolygonPoint point in polygonLabel.Polygon)
                            {
                                polygonPoints.Add(new Point(point.X, point.Y));
                            }

                        AnnoListItem item = (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0];

                        if (!polygonUtilities.polygonPositionsEqual(editInfos.PolygonStartPosition, polygonPoints))
                            item.UndoRedoStack.Do(new ChangeLabelCommand(editInfos.PolygonStartPosition, polygonPoints, polygonLabel));
                    }
                }
            }
        }


        private void OnPolygonMedia_MouseDown(IMedia media, double x, double y)
        {
            if (!dataGridChecker.isSchemeTypePolygon() || Mouse.LeftButton != MouseButtonState.Pressed)
                return;

            x = Math.Round(x);
            y = Math.Round(y);

            if (polygonHandlerPerformer.ContextMenuOpen)
            {
                editInfos.resetSelectedElements();
                polygonHandlerPerformer.ContextMenuOpen = false;
                polygonUtilities.setPossiblySelectedPolygonAndSelectedPoint(x, y);
                polygonDrawUnit.polygonOverlayUpdate((AnnoListItem)control.annoListControl.annoDataGrid.SelectedItem);
                creationInfos.LastKnownPoint = new System.Windows.Point(x, y);
                return;
            }

            creationInfos.LastKnownPoint = new System.Windows.Point(x, y);

            if (dataGridChecker.annonDGIsNotNull() && dataGridChecker.polygonDGIsNotNull())
            {
                if (creationInfos.IsCreateModeOn)
                {
                    if(polygonUtilities.isPos5pxFromBottomAway(y))
                    {
                        bool lastPoint = labelCreator.addNewPoint(x, y);

                        if (lastPoint)
                        {
                            creationInfos.IsPolylineToDraw = false;
                            control.polygonListControl.polygonDataGrid.SelectedItem = null;
                        }
                    }
                }
                else
                {
                    if (!polygonUtilities.selectOrUnselectDependentOnMousePos(x, y))
                        return;

                    if (editInfos.IsAboveSelectedPolygonLineAndCtrlPressed && polygonUtilities.controlPressed())
                    {
                        polygonEditor.addNewPointToDonePolygon(x, y);
                    }
                    else if (editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null)
                    {
                        polygonEditor.editPolygon(x, y);
                    }
                    else
                    {
                        polygonUtilities.startSelectionRectangle(x, y);
                    }
                }
            }
        }

        void OnPolygonMedia_MouseMove(IMedia media, double x, double y)
        {
            if (!dataGridChecker.isSchemeTypePolygon() || polygonHandlerPerformer.ContextMenuOpen)
                return;

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

                if (Mouse.LeftButton == MouseButtonState.Pressed && (editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null))
                {
                    if (editInfos.SelectedPoint != null)
                    {
                        polygonUtilities.updatePoint(x, y, item);
                    }
                    else if (editInfos.SelectedPolygon != null)
                    {
                        polygonEditor.updatePolygon(x, y);
                    }

                    polygonDrawUnit.polygonOverlayUpdate(item);
                }
                else if(Mouse.LeftButton == MouseButtonState.Pressed && !(editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null || editInfos.IsAboveSelectedPolygonLineAndCtrlPressed))
                {
                    uIElementsController.enableOrDisableControls(false);
                    control.polygonListControl.polygonDataGrid.SelectedItem = null;
                    polygonUtilities.selectLabelsInSelectionRectangle(editInfos.StartPosition.X, editInfos.StartPosition.Y, x, y);
                    List<int> rectAsPolyline = polygonDrawUnit.getDrawUtilities().getRectanglePointsAsList(editInfos.StartPosition.X, editInfos.StartPosition.Y, x, y);
                    polygonDrawUnit.polygonOverlayUpdate(item, rectAsPolyline);
                }
                else
                { 
                    editInfos.restetInfos();
                    if (polygonUtilities.controlPressed() && mousePositionInformation.mouseIsOnLine())
                    {
                        editInfos.IsAboveSelectedPolygonLineAndCtrlPressed = true;
                    }
                    else
                    {
                        polygonUtilities.setPossiblySelectedPolygonAndSelectedPoint(x, y);
                        polygonDrawUnit.polygonOverlayUpdate(item);
                    }
                }
            }
        }

        // We handel here all the cursor stuff
        private void OnPolygonMainControlMouseMove(object sender, MouseEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            if (this.editInfos.MouseOnMedia)
            {
                if (creationInfos.IsCreateModeOn && polygonUtilities.isPos5pxFromBottomAway(creationInfos.LastKnownPoint.Y)) 
                {
                    control.Cursor = Cursors.Cross;
                }
                else
                {
                    if (polygonUtilities.controlPressed() && editInfos.IsAboveSelectedPolygonLineAndCtrlPressed)
                    {
                        control.Cursor = Cursors.Cross;
                    }
                    else if (editInfos.SelectedPoint != null || editInfos.SelectedPolygon != null)
                    {
                        if (Mouse.LeftButton == MouseButtonState.Pressed)
                            control.Cursor = Cursors.SizeAll;
                        else
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

        private void OnPolygonDataGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            control.polygonListControl.polygonDataGrid.SelectedItem = null;
        }
        #endregion MOUSE

        #region KEY

        public void handlePolygonKeyUpEvent(object sender, KeyEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            if (!polygonUtilities.controlPressed() && editInfos.IsAboveSelectedPolygonLineAndCtrlPressed)
            {
                editInfos.IsAboveSelectedPolygonLineAndCtrlPressed = false;
                control.Cursor = Cursors.Hand;
            }
        }

        public void handleKeyDownEvent(object sender, KeyEventArgs e)
        {
            if (!dataGridChecker.isSchemeTypePolygon())
                return;

            if (creationInfos.IsCreateModeOn)
            {
                if (Keyboard.IsKeyDown(Key.Escape))
                {
                    polygonKeyHandler.escClickedCWhileProbablyCreatingPolygon();
                    return;
                }
            }

            if(Keyboard.IsKeyDown(Key.Delete) || (Keyboard.IsKeyDown(Key.Back) && (Keyboard.IsKeyDown(Key.RightCtrl) || (Keyboard.IsKeyDown(Key.LeftCtrl)))))
            {
                polygonDelete_Click(sender, null);
                return;
            }

            if (polygonUtilities.controlPressed() && mousePositionInformation.mouseIsOnLine() && !Keyboard.IsKeyDown(Key.Z))
            {
                editInfos.IsAboveSelectedPolygonLineAndCtrlPressed = true;
                control.Cursor = Cursors.Cross;
                return;
            }
        }
    
        #endregion KEY
    }
}

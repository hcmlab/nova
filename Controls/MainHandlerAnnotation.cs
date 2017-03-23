using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ssi
{
    public partial class MainHandler
    {
        private void addAnnoTierFromList(AnnoList annoList)
        {
            double maxdur = 0;
            if (annoList.Count > 0)
            {
                maxdur = annoList[annoList.Count - 1].Stop;
            }
            
            addAnnoTier(annoList);            

            updateTimeRange(maxdur);
            if (annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) updateTimeRange(maxdur);

            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {           
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
            }

            annoList.HasChanged = false;          
        }

        private void removeAnnoTier()
        {
            AnnoTier tier = AnnoTierStatic.Selected;

            if (tier != null)
            {
                MessageBoxResult mb = MessageBoxResult.No;

                if (tier.AnnoList.HasChanged)
                {
                    mb = MessageBox.Show("Save annotations on tier #" + tier.Name + " first?", "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                    if (mb == MessageBoxResult.Yes)
                    {
                        tier.AnnoList.Save();
                    }
                }

                if (mb != MessageBoxResult.Cancel)
                {
                    control.annoTierControl.Remove(tier);

                    AnnoTierStatic.Unselect();
                    tier.Children.Clear();
                    tier.AnnoList.Clear();
                    annoTiers.Remove(tier);

                    if (annoTiers.Count > 0)
                    {
                        AnnoTierStatic.Select(annoTiers[0]);
                    }
                    else
                    {
                        clearAnnoInfo();
                    }
                }
            }
        }

        private void setAnnoList(AnnoList anno)
        {
            control.annoListControl.annoDataGrid.ItemsSource = anno;
        }

        private void setPointList(PointList pl)
        {
            control.geometricListControl.geometricDataGrid.ItemsSource = pl;
        }

        private void showHideGeometricGrid(bool show, AnnoScheme.TYPE type = AnnoScheme.TYPE.POINT)
        {
            double width = control.viewGridCol3.ActualWidth + control.viewGridCol1.ActualWidth;
            double col1MinWidth = Convert.ToDouble(control.viewGridCol1.MinWidth.ToString());
            double col3MinWidth = Convert.ToDouble(control.viewGridCol3.MinWidth.ToString());


            double height = control.myGridRow3.ActualHeight + control.myGridRow1.ActualHeight;
            double row1MinHeight = Convert.ToDouble(control.myGridRow1.MinHeight.ToString());
            double row3MinHeight = Convert.ToDouble(control.myGridRow3.MinHeight.ToString());

            Visibility visibility = Visibility.Visible;

            if (show)
            {
                control.geometricListControl.Visibility = Visibility.Visible;
                control.ListGridCol3.MinWidth = 305;
                control.ListGridCol3.Width = new GridLength(1, GridUnitType.Star);
                control.ListGridSplitter.Visibility = Visibility.Visible;

                control.viewGridCol3.Width = new GridLength(col3MinWidth, GridUnitType.Pixel);
                control.viewGridCol3.MaxWidth = width - col1MinWidth;
                control.viewGridCol1.Width = new GridLength(1, GridUnitType.Star);
                control.viewGridCol1.MaxWidth = width - col3MinWidth;

                control.myGridRow3.Height = new GridLength(row3MinHeight, GridUnitType.Pixel);
                control.myGridRow3.MaxHeight = height - row1MinHeight;
                control.myGridRow1.Height = new GridLength(1, GridUnitType.Star);
                control.myGridRow1.MaxHeight = height - row3MinHeight;
            }
            else
            {

                control.geometricListControl.Visibility = Visibility.Collapsed;
                control.ListGridCol3.MinWidth = 0;
                control.ListGridCol3.Width = new GridLength(0, GridUnitType.Star);
                control.ListGridSplitter.Visibility = Visibility.Collapsed;

                control.viewGridCol3.Width = new GridLength(1, GridUnitType.Star);
                control.viewGridCol3.MaxWidth = width - col1MinWidth;
                control.viewGridCol1.Width = new GridLength(col1MinWidth, GridUnitType.Pixel);
                control.viewGridCol1.MaxWidth = width - col3MinWidth;

                if(type != AnnoScheme.TYPE.DISCRETE && type != AnnoScheme.TYPE.FREE && type != AnnoScheme.TYPE.CONTINUOUS)
                {
                    control.myGridRow1.Height = new GridLength(row1MinHeight, GridUnitType.Pixel);
                    control.myGridRow1.MaxHeight = height - row3MinHeight;
                    control.myGridRow3.Height = new GridLength(1, GridUnitType.Star);
                    control.myGridRow3.MaxHeight = height - row1MinHeight;
                }

              

                visibility = Visibility.Collapsed;
            }

            Grid videoGrid = control.mediaVideoControl.videoGrid;
            int VNumChildren = videoGrid.Children.Count;
            for (int i = 0; i < VNumChildren; ++i)
            {
                if (videoGrid.Children[i].GetType().Name == "MediaBox")
                {
                    MediaBox mb = (MediaBox)videoGrid.Children[i];
                    int MBNumChildren = mb.mediaBoxGrid.Children.Count;
                    for (int j = 0; j < MBNumChildren; ++j)
                    {
                        var a = mb.mediaBoxGrid.Children[j].GetType().Name;
                        if (mb.mediaBoxGrid.Children[j].GetType().Name == "GeometricOverlay")
                        {
                            mb.mediaBoxGrid.Children[j].Visibility = visibility;
                        }
                    }

                }
            }
        }

        private void clearAnnoInfo()
        {
            control.annoStatusSettingsButton.IsEnabled = false;
            control.annoStatusFileNameOrSessionLabel.Text = "";
            control.annoStatusFileNameOrSessionLabel.ToolTip = "";
            control.annoStatusSchemeNameLabel.Text = "";
            control.annoStatusSchemeTypeLabel.Text = "";
            control.annoStatusSchemeContinuousPanel.Visibility = Visibility.Collapsed;
            control.annoStatusAnnotatorLabel.Text = "";
            control.annoStatusRoleLabel.Text = "";
            control.annoStatusSchemeTypeLabel.Text = "";
            control.annoStatusPositionLabel.Text = "00:00:00.00";
            control.annoStatusCloseButton.IsEnabled = false;
        }



        private void setAnnoInfo(AnnoList annoList)
        {
            control.annoStatusSettingsButton.IsEnabled = true;
            if (annoList.Source.HasFile())
            {
                control.annoStatusFileNameOrSessionLabel.Text = annoList.Source.File.FullName;
                control.annoStatusFileNameOrSessionLabel.ToolTip = annoList.Source.File.Path;
            }
            else if (annoList.Source.HasDatabase())
            {
                control.annoStatusFileNameOrSessionLabel.Text = annoList.Source.Database.Session;
                control.annoStatusFileNameOrSessionLabel.ToolTip = annoList.Source.Database.OID;
            }
            else
            {
                control.annoStatusFileNameOrSessionLabel.Text = "*";
                control.annoStatusFileNameOrSessionLabel.ToolTip = "Not saved yet";
            }

            control.annoStatusSchemeNameLabel.Text = annoList.Scheme.Name;
            if (annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                control.annoStatusSchemeTypeLabel.Text = annoList.Scheme.Type.ToString();
                control.annoStatusSchemeContinuousPanel.Visibility = Visibility.Visible;
                control.annoSchemeContinuousSrLabel.Text = annoList.Scheme.SampleRate.ToString() + " Hz";
                control.annoSchemeContinuousMinLabel.Text = "min " + annoList.Scheme.MinScore.ToString();
                control.annoSchemeContinuousMaxLabel.Text = "max " + annoList.Scheme.MaxScore.ToString();
            }
            else
            {
                control.annoStatusSchemeTypeLabel.Text = annoList.Scheme.Type.ToString();
                control.annoStatusSchemeContinuousPanel.Visibility = Visibility.Collapsed;
            }
            control.annoStatusAnnotatorLabel.Text = annoList.Meta.AnnotatorFullName != "" ? annoList.Meta.AnnotatorFullName : annoList.Meta.Annotator;
            control.annoStatusRoleLabel.Text = annoList.Meta.Role;
            control.annoStatusCloseButton.IsEnabled = true;
        }

        private void annoTierChange(AnnoTier tier, EventArgs e)
        {
            setAnnoInfo(tier.AnnoList);
            setAnnoList(tier.AnnoList);
            control.annoListControl.editComboBox.Items.Clear();

            if (AnnoTierStatic.Selected != null)
            {

                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.GRPAH ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                {
                    showHideGeometricGrid(true);
                }
                else
                {
                    showHideGeometricGrid(false, tier.AnnoList.Scheme.Type);
                }

                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    control.annoListControl.editButton.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.IsEnabled = false;
                    control.annoListControl.editTextBox.IsEnabled = false;
                }
                else if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    control.annoListControl.editButton.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.IsEnabled = false;
                    control.annoListControl.editTextBox.IsEnabled = false;
                }

                control.annoListControl.editComboBox.Items.Clear();
                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    control.annoListControl.editComboBox.IsEnabled = true;
                    control.annoListControl.editComboBox.Visibility = Visibility.Visible;
                    control.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editTextBox.IsEnabled = false;
                    control.annoListControl.editButton.Visibility = Visibility.Visible;

                    if (AnnoTierStatic.Selected.AnnoList.Scheme != null
                        && AnnoTierStatic.Selected.AnnoList.Scheme.Labels != null)
                    {
                        foreach (AnnoScheme.Label lcp in AnnoTierStatic.Selected.AnnoList.Scheme.Labels)
                        {
                            control.annoListControl.editComboBox.Items.Add(lcp.Name);
                        }

                        control.annoListControl.editComboBox.SelectedIndex = 0;
                    }
                }
                else if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    control.annoListControl.editTextBox.Visibility = Visibility.Visible;
                    control.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editButton.Visibility = Visibility.Visible;
                    control.annoListControl.editTextBox.IsEnabled = true;
                }
            }
        }

        private void changeAnnoTierSegmentHandler(AnnoTierSegment segment, EventArgs e)
        {

            if (IsPlaying())
            {
                Stop();
                Play();
            }

            foreach (AnnoListItem item in control.annoListControl.annoDataGrid.Items)
            {
                if (segment.Item.Start == item.Start)
                {
                    control.annoListControl.annoDataGrid.SelectedItem = item;
                    control.annoListControl.annoDataGrid.ScrollIntoView(control.annoListControl.annoDataGrid.SelectedItem);
                    break;
                }
            }
        }

        private void ShowLabelBox()
        {
            if (AnnoTierStatic.Selected == null || AnnoTierStatic.Label == null)
            {
                return;
            }

            AnnoTierNewLabelWindow dialog = new AnnoTierNewLabelWindow(AnnoTierStatic.Selected.AnnoList.Scheme, AnnoTierStatic.Label.Item);
            dialog.ShowDialog();

            if (dialog.DialogResult == false)
            {
                return;
            }

            AnnoTierStatic.Label.Item.Color = dialog.Result.Color;
            AnnoTierStatic.Label.Item.Confidence = dialog.Result.Confidence;
            AnnoTierStatic.Label.Item.Label = dialog.Result.Label;

            AnnoTierStatic.Selected.DefaultColor = dialog.Result.Color;
            AnnoTierStatic.Selected.DefaultLabel = dialog.Result.Label;

            if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
            {
                if (dialog.Result.Color != AnnoTierStatic.Selected.AnnoList.Scheme.MaxOrForeColor)
                {
                    foreach (AnnoListItem item in AnnoTierStatic.Selected.AnnoList)
                    {
                        if (item.Label == dialog.Result.Label)
                        {
                            item.Color = dialog.Result.Color;
                        }
                    }
                }
            }
        }

        private void ShowLabelBoxCont()
        {
            if (AnnoTierStatic.Selected != null)
            {
                AnnoTierNewLabelWindow dialog = new AnnoTierNewLabelWindow(AnnoTierStatic.Selected.AnnoList.Scheme, AnnoTierStatic.Label.Item);
                dialog.ShowDialog();

                double value;
                double confidence = dialog.Result.Confidence;
                bool success = false;

                if (dialog.DialogResult == true && double.TryParse(dialog.Result.Label, out value))
                {
                    string valueString = value.ToString();
                    if (value >= AnnoTierStatic.Selected.AnnoList.Scheme.MinScore && value <= AnnoTierStatic.Selected.AnnoList.Scheme.MaxScore)
                    {
                        success = true;
                        foreach (AnnoListItem ali in AnnoTierStatic.Selected.AnnoList)
                        {
                            if (ali.Start >= AnnoTierStatic.Label.Item.Start && ali.Stop <= AnnoTierStatic.Label.Item.Stop)
                            {
                                ali.Label = valueString;
                            }
                        }
                    }
                }

                if (!success)
                {
                    MessageBox.Show("Value must be a number in range [" + AnnoTierStatic.Selected.AnnoList.Scheme.MinScore + "..." + AnnoTierStatic.Selected.AnnoList.Scheme.MaxScore + "]");
                }
                else
                {
                    AnnoTierStatic.Selected.TimeRangeChanged(timeline);
                    AnnoTierStatic.Selected.TimeRangeChanged(timeline);
                }
            }
        }

        public void addAnnoTier(AnnoList anno)
        {
            setAnnoList(anno);

            AnnoTier tier = new AnnoTier(anno);
            control.annoTierControl.Add(tier);
            control.timeLineControl.rangeSlider.OnTimeRangeChanged += tier.TimeRangeChanged;

            annoTiers.Add(tier);
            annoLists.Add(anno);

            AnnoTierStatic.Select(tier);
            tier.TimeRangeChanged(Time);
        }

        private void reloadAnnoTier(string filename)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList anno = AnnoList.LoadfromFile(filename);
            double maxdur = 0;

            maxdur = anno[anno.Count - 1].Stop;

            if (anno != null && AnnoTierStatic.Selected != null)
            {
                setAnnoList(anno);
                AnnoTierStatic.Selected.Children.Clear();
                AnnoTierStatic.Selected.AnnoList.Clear();
                AnnoTierStatic.Selected.segments.Clear();
                AnnoTierStatic.Selected.AnnoList = anno;

                foreach (AnnoListItem item in anno)
                {
                    AnnoTierStatic.Selected.AddSegment(item);
                }

                AnnoTierStatic.Selected.TimeRangeChanged(MainHandler.Time);
            }

            updateTimeRange(maxdur);
            // if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
        }

        private void annoSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                AnnoTierSettingsWindow window = new AnnoTierSettingsWindow();
                window.DataContext = AnnoTierStatic.Selected;
                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                
                window.ShowDialog();                
                setAnnoInfo(AnnoTierStatic.Selected.AnnoList);
                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    foreach (AnnoListItem ali in control.annoListControl.annoDataGrid.ItemsSource)
                    {
                        ali.Color = AnnoTierStatic.Selected.MinOrBackColor;
                    }
                    geometricOverlayUpdate(AnnoScheme.TYPE.POINT);
                }


                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
            }
        }


        #region EVENTHANDLERS

        private void annoTrackGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double pos = e.GetPosition(control.trackGrid).X;
                annoCursor.X = pos;
                Time.CurrentSelectPosition = pos;
                double time = Time.TimeFromPixel(pos);
                control.annoStatusPositionLabel.Text = FileTools.FormatSeconds(time);
            }
            if ((e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed) && control.navigator.followAnnoCheckBox.IsChecked == true)
            {
                if (mediaList.Medias.Count > 0)
                {
                    mediaList.move(Time.TimeFromPixel(e.GetPosition(control.trackGrid).X));
                    moveSignalCursorToSecond(Time.TimeFromPixel(e.GetPosition(control.trackGrid).X));
                    Stop();
                }
            }

            if (e.RightButton == MouseButtonState.Released && isMouseButtonDown == true)
            {
                isMouseButtonDown = false;

                if (control.navigator.followAnnoCheckBox.IsChecked == true)
                {
                    bool is_playing = IsPlaying();

                    if (!is_playing)
                    {
                        Play();
                    }
                }
                if (control.navigator.askforlabels.IsChecked == true)
                {
                    if (AnnoTierStatic.Selected != null)
                    {
                        if (AnnoTierStatic.Selected.IsDiscreteOrFree)
                        {
                            ShowLabelBox();
                        }
                    }
                }
            }
        }

        private void annoTrackGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (control.navigator.askforlabels.IsChecked == true) AnnoTier.askForLabel = true;
            else AnnoTier.askForLabel = false;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (AnnoTierStatic.Label != null)
                {
                    AnnoTierStatic.Label.isMoveable = true;
                    AnnoTierStatic.Selected.LeftMouseButtonDown(e);
                }
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (AnnoTierStatic.Label != null) AnnoTierStatic.Label.select(false);

                if (AnnoTierStatic.Selected != null && (AnnoTierStatic.Selected.AnnoList.Scheme.Type != AnnoScheme.TYPE.CONTINUOUS || isMouseButtonDown == false))
                {
                    foreach (AnnoTier a in annoTiers)
                    {
                        if (a.IsMouseOver)
                        {
                            AnnoTier.SelectLabel(null);
                            AnnoTier.Select(a);
                            break;
                        }
                    }
                }

                if (AnnoTierStatic.Selected != null) AnnoTierStatic.Selected.RightMouseButtonDown(e);
                isMouseButtonDown = true;
            }

            if (AnnoTierStatic.Selected != null)
            {
                if (AnnoTierStatic.Selected.IsDiscreteOrFree || (!AnnoTierStatic.Selected.IsDiscreteOrFree && Keyboard.IsKeyDown(Key.LeftShift)))
                {
                    double pos = e.GetPosition(control.trackGrid).X;
                    annoCursor.X = pos;
                    Time.CurrentSelectPosition = pos;

                    annoCursor.Visibility = Visibility.Visible;
                    double time = Time.TimeFromPixel(pos);
                    control.annoStatusPositionLabel.Text = FileTools.FormatSeconds(time);
                }
                else
                {
                    annoCursor.X = 0;
                    double time = Time.TimeFromPixel(0);
                    annoCursor.Visibility = Visibility.Hidden;
                    control.annoStatusPositionLabel.Text = FileTools.FormatSeconds(time);
                }
            }
        }

        private void annoTrackGrid_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseButtonDown = false;

            if (AnnoTier.askForLabel == true)
                ShowLabelBox();
        }

        private void annoList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView grid = (ListView)sender;

            if (grid.SelectedIndex >= 0 && grid.SelectedIndex < grid.Items.Count)
            {
                AnnoListItem item = (AnnoListItem)grid.SelectedItem;
                control.annoListControl.editComboBox.SelectedItem = item.Label;

                movemedialock = true;
                Time.CurrentPlayPosition = item.Start;
                Time.CurrentPlayPositionPrecise = item.Start;

                mediaList.move(item.Start);
                moveSignalCursorToSecond(item.Start);

                if (item.Start >= timeline.SelectionStop)
                {
                    float factor = (float)(((item.Start - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));
                    control.timeLineControl.rangeSlider.MoveAndUpdate(true, factor);
                }
                else if (item.Stop <= timeline.SelectionStart)
                {
                    float factor = (float)(((Time.SelectionStart - item.Start)) / (Time.SelectionStop - Time.SelectionStart));
                    control.timeLineControl.rangeSlider.MoveAndUpdate(false, factor);
                }

                foreach (AnnoListItem a in AnnoTierStatic.Selected.AnnoList)
                {
                    if (a.Start == item.Start && a.Stop == item.Stop && item.Label == a.Label)
                    {
                        AnnoTierStatic.SelectLabel(AnnoTierStatic.Selected.GetSegment(a));
                        control.annoListControl.editComboBox.SelectedItem = item.Label;
                        control.annoListControl.editTextBox.Text = item.Label;

                        break;
                    }
                }
                if (item.Geometric)
                {
                    if (item.Points != null && item.Points.Count > 0)
                    {
                        if (control.geometricListControl.Visibility == Visibility.Visible &&
                            double.IsNaN(control.geometricListControl.Width))
                        {
                            showHideGeometricGrid(true);

                        }
                        setPointList(item.Points);
                        Grid videoGrid = control.mediaVideoControl.videoGrid;
                        int VNumChildren = videoGrid.Children.Count;
                        for (int i = 0; i < VNumChildren; ++i)
                        {
                            if (videoGrid.Children[i].GetType().Name == "MediaBox")
                            {
                                MediaBox mb = (MediaBox)videoGrid.Children[i];
                                int MBNumChildren = mb.mediaBoxGrid.Children.Count;
                                for (int j = 0; j < MBNumChildren; ++j)
                                {
                                    if (mb.mediaBoxGrid.Children[j].GetType().Name == "GeometricOverlay")
                                    {
                                        mb.zoomBoxControl.ZoomToFill();
                                        double scale = mb.zoomBoxControl.Zoom;
                                        double width = ((UIElement)mb.mediaelement.GetView()).RenderSize.Width;
                                        double height = ((UIElement)mb.mediaelement.GetView()).RenderSize.Height;
                                        GeometricOverlay go = (GeometricOverlay)mb.mediaBoxGrid.Children[j];
                                        if (go.Name == "overlay")
                                        {
                                            go.Width = scale * width;
                                            go.Height = scale * height;
                                        }
                                    }
                                }
                            }
                        }
                        geometricOverlayUpdate(AnnoScheme.TYPE.POINT);
                    }

                }
                movemedialock = false;
            }
        }

        private void annoTierCloseButton_Click(object sender, RoutedEventArgs e)
        {
            removeAnnoTier();
        }

        private void annoListEdit_Focused(object sender, MouseEventArgs e)
        {
            control.annoListControl.editTextBox.SelectAll();
        }
        
        private void annoListEdit_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                foreach (AnnoListItem item in control.annoListControl.annoDataGrid.SelectedItems)
                {
                    if (control.annoListControl.editComboBox.Visibility == Visibility.Visible && control.annoListControl.editComboBox.Items.Count > 0)
                    {
                        item.Label = control.annoListControl.editComboBox.SelectedItem.ToString();
                    }
                    else item.Label = control.annoListControl.editTextBox.Text;
                }
            }
        }

        private void annoListEdit_Click(object sender, RoutedEventArgs e)
        {
            foreach (AnnoListItem item in control.annoListControl.annoDataGrid.SelectedItems)
            {
                if (control.annoListControl.editComboBox.Visibility == Visibility.Visible && control.annoListControl.editComboBox.Items.Count > 0)
                {
                    item.Label = control.annoListControl.editComboBox.SelectedItem.ToString();
                    foreach (AnnoScheme.Label lcp in AnnoTierStatic.Selected.AnnoList.Scheme.Labels)
                    {
                        if (lcp.Name == item.Label)
                        {
                            item.Color = lcp.Color;
                            break;
                        }
                    }
                }
                else item.Label = control.annoListControl.editTextBox.Text;
            }
        }
        private void geometricTableUpdate()
        {
            control.geometricListControl.geometricDataGrid.Items.Refresh();
            return;
            //switch (type)
            //{
            //    case AnnoScheme.TYPE.POINT:
            //        //control.list
            //        break;
            //    case AnnoScheme.TYPE.POLYGON:
            //        break;
            //    case AnnoScheme.TYPE.GRPAH:
            //        break;
            //    case AnnoScheme.TYPE.SEGMENTATION:
            //        break;
            //}
        }

        public void geometricOverlayUpdate(AnnoScheme.TYPE type, AnnoListItem ali = null)
        {
            double scale = 0.0;

            Grid videoGrid = control.mediaVideoControl.videoGrid;
            int VNumChildren = videoGrid.Children.Count;
            GeometricOverlay go = null;
            for (int i = 0; i < VNumChildren; ++i)
            {
                bool stop = false;
                if (videoGrid.Children[i].GetType().Name == "MediaBox")
                {
                    MediaBox mb = (MediaBox)videoGrid.Children[i];
                    int MBNumChildren = mb.mediaBoxGrid.Children.Count;
                    for (int j = 0; j < MBNumChildren; ++j)
                    {
                        if (mb.mediaBoxGrid.Children[j].GetType().Name == "GeometricOverlay")
                        {
                            go = (GeometricOverlay)mb.mediaBoxGrid.Children[j];
                            if (go.Name == "overlay")
                            {
                                scale = mb.zoomBoxControl.Zoom;
                                stop = true;
                                break;
                            }
                            else
                            {
                                go = null;
                            }
                        }
                    }
                }
                if (stop) break;
            }

            if (go == null) return;

            go.canvas.Children.Clear();

            switch (type)
            {
                case AnnoScheme.TYPE.POINT:
                    if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1  || ali != null)
                    {
                        if (ali != null || control.geometricListControl.geometricDataGrid.Items[0].GetType().Name == "PointListItem")
                        {

                            AnnoListItem item = (ali == null) ? (AnnoListItem)control.annoListControl.annoDataGrid.SelectedItems[0] : ali;

                            if(item.Points != null)
                            {
                                foreach (PointListItem p in item.Points)
                                {
                                    if (p.XCoord != -1 && p.YCoord != -1)
                                    {
                                        Ellipse dot = new Ellipse();
                                        dot.Stroke = new SolidColorBrush(Colors.Black);
                                        dot.StrokeThickness = 1;
                                        dot.Fill = new SolidColorBrush(item.Color);
                                        dot.Height = 10;
                                        dot.Width = 10;
                                        Canvas.SetLeft(dot, (p.XCoord * scale) - dot.Width / 2);
                                        Canvas.SetTop(dot, (p.YCoord * scale) - dot.Height / 2);
                                        go.canvas.Children.Add(dot);
                                    }
                                }
                            }
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

        private static bool rightHeld;
        private static bool RightHeld
        {
            get { return rightHeld; }
            set
            {
                rightHeld = value;
                if (!value)
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


        private void geometricOverlay_MouseMove(object sender, MouseEventArgs e)
        {
            if (RightHeld)
            {
                Grid videoGrid = control.mediaVideoControl.videoGrid;
                int VNumChildren = videoGrid.Children.Count;
                for (int i = 0; i < VNumChildren; ++i)
                {
                    if (videoGrid.Children[i].GetType().Name == "MediaBox")
                    {
                        MediaBox mb = (MediaBox)videoGrid.Children[i];
                        int MBNumChildren = mb.mediaBoxGrid.Children.Count;
                        for (int j = 0; j < MBNumChildren; ++j)
                        {
                            if (mb.mediaBoxGrid.Children[j].GetType().Name == "GeometricOverlay")
                            {
                                GeometricOverlay go = (GeometricOverlay)mb.mediaBoxGrid.Children[j];
                                if (go.Name == "overlay")
                                {
                                    if (go.Visibility == Visibility.Visible)
                                    {

                                        double scale = mb.zoomBoxControl.Zoom;
                                        double x, y;
                                        Point p = Mouse.GetPosition(go);
                                        x = Math.Round(p.X, 2);
                                        y = Math.Round(p.Y, 2);

                                        double deltaX = x - RightHeldPos[0];
                                        double deltaY = y - RightHeldPos[1];

                                        RightHeldPos = new double[] { x, y };

                                        deltaX /= scale;
                                        deltaY /= scale;
                                        if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1)
                                        {
                                            if (control.geometricListControl.geometricDataGrid.Items[0].GetType().Name == "PointListItem")
                                            {
                                                AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;

                                                foreach (PointListItem pli in control.geometricListControl.geometricDataGrid.SelectedItems)
                                                {
                                                    if (pli.XCoord != -1 && pli.YCoord != -1)
                                                    {
                                                        pli.XCoord += deltaX;
                                                        pli.YCoord += deltaY;
                                                    }
                                                }
                                                geometricOverlayUpdate(AnnoScheme.TYPE.POINT);
                                                geometricTableUpdate();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void geometricOverlay_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Released)
            {
                RightHeld = false;
            }
        }

        private void geometricOverlay_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid videoGrid = control.mediaVideoControl.videoGrid;
            int VNumChildren = videoGrid.Children.Count;
            for (int i = 0; i < VNumChildren; ++i)
            {
                if (videoGrid.Children[i].GetType().Name == "MediaBox")
                {
                    MediaBox mb = (MediaBox)videoGrid.Children[i];
                    int MBNumChildren = mb.mediaBoxGrid.Children.Count;
                    for (int j = 0; j < MBNumChildren; ++j)
                    {
                        if (mb.mediaBoxGrid.Children[j].GetType().Name == "GeometricOverlay")
                        {
                            GeometricOverlay go = (GeometricOverlay)mb.mediaBoxGrid.Children[j];
                            if (go.Name == "overlay")
                            {
                                if (go.Visibility == Visibility.Visible)
                                {
                                    double scale = mb.zoomBoxControl.Zoom;

                                    if (double.IsNaN(go.Width) || double.IsNaN(go.Height))
                                    {
                                        mb.zoomBoxControl.ZoomToFill();
                                        double width = ((UIElement)mb.mediaelement.GetView()).RenderSize.Width;
                                        double height = ((UIElement)mb.mediaelement.GetView()).RenderSize.Height;
                                        go.Width = scale * width;
                                        go.Height = scale * height;
                                    }

                                    double x, y;
                                    Point p = Mouse.GetPosition(go);
                                    x = Math.Round(p.X, 2);
                                    y = Math.Round(p.Y, 2);

                                    if (e.LeftButton == MouseButtonState.Pressed)
                                    {
                                        if ((x > 0 && x < go.Width &&
                                            y > 0 && y < go.Height))
                                        {

                                            if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1)
                                            {
                                                if (control.geometricListControl.geometricDataGrid.Items[0].GetType().Name == "PointListItem")
                                                {
                                                    if (control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
                                                    {
                                                        string name = control.geometricListControl.geometricDataGrid.SelectedItems[0].GetType().Name;
                                                        if (name == "PointListItem")
                                                        {
                                                            foreach (PointListItem plt in control.geometricListControl.geometricDataGrid.SelectedItems)
                                                            {
                                                                plt.Label = control.geometricListControl.editTextBox.Text;

                                                                plt.XCoord = x / scale;
                                                                plt.YCoord = y / scale;
                                                            }
                                                        }
                                                        geometricOverlayUpdate(AnnoScheme.TYPE.POINT);
                                                        geometricTableUpdate();
                                                    }
                                                    else if (control.geometricListControl.geometricDataGrid.SelectedItems.Count > 1)
                                                    {
                                                        MessageBox.Show("Please select a single point from the points list");
                                                    }
                                                    else
                                                    {
                                                        MessageBox.Show("Please select a point from the points list");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                MessageBox.Show("Please select a single frame then a point from the points list");
                                            }
                                        }
                                        else
                                        {
                                            if (control.annoListControl.annoDataGrid.SelectedItems.Count == 0)
                                            {
                                                MessageBox.Show("Please select a single frame then a single point from the points list");
                                            }
                                        }
                                    }
                                    else if (e.RightButton == MouseButtonState.Pressed)
                                    {
                                        RightHeldPos = new double[] { x, y };
                                        RightHeld = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        private void geometricListEdit_Click(object sender, RoutedEventArgs e)
        {
            
            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                string name = control.geometricListControl.geometricDataGrid.SelectedItems[0].GetType().Name;
                if (name == "PointListItem")
                {
                    int index = control.geometricListControl.geometricDataGrid.SelectedIndex;
                    foreach (AnnoListItem ali in control.annoListControl.annoDataGrid.ItemsSource)
                    {
                        ali.Points[index].Label = control.geometricListControl.editTextBox.Text;
                    }
                }
            }

        }

        private bool isNumeric(string text)
        {
            Regex regex = new Regex("[0-9]+");
            bool r = regex.IsMatch(text);
            return r;
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
            AnnoList list = (AnnoList)control.annoListControl.annoDataGrid.ItemsSource;
            if (list != null) geometricOverlayUpdate(list.Scheme.Type);
            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                PointListItem item = (PointListItem)control.geometricListControl.geometricDataGrid.SelectedItems[0];
                control.geometricListControl.editTextBox.Text = item.Label;
            }
        }

        private void geometricListDelete(object sender, RoutedEventArgs e)
        {
            if (control.geometricListControl.geometricDataGrid.SelectedItems.Count != 0)
            {
                string name = control.geometricListControl.geometricDataGrid.SelectedItem.GetType().Name;
                if (name == "PointListItem")
                {
                    PointListItem[] selected = new PointListItem[control.geometricListControl.geometricDataGrid.SelectedItems.Count];
                    control.geometricListControl.geometricDataGrid.SelectedItems.CopyTo(selected, 0);
                    control.geometricListControl.geometricDataGrid.SelectedIndex = -1;

                    foreach (PointListItem pli in selected)
                    {
                        pli.XCoord = -1;
                        pli.YCoord = -1;
                    }
                    geometricOverlayUpdate(AnnoScheme.TYPE.POINT);
                    geometricTableUpdate();
                }
            }
        }

        
        private void geometricKeyDown(object sender, KeyEventArgs e)
        {
            if (control.annoListControl.annoDataGrid.SelectedItems.Count == 1 && control.geometricListControl.geometricDataGrid.SelectedItems.Count == 1)
            {
                int index = control.geometricListControl.geometricDataGrid.SelectedIndex;
                if (e.Key == Key.OemPeriod)
                {
                    if (index+1 < control.geometricListControl.geometricDataGrid.Items.Count)
                    {
                        while (control.geometricListControl.geometricDataGrid.SelectedItems.Count > 0)
                        {
                            control.geometricListControl.geometricDataGrid.SelectedItems.RemoveAt(0);
                        }
                        control.geometricListControl.geometricDataGrid.SelectedItems.Add(control.geometricListControl.geometricDataGrid.Items[index+1]);
                        
                    }
                }
                else if (e.Key == Key.OemComma)
                {
                    if (index - 1 >= 0 )
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

        #endregion EVENTHANDLERS
    }
}
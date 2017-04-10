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
                        updateNavigator();
                    }
                }
            }
        }

        private void setAnnoList(AnnoList anno)
        {
            control.annoListControl.annoDataGrid.ItemsSource = anno;
        }

        private void clearAnnoInfo()
        {
            control.annoSettingsButton.Visibility = Visibility.Hidden;
            control.annoStatusFileNameOrSessionLabel.Text = "";
            control.annoStatusFileNameOrSessionLabel.ToolTip = "";
            control.annoStatusSchemeNameLabel.Text = "";
            control.annoStatusSchemeTypeLabel.Text = "";
            control.annoStatusSchemeContinuousPanel.Visibility = Visibility.Collapsed;
            control.annoStatusAnnotatorLabel.Text = "";
            control.annoStatusRoleLabel.Text = "";
            control.annoStatusSchemeTypeLabel.Text = "";
            control.annoPositionLabel.Text = "00:00:00.00";
            control.annoCloseButton.Visibility = Visibility.Hidden;
        }

        private void updateAnnoInfo(AnnoTier annoTier)
        {
            if (annoTier == null)
            {
                return;
            }

            AnnoList annoList = annoTier.AnnoList;

            control.annoSettingsButton.Visibility = Visibility.Visible;
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
            control.annoCloseButton.Visibility = playIsPlaying ? Visibility.Hidden : Visibility.Visible;
        }

        private void onAnnoTierChange(AnnoTier tier, EventArgs e)
        {
            updateAnnoInfo(tier);
            setAnnoList(tier.AnnoList);
            control.annoListControl.editComboBox.Items.Clear();

            if (AnnoTierStatic.Selected != null)
            {
                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.GRAPH ||
                    AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                {
                    control.geometricListControl.Visibility = Visibility.Visible;
                    control.annorightMenu.Children[0].Visibility = Visibility.Collapsed;
                }
                else
                {
                    control.geometricListControl.Visibility = Visibility.Collapsed;
                }

                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    control.annoListControl.editButton.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.IsEnabled = false;
                    control.annoListControl.editTextBox.IsEnabled = false;
                    control.annorightMenu.Children[0].Visibility = Visibility.Visible;
                }
                else if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    control.annoListControl.editButton.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editComboBox.IsEnabled = false;
                    control.annoListControl.editTextBox.IsEnabled = false;
                    control.annorightMenu.Children[0].Visibility = Visibility.Collapsed;
                }

                control.annoListControl.editComboBox.Items.Clear();
                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    control.annoListControl.editComboBox.IsEnabled = true;
                    control.annoListControl.editComboBox.Visibility = Visibility.Visible;
                    control.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    control.annoListControl.editTextBox.IsEnabled = false;
                    control.annoListControl.editButton.Visibility = Visibility.Visible;
                    control.annorightMenu.Children[0].Visibility = Visibility.Collapsed;

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
                    control.annorightMenu.Children[0].Visibility = Visibility.Collapsed;
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

            updateNavigator();
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
                updateAnnoInfo(AnnoTierStatic.Selected);
                AnnoTierStatic.Selected.AnnoList.HasChanged = true;
            }
        }


        #region EVENTHANDLERS

        private void annoTierControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double pos = e.GetPosition(control.signalAndAnnoGrid).X;
                annoCursor.X = pos;
                Time.CurrentSelectPosition = pos;
                double time = Time.TimeFromPixel(pos);
                control.annoPositionLabel.Text = FileTools.FormatSeconds(time);
            }
            if ((e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed) && control.navigator.followAnnoCheckBox.IsChecked == true)
            {
                if (mediaList.Count > 0)
                {
                    mediaList.Move(Time.TimeFromPixel(e.GetPosition(control.signalAndAnnoGrid).X));
                    moveSignalCursor(Time.TimeFromPixel(e.GetPosition(control.signalAndAnnoGrid).X));
                    Stop();
                }
            }

            if (e.RightButton == MouseButtonState.Released && isMouseButtonDown == true)
            {
                isMouseButtonDown = false;

                if (control.navigator.followAnnoCheckBox.IsChecked == true)
                {                    
                    if (!IsPlaying())
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

        private void annoTierControl_MouseDown(object sender, MouseButtonEventArgs e)
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
            else if (e.RightButton == MouseButtonState.Pressed)
            {
                if (AnnoTierStatic.Selected.IsGeometric)
                {
                    int pos = control.annoListControl.annoDataGrid.SelectedIndex;
                    geometricOverlayUpdate(pos);
                }
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
                    double pos = e.GetPosition(control.signalAndAnnoGrid).X;
                    annoCursor.X = pos;
                    Time.CurrentSelectPosition = pos;

                    annoCursor.Visibility = Visibility.Visible;
                    double time = Time.TimeFromPixel(pos);
                    control.annoPositionLabel.Text = FileTools.FormatSeconds(time);
                }
                else
                {
                    annoCursor.X = 0;
                    double time = Time.TimeFromPixel(0);
                    annoCursor.Visibility = Visibility.Hidden;
                    control.annoPositionLabel.Text = FileTools.FormatSeconds(time);
                }
            }
        }

        private void annoTierControl_MouseRightButtonUp(object sender, MouseEventArgs e)
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

                Time.CurrentPlayPosition = item.Start;
                Time.CurrentPlayPositionPrecise = item.Start;

                mediaList.Move(item.Start);
                moveSignalCursor(item.Start);

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

                if (item.isGeometric)
                {                                        
                    int position = (int) (Time.CurrentPlayPosition * AnnoTierStatic.Selected.AnnoList.Scheme.SampleRate);
                    geometricSelectItem(item, position);
                }
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

       

        #endregion EVENTHANDLERS
    }
}
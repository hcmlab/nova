using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    public partial class MainHandler
    {
        private void handleAnnotation(AnnoList annoList)
        {
            double maxdur = 0;
            if (annoList.Count > 0)
            {
                maxdur = annoList[annoList.Count - 1].Stop;
            }

            annoList.HasChanged = false;


            if (annoList.Scheme == null)
            {
                annoList.Scheme = new AnnoScheme();
            }
 
                addAnnoTier(annoList);

            updateTimeRange(maxdur);
            if (annoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS) updateTimeRange(maxdur);


            if (this.annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0)
            {

                fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            }

           

        }

        private void removeAnnoTier()
        {
            AnnoTier at = AnnoTierStatic.Selected;

            if (at != null)
            {
                MessageBoxResult mb = MessageBoxResult.No;
                if (at.AnnoList.HasChanged)
                {
                    mb = MessageBox.Show("Save annotations on tier #" + at.Name + " first?", "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (mb == MessageBoxResult.Yes)
                    {
                        if (DatabaseLoaded)
                        {
                            databaseStore();
                        }
                        else saveAnno();

                        at.AnnoList.HasChanged = false;
                    }
                }

                if (mb != MessageBoxResult.Cancel)
                {
                    control.annoTrackControl.annoTrackGrid.RowDefinitions[Grid.GetRow(at)].Height = new GridLength(0);
                    if (control.annoTrackControl.annoTrackGrid.Children.IndexOf(at) > 0)
                    {
                        control.annoTrackControl.annoTrackGrid.Children.RemoveAt(control.annoTrackControl.annoTrackGrid.Children.IndexOf(at) - 1);
                        control.annoTrackControl.annoTrackGrid.Children.RemoveAt(control.annoTrackControl.annoTrackGrid.Children.IndexOf(at));
                    }

                    AnnoTier.UnselectTier();
                    at.Children.Clear();
                    at.AnnoList.Clear();

                    annoTiers.Remove(at);
                    control.annoNameLabel.Content = "#NoTier";
                }
            }
        }

        private void setAnnoList(AnnoList anno)
        {
            control.annoListControl.annoDataGrid.ItemsSource = anno;
        }

        private string buildAnnoNameLabel(AnnoList AnnoList)
        {

            string annoNameLabel = "";
            if (AnnoList.Scheme.Name != null && AnnoList.Scheme.Name != "") annoNameLabel += "#" + AnnoList.Scheme.Name + " ";
            else annoNameLabel += "#NewTier ";
            if (AnnoList.Role != null && AnnoList.Role != "") annoNameLabel += "#" + AnnoList.Role + " ";
            if (AnnoList.AnnotatorFullName != null && AnnoList.AnnotatorFullName != "") annoNameLabel += "#" + AnnoList.AnnotatorFullName;
            else if (AnnoList.Annotator != null && AnnoList.Annotator != "") annoNameLabel += "#" + AnnoList.Annotator;

            return annoNameLabel;
        }

        private void changeAnnoTierHandler(AnnoTier tier, EventArgs e)
        {
            control.annoNameLabel.Content = buildAnnoNameLabel(tier.AnnoList);
            setAnnoList(tier.AnnoList);
            control.annoListControl.editComboBox.Items.Clear();

            if (AnnoTierStatic.Selected != null)
            {
                if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
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

            //this.view.annoNameLabel.Text = track.AnnoList.Filename;
        }

        private void changeAnnoTierSegmentHandler(AnnoTierLabel segment, EventArgs e)
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

            AnnoTier tier = control.annoTrackControl.addAnnoTier(anno);
            tier.AnnoList.HasChanged = false;

            control.timeTrackControl.rangeSlider.OnTimeRangeChanged += tier.TimeRangeChanged;

            annoTiers.Add(tier);
            annoLists.Add(anno);

            if (tier.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                tier.Background = new LinearGradientBrush(tier.AnnoList.Scheme.MaxOrForeColor, tier.AnnoList.Scheme.MinOrBackColor, 90.0);
                tier.ContinuousBrush = new LinearGradientBrush(tier.AnnoList.Scheme.MaxOrForeColor, tier.AnnoList.Scheme.MinOrBackColor, 90.0);
            }
            else
            {
                tier.Background = new SolidColorBrush(tier.AnnoList.Scheme.MinOrBackColor);
                tier.BackgroundBrush = new SolidColorBrush(tier.AnnoList.Scheme.MinOrBackColor);
            }

            AnnoTierStatic.SelectTier(tier);
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
                    AnnoTierStatic.Selected.addSegment(item);
                }

                AnnoTierStatic.Selected.TimeRangeChanged(MainHandler.Time);
            }

            updateTimeRange(maxdur);
            // if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
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
                control.annoPositionLabel.Text = FileTools.FormatSeconds(time);
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
                        if (AnnoTierStatic.Selected.isDiscreteOrFree)
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
                            AnnoTier.SelectTier(a);
                            break;
                        }
                    }
                }

                if (AnnoTierStatic.Selected != null) AnnoTierStatic.Selected.RightMouseButtonDown(e);
                isMouseButtonDown = true;
            }

            if (AnnoTierStatic.Selected != null)
            {
                if (AnnoTierStatic.Selected.isDiscreteOrFree || (!AnnoTierStatic.Selected.isDiscreteOrFree && Keyboard.IsKeyDown(Key.LeftShift)))
                {
                    double pos = e.GetPosition(control.trackGrid).X;
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
                    control.timeTrackControl.rangeSlider.MoveAndUpdate(true, factor);
                }
                else if (item.Stop <= timeline.SelectionStart)
                {
                    float factor = (float)(((Time.SelectionStart - item.Start)) / (Time.SelectionStop - Time.SelectionStart));
                    control.timeTrackControl.rangeSlider.MoveAndUpdate(false, factor);
                }

                foreach (AnnoListItem a in AnnoTierStatic.Selected.AnnoList)
                {
                    if (a.Start == item.Start && a.Stop == item.Stop && item.Label == a.Label)
                    {
                        AnnoTier.SelectLabel(AnnoTierStatic.Selected.getSegment(a));
                        control.annoListControl.editComboBox.SelectedItem = item.Label;
                        control.annoListControl.editTextBox.Text = item.Label;

                        break;
                    }
                }

                movemedialock = false;
            }
        }

        private void closeAnnoTier_Click(object sender, RoutedEventArgs e)
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
using PropertyTools.Wpf;
using ssi.Interfaces;
using ssi.Types.Polygon;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoListControl.xaml
    /// </summary>
    public partial class AnnoListControl : UserControl
    {

        private GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Ascending;
        private PolygonUtilities polygonUtilities;
        public bool itemhasFocus = false;

        public AnnoListControl()
        {
            InitializeComponent();
            annoDataGrid.Background = Defaults.Brushes.AppBackground;
            editButton.Background = Defaults.Brushes.ButtonColor;
            editButton.Foreground = Defaults.Brushes.ButtonForeColor;
            if (!editButton.IsEnabled)
            {
                editButton.Foreground = Brushes.Black;
            }
        }

        private void AnnoDataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(annoDataGrid.ItemsSource);
            view.Filter = UserFilter;

            if (AnnoTier.Selected != null && AnnoTier.Selected.IsContinuous)
            {
                NaNToDefaultMenu.Visibility = Visibility.Visible;
            }

            if (AnnoTier.Selected != null && AnnoTier.Selected.IsDiscreteOrFree)
            {
                NaNToDefaultMenu.Visibility = Visibility.Collapsed;
            }
        }

        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(searchTextBox.Text))
                return true;
            else if (AnnoTier.Selected.IsDiscreteOrFree)
            {
                return ((item as AnnoListItem).Label.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            else if (AnnoTier.Selected.IsContinuous)
            {
                return ((item as AnnoListItem).Score.ToString().IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            else return false;
        }

        private void MenuItemDeleteClick(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected.IsDiscreteOrFree)
            {
                if (annoDataGrid.SelectedItems.Count > 0)
                {
                    AnnoListItem[] selected = new AnnoListItem[annoDataGrid.SelectedItems.Count];
                    annoDataGrid.SelectedItems.CopyTo(selected, 0);
                    annoDataGrid.SelectedIndex = -1;

                    AnnoTier track = AnnoTierStatic.Selected;
                    foreach (AnnoListItem s in selected)
                    {
                        AnnoTierSegment segment = track.GetSegment(s);
                        if (segment != null)
                        {
                            track.RemoveSegment(segment);
                        }
                    }
                }
            }
            else if (AnnoTierStatic.Selected.IsContinuous)
            {
                AnnoListItem[] selected = new AnnoListItem[annoDataGrid.SelectedItems.Count];
                annoDataGrid.SelectedItems.CopyTo(selected, 0);
                annoDataGrid.SelectedIndex = -1;
                foreach (AnnoListItem s in selected)
                {
                    s.Score = double.NaN;
                }
                AnnoTier.Selected.TimeRangeChanged(MainHandler.Time);
            }
        }

        private void MenuItemCopyWithMetaClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    sb.AppendLine("name=" + s.Label + ";from=" + s.Start + ";to=" + s.Stop + ";" + s.Meta.Replace('\n', ';'));
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyWithoutMetaClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    sb.AppendLine("name=" + s.Label + ";from=" + s.Start + ";to=" + s.Stop + ";");
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyMetaOnlyClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    sb.AppendLine(s.Meta.Replace('\n', ';'));
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyMetaNumbersOnlyClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    string[] split = Regex.Split(s.Meta, @"[=\n]");
                    if (split.Length > 1)
                    {
                        sb.Append(split[1]);
                        for (int i = 3; i < split.Length; i += 2)
                        {
                            sb.Append(";" + split[i]);
                        }
                        sb.AppendLine();
                    }
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void MenuItemCopyMetaStringsOnlyClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                var sb = new StringBuilder();
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    string[] split = Regex.Split(s.Meta, @"[=\n]");
                    if (split.Length > 0)
                    {
                        sb.Append(split[0]);
                        for (int i = 2; i < split.Length; i += 2)
                        {
                            sb.Append(";" + split[i]);
                        }
                        sb.AppendLine();
                    }
                }
                try
                {
                    System.Windows.Clipboard.SetData(DataFormats.Text, sb.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to copy to the clipboard (" + ex.ToString() + ")");
                }
            }
        }

        private void editTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Return)
            {
                e.Handled = true;
                // Kill logical focus
                FocusManager.SetFocusedElement(FocusManager.GetFocusScope(editTextBox), null);
                // Kill keyboard focus
                Keyboard.ClearFocus();
            }
        }

        private void MenuItemSetConfidenceZeroClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    s.Confidence = 0.0;
                }
            }
        }

        private void MenuItemSetConfidenceOneClick(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.SelectedItems.Count != 0)
            {
                foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                {
                    s.Confidence = 1.0;
                }
            }
        }

        private void MenuItemSetNanClick(object sender, RoutedEventArgs e)
        {
            if (AnnoTier.Selected.IsContinuous)
            {
                if (annoDataGrid.SelectedItems.Count != 0)
                {
                    double mean = (AnnoTier.Selected.AnnoList.Scheme.MinScore + AnnoTier.Selected.AnnoList.Scheme.MaxScore) / 2.0;
                    foreach (AnnoListItem s in annoDataGrid.SelectedItems)
                    {
                        if (double.IsNaN(s.Score))
                        {
                            s.Score = mean;
                        }
                    }

                    AnnoTier.Selected.TimeRangeChanged(MainHandler.Time);
                    AnnoTier.Selected.TimeRangeChanged(MainHandler.Time);
                }
            }
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (annoDataGrid.ItemsSource != null)
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(annoDataGrid.ItemsSource);
                view.Filter = UserFilter;
                CollectionViewSource.GetDefaultView(annoDataGrid.ItemsSource).Refresh();
            }
        }

        private void SortListView(object sender, RoutedEventArgs e)
        {
            if (annoDataGrid.Items.Count > 0)
            {
                GridViewColumnHeader headerClicked =
                 e.OriginalSource as GridViewColumnHeader;
                ListSortDirection direction;

                if (headerClicked != null)
                {
                    if (headerClicked.Role != GridViewColumnHeaderRole.Padding)
                    {
                        if (headerClicked != _lastHeaderClicked)
                        {
                            direction = ListSortDirection.Ascending;
                        }
                        else
                        {
                            if (_lastDirection == ListSortDirection.Ascending)
                            {
                                direction = ListSortDirection.Descending;
                            }
                            else
                            {
                                direction = ListSortDirection.Ascending;
                            }
                        }

                        string header = headerClicked.Column.Header as string;
                        ICollectionView dataView = CollectionViewSource.GetDefaultView(((ListView)sender).ItemsSource);

                        dataView.SortDescriptions.Clear();
                        SortDescription sd = new SortDescription(header, direction);
                        dataView.SortDescriptions.Add(sd);
                        dataView.Refresh();

                        if (direction == ListSortDirection.Ascending)
                        {
                            headerClicked.Column.HeaderTemplate =
                              Resources["HeaderTemplateArrowUp"] as DataTemplate;
                        }
                        else
                        {
                            headerClicked.Column.HeaderTemplate =
                              Resources["HeaderTemplateArrowDown"] as DataTemplate;
                        }

                        // Remove arrow from previously sorted header
                        if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked)
                        {
                            _lastHeaderClicked.Column.HeaderTemplate = null;
                        }

                        _lastHeaderClicked = headerClicked;
                        _lastDirection = direction;
                    }
                }
            }
        }

        private void MenuItemDeletAllPolygonsClick(object sender, RoutedEventArgs e)
        {
            foreach(Object obj in this.annoDataGrid.SelectedItems)
            {
                AnnoListItem item = (AnnoListItem)obj;
                item.PolygonList = new PolygonList(new List<PolygonLabel>());
                polygonUtilities.polygonSelectItem(item);
                item.updateLabelCount();
            }
        }

        internal PolygonUtilities PolygonUtilities { set => polygonUtilities = value; }

        private void TextBoxEx_GotFocus(object sender, RoutedEventArgs e)
        {
            itemhasFocus = true;
            
        }

        private void TextBoxEx_LostFocus(object sender, RoutedEventArgs e)
        {
            itemhasFocus = false;
        }

    }


    
}
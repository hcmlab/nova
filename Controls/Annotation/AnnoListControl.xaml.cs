using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections;
using System.Collections.ObjectModel;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoListControl.xaml
    /// </summary>
    public partial class AnnoListControl : UserControl
    {
        private GridViewColumnHeader listViewSortCol = null;
        private ListViewSortAdorner listViewSortAdorner = null;

 

        public AnnoListControl()
        {
            InitializeComponent();

            annoDataGrid.SourceUpdated += AnnoDataGrid_SourceUpdated;
           

        }

        private void AnnoDataGrid_SourceUpdated(object sender, DataTransferEventArgs e)
        {
            CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(annoDataGrid.ItemsSource);
            view.Filter = UserFilter;
        }



        private bool UserFilter(object item)
        {
            if (String.IsNullOrEmpty(searchTextBox.Text))
                return true;
            else
                return ((item as AnnoListItem).Label.IndexOf(searchTextBox.Text, StringComparison.OrdinalIgnoreCase) >= 0);
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

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            if (column.Tag != null)
            {                
                string sortBy = column.Tag.ToString();
                if (sortBy == "Label")
                {

                    if (listViewSortCol != null)
                    {
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Remove(listViewSortAdorner);
                        annoDataGrid.Items.SortDescriptions.Clear();
                    }

                    if (listViewSortCol == null)
                    {
                        listViewSortCol = column;
                        listViewSortAdorner = new ListViewSortAdorner(listViewSortCol, ListSortDirection.Ascending);
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                        annoDataGrid.Items.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Ascending));
                    }
                    else if (listViewSortAdorner.Direction == ListSortDirection.Ascending)
                    {
                        listViewSortAdorner = new ListViewSortAdorner(listViewSortCol, ListSortDirection.Descending);
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                        annoDataGrid.Items.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Descending));
                    }
                    else
                    {
                        listViewSortCol = null;
                    }
                }               
            }     
        }

        private void searchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(annoDataGrid.ItemsSource != null)
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(annoDataGrid.ItemsSource);
                view.Filter = UserFilter;
                CollectionViewSource.GetDefaultView(annoDataGrid.ItemsSource).Refresh();
            }
           
        }


    
    }



}
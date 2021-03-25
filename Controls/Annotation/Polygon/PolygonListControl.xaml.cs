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
    /// Interaction logic for PolygonListControl.xaml
    /// </summary>
    public partial class PolygonListControl : UserControl
    {
        private GridViewColumnHeader listViewSortCol = null;
        private ListViewSortAdorner listViewSortAdorner = null;

        public PolygonListControl()
        {
            InitializeComponent();
        }


        private void editTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
        }

        private void MenuItemSetConfidenceZeroClick(object sender, RoutedEventArgs e)
        {
            if (geometricDataGrid.SelectedItems.Count != 0)
            {
                string name = geometricDataGrid.SelectedItem.GetType().Name;
                if (name == "PointListItem")
                {
                    foreach (PointListItem s in geometricDataGrid.SelectedItems)
                    {
                        s.Confidence = 0.0;
                    }
                }
            }
        }

        private void MenuItemSetConfidenceOneClick(object sender, RoutedEventArgs e)
        {
            if (geometricDataGrid.SelectedItems.Count != 0)
            {
                string name = geometricDataGrid.SelectedItem.GetType().Name;
                if (name == "PointListItem")
                {
                    foreach (PointListItem s in geometricDataGrid.SelectedItems)
                    {
                        s.Confidence = 1.0;
                    }
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
                        geometricDataGrid.Items.SortDescriptions.Clear();
                    }

                    if (listViewSortCol == null)
                    {
                        listViewSortCol = column;
                        listViewSortAdorner = new ListViewSortAdorner(listViewSortCol, ListSortDirection.Ascending);
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                        geometricDataGrid.Items.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Ascending));
                    }
                    else if (listViewSortAdorner.Direction == ListSortDirection.Ascending)
                    {
                        listViewSortAdorner = new ListViewSortAdorner(listViewSortCol, ListSortDirection.Descending);
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                        geometricDataGrid.Items.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Descending));
                    }
                    else
                    {
                        listViewSortCol = null;
                    }
                }               
            }     
        }

    }
}
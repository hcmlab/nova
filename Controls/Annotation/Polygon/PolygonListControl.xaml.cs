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
using PropertyTools.Wpf;

namespace ssi
{
    /// <summary>
    /// Interaction logic for PolygonListControl.xaml
    /// </summary>
    public partial class PolygonListControl : UserControl
    {
        private GridViewColumnHeader listViewSortCol = null;
        private ListViewSortAdorner listViewSortAdorner = null;
        private MainHandler mainHandler;
        private Color color;

        public PolygonListControl()
        {
            InitializeComponent();
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
                        polygonDataGrid.Items.SortDescriptions.Clear();
                    }

                    if (listViewSortCol == null)
                    {
                        listViewSortCol = column;
                        listViewSortAdorner = new ListViewSortAdorner(listViewSortCol, ListSortDirection.Ascending);
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                        polygonDataGrid.Items.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Ascending));
                    }
                    else if (listViewSortAdorner.Direction == ListSortDirection.Ascending)
                    {
                        listViewSortAdorner = new ListViewSortAdorner(listViewSortCol, ListSortDirection.Descending);
                        AdornerLayer.GetAdornerLayer(listViewSortCol).Add(listViewSortAdorner);
                        polygonDataGrid.Items.SortDescriptions.Add(new SortDescription(sortBy, ListSortDirection.Descending));
                    }
                    else
                    {
                        listViewSortCol = null;
                    }
                }               
            }     
        }

        public void setMainHandler(MainHandler mh)
        {
            this.mainHandler = mh;
        }

        private void ColorPicker_DropDownClosed(object sender, EventArgs e)
        {
            PolygonLabel pl = (PolygonLabel)((ColorPicker)sender).DataContext;

            if (!this.mainHandler.updateFrameData(pl))
            {
                this.mainHandler.undoColorChanges(pl, this.color);
            }
        }

        private void ColorPicker_DropDownOpened(object sender, EventArgs e)
        {
            if(AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
            {
                ((ColorPicker)sender).IsEnabled = false;
            }
            else
            {
                PolygonLabel pl = (PolygonLabel)((ColorPicker)sender).DataContext;
                this.color = pl.Color;
                this.mainHandler.selectSpecificLabel(pl);
            }
        }
    }
}
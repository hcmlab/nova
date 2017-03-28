using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{

    public partial class MediaBoxControl : UserControl
    {
        public MediaBoxControl()
        {
            InitializeComponent();
        }

        public void Clear()
        {            
            grid.Children.Clear();
            grid.ColumnDefinitions.Clear();
        }

        public void Add(MediaBox box)
        {
            for (int i = 0; i < grid.ColumnDefinitions.Count; i+=2)
            {
                grid.ColumnDefinitions[i].Width = new GridLength(1, GridUnitType.Star);
            }

            if (grid.Children.Count > 0)
            {
                // splitter
                ColumnDefinition split = new ColumnDefinition();
                split.Width = new GridLength(1, GridUnitType.Auto);
                grid.ColumnDefinitions.Add(split);
                GridSplitter splitter = new GridSplitter();
                splitter.ResizeDirection = GridResizeDirection.Columns;
                splitter.Width = 5;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                Grid.SetRowSpan(splitter, 1);
                Grid.SetRow(splitter, 0);
                Grid.SetColumn(splitter, grid.ColumnDefinitions.Count - 1);
                grid.Children.Add(splitter);
            }            

            // media
            ColumnDefinition column = new ColumnDefinition();
            column.Width = new GridLength(1, GridUnitType.Star);
            grid.ColumnDefinitions.Add(column);

            Border border = new Border();
            border.BorderThickness = new Thickness(Defaults.SelectionBorderWidth, 0, 0, 0);
            border.BorderBrush = Defaults.Brushes.Highlight;
            border.Child = box;

            Grid.SetRow(border, 0);
            Grid.SetColumn(border, grid.ColumnDefinitions.Count - 1);
            grid.Children.Add(border);

            box.Border = border;
        }

        public void Remove(MediaBox box)
        {
            grid.ColumnDefinitions[Grid.GetColumn(box.Border)].Width = new GridLength(0);
            if (grid.Children.IndexOf(box.Border) > 0)
            {
                grid.Children.RemoveAt(grid.Children.IndexOf(box.Border) - 1);
                grid.Children.RemoveAt(grid.Children.IndexOf(box.Border));
            }
        }        
    }
}
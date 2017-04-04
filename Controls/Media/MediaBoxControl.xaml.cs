using System;
using System.IO;
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

            Label label = new Label();
            string path = box.Media.GetFilepath();
            label.Content = Path.GetFileName(path);
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Top;
            label.Foreground = Brushes.Black;
            Color color = Defaults.Colors.Highlight;
            color.A = 128;
            label.Background = new SolidColorBrush(color);
            label.IsHitTestVisible = false;
            Grid.SetRow(label, 0);
            Grid.SetColumn(label, grid.ColumnDefinitions.Count - 1);
            grid.Children.Add(label);

            box.Border = border;
        }

        public void Remove(MediaBox box)
        {
            int columnIndex = Grid.GetColumn(box.Border);
            int childIndex = 0;

            bool isLast = columnIndex == grid.ColumnDefinitions.Count - 1;

            // remove children:

            // splitter            
            childIndex = grid.Children.IndexOf(box.Border);
            if (!isLast) grid.Children.RemoveAt(childIndex + 2);
            // label
            childIndex = grid.Children.IndexOf(box.Border);
            grid.Children.RemoveAt(childIndex + 1);
            // border
            childIndex = grid.Children.IndexOf(box.Border);
            grid.Children.RemoveAt(childIndex);

            // update row indices of remaining children:

            int row = 0;
            for (int i = 0; i < grid.Children.Count; i++)
            {
                if ((i + 1) % 3 == 0)
                {
                    row++;
                }
                Grid.SetColumn(grid.Children[i], row);
                if ((i + 1) % 3 == 0)
                {
                    row++;
                }
            }

            // remove rows:

            grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);
            if (!isLast) grid.ColumnDefinitions.RemoveAt(grid.ColumnDefinitions.Count - 1);

            // resize 

            for (int i = 0; i < grid.ColumnDefinitions.Count; i += 2)
            {
                grid.ColumnDefinitions[i].Width = new GridLength(1, GridUnitType.Star);
            }
        }        
    }
}
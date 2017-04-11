using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    public partial class SignalTrackControl : UserControl
    {
        public SignalTrackControl()
        {
            InitializeComponent();
        }

        public void Add(SignalTrack track, Color signalColor, Color backgroundColor)
        {
            for (int i = 0; i < grid.RowDefinitions.Count; i += 2)
            {
                grid.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
            }

            if (grid.Children.Count > 0)
            {
                RowDefinition split = new RowDefinition();
                split.Height = new GridLength(1, GridUnitType.Auto);
                grid.RowDefinitions.Add(split);
                GridSplitter splitter = new GridSplitter();
                splitter.Background = Defaults.Brushes.Splitter;
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 5;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, grid.RowDefinitions.Count - 1);
                grid.Children.Add(splitter);
            }

            // add signal track
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(row);
            
            track.SignalColor = signalColor;
            track.BackgroundColor = backgroundColor;

            Grid.SetColumn(track, 0);
            Grid.SetRow(track, grid.RowDefinitions.Count - 1);
            grid.Children.Add(track);

            Label label = new Label();
            label.Content = " " + track.Signal.Name;
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Foreground = Brushes.Black;
            Color color = Defaults.Colors.Highlight;
            color.A = 128;
            label.Background = new SolidColorBrush(color);
            label.IsHitTestVisible = false;
            Grid.SetColumn(label, 0);
            Grid.SetRow(label, grid.RowDefinitions.Count - 1);
            grid.Children.Add(label);

            Border border = new Border();
            border.BorderThickness = new Thickness(Defaults.SelectionBorderWidth, 0, 0, 0);
            border.BorderBrush = Defaults.Brushes.Highlight;
            border.IsHitTestVisible = false;
            Grid.SetColumn(border, 0);
            Grid.SetRow(border, grid.RowDefinitions.Count - 1);
            grid.Children.Add(border);

            track.Border = border;
        }        

        public void Remove(SignalTrack track)
        {
            int rowIndex = Grid.GetRow(track.Border);
            int childIndex = 0;
                        
            bool isLast = rowIndex == grid.RowDefinitions.Count - 1;

            // remove children:

            // splitter            
            childIndex = grid.Children.IndexOf(track.Border);
            if (!isLast) grid.Children.RemoveAt(childIndex + 1);
            // track
            childIndex = grid.Children.IndexOf(track.Border);
            grid.Children.RemoveAt(childIndex - 2);
            // label
            childIndex = grid.Children.IndexOf(track.Border);
            grid.Children.RemoveAt(childIndex - 1);
            // border
            childIndex = grid.Children.IndexOf(track.Border);
            grid.Children.RemoveAt(childIndex);

            // update row indices of remaining children:

            int row = 0;
            for (int i = 0; i < grid.Children.Count; i++)
            {
                if ((i + 1) % 4 == 0)
                {
                    row++;
                }
                Grid.SetRow(grid.Children[i], row);
                if ((i + 1) % 4 == 0)
                {
                    row++;
                }
            }

            // remove rows:

            grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count-1);
            if (!isLast) grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);

            // resize 

            for (int i = 0; i < grid.RowDefinitions.Count; i += 2)
            {
                grid.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
            }
        }
    }
}
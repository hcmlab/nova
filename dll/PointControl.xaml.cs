using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for SkeletonControl.xaml
    /// </summary>
    ///

    public partial class PointControl : UserControl
    {
        public event EventHandler<MediaRemoveEventArgs> RemoveMedia;

        public PointControl()
        {
            InitializeComponent();
        }

        public void clear()
        {
            this.pointGrid.RowDefinitions.Clear();
            this.pointGrid.Children.Clear();
        }

        public void addPoints(Signal signal)
        {
            Grid grid = pointGrid;
            if (grid.RowDefinitions.Count > 0)
            {
                // splitter
                RowDefinition split_row = new RowDefinition();
                split_row.Height = new GridLength(1, GridUnitType.Auto);
                grid.RowDefinitions.Add(split_row);
                GridSplitter splitter = new GridSplitter();
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 3;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                Grid.SetColumnSpan(splitter, 1);
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, grid.RowDefinitions.Count - 1);
                grid.Children.Add(splitter);
            }

            // video
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(row);

           
            if (signal.meta_name == "face")
            {
                PointsPainter points_painter = new PointsPainter(signal);
                points_painter.CloseButton.Click += removePoints;
                Grid.SetColumn(points_painter, 0);
                Grid.SetRow(points_painter, grid.RowDefinitions.Count - 1);
                grid.Children.Add(points_painter);
            }

            else if (signal.meta_name == "skeleton")
            {
                SkeletonPainter skel_painter = new SkeletonPainter(signal);
                skel_painter.CloseButton.Click += removeSkeleton;
                Grid.SetColumn(skel_painter, 0);
                Grid.SetRow(skel_painter, grid.RowDefinitions.Count - 1);
                grid.Children.Add(skel_painter);
            }
        }

        public void removePoints(object sender, RoutedEventArgs e)
        {
            PointsPainter points = GetAncestorOfType<PointsPainter>(sender as Button);
            Grid grid = pointGrid;
            grid.Children.Remove(points);
            grid.RowDefinitions[Grid.GetRow(points)].Height = new GridLength(0);
            if (grid.RowDefinitions.Count > Grid.GetRow(points) + 1) grid.RowDefinitions[Grid.GetRow(points) + 1].Height = new GridLength(0);

        }

        public void removeSkeleton(object sender, RoutedEventArgs e)
        {
            SkeletonPainter skel = GetAncestorOfType<SkeletonPainter>(sender as Button);
            Grid grid = pointGrid;
            grid.Children.Remove(skel);
            grid.RowDefinitions[Grid.GetRow(skel)].Height = new GridLength(0);
            if (grid.RowDefinitions.Count > Grid.GetRow(skel) + 1) grid.RowDefinitions[Grid.GetRow(skel) + 1].Height = new GridLength(0);
        }

        public static T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent != null && !(parent is T))
                return (T)GetAncestorOfType<T>((FrameworkElement)parent);
            return (T)parent;
        }
    }
}
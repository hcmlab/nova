using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoTierControl.xaml
    /// </summary>
    public partial class AnnoTierControl : UserControl
    {
        public double currentTime = 0;

        public AnnoTierControl()
        {
            InitializeComponent();
        }

        public void Clear()
        {
            grid.RowDefinitions.Clear();
            grid.Children.Clear();
        }

        public void Add(AnnoTier tier)
        {
            for (int i = 0; i < grid.RowDefinitions.Count; i += 2)
            {
                grid.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
            }

            if (grid.RowDefinitions.Count > 0)
            {
                // add splitter
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
                Grid.SetColumnSpan(splitter, 1);
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, grid.RowDefinitions.Count - 1);
                grid.Children.Add(splitter);
            }

            // add anno tier
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(row);

            Grid.SetColumn(tier, 0);
            Grid.SetRow(tier, grid.RowDefinitions.Count - 1);
            grid.Children.Add(tier);

            Label label = new Label();
            label.Content = " " + tier.AnnoList.Scheme.Name;
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

            tier.Border = border;
        }

        public void Remove(AnnoTier tier)
        {
            grid.RowDefinitions[Grid.GetRow(tier)].Height = new GridLength(0);
            if (grid.Children.IndexOf(tier) > 0)
            {
                grid.Children.RemoveAt(grid.Children.IndexOf(tier) - 1);
                grid.Children.RemoveAt(grid.Children.IndexOf(tier));
            }
        }
    }
}
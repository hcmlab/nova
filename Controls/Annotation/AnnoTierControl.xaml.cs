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
            annoTierGrid.RowDefinitions.Clear();
            annoTierGrid.Children.Clear();
        }

        public void Add(AnnoTier tier)
        {
            if (annoTierGrid.RowDefinitions.Count > 0)
            {
                // add splitter
                RowDefinition split_row = new RowDefinition();
                split_row.Height = new GridLength(1, GridUnitType.Auto);
                annoTierGrid.RowDefinitions.Add(split_row);
                GridSplitter splitter = new GridSplitter();
                splitter.Background = Defaults.Brushes.Conceal;
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 3;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                Grid.SetColumnSpan(splitter, 1);
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, annoTierGrid.RowDefinitions.Count - 1);
                annoTierGrid.Children.Add(splitter);
            }

            // add anno tier
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            annoTierGrid.RowDefinitions.Add(row);

            Border border = new Border();
            border.BorderThickness = new Thickness(7, 0, 0, 0);
            border.BorderBrush = Defaults.Brushes.Conceal;
            border.Child = tier;

            Grid.SetColumn(border, 0);
            Grid.SetRow(border, annoTierGrid.RowDefinitions.Count - 1);
            annoTierGrid.Children.Add(border);

            tier.Border = border;
        }

        public void Remove(AnnoTier tier)
        {
            annoTierGrid.RowDefinitions[Grid.GetRow(tier)].Height = new GridLength(0);
            if (annoTierGrid.Children.IndexOf(tier) > 0)
            {
                annoTierGrid.Children.RemoveAt(annoTierGrid.Children.IndexOf(tier) - 1);
                annoTierGrid.Children.RemoveAt(annoTierGrid.Children.IndexOf(tier));
            }
        }
    }
}
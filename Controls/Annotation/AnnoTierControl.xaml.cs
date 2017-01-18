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
        public double currenttime = 0;

        public AnnoTierControl()
        {
            InitializeComponent();
        }

        public void clear()
        {
            this.annoTrackGrid.RowDefinitions.Clear();
            this.annoTrackGrid.Children.Clear();
        }

        public AnnoTier addAnnoTier(AnnoList anno)
        {
            if (this.annoTrackGrid.RowDefinitions.Count > 0)
            {
                // add splitter
                RowDefinition split_row = new RowDefinition();
                split_row.Height = new GridLength(1, GridUnitType.Auto);
                this.annoTrackGrid.RowDefinitions.Add(split_row);
                GridSplitter splitter = new GridSplitter();
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 3;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                Grid.SetColumnSpan(splitter, 1);
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, this.annoTrackGrid.RowDefinitions.Count - 1);
                this.annoTrackGrid.Children.Add(splitter);
            }

            // add anno track
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            this.annoTrackGrid.RowDefinitions.Add(row);
            if (anno.Scheme.MinOrBackColor == Colors.Transparent)
            {
                anno.Scheme.MinOrBackColor = selectColor(this.annoTrackGrid.RowDefinitions.Count - 1);
            }
            AnnoTier tier = new AnnoTier(anno);

            Grid.SetColumn(tier, 0);
            Grid.SetRow(tier, this.annoTrackGrid.RowDefinitions.Count - 1);
          
            this.annoTrackGrid.Children.Add(tier);

            return tier;
        }

        //Default Color Scheme
        private Color selectColor(int index)
        {
            index = index / 2;
            if (index % 8 == 0) return Colors.Khaki;
            else if (index % 8 == 1) return Colors.SkyBlue;
            else if (index % 8 == 2) return Colors.YellowGreen;
            else if (index % 8 == 3) return Colors.Tomato;
            else if (index % 8 == 4) return Colors.RosyBrown;
            else if (index % 8 == 5) return Colors.Goldenrod;
            else if (index % 8 == 6) return Colors.LightSeaGreen;
            else if (index % 8 == 7) return Colors.LightGray;
            else return Colors.AliceBlue;
        }
    }
}
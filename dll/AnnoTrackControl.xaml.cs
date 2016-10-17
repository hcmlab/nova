using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoTrackControl.xaml
    /// </summary>
    public partial class AnnoTrackControl : UserControl
    {
        public double currenttime = 0;

        // True if a drag is in progress.
        private bool DragInProgress = false;

        // The drag's last point.
        private Point LastPoint;

        private UIElement track;

        public AnnoTrackControl()
        {
            InitializeComponent();
        }

        public void clear()
        {
            this.annoTrackGrid.RowDefinitions.Clear();
            this.annoTrackGrid.Children.Clear();
        }

        public AnnoTrack addAnnoTrack(AnnoList list, bool isdiscrete, double samplerate = 1, string tierid = "default", double borderlow = 0.0, double borderhigh = 1.0)
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
            AnnoTrack track = new AnnoTrack(list, isdiscrete, samplerate, tierid, borderlow, borderhigh);

            Grid.SetColumn(track, 0);
            Grid.SetRow(track, this.annoTrackGrid.RowDefinitions.Count - 1);
            track.BackgroundColor = selectColor(this.annoTrackGrid.RowDefinitions.Count - 1); ;
            track.Background = track.BackgroundColor;
            this.annoTrackGrid.Children.Add(track);

            return track;
        }

        //Default Color Scheme, make changeable in the config
        private Brush selectColor(int index)
        {
            index = index / 2;
            if (index % 8 == 0) return Brushes.Khaki;
            else if (index % 8 == 1) return Brushes.SkyBlue;
            else if (index % 8 == 2) return Brushes.YellowGreen;
            else if (index % 8 == 3) return Brushes.Tomato;
            else if (index % 8 == 4) return Brushes.RosyBrown;
            else if (index % 8 == 5) return Brushes.Goldenrod;
            else if (index % 8 == 6) return Brushes.LightSeaGreen;
            else if (index % 8 == 7) return Brushes.LightGray;
            else return Brushes.AliceBlue;
        }
    }
}
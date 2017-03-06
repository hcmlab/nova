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

        public void Clear()
        {
            signalTrackGrid.Children.Clear();
            signalTrackGrid.RowDefinitions.Clear();
        }

        public void Add(SignalTrack track, Color signalColor, Color backgroundColor)
        {
            if (signalTrackGrid.Children.Count > 0)
            {
                RowDefinition split_row = new RowDefinition();
                split_row.Height = new GridLength(1, GridUnitType.Auto);
                signalTrackGrid.RowDefinitions.Add(split_row);
                GridSplitter splitter = new GridSplitter();
                splitter.Background = Defaults.Brushes.Conceal;
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 3;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                Grid.SetColumnSpan(splitter, 1);
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, signalTrackGrid.RowDefinitions.Count - 1);
                signalTrackGrid.Children.Add(splitter);
            }

            // add signal track
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            signalTrackGrid.RowDefinitions.Add(row);
            
            track.SignalColor = signalColor;
            track.BackgroundColor = backgroundColor;

            Border border = new Border();                        
            border.BorderThickness = new Thickness(7,0,0,0);
            border.BorderBrush = Defaults.Brushes.Highlight;
            border.Child = track;           

            Grid.SetColumn(border, 0);
            Grid.SetRow(border, signalTrackGrid.RowDefinitions.Count - 1);
            signalTrackGrid.Children.Add(border);

            track.Border = border;
        }        

        public void Remove(SignalTrack track)
        {
            signalTrackGrid.RowDefinitions[Grid.GetRow(track.Border)].Height = new GridLength(0);
            if (signalTrackGrid.Children.IndexOf(track.Border) > 0)
            {
                signalTrackGrid.Children.RemoveAt(signalTrackGrid.Children.IndexOf(track.Border) - 1);
                signalTrackGrid.Children.RemoveAt(signalTrackGrid.Children.IndexOf(track.Border));
            }
        }
    }
}
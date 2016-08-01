using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for SignalTrackControl.xaml
    /// </summary>
    ///

    public class SignalRemoveEventArgs : EventArgs
    {
        private readonly SignalTrack _signal;

        public SignalRemoveEventArgs(SignalTrack signal)
        {
            _signal = signal;
        }

        public SignalTrack signal
        {
            get { return _signal; }
        }
    }

    public partial class SignalTrackControl : UserControl
    {
        public event EventHandler<SignalRemoveEventArgs> RemoveSignal;

        protected virtual void OnRemoveSignal(SignalTrack signal)
        {
            if (RemoveSignal != null) RemoveSignal(this, new SignalRemoveEventArgs(signal));
        }

        public SignalTrackControl()
        {
            InitializeComponent();
        }

        public void clear()
        {
            this.signalTrackGrid.RowDefinitions.Clear();
            this.signalTrackGrid.Children.Clear();
        }

        public ISignalTrack addSignalTrack(Signal signal, string color, string background)
        {
            if (this.signalTrackGrid.Children.Count > 0)
            {
                // add splitter
                GridSplitter splitter = new GridSplitter();
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 3;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                this.signalTrackGrid.Children.Add(splitter);
            }

            // add signal track
            SignalTrack track = new SignalTrack(signal);
            track.SignalColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

            //track.BackgroundColor = SystemColors.ActiveBorderBrush;
            track.BackgroundColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(background));

            SignalTrackEx trackex = new SignalTrackEx();
            trackex.CloseButton.Click += removeTrack;
            trackex.AddTrack(track);
            trackex.SignalColor = track.SignalColor.Color;
            trackex.BackColor = track.BackgroundColor.Color;

            this.signalTrackGrid.Children.Add(trackex);

            this.signalTrackGrid.RowDefinitions.Clear();
            foreach (UIElement ui in this.signalTrackGrid.Children)
            {
                if (ui is GridSplitter)
                {
                    RowDefinition split_row = new RowDefinition();
                    split_row.Height = new GridLength(1, GridUnitType.Auto);
                    this.signalTrackGrid.RowDefinitions.Add(split_row);
                    Grid.SetRow(ui, this.signalTrackGrid.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(track, 2);
                }
                else if (ui is SignalTrackEx)
                {
                    RowDefinition track_row = new RowDefinition();

                    track_row.Height = new GridLength(1, GridUnitType.Star);
                    this.signalTrackGrid.RowDefinitions.Add(track_row);
                    Grid.SetRow(ui, this.signalTrackGrid.RowDefinitions.Count - 1);
                    Grid.SetColumnSpan(track, 2);
                }
            }

            return track;
        }

        private void removeTrack(object sender, RoutedEventArgs e)
        {
            SignalTrackEx trackex = GetAncestorOfType<SignalTrackEx>(sender as Button);
            trackex.RemoveTrack(trackex.signaltrack);
            GridSplitter splitter = null;

            if (this.signalTrackGrid.Children.IndexOf(trackex) < this.signalTrackGrid.Children.Count - 1)
            {
                splitter = (GridSplitter)this.signalTrackGrid.Children[this.signalTrackGrid.Children.IndexOf(trackex) + 1];
                this.signalTrackGrid.Children.Remove(splitter);
            }

            this.signalTrackGrid.Children.Remove(trackex);

            this.signalTrackGrid.RowDefinitions.Clear();
            foreach (UIElement ui in this.signalTrackGrid.Children)
            {
                if (ui is GridSplitter)
                {
                    RowDefinition split_row = new RowDefinition();
                    split_row.Height = new GridLength(1, GridUnitType.Auto);
                    this.signalTrackGrid.RowDefinitions.Add(split_row);
                    Grid.SetRow(ui, this.signalTrackGrid.RowDefinitions.Count - 1);
                }
                else if (ui is SignalTrackEx)
                {
                    RowDefinition track_row = new RowDefinition();
                    track_row.Height = new GridLength(1, GridUnitType.Star);
                    this.signalTrackGrid.RowDefinitions.Add(track_row);
                    Grid.SetRow(ui, this.signalTrackGrid.RowDefinitions.Count - 1);
                }
            }

            //We tell the viewhandler here to remove the track. this is only important for the project file.
            OnRemoveSignal(trackex.signaltrack);
        }

        public T GetAncestorOfType<T>(FrameworkElement child) where T : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(child);
            if (parent != null && !(parent is T))
                return (T)GetAncestorOfType<T>((FrameworkElement)parent);
            return (T)parent;
        }
    }
}
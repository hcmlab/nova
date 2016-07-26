using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for MediaControl.xaml
    /// </summary>
    ///

    public class MediaRemoveEventArgs : EventArgs
    {
        private readonly IMedia _media;

        public MediaRemoveEventArgs(IMedia media)
        {
            _media = media;
        }

        public IMedia media
        {
            get { return _media; }
        }
    }

    public partial class MediaControl : UserControl
    {
        public event EventHandler<MediaRemoveEventArgs> RemoveMedia;

        protected virtual void OnRemoveMedia(IMedia media)
        {
            if (RemoveMedia != null) RemoveMedia(this, new MediaRemoveEventArgs(media));
        }

        public MediaControl()
        {
            InitializeComponent();
        }

        public void clear()
        {
            this.audioGrid.RowDefinitions.Clear();
            this.audioGrid.Children.Clear();
            this.videoGrid.RowDefinitions.Clear();
            this.videoGrid.Children.Clear();
        }

        public void addMedia(IMedia media, bool is_video)
        {
            Grid grid = is_video ? videoGrid : audioGrid;

            if (is_video == true && grid.RowDefinitions.Count > 0)
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

            MediaBox media_box = new MediaBox(media, is_video);
            media_box.CloseButton.Click += removeMedia;
            media_box.zoomIn.Click += zoomIn;
            media_box.zoomOut.Click += zoomOut;
            Grid.SetColumn(media_box, 0);
            Grid.SetRow(media_box, grid.RowDefinitions.Count - 1);
            grid.Children.Add(media_box);
        }

        public void removeMedia(object sender, RoutedEventArgs e)
        {
            MediaBox media = GetAncestorOfType<MediaBox>(sender as Button);
            Grid grid = media.isvideo ? this.videoGrid : this.audioGrid;
            media.RemoveMediaBox(media.mediaelement);
            grid.Children.Remove(media);
            grid.RowDefinitions[Grid.GetRow(media)].Height = new GridLength(0);
            if (grid.RowDefinitions.Count > Grid.GetRow(media) + 1) grid.RowDefinitions[Grid.GetRow(media) + 1].Height = new GridLength(0);
            OnRemoveMedia(media.mediaelement);
        }

        public void zoomIn(object sender, RoutedEventArgs e)
        {
            MediaBox media = GetAncestorOfType<MediaBox>(sender as Button);
            Grid grid = media.isvideo ? this.videoGrid : this.audioGrid;
            media.mediaelement.zoomIn(1.5, this.ActualWidth, this.ActualHeight);
        }

        public void zoomOut(object sender, RoutedEventArgs e)
        {
            MediaBox media = GetAncestorOfType<MediaBox>(sender as Button);
            Grid grid = media.isvideo ? this.videoGrid : this.audioGrid;
            media.mediaelement.zoomOut(1.5, this.ActualWidth, this.ActualHeight);
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
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for MediaBox.xaml
    /// </summary>
    public partial class MediaBox : UserControl
    {
        private IMedia media = null;
        private bool is_video;

        public MediaBox(IMedia media, bool is_video)
        {
            this.media = media;

            InitializeComponent();

            string filepath = media.GetFilepath();
            string[] tmp = filepath.Split('\\');
            string filename = tmp[tmp.Length - 1];
            this.nameLabel.Text = filename;
            this.nameLabel.ToolTip = filepath;
            this.is_video = is_video;
            if (is_video)
            {
                /* VideoDrawing aVideoDrawing = new VideoDrawing();
                 aVideoDrawing.Rect = new Rect(0, 0, 100, 100);
                 aVideoDrawing.Player = media;

                 Image imgVideoPlayer = new Image();
                 DrawingImage di = new DrawingImage(aVideoDrawing);
                 imgVideoPlayer.Source = di; */

                Grid.SetColumn(media.GetView(), 0);
                Grid.SetRow(media.GetView(), 0);
                this.mediaBoxGrid.Children.Add(media.GetView());

                /*Grid.SetColumn(imgVideoPlayer, 0);
                Grid.SetRow(imgVideoPlayer, 0);
                this.mediaBoxGrid.Children.Add(imgVideoPlayer);*/
            }
        }

        public IMedia mediaelement
        {
            get { return media; }
            set { media = value; }
        }

        public bool isvideo
        {
            get { return is_video; }
            set { is_video = value; }
        }

        public void RemoveMediaBox(IMedia media)
        {
            media.Stop();
            media.Clear();
            this.mediaBoxGrid.Children.Remove(media.GetView());
        }

        private void volumeCheck_Checked(object sender, RoutedEventArgs e)
        {
            this.media.SetVolume(0);
        }

        private void volumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (this.volumeCheck.IsChecked == false)
            {
                this.media.SetVolume((double)volumeSlider.Value);
            }
        }

        private void volumeCheck_Unchecked(object sender, RoutedEventArgs e)
        {
            this.media.SetVolume(this.volumeSlider.Value);
        }
    }
}
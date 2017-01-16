using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ssi
{
    public partial class MainHandler
    {
        protected void removeMedia(object sender, MediaRemoveEventArgs e)
        {
            mediaList.Medias.Remove(e.media);
        }

        private void loadMedia(string filename, bool is_video, string url = null)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Media file not found '" + filename + "'");
                return;
            }

            double pos = MainHandler.Time.TimeFromPixel(signalCursor.X);
            IMedia media = mediaList.addMedia(filename, pos, url);
            control.mediaVideoControl.addMedia(media, is_video);
            control.navigator.playButton.IsEnabled = true;
            innomediaplaymode = false;
            noMediaPlayHandler(null);

            ColumnDefinition columvideo = control.videoskel.ColumnDefinitions[0];
            columvideo.Width = new GridLength(1, GridUnitType.Star);

            DispatcherTimer _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += new EventHandler(delegate (object s, EventArgs a)
            {
                if (media.GetLength() > 0)
                {
                    updateTimeRange(media.GetLength());
                    if (this.mediaList.Medias.Count == 1 && media.GetLength() > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
                    _timer.Stop();
                }
            });
            _timer.Start();
        }

    }
}

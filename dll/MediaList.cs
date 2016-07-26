using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace ssi
{
    public class MediaPlayEventArgs : EventArgs
    {
        public double pos = 0;
    }

    public delegate void MediaPlayEventHandler(MediaList videos, MediaPlayEventArgs e);

    public class MediaList
    {
        private bool isPlaying = false;

        public bool IsPlaying
        {
            get { return isPlaying; }
        }

        private List<IMedia> medias = new List<IMedia>();

        public List<IMedia> Medias
        {
            get { return medias; }
        }

        private DispatcherTimer timer = null;
        private MediaPlayEventArgs media_play_args = new MediaPlayEventArgs();
        public MediaPlayEventHandler OnMediaPlay;

        private AnnoListItem play_item = null;
        private bool play_loop = false;

        public MediaList()
        {
            this.timer = new DispatcherTimer();
            this.timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            this.timer.Tick += new EventHandler(timer_Tick);
        }

        public void clear()
        {
            foreach (IMedia media in medias)
            {
                media.Clear();
            }
            medias.Clear();
        }

        public IMedia addMedia(string filename, double pos_in_seconds)
        {
            MediaKit media = new MediaKit(filename, pos_in_seconds);
            // Media media = new Media(filename, pos_in_seconds);
            this.medias.Add(media);
            return media;
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            if (Medias.Count > 0)
            {
                double pos = medias[0].GetPosition();
                if (pos >= play_item.Stop)
                {
                    if (this.play_loop)
                    {
                        play(play_item, true);
                    }
                    else
                    {
                        stop();
                    }
                }
                else
                {
                    media_play_args.pos = pos;
                    if (OnMediaPlay != null)
                    {
                        OnMediaPlay(this, media_play_args);
                    }
                }
            }
        }

        public void play(AnnoListItem item, bool loop)
        {
            if (medias.Count > 0)
            {
                this.play_item = item;
                this.play_loop = loop;
                this.timer.Start();
                foreach (IMedia v in medias)
                {
                    //v.Position = TimeSpan.FromSeconds(item.Start);
                    //v.MediaPosition = (long) (item.Start * 1000.0);
                    v.Move(item.Start);
                    v.Play();
                }
                isPlaying = true;
            }
        }

        public void stop()
        {
            if (medias.Count > 0)
            {
                this.timer.Stop();
                foreach (IMedia v in medias)
                {
                    v.Pause();
                }
                isPlaying = false;
            }
        }

        public void move(double to_in_ms)
        {
            foreach (IMedia v in medias)
            {
                if (v.IsVideo())
                {
                    v.Move(to_in_ms);
                }
            }
        }
    }
}
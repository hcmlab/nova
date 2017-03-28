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

    public class MediaList : List<IMedia>
    {
        private bool isPlaying = false;

        public bool IsPlaying
        {
            get { return isPlaying; }
        }

        private DispatcherTimer timer = null;
        private MediaPlayEventArgs arguments = new MediaPlayEventArgs();
        public MediaPlayEventHandler OnMediaPlay;

        private AnnoListItem play_item = null;
        private bool play_loop = false;

        public MediaList()
        {
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            timer.Tick += new EventHandler(timer_Tick);
        }

        public new void Clear()
        {
            foreach (IMedia media in this)
            {
                media.Clear();
            }
            base.Clear();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            
            if (Count > 0)
            {
                double pos = this[0].GetPosition();
                if (pos >= play_item.Stop)
                {
                    if (play_loop)
                    {
                        Play(play_item, true);
                    }
                    else
                    {
                        Stop();
                    }
                }
                else
                {
                    arguments.pos = pos;                    
                    OnMediaPlay?.Invoke(this, arguments);
                }
            }
        }

        public void Play(AnnoListItem item, bool loop)
        {
            if (Count > 0)
            {
                play_item = item;
                play_loop = loop;
                timer.Start();
                foreach (IMedia media in this)
                {
                    media.Move(item.Start);
                    media.Play();
                }
                isPlaying = true;
            }
        }

        public void Stop()
        {
            if (Count > 0)
            {
                timer.Stop();
                foreach (IMedia media in this)
                {
                    media.Pause();
                }
                isPlaying = false;
            }
        }

        public void Move(double time)
        {
            foreach (IMedia media in this)
            {
                if (media.GetMediaType() != MediaType.AUDIO)
                {
                    media.Move(time);
                }
            }
        }
    }
}
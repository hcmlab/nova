using System;
using System.Collections.Generic;
using System.Windows.Threading;

namespace ssi
{
    public class MediaList : List<IMedia>
    {   
        public MediaList()
        {
        }

        public IMedia GetFirstVideo()
        {
            foreach (IMedia video in this)
            {
                if (video.GetMediaType() == MediaType.VIDEO)
                {
                    return video;
                }
            }

            return null;
        }

        public void Play()
        {
            foreach (IMedia media in this)
            {
                if (media.GetMediaType() == MediaType.VIDEO
                    || media.GetMediaType() == MediaType.AUDIO
                    || media.GetMediaType() == MediaType.PIPELINE)
                {
                    media.Play();
                }
            }

        }

        public void Stop()
        {
            foreach (IMedia media in this)
            {
                if (media.GetMediaType() == MediaType.VIDEO
                    || media.GetMediaType() == MediaType.AUDIO
                    || media.GetMediaType() == MediaType.PIPELINE)
                {
                    media.Pause();
                }
            }
        }

        public new void Clear()
        {
            foreach (IMedia media in this)
            {
                media.Clear();
            }
            base.Clear();
        }      

        public void Move(double time)
        {
            foreach (IMedia media in this)
            {
                media.Move(time);
            }
        }
    }
}
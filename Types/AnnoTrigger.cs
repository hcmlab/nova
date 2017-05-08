using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ssi
{
    public class AnnoTrigger : IMedia
    {
        AnnoList annoList;
        double position;
        int currentOrNextItemIndex;
        PluginCaller trigger;
        Dictionary<string, object> args;

        public AnnoTrigger(AnnoList annoList, PluginCaller trigger, Dictionary<string, object> args)
        {
            this.annoList = annoList;
            this.trigger = trigger;
            this.args = args;

            position = 0;
            currentOrNextItemIndex = annoList.Count == 0 ? -1 : 0;

            object result = trigger.call("open", args);
            if (result != null)
            {
                MessageTools.Error(result.ToString());
            }
        }

        ~AnnoTrigger()
        {
            object result = trigger.call("close", args);
            if (result != null)
            {
                MessageTools.Error(result.ToString());
            }
        }

        private int findItem(double position)
        {
            if (annoList.Count == 0)
            {
                return -1;
            }

            for (int index = 0; index < annoList.Count; index++)
            {
                if (position <= annoList[index].Stop)
                {
                    return index;
                }
            }

            return -1;
        }

        public void move(double newPosition, double threshold)
        {
            if (Math.Abs(newPosition - position) <= threshold)
            {

                if (currentOrNextItemIndex != -1)
                {
                    if (position < annoList[currentOrNextItemIndex].Start &&
                        newPosition >= annoList[currentOrNextItemIndex].Start)
                    {
                        args["label"] = annoList[currentOrNextItemIndex].Label;
                        trigger.call("update", args);
                    }

                    if (newPosition > annoList[currentOrNextItemIndex].Stop)
                    {
                        currentOrNextItemIndex++;
                        if (currentOrNextItemIndex >= annoList.Count)
                        {
                            currentOrNextItemIndex = -1;
                        }
                    }
                }
            }
            else
            {
                currentOrNextItemIndex = findItem(newPosition);
            }

            position = newPosition;
        }

        public void Clear()
        { 
        }

        public string GetDirectory()
        {
            if (annoList.Source.HasFile)
            {
                return annoList.Source.File.Directory;
            }
            return "";
        }


        public string GetFilepath()
        {
            if (annoList.Source.HasFile)
            {
                return annoList.Source.File.Path;
            }
            return "";
        }

        public double GetLength()
        {
            return annoList[annoList.Count-1].Stop;
        }

        public MediaType GetMediaType()
        {
            return MediaType.TRIGGER;
        }

        public WriteableBitmap GetOverlay()
        {
            return null;
        }

        public double GetPosition()
        {
            return position;
        }

        public double GetSampleRate()
        {
            return 0;
        }

        public UIElement GetView()
        {
            return null;
        }

        public double GetVolume()
        {
            return 0;
        }

        public void SetVolume(double volume)
        {
        }

        public bool HasAudio()
        {
            return false;
        }

        public void Move(double time)
        {
            if (annoList != null && annoList.Count > 0)
            {
                move(time, 0.2);
            }
        }

        public void Pause()
        {
        }

        public void Play()
        {
        }

        public void Stop()
        {
        }
    }
}

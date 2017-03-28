using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace ssi
{
    public class Media : MediaElement, IMedia
    {
        private string filepath;
        private MediaType type;

        public MediaType GetMediaType()
        {
            return type;
        }

        public string GetFilepath()
        {
            return filepath;
        }

        public string GetDirectory()
        {
            return filepath.Substring(0, filepath.LastIndexOf("/") + 1);
        }

        public new bool HasAudio()
        {
            return base.HasAudio;
        }

        public void SetVolume(double volume)
        {
            Volume = volume;
        }

        public double GetVolume()
        {
            return Volume;
        }

        public double GetSampleRate()
        {
            return 0;
        }

        public Media(string filepath, MediaType type)
        {
            this.LoadedBehavior = MediaState.Manual;
            this.UnloadedBehavior = MediaState.Manual;
            this.Source = new Uri(filepath);
            //this.Open (new Uri (filename));
            // if ScrubbingEnabled is true move correctly shows selected frame, but cursor won't work any more...
            //this.ScrubbingEnabled = true;
            this.Pause();
            this.filepath = filepath;
            this.type = type;
        }

        public void Move(double time)
        {
            this.Position = TimeSpan.FromSeconds(time);
        }

        public double GetPosition()
        {
            return Position.TotalMilliseconds / 1000.0;
        }

        public UIElement GetView()
        {
            return this;
        }

        public WriteableBitmap GetOverlay()
        {
            throw new NotImplementedException();
        }

        public double GetLength()
        {
            return this.NaturalDuration.TimeSpan.TotalSeconds;
        }

        public void Clear()
        {
            this.Close();
        }

    }
}
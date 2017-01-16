using System;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    public class Media : MediaElement, IMedia
    {
        private string filepath;
        private string dbId;

        public string GetFilepath()
        {
            return filepath;
        }

        public string GetFolderepath()
        {
            return filepath.Substring(0, filepath.LastIndexOf("/") + 1);
        }

        public void SetVolume(double volume)
        {
            this.Volume = volume;
        }

        public double GetSampleRate()
        {
            return 0;
        }

        public void DBID(string url)
        {
            this.dbId = url;
        }

        public Media(string filepath, double pos_in_seconds)
        {
            this.LoadedBehavior = MediaState.Manual;
            this.UnloadedBehavior = MediaState.Manual;
            this.Source = new Uri(filepath);
            //this.Open (new Uri (filename));
            // if ScrubbingEnabled is true move correctly shows selected frame, but cursor won't work any more...
            //this.ScrubbingEnabled = true;
            this.Pause();
            this.filepath = filepath;
        }

        public void Move(double to_in_seconds)
        {
            this.Position = TimeSpan.FromSeconds(to_in_seconds);
        }

        public double GetPosition()
        {
            return Position.TotalMilliseconds / 1000.0;
        }

        public bool IsVideo()
        {
            return this.HasVideo;
        }

        public UIElement GetView()
        {
            return this;
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
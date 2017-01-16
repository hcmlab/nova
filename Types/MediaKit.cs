using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Windows;
using WPFMediaKit.DirectShow.Controls;

namespace ssi
{
    public class MediaKit : MediaUriElement, IMedia
    {
        private string filepath;
        private string dbId;
        private MediaFile inputFile;

        public string GetFilepath()
        {
            return filepath;
        }

        public string GetFolderepath()
        {
            return filepath.Substring(0, filepath.LastIndexOf("\\") + 1);
        }

        public void SetVolume(double volume)
        {
            this.Volume = volume;
        }

        public void DBID(string url)
        {
            this.dbId = url;
        }

        public MediaKit(string filepath, double pos_in_seconds)
        {
            this.LoadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;
            this.UnloadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Close;

   
            this.BeginInit();
            this.Source = new Uri(filepath);
            this.EndInit();
            // if ScrubbingEnabled is true move correctly shows selected frame, but cursor won't work any more...
            //  this.ScrubbingEnabled = true;
            this.Volume = 1.0;
            this.Pause();
            this.filepath = filepath;

            inputFile = new MediaFile { Filename = this.filepath };
            //using (var engine = new Engine())
            //{
            //    engine.GetMetadata(inputFile);
            //}

            //string[] split = inputFile.Metadata.VideoData.FrameSize.Split('x');
            //int w = int.Parse(split[0]);
            //int h = int.Parse(split[1]);
        }

        public void Move(double to_in_seconds)
        {
            this.MediaPosition = (long)(to_in_seconds * 10000000.0);
        }

        public double GetPosition()
        {
            return this.MediaPosition / 10000000.0;
        }

        public bool IsVideo()
        {
            return this.HasVideo;
        }

        public double GetSampleRate()
        {

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
            }

            if (this.HasVideo && inputFile.Metadata.VideoData != null)
                return inputFile.Metadata.VideoData.Fps;
            else return 25.0;
        }

        public UIElement GetView()
        {
            return this;
        }

        public double GetLength()
        {
            return this.MediaDuration / 10000000.0;
        }

        public void Clear()
        {
            this.Close();
        }

    }
}
using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ssi
{
    public class Media : MediaElement, IMedia
    {
        private string filepath;
        private MediaType type;

        public delegate void MediaMouseDown(Media media, double x, double y);
        public event MediaMouseDown OnMediaMouseDown;

        public delegate void MediaMouseUp(Media media, double x, double y);
        public event MediaMouseUp OnMediaMouseUp;

        public delegate void MediaMouseMove(Media media, double x, double y);
        public event MediaMouseMove OnMediaMouseMove;


        private Grid grid;
        private WriteableBitmap overlayBitmap;
        private Image overlayImage;

        private MediaFile inputFile;
        double sampleRate;
        int width;
        int height;


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


            this.filepath = filepath;
            this.type = type;

            inputFile = new MediaFile { Filename = this.filepath };
            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
            }
            if (type == MediaType.VIDEO)
            {
                sampleRate = inputFile.Metadata.VideoData.Fps;
                string frameSize = inputFile.Metadata.VideoData.FrameSize;
                string[] tokens = frameSize.Split('x');
                int.TryParse(tokens[0], out width);
                int.TryParse(tokens[1], out height);
            }
            else if (type == MediaType.AUDIO)
            {
                string[] tokens = inputFile.Metadata.AudioData.SampleRate.Split(' ');
                double.TryParse(tokens[0], out sampleRate);
            }

            // add grid
            grid = new Grid();
            grid.MouseDown += OnMouseDown;
            grid.MouseUp += OnMouseUp;
            grid.MouseMove += OnMouseMove;

            // add video
            Grid.SetColumn(this, 0);
            Grid.SetRow(this, 0);
            grid.Children.Add(this);

            // add overlay
            if (type == MediaType.VIDEO)
            {
                overlayImage = new Image();
                overlayBitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
                overlayBitmap.Clear(Colors.Transparent);
                overlayImage.Source = overlayBitmap;
                Grid.SetColumn(overlayImage, 0);
                Grid.SetRow(overlayImage, 0);
                grid.Children.Add(overlayImage);
            }
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
            return grid;
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

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (this.HasVideo)
            {
                Point p = e.GetPosition(this);
                double pixelWidth = this.Width;
                double pixelHeight = this.Height;
                double x = pixelWidth * p.X / this.ActualWidth;
                double y = pixelHeight * p.Y / this.ActualHeight;

                OnMediaMouseMove?.Invoke(this, x, y);
            }
        }

        public void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.HasVideo)
            {
                Point p = e.GetPosition(this);
                double pixelWidth = this.Width;
                double pixelHeight = this.Height;
                double x = pixelWidth * p.X / this.ActualWidth;
                double y = pixelHeight * p.Y / this.ActualHeight;

                OnMediaMouseDown?.Invoke(this, x, y);
            }
        }

        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if(this.HasVideo)
            {
            Point p = e.GetPosition(this);
            double pixelWidth = this.Width;
            double pixelHeight = this.Height;
            double x = pixelWidth * p.X / this.ActualWidth;
            double y = pixelHeight * p.Y / this.ActualHeight;
            OnMediaMouseUp?.Invoke(this, x, y);
            }
        }

    }
}
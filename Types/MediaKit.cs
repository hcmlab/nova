using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFMediaKit.DirectShow.Controls;

namespace ssi
{
    public class MediaKit : MediaUriElement, IMedia
    {
        public delegate void MediaMouseDown(MediaKit media, double x, double y);
        public event MediaMouseDown OnMediaMouseDown;

        public delegate void MediaMouseUp(MediaKit media, double x, double y);
        public event MediaMouseUp OnMediaMouseUp;

        public delegate void MediaMouseMove(MediaKit media, double x, double y);
        public event MediaMouseMove OnMediaMouseMove;

        private Grid grid;
        private WriteableBitmap overlayBitmap;
        private Image overlayImage;

        private string filepath;
        private MediaFile inputFile;
        private MediaType type;
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
            return filepath.Substring(0, filepath.LastIndexOf("\\") + 1);
        }

        public bool HasAudio()
        {
            return true;
        }

        public void SetVolume(double volume)
        {
            Volume = volume;
        }

        public double GetVolume()
        {
            return Volume;
        }

        public MediaKit(string filepath, MediaType type)
        {
            LoadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Manual;
            UnloadedBehavior = WPFMediaKit.DirectShow.MediaPlayers.MediaState.Close;
                         
            BeginInit();
            Source = new Uri(filepath);
            EndInit();

            // if ScrubbingEnabled is true move correctly shows selected frame, but cursor won't work any more...
            //  this.ScrubbingEnabled = true;
            Volume = 1.0;
            Pause();

            this.filepath = filepath;
            this.type = type;

            inputFile = new MediaFile { Filename = this.filepath };
            using (var engine = new Engine())
            {
               engine.GetMetadata(inputFile);
            }

            try
            {
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
            catch
            {
                Console.WriteLine("Cannot open Media file: " + inputFile.Filename);
            }
        }

        public void OnMouseMove(object sender, MouseEventArgs e)
        {
            Point p = e.GetPosition(VideoImage);
            double pixelWidth = VideoImage.Source.Width;
            double pixelHeight = VideoImage.Source.Height;
            double x = pixelWidth * p.X / VideoImage.ActualWidth;
            double y = pixelHeight * p.Y / VideoImage.ActualHeight;

            OnMediaMouseMove?.Invoke(this, x, y);
        }

        public void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(VideoImage);
            double pixelWidth = VideoImage.Source.Width;
            double pixelHeight = VideoImage.Source.Height;
            double x = pixelWidth * p.X / VideoImage.ActualWidth;
            double y = pixelHeight * p.Y / VideoImage.ActualHeight;

            OnMediaMouseDown?.Invoke (this, x, y);
        }

        public void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            Point p = e.GetPosition(VideoImage);
            double pixelWidth = VideoImage.Source.Width;
            double pixelHeight = VideoImage.Source.Height;
            double x = pixelWidth * p.X / VideoImage.ActualWidth;
            double y = pixelHeight * p.Y / VideoImage.ActualHeight;

            OnMediaMouseUp?.Invoke(this, x, y);
        }

        public void Move(double time)
        {
            MediaPosition = (long)(time * 10000000.0);
        }

        public double GetPosition()
        {
            return MediaPosition / 10000000.0;
        }

        public double GetSampleRate()
        {
            return sampleRate;
        }

        public UIElement GetView()
        {
            return grid;
        }

        public WriteableBitmap GetOverlay()
        {
            return overlayBitmap;
        }

        public double GetLength()
        {
            return MediaDuration / 10000000.0;
        }

        public void Clear()
        {
            Close();
        }

    }
}
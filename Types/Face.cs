using MediaToolkit;
using MediaToolkit.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using WPFMediaKit.DirectShow.Controls;

namespace ssi
{
    public class Face : Image, IMedia, INotifyPropertyChanged
    {
        private string filepath;
        private Signal signal;
        private MediaType type;

        private Color signalColor;
        private Color headColor;
        private Color backColor;

        private int renderWidth = 640;
        private int renderHeight = 360;
        private double zoomFactor = 1.0;
        private double offsetY = 0.0;
        private double offsetX = 0.0;

        private int numJoints = 0;
        private int jointValues = 0;
        private int numSkeletons = 1;

        private WriteableBitmap writeableBmp;
        private DispatcherTimer timer;
        private List<Point3D> joints = new List<Point3D>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Face(string filepath, Signal signal)
        {
            this.filepath = filepath;
            this.signal = signal;
            type = MediaType.SKELETON;

            BackColor = Defaults.Colors.Background;
            SignalColor = Defaults.Colors.Foreground;
            HeadColor = Defaults.Colors.Foreground;

            numSkeletons = signal.meta_num;

            this.numJoints = 25;
            if (signal.meta_type == "kinect1") this.numJoints = 20;
            else if (signal.meta_type == "kinect2") this.numJoints = 25;
            

            jointValues = (int)((signal.dim / numSkeletons) / numJoints);

            writeableBmp = new WriteableBitmap(renderWidth, renderHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            writeableBmp.Clear(BackColor);            

            Source = writeableBmp;
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000.0 / signal.rate);
            timer.Tick += new EventHandler(Draw);
        }
        
        public void Draw(object myObject, EventArgs myEventArgs)
        {
            Draw(MainHandler.Time.CurrentPlayPositionPrecise);
        }

        public void Draw(double time)
        {
            int actualdim = 3;
            int index = (int)((int)(time * signal.rate) * signal.dim);

            Color col = SignalColor;
            double width = renderWidth;
            double height = renderHeight;

            writeableBmp.Lock();
            writeableBmp.Clear(BackColor);
        
            //add some logic here  
            //  if(signal.meta_type == "kinect2")

            if (index < signal.data.Length)
            {
                for (int i = 0; i < signal.dim; i = i + 3)
                {
                    double X = (signal.data[index + i * actualdim]) * zoomFactor * height + (width / 2) + offsetX;
                    double Y = height - (signal.data[index + i * actualdim + 1]) * zoomFactor * width - height / 2 + offsetY;
                    double Z = signal.data[index + i * actualdim + 2] * 100;
                    writeableBmp.DrawLineAa((int)X, (int)Y, (int)X + 1, (int)Y, this.SignalColor);
                }
            }

            writeableBmp.Unlock();

        }

        private Color getColor(int Z)
        {
            Color c = this.SignalColor;
            c.A = (byte)(Z < 255 ? Z : 255);
            return c;
        }

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
            return false;
        }

        public void SetVolume(double volume)
        {
        }

        public double GetVolume()
        {
            return 0;
        }
   
        public void Move(double time)
        {
            Draw(time);
        }

        public double GetPosition()
        {
            return 0;
        }

        public double GetSampleRate()
        {
            return 0;
        }

        public UIElement GetView()
        {
            return this;
        }

        public double GetLength()
        {
            return 0;
        }

        public void Clear()
        {
        }

        public void Play()
        {
            timer.Start();
        }

        public void Stop()
        {
            timer.Stop();
        }

        public void Pause()
        {
            timer.Stop();
        }

        public Color SignalColor
        {
            get
            {
                return signalColor;
            }

            set
            {
                signalColor = value;
                RaisePropertyChanged("SignalColor");
            }
        }

        public Color HeadColor
        {
            get
            {
                return headColor;
            }

            set
            {
                headColor = value;
                RaisePropertyChanged("HeadColor");
            }
        }

        public Color BackColor
        {
            get
            {
                return backColor;
            }

            set
            {
                backColor = value;
                RaisePropertyChanged("BackColor");
            }
        }


    }
}
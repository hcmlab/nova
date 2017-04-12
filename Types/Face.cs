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

        private int width;
        private int height;

        private double postion = 0;

        private float[] mins = { 0, 0, 0 };
        private float[] maxs = { 0, 0, 0 };
        private float[] facs = { 1, 1, 1 };

        private WriteableBitmap writeableBmp;
        private DispatcherTimer timer;
        private List<Point3D> joints = new List<Point3D>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Face(string filepath, Signal signal, int width = 600, int height = 600)
        {
            this.filepath = filepath;
            this.signal = signal;
            this.width = width;
            this.height = height;

            type = MediaType.FACE;

            BackColor = Defaults.Colors.Background;
            SignalColor = Defaults.Colors.Foreground;
            HeadColor = Defaults.Colors.Foreground;
    
            writeableBmp = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgr32, null);
            writeableBmp.Clear(BackColor);            

            Source = writeableBmp;
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000.0 / signal.rate);
            timer.Tick += new EventHandler(Draw);

            minMax();
            scale();
        }

        public void minMax()
        {
            for (int i = 0; i < 3; i++)
            {
                mins[i] = signal.min[i];
                maxs[i] = signal.max[i];
            }
            
            for (int i = 3; i < signal.dim; i++)
            {
                int dim = i % 3;
                if (mins[dim] > signal.min[i])
                {
                    mins[dim] = signal.min[i];
                }
                else if (maxs[dim] < signal.max[i])
                {
                    maxs[dim] = signal.max[i];
                }                                      
            }

            mins[0] = mins[1] = Math.Min(mins[0], mins[1]);
            maxs[0] = maxs[1] = Math.Max(maxs[0], maxs[1]);
        }

        public void scale()
        {
            for (int i = 0; i < signal.number * signal.dim; i++)
            {
                int dim = i % 3;
                if (maxs[dim] - mins[dim] != 0)
                {
                    signal.data[i] = (signal.data[i] - mins[dim]) / (maxs[dim] - mins[dim]);
                }
                else
                {
                    signal.data[i] = signal.data[i] - mins[dim];
                }
            }
        }
        
        public void Draw(object myObject, EventArgs myEventArgs)
        {
            Draw(MainHandler.Time.CurrentPlayPositionPrecise);
        }

        public void Draw(double time)
        {
            postion = time;
            uint index = (uint)(time * signal.rate);

            writeableBmp.Lock();
            writeableBmp.Clear(BackColor);

            if (index < signal.number)
            {
                for (uint i = index * signal.dim; i < (index+1) * signal.dim; i += 3)
                {
                    double X = signal.data[i] * width;
                    double Y = height - signal.data[i+1] * height;
                    writeableBmp.SetPixel((int)X, (int)Y, SignalColor);
                }
            }

            writeableBmp.Unlock();

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
            return postion;
        }

        public double GetSampleRate()
        {
            return signal.rate;
        }

        public UIElement GetView()
        {
            return this;
        }

        public WriteableBitmap GetOverlay()
        {
            return writeableBmp;
        }

        public double GetLength()
        {
            return signal.number/signal.rate;
        }

        public void Clear()
        {
            if (writeableBmp != null)
            {
                writeableBmp.Clear(BackColor);
            }
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
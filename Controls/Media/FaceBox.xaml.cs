using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace ssi
{
    public partial class FaceBox : UserControl, INotifyPropertyChanged
    {
        private Color signalColor;
        private Color backColor;
        private double zoomFactor = 1.0;
        private double offsetY = 0.0;
        private double offsetX = 0.0;
        private int renderWidth = 640;
        private int renderHeight = 360;
        private WriteableBitmap writeableBmp;
        private Signal signal;
        private uint dim = 0;
        private double sr = 25;
        private DispatcherTimer timer = new DispatcherTimer();
        private List<Point3D> joints = new List<Point3D>();

        public event PropertyChangedEventHandler PropertyChanged;

        public Color SignalColor
        {
            get
            {
                return this.signalColor;
            }

            set
            {
                this.signalColor = value;

                this.RaisePropertyChanged("SignalColor");
            }
        }

        public Color BackColor
        {
            get
            {
                return this.backColor;
            }

            set
            {
                this.backColor = value;
                this.RaisePropertyChanged("BackColor");
            }
        }

        protected void RaisePropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        public FaceBox(Signal s)
        {
            InitializeComponent();

            this.BackColor = Color.FromArgb(255, 255, 255, 255);
            this.SignalColor = Color.FromArgb(255, 0, 0, 0);
            this.DataContext = this;

            if (s != null)
            {
                this.signal = s;
                this.dim = signal.dim;
                this.sr = signal.rate;

                writeableBmp = new WriteableBitmap(renderWidth, renderHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
                writeableBmp.Clear(this.BackColor);
                this.ImageViewport.Source = writeableBmp;

                drawTraceHandler();
            }
        }

        private void drawTraceHandler()
        {
            int normnedfps = (int) (1000 / sr);

            timer.Stop();
            timer.Interval = TimeSpan.FromMilliseconds(normnedfps);
            timer.Tick += new EventHandler(drawPoints);
            timer.Start();
        }

        public void drawPoints(Object myObject, EventArgs myEventArgs)
        {
            double pos = MainHandler.Time.CurrentPlayPositionPrecise;
            int actualdim = 3;
            int index = (int)((int)(pos * sr) * signal.dim);
            if (pos >= MainHandler.Time.TotalDuration) index = (int)MainHandler.Time.TotalDuration;

            Color col = this.SignalColor;
            double width = renderWidth;
            double height = renderHeight;
       
         
            writeableBmp.Lock();
            writeableBmp.Clear(this.BackColor);

          //add some logic here  
          //  if(signal.meta_type == "kinect2")
            {
                if (index < signal.data.Length)
                {
                    for (int i = 0; i < signal.dim; i = i + 3)
                    {
                        double X = (signal.data[index + i * actualdim]) * zoomFactor * height + (width/2) + offsetX;
                        double Y = height - (signal.data[index + i * actualdim + 1]) * zoomFactor * width   -height/2 + offsetY;
                        double Z = signal.data[index + i * actualdim + 2] * 100 ;
                        writeableBmp.DrawLineAa((int)X, (int)Y, (int)X + 1, (int)Y, this.SignalColor);
                    }
                }
            }
            writeableBmp.Unlock();
        }

        private Color getColor(int Z)
        {
            Color c = this.SignalColor;
            c.A = (byte) ( Z< 255 ? Z  : 255);
            return c;
        }

        private void zoomIn_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor = zoomFactor + 0.1;
        }

        private void zoomOut_Click(object sender, RoutedEventArgs e)
        {
            zoomFactor = zoomFactor - 0.1;
        }

        private void up_Click(object sender, RoutedEventArgs e)
        {
            offsetY = offsetY + 50;
        }

        private void down_Click(object sender, RoutedEventArgs e)
        {
            offsetY = offsetY - 50;
        }

        private void left_Click(object sender, RoutedEventArgs e)
        {
            offsetX = offsetX - 50;
        }

        private void right_Click(object sender, RoutedEventArgs e)
        {
            offsetX = offsetX + 50;
        }
    }

    }

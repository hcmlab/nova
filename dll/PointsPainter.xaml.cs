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
    public partial class PointsPainter : UserControl, INotifyPropertyChanged
    {
        private Color signalcolor;
        private Color backcolor;
        private double zoomfactor = 1.0;
        private double offsetY = 0.0;
        private double offsetX = 0.0;
        private int renderwidth = 640;
        private int renderheight = 360;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color SignalColor
        {
            get
            {
                return this.signalcolor;
            }

            set
            {
                this.signalcolor = value;

                this.RaisePropertyChanged("SignalColor");
            }
        }


        public Color BackColor
        {
            get
            {
                return this.backcolor;
            }

            set
            {
                this.backcolor = value;

                this.RaisePropertyChanged("BackColor");
            }
        }

        public SolidColorBrush Brush { get; set; }

        protected void RaisePropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        private WriteableBitmap writeableBmp;
        private Signal signal;
        private uint dim = 0;
        private double sr = 25;

        private DispatcherTimer _timer = new DispatcherTimer();

        private List<Point3D> Joints = new List<Point3D>();

        public PointsPainter(Signal s)
        {
            InitializeComponent();

            this.BackColor = Color.FromArgb(255, 0, 0, 0);

            this.SignalColor = Color.FromArgb(255, 255, 255, 0);
            this.Brush = Brushes.Green;


            this.DataContext = this;

            if (s != null)
            {
                this.signal = s;
                this.dim = signal.dim;
                this.sr = signal.rate;


                writeableBmp = new WriteableBitmap(renderwidth, renderheight, 96.0, 96.0, PixelFormats.Bgra32, null);
                writeableBmp.Clear(this.BackColor);
                this.ImageViewport.Source = writeableBmp;

                drawTraceHandler();
            }
        }

        private void drawTraceHandler()
        {
            int normnedfps = (int) (1000 / sr);

            _timer.Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(normnedfps);
            _timer.Tick += new EventHandler(drawPoints);
            _timer.Start();
        }

        public void drawPoints(Object myObject, EventArgs myEventArgs)
        {
            double pos = ViewHandler.Time.CurrentPlayPositionPrecise;
            int actualdim = 3;
            int index = (int)((int)(pos * sr) * signal.dim);
            if (pos >= ViewHandler.Time.TotalDuration) index = (int)ViewHandler.Time.TotalDuration;

            Color col = this.SignalColor;
            double width = renderwidth;
            double height = renderheight;
       
         
            writeableBmp.Lock();
            writeableBmp.Clear(this.BackColor);

          //add some logic here  
          //  if(signal.meta_type == "kinect2")
            {
                if (index < signal.data.Length)
                {
                    for (int i = 0; i < signal.dim; i = i + 3)
                    {
                        double X = (signal.data[index + i * actualdim]) * zoomfactor * height + (width/2) + offsetX;
                        double Y = height - (signal.data[index + i * actualdim + 1]) * zoomfactor * width   -height/2 + offsetY;
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
            zoomfactor = zoomfactor + 0.1;
        }

        private void zoomOut_Click(object sender, RoutedEventArgs e)
        {
            zoomfactor = zoomfactor - 0.1;
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

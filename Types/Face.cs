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

        public enum FaceType
        {
            NONE,
            KINECT1,
            KINECT2,
            OPENFACE
        }

        private float KINECT_SMALL_VALUES_BUG_THRES = -10f;
        private float KINECT_LARGE_VALUES_BUG_THRES = 10f;

        private string filepath;
        private Signal signal;
        private MediaType type;
        private FaceType facetype;



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

        public Face(string filepath, Signal signal, FaceType ftype, int width = 600, int height = 600)
        {
            this.filepath = filepath;
            this.signal = new Signal();


            this.signal.data = new float[signal.data.Length];
            for(int i=0; i<signal.data.Length; i++)
            {
                this.signal.data[i] = signal.data[i];
            }

            this.signal.dim = signal.dim;
            this.signal.bytes = signal.bytes;
            this.signal.max = signal.max;
            this.signal.min = signal.min;
            this.signal.number = signal.number;
            this.signal.time = signal.time;
            this.signal.rate = signal.rate;
            this.signal.loaded = signal.loaded;
            this.signal.type = signal.type;
            this.signal.Meta = signal.Meta;



            this.width = width;
            this.height = height;

            type = MediaType.FACE;
            facetype = ftype;

            BackColor = Defaults.Colors.Background;
            SignalColor = Defaults.Colors.Foreground;
            HeadColor = Defaults.Colors.Foreground;
    
            writeableBmp = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgr32, null);
            writeableBmp.Clear(BackColor);            

            Source = writeableBmp;
            
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000.0 / signal.rate);
            timer.Tick += new EventHandler(Draw);


            if(facetype == FaceType.OPENFACE)
            {
                minMaxOF();
                scaleOF();
            }

            else
            {
                minMax();
                scale();
            }

          

        }

        public void minMaxOF()
        {

            for (int i = 0; i < 2; i++)
            {
                mins[i] = float.MaxValue;
                maxs[i] = float.MinValue;
            }

            for (int i = 0; i < signal.number * signal.dim; i++)
            {

                if(i % signal.dim >= 26 && i % signal.dim <= 161)

                {
                    int dim = i % 2;
                    if (mins[dim] > signal.data[i])
                    {
                        mins[dim] = signal.data[i];
                    }
                    else if (maxs[dim] < signal.data[i])
                    {
                        maxs[dim] = signal.data[i];
                    }


                    mins[0] = mins[1] = Math.Min(mins[0], mins[1]);

                    maxs[0] = maxs[1] = Math.Max(maxs[0], maxs[1]);
                }

               


            }

      
                
                
                
        }

        public void scaleOF()
        {
            for (int i = 0; i < signal.number * signal.dim; i++)
            {
                // negative values are evil, sometimes occur though
                //if (i >= signal.dim && signal.data[i] <= KINECT_SMALL_VALUES_BUG_THRES
                //    && signal.data[i] < KINECT_LARGE_VALUES_BUG_THRES)
                //{
                //    signal.data[i] = signal.data[i - signal.dim];
                //}
                if (i % signal.dim >= 26 && i % signal.dim <= 161)

                {
                   

                    int dim = i % 2;
                    float tempmin = mins[dim];
                    float tempmax = maxs[dim];
                    if (maxs[dim] - mins[dim] != 0)
                    {
                        if (mins[dim] < 0)
                        {
                            signal.data[i] += Math.Abs(tempmin);
                            tempmin = 0;
                            tempmax = tempmax + Math.Abs(mins[dim]);

                        }
                        signal.data[i] = (signal.data[i] - tempmin) / (tempmax - tempmin);
   
                    }
                    else
                    {
                        signal.data[i] = signal.data[i] - mins[dim];
                    }
                }
            }
        }



        public void minMax()
        {
            for (int i = 0; i < 3; i++)
            {
                mins[i] = float.MaxValue;
                maxs[i] = float.MinValue;
            }

            for (int i = 0; i < signal.number * signal.dim; i++)
            {
                // negative values are evil, sometimes occur though            
                int dim = i % 3;
                if (signal.data[i] > KINECT_SMALL_VALUES_BUG_THRES 
                    && signal.data[i] < KINECT_LARGE_VALUES_BUG_THRES)
                { 
                    if (mins[dim] > signal.data[i])
                    {
                        mins[dim] = signal.data[i];
                    }
                    else if (maxs[dim] < signal.data[i])
                    {
                        maxs[dim] = signal.data[i];
                    }
                }                                
            }

            mins[0] = mins[1] = Math.Min(mins[0], mins[1]);
            maxs[0] = maxs[1] = Math.Max(maxs[0], maxs[1]);
        }




        public void scale()
        {
            for (int i = 0; i < signal.number * signal.dim; i++)
            {
                // negative values are evil, sometimes occur though
                if (i >= signal.dim && signal.data[i] <= KINECT_SMALL_VALUES_BUG_THRES
                    && signal.data[i] < KINECT_LARGE_VALUES_BUG_THRES)
                {
                    signal.data[i] = signal.data[i - signal.dim];
                }

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
            Draw(MainHandler.Time.CurrentPlayPosition);
        }

        public void Draw(double time)
        {
            postion = time;




             uint index = (uint)((time * signal.rate) +0.5F);

            writeableBmp.Lock();
            writeableBmp.Clear(BackColor);

   
            if(facetype == FaceType.KINECT1 || facetype == FaceType.KINECT2)
            {
                if (index < signal.number)
                {
                    for (uint i = index * signal.dim; i < (index + 1) * signal.dim; i += 3)
                    {
                        double X = signal.data[i] * width;
                        double Y = height - signal.data[i + 1] * height;
                        writeableBmp.SetPixel((int)X, (int)Y, SignalColor);
                    }
                }
            }

            else if (facetype == FaceType.OPENFACE)
            {
                if (index < signal.number)
                {
                    for (uint i = (index * signal.dim) + 26; i < (index * signal.dim) + 161; i += 2)
                    {
                        //double X = signal.data[i] > 0 ? signal.data[i]  * width   - width/2: 0;
                        //double Y = signal.data[i + 1] > 0 ?  signal.data[i + 1] * height   + height/2: 0;


                        double X = signal.data[i] * width;
                        double Y = signal.data[i + 1] * height;
                        try
                            {
                            if(X < width && Y < height)
                            {
                                writeableBmp.SetPixel((int)X, (int)Y, SignalColor);
                            }
                              
                            }
                             catch(Exception e)
                            {
                                Console.WriteLine(e);
                            }


        }

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
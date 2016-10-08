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
    public partial class SkeletonPainter : UserControl, INotifyPropertyChanged
    {
        private Color color1;
        private Color color2;
        private Color color3;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color SignalColor
        {
            get
            {
                return this.color1;
            }

            set
            {
                this.color1 = value;

                this.RaisePropertyChanged("SignalColor");
            }
        }

        public Color HeadColor
        {
            get
            {
                return this.color2;
            }

            set
            {
                this.color2 = value;

                this.RaisePropertyChanged("HeadColor");
            }
        }

        public Color BackColor
        {
            get
            {
                return this.color3;
            }

            set
            {
                this.color3 = value;

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
        private double sr = 0;
        private int joints = 0;
        private int jointvalues = 0;
        private int numskeletons = 1;

        //double[] _min_value_y = new double[numskeletons];
        //double[] _max_value_y = new double[numskeletons];
        //double[] _min_value_x = new double[numskeletons];
        //double[] _max_value_x = new double[numskeletons];

        private DispatcherTimer _timer = new DispatcherTimer();

        private List<Point3D> Joints = new List<Point3D>();

        public SkeletonPainter(Signal s)
        {
            InitializeComponent();

            this.BackColor = Color.FromArgb(255, 0, 0, 0);
            this.SignalColor = Color.FromArgb(255, 0, 255, 0);
            this.HeadColor = Color.FromArgb(255, 255, 255, 0);
            this.Brush = Brushes.Green;
            this.DataContext = this;

            if (s != null)
            {
                this.signal = s;
                this.dim = signal.dim;
                this.sr = signal.rate;
                this.numskeletons = signal.meta_num;

                if (signal.meta_type == "ssi") this.joints = 25;
                else if (signal.meta_type == "kinect1") this.joints = 20;
                else if (signal.meta_type == "kinect2") this.joints = 25;

                this.jointvalues = (int)((dim / numskeletons) / joints);

                //for (int i = 0; i < numskeletons; i++)
                //{
                //    _min_value_x[i] = _min_value_y[i] = Double.MaxValue;
                //    _max_value_x[i] = _max_value_y[i] = -Double.MaxValue;
                //}

                writeableBmp = new WriteableBitmap(640, 360, 96.0, 96.0, PixelFormats.Bgra32, null);
                writeableBmp.Clear(this.BackColor);
                this.ImageViewport.Source = writeableBmp;

                drawTraceHandler();
            }
        }

        private void drawTraceHandler()
        {
            double normnedfps = 1000.0 / sr;

            _timer.Stop();
            _timer.Interval = TimeSpan.FromMilliseconds(normnedfps);
            _timer.Tick += new EventHandler(drawTraces);
            _timer.Start();
        }

        public void drawTraces(Object myObject, EventArgs myEventArgs)
        {
            double pos = ViewHandler.Time.CurrentPlayPositionPrecise;
            int index = (int)(pos * sr);
            if (pos >= ViewHandler.Time.TotalDuration) index = (int)ViewHandler.Time.TotalDuration;

            Color col = this.SignalColor;
            Color colhead = this.HeadColor;

            writeableBmp.Clear(this.BackColor);
            writeableBmp.Lock();

            Point[] points = new Point[joints];

            //for (int i = 0; i < joints; i++)
            //{
            //    if (signal.data[(index * dim) + i*jointvalues + 0] != 0.0f && _min_value_x[0] > signal.data[(index * dim) + i * jointvalues + 0])
            //    {
            //        _min_value_x[0] = signal.data[(index * dim) + i * jointvalues + 0];
            //    }
            //    if (signal.data[(index * dim) + i * jointvalues + 0] != 0.0f && _max_value_x[0] < signal.data[(index * dim) + i * jointvalues + 0])
            //    {
            //        _max_value_x[0] = signal.data[(index * dim) + i * jointvalues + 0];
            //    }
            //    if (signal.data[(index * dim) + i * jointvalues + 1] != 0.0f && _min_value_y[0] > signal.data[(index * dim) + i * jointvalues + 1])
            //    {
            //        _min_value_y[0] = signal.data[(index * dim) + i * jointvalues + 1];
            //    }
            //    if (signal.data[(index * dim) + i * jointvalues + 1] != 0.0f && _max_value_y[0] < signal.data[(index * dim) + i * jointvalues + 1])
            //    {
            //       _max_value_y[0] = signal.data[(index * dim) + i  * jointvalues + 1];
            //    }
            //}

            //if (_min_value_x[0] == 0.0f || _max_value_x[0] == 0.0f || _min_value_x[0] == _max_value_x[0] ||   _min_value_y[0] == 0.0f || _max_value_y[0] == 0.0f || _min_value_y[0] == _max_value_y[0])
            //{
            //    return;
            //}

            //Kinect Stream Resolution
            double width = 640;
            double height = 360;

            for (int s = 0; s < numskeletons; s++)

            {
                for (int i = 0; i < joints; i++)
                {
                    //points[i].X = (int) (signal.data[(index * dim) + i * jointvalues + 0] - _min_value_x[0] / (_max_value_x[0] - _min_value_x[0]) * width + 0.5f) + width/2;
                    //points[i].Y = (int) (( height - signal.data[(index * dim) + i * jointvalues + 1] - _min_value_y[0]) / (_max_value_y[0] - _min_value_y[0]) * height + 0.5f) - height/2;

                    if (index * dim < signal.data.Length)
                    {
                        if (signal.meta_type == "ssi")

                        {
                            points[i].X = (int)(signal.data[s * dim / numskeletons + (index * dim) + i * jointvalues + 0] * 75 / width + width / 2);
                            points[i].Y = (int)(height - signal.data[s * dim / numskeletons + (index * dim) + i * jointvalues + 1] * 75 / height - height / 2);
                        }
                        else if (signal.meta_type == "kinect1")
                        {
                            points[i].X = (int)(signal.data[s * dim / numskeletons + (index * dim) + i * jointvalues + 0] * 75000 / width + width / 2);
                            points[i].Y = (int)(height - signal.data[s * dim / numskeletons + (index * dim) + i * jointvalues + 1] * 75000 / height - height / 2);
                        }
                        else if (signal.meta_type == "kinect2")
                        {
                            points[i].X = (int)(signal.data[s * dim / numskeletons + (index * dim) + i * jointvalues + 0] * 75000 / width + width / 2);
                            points[i].Y = (int)(height - signal.data[s * dim / numskeletons + (index * dim) + i * jointvalues + 1] * 75000 / height - height / 2);
                        }
                    }
                }

                if (signal.meta_type == "ssi")

                {
                    //head
                    writeableBmp.DrawLine((int)points[23].X, (int)points[23].Y, (int)points[21].X, (int)points[21].Y, this.HeadColor);
                    writeableBmp.DrawLine((int)points[23].X, (int)points[23].Y, (int)points[22].X, (int)points[22].Y, colhead);
                    writeableBmp.DrawLine((int)points[21].X, (int)points[21].Y, (int)points[24].X, (int)points[24].Y, colhead);
                    writeableBmp.DrawLine((int)points[22].X, (int)points[22].Y, (int)points[24].X, (int)points[24].Y, colhead);

                    writeableBmp.DrawLine((int)points[0].X, (int)points[0].Y, (int)points[1].X, (int)points[1].Y, getColor(1)); //Head to Neck

                    writeableBmp.DrawLine((int)points[1].X, (int)points[1].Y, (int)points[4].X, (int)points[4].Y, getColor(4)); //Neck to Left  Shoulder
                    writeableBmp.DrawLine((int)points[1].X, (int)points[1].Y, (int)points[8].X, (int)points[8].Y, getColor(8)); //Neck to Right Shoulder

                    writeableBmp.DrawLine((int)points[4].X, (int)points[4].Y, (int)points[5].X, (int)points[5].Y, getColor(5)); // Left Shoulder to Left Elbow
                    writeableBmp.DrawLine((int)points[8].X, (int)points[8].Y, (int)points[9].X, (int)points[9].Y, getColor(9)); //Rigtht Shoulder to Right Elbow

                    writeableBmp.DrawLine((int)points[5].X, (int)points[5].Y, (int)points[6].X, (int)points[6].Y, getColor(6)); // Left Elbow to Left Wrist
                    writeableBmp.DrawLine((int)points[9].X, (int)points[9].Y, (int)points[10].X, (int)points[10].Y, getColor(10)); //Rigth Elbow to Right Wrist

                    writeableBmp.DrawLine((int)points[6].X, (int)points[6].Y, (int)points[7].X, (int)points[7].Y, getColor(7)); // Left Wrist to Left Hand
                    writeableBmp.DrawLine((int)points[10].X, (int)points[10].Y, (int)points[11].X, (int)points[11].Y, getColor(11)); //Rigth Wrist to Right Hand

                    writeableBmp.DrawLine((int)points[1].X, (int)points[1].Y, (int)points[2].X, (int)points[2].Y, getColor(2)); //Neck to Torso
                    writeableBmp.DrawLine((int)points[2].X, (int)points[2].Y, (int)points[3].X, (int)points[3].Y, getColor(3)); //Torso to Hip Center

                    writeableBmp.DrawLine((int)points[3].X, (int)points[3].Y, (int)points[12].X, (int)points[12].Y, getColor(12)); // Hip Center to Left Hip
                    writeableBmp.DrawLine((int)points[3].X, (int)points[3].Y, (int)points[16].X, (int)points[16].Y, getColor(16)); //Hip Center to Right Hip

                    writeableBmp.DrawLine((int)points[12].X, (int)points[12].Y, (int)points[13].X, (int)points[13].Y, getColor(13)); // Left Hip to Knee
                    writeableBmp.DrawLine((int)points[16].X, (int)points[16].Y, (int)points[17].X, (int)points[17].Y, getColor(17)); // Right Hip to Knee

                    writeableBmp.DrawLine((int)points[13].X, (int)points[13].Y, (int)points[14].X, (int)points[14].Y, getColor(14)); // Left Knee to Ankle
                    writeableBmp.DrawLine((int)points[17].X, (int)points[17].Y, (int)points[18].X, (int)points[18].Y, getColor(18)); // Right Knee to Ankle

                    writeableBmp.DrawLine((int)points[14].X, (int)points[14].Y, (int)points[15].X, (int)points[15].Y, getColor(15)); // Left Ankle to foot
                    writeableBmp.DrawLine((int)points[18].X, (int)points[18].Y, (int)points[19].X, (int)points[19].Y, getColor(19)); // Right Ankle to foot
                }
                else if (signal.meta_type == "kinect1")

                {
                    writeableBmp.DrawLine((int)points[3].X, (int)points[3].Y, (int)points[2].X, (int)points[2].Y, getColor(2)); //Head to Neck
                    writeableBmp.DrawLine((int)points[2].X, (int)points[2].Y, (int)points[4].X, (int)points[4].Y, getColor(4)); //Neck to LeftShoulder
                    writeableBmp.DrawLine((int)points[2].X, (int)points[2].Y, (int)points[8].X, (int)points[8].Y, getColor(8)); //Neck to RightShoulder
                    writeableBmp.DrawLine((int)points[2].X, (int)points[2].Y, (int)points[1].X, (int)points[1].Y, getColor(1)); //Neck to Spine
                    writeableBmp.DrawLine((int)points[1].X, (int)points[1].Y, (int)points[0].X, (int)points[0].Y, getColor(0)); //Spine to HipCenter

                    writeableBmp.DrawLine((int)points[0].X, (int)points[0].Y, (int)points[12].X, (int)points[12].Y, getColor(12)); //HipCenter to HipLeft
                    writeableBmp.DrawLine((int)points[0].X, (int)points[0].Y, (int)points[16].X, (int)points[16].Y, getColor(16)); //HipCenter to HipRight

                    writeableBmp.DrawLine((int)points[4].X, (int)points[4].Y, (int)points[5].X, (int)points[5].Y, getColor(5)); //LeftShoulder to LeftElbow
                    writeableBmp.DrawLine((int)points[5].X, (int)points[5].Y, (int)points[6].X, (int)points[6].Y, getColor(6)); //LeftElbow to LeftWrist
                    writeableBmp.DrawLine((int)points[6].X, (int)points[6].Y, (int)points[7].X, (int)points[7].Y, getColor(7)); //LeftWrist to LeftHand

                    writeableBmp.DrawLine((int)points[8].X, (int)points[8].Y, (int)points[9].X, (int)points[9].Y, getColor(9)); //RightShoulder to RightElbow
                    writeableBmp.DrawLine((int)points[9].X, (int)points[9].Y, (int)points[10].X, (int)points[10].Y, getColor(10)); //RightElbow to RightWrist
                    writeableBmp.DrawLine((int)points[10].X, (int)points[10].Y, (int)points[11].X, (int)points[11].Y, getColor(11)); //RightWrist to RightHand

                    writeableBmp.DrawLine((int)points[12].X, (int)points[12].Y, (int)points[13].X, (int)points[13].Y, getColor(13)); //LeftHip to LeftKnee
                    writeableBmp.DrawLine((int)points[13].X, (int)points[13].Y, (int)points[14].X, (int)points[14].Y, getColor(14)); //LeftKnee to LeftAnkle
                    writeableBmp.DrawLine((int)points[14].X, (int)points[14].Y, (int)points[15].X, (int)points[15].Y, getColor(15)); //LeftAnkle to LeftFoot

                    writeableBmp.DrawLine((int)points[16].X, (int)points[16].Y, (int)points[17].X, (int)points[17].Y, getColor(17)); //RightHip to RightKnee
                    writeableBmp.DrawLine((int)points[17].X, (int)points[17].Y, (int)points[18].X, (int)points[18].Y, getColor(18)); //RightKnee to RightAnkle
                    writeableBmp.DrawLine((int)points[18].X, (int)points[18].Y, (int)points[19].X, (int)points[19].Y, getColor(19)); //RightAnkle to RightFoot
                }
                else if (signal.meta_type == "kinect2")

                {
                    writeableBmp.DrawLine((int)points[3].X, (int)points[3].Y, (int)points[2].X, (int)points[2].Y, getColor(2)); //Head to Neck
                    writeableBmp.DrawLine((int)points[2].X, (int)points[2].Y, (int)points[20].X, (int)points[20].Y, getColor(20)); //Neck to Shoulder Spine

                    writeableBmp.DrawLine((int)points[20].X, (int)points[20].Y, (int)points[4].X, (int)points[4].Y, getColor(4)); //Neck to Left  Shoulder
                    writeableBmp.DrawLine((int)points[20].X, (int)points[20].Y, (int)points[8].X, (int)points[8].Y, getColor(8)); //Neck to Right Shoulder

                    writeableBmp.DrawLine((int)points[4].X, (int)points[4].Y, (int)points[5].X, (int)points[5].Y, getColor(5)); // Left Shoulder to Left Elbow
                    writeableBmp.DrawLine((int)points[8].X, (int)points[8].Y, (int)points[9].X, (int)points[9].Y, getColor(9)); //Rigtht Shoulder to Right Elbow

                    writeableBmp.DrawLine((int)points[5].X, (int)points[5].Y, (int)points[6].X, (int)points[6].Y, getColor(6)); // Left Elbow to Left Wrist
                    writeableBmp.DrawLine((int)points[9].X, (int)points[9].Y, (int)points[10].X, (int)points[10].Y, getColor(10)); //Rigth Elbow to Right Wrist

                    writeableBmp.DrawLine((int)points[6].X, (int)points[6].Y, (int)points[7].X, (int)points[7].Y, getColor(7)); // Left Wrist to Left Hand
                    writeableBmp.DrawLine((int)points[10].X, (int)points[10].Y, (int)points[11].X, (int)points[11].Y, getColor(11)); //Rigth Wrist to Right Hand

                    writeableBmp.DrawLine((int)points[7].X, (int)points[7].Y, (int)points[21].X, (int)points[21].Y, getColor(21)); // Left Hand to Left Handtip
                    writeableBmp.DrawLine((int)points[11].X, (int)points[11].Y, (int)points[23].X, (int)points[23].Y, getColor(23)); //Rigth Hand to Right Handtip

                    writeableBmp.DrawLine((int)points[7].X, (int)points[7].Y, (int)points[22].X, (int)points[22].Y, getColor(22)); // Left Hand to Left thumb
                    writeableBmp.DrawLine((int)points[11].X, (int)points[11].Y, (int)points[24].X, (int)points[24].Y, getColor(24)); //Rigth Hand to Right thumb

                    writeableBmp.DrawLine((int)points[20].X, (int)points[20].Y, (int)points[1].X, (int)points[1].Y, getColor(1)); //Shoulder Spine to Spine
                    writeableBmp.DrawLine((int)points[1].X, (int)points[1].Y, (int)points[0].X, (int)points[0].Y, getColor(0)); //Torso to Hip Center

                    writeableBmp.DrawLine((int)points[0].X, (int)points[0].Y, (int)points[12].X, (int)points[12].Y, getColor(12)); // Hip Center to Left Hip
                    writeableBmp.DrawLine((int)points[0].X, (int)points[0].Y, (int)points[16].X, (int)points[16].Y, getColor(16)); //Hip Center to Right Hip

                    writeableBmp.DrawLine((int)points[12].X, (int)points[12].Y, (int)points[13].X, (int)points[13].Y, getColor(13)); // Left Hip to Knee
                    writeableBmp.DrawLine((int)points[16].X, (int)points[16].Y, (int)points[17].X, (int)points[17].Y, getColor(17)); // Right Hip to Knee

                    writeableBmp.DrawLine((int)points[13].X, (int)points[13].Y, (int)points[14].X, (int)points[14].Y, getColor(14)); // Left Knee to Ankle
                    writeableBmp.DrawLine((int)points[17].X, (int)points[17].Y, (int)points[18].X, (int)points[18].Y, getColor(18)); // Right Knee to Ankle

                    writeableBmp.DrawLine((int)points[14].X, (int)points[14].Y, (int)points[15].X, (int)points[15].Y, getColor(15)); // Left Ankle to foot
                    writeableBmp.DrawLine((int)points[18].X, (int)points[18].Y, (int)points[19].X, (int)points[19].Y, getColor(19)); // Right Ankle to foot
                }

                //for (int i = 0; i < numskeletons; i++)
                //{
                //    _min_value_x[i] = _min_value_y[i] = Double.MaxValue;
                //    _max_value_x[i] = _max_value_y[i] = -Double.MaxValue;
            }//}

            writeableBmp.Unlock();
        }

        private Color getColor(int i)
        {
            Color c = this.SignalColor ;
            double pos = ViewHandler.Time.CurrentPlayPosition;
            int index = (int)(pos * sr);

        if(signal.data.Length >= (index * dim) + i * jointvalues + 3)

            {

            if ((signal.data[(index * dim) + i * jointvalues + 3]) < 0.5)
            {
                c = this.SignalColor;
                c.A = 128;
            }
            else if ((signal.data[(index * dim) + i * jointvalues + 3]) < 1.0)
            {
                c = this.SignalColor;
                c.A = 0;
            }
            else
            {
                c = this.SignalColor;
            }
            }
            return c;
        }
    }
}
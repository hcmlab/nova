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
    public class Skeleton : Image, IMedia, INotifyPropertyChanged
    {
        public enum SkeletonType
        {
            NONE,
            SSI,
            KINECT1,
            KINECT2,
        }

        private string filepath;
        private Signal signal;
        private MediaType type;

        private Color signalColor;
        private Color headColor;
        private Color backColor;

        private int width;
        private int height;

        private int numJoints = 0;
        private int jointValues = 0;
        private int numSkeletons = 1;
        private double postion = 0;
        private SkeletonType skelType;

        private WriteableBitmap writeableBmp;
        private DispatcherTimer timer;
        private List<Point3D> joints = new List<Point3D>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            handler?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Skeleton(string filepath, Signal signal, int width = 640, int height = 480)
        {
            this.filepath = filepath;
            this.signal = signal;
            this.width = width;
            this.height = height;

            type = MediaType.SKELETON;
            skelType = SkeletonType.NONE;

            BackColor = Defaults.Colors.Background;
            SignalColor = Defaults.Colors.Foreground;
            HeadColor = Colors.YellowGreen; // Defaults.Colors.Foreground;

            numSkeletons = 0;
            if (signal.Meta.ContainsKey("num"))
            {
                int.TryParse(signal.Meta["num"], out numSkeletons);
            }

            numJoints = 0;
            if (signal.Meta.ContainsKey("type"))
            {
                if (signal.Meta["type"] == "ssi")
                {
                    skelType = SkeletonType.SSI;
                    numJoints = 25;
                }
                else if (signal.Meta["type"] == "kinect1")
                {
                    skelType = SkeletonType.KINECT1;
                    numJoints = 20;
                }
                else if (signal.Meta["type"] == "kinect2")
                {
                    skelType = SkeletonType.KINECT2;
                    numJoints = 25;
                }
            }

            jointValues = 0;
            if (numSkeletons > 0 && numJoints > 0)
            {
                jointValues = (int)((signal.dim / numSkeletons) / numJoints);
            }

            writeableBmp = new WriteableBitmap(width, height, 96.0, 96.0, PixelFormats.Bgr32, null);
            writeableBmp.Clear(BackColor);

            Source = writeableBmp;

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000.0 / signal.rate);
            timer.Tick += new EventHandler(Draw);
        }

        public void Draw(object myObject, EventArgs myEventArgs)
        {
            Draw(MainHandler.Time.CurrentPlayPosition);
        }

        public void Draw(double time)
        {
            if (skelType == SkeletonType.NONE || jointValues == 0 || numJoints == 0 || numSkeletons == 0)
            {
                return;
            }

            postion = time;
            int index = (int)(time * signal.rate);

            Color col = SignalColor;
            Color colhead = HeadColor;

            writeableBmp.Lock();
            writeableBmp.Clear(BackColor);

            if (index < signal.number)
            {
                Point[] points = new Point[numJoints];

                //Kinect Stream Resolution
                uint dim = signal.dim;

                for (int s = 0; s < numSkeletons; s++)
                {
                    for (int i = 0; i < numJoints; i++)
                    {
                        if (index * dim < signal.data.Length)
                        {
                            switch (skelType)
                            {
                                case SkeletonType.SSI:
                                    {
                                        points[i].X = (int)(signal.data[s * dim / numSkeletons + (index * dim) + i * jointValues + 0] * 75 / width + width / 2);
                                        points[i].Y = (int)(height - signal.data[s * dim / numSkeletons + (index * dim) + i * jointValues + 1] * 75 / height - height / 2);
                                        break;
                                    }
                                case SkeletonType.KINECT1:
                                    {
                                        points[i].X = (int)(signal.data[s * dim / numSkeletons + (index * dim) + i * jointValues + 0] * 75000 / width + width / 2);
                                        points[i].Y = (int)(height - signal.data[s * dim / numSkeletons + (index * dim) + i * jointValues + 1] * 75000 / height - height / 2);
                                        break;
                                    }
                                case SkeletonType.KINECT2:
                                    {
                                        points[i].X = (int)(signal.data[s * dim / numSkeletons + (index * dim) + i * jointValues + 0] * 75000 / width + width / 2);
                                        points[i].Y = (int)(height - signal.data[s * dim / numSkeletons + (index * dim) + i * jointValues + 1] * 75000 / height - height / 2);
                                        break;
                                    }
                            }
                        }
                    }

                    switch (skelType)
                    {
                        case SkeletonType.SSI:
                            {

                                writeableBmp.DrawLine((int)points[3].X, (int)points[3].Y, (int)points[12].X, (int)points[12].Y, getColor(12)); // Hip Center to Left Hip
                                writeableBmp.DrawLine((int)points[3].X, (int)points[3].Y, (int)points[16].X, (int)points[16].Y, getColor(16)); //Hip Center to Right Hip

                                writeableBmp.DrawLine((int)points[12].X, (int)points[12].Y, (int)points[13].X, (int)points[13].Y, getColor(13)); // Left Hip to Knee
                                writeableBmp.DrawLine((int)points[16].X, (int)points[16].Y, (int)points[17].X, (int)points[17].Y, getColor(17)); // Right Hip to Knee

                                writeableBmp.DrawLine((int)points[13].X, (int)points[13].Y, (int)points[14].X, (int)points[14].Y, getColor(14)); // Left Knee to Ankle
                                writeableBmp.DrawLine((int)points[17].X, (int)points[17].Y, (int)points[18].X, (int)points[18].Y, getColor(18)); // Right Knee to Ankle

                                writeableBmp.DrawLine((int)points[14].X, (int)points[14].Y, (int)points[15].X, (int)points[15].Y, getColor(15)); // Left Ankle to foot
                                writeableBmp.DrawLine((int)points[18].X, (int)points[18].Y, (int)points[19].X, (int)points[19].Y, getColor(19)); // Right Ankle to foot




                                //head
                                writeableBmp.DrawLine((int)points[23].X, (int)points[23].Y, (int)points[21].X, (int)points[21].Y, getColor(21, true));
                                writeableBmp.DrawLine((int)points[21].X, (int)points[21].Y, (int)points[24].X, (int)points[24].Y, getColor(24, true));
                                writeableBmp.DrawLine((int)points[24].X, (int)points[24].Y, (int)points[22].X, (int)points[22].Y, getColor(22, true));
                                writeableBmp.DrawLine((int)points[22].X, (int)points[22].Y, (int)points[23].X, (int)points[23].Y, getColor(23, true));

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

                              
                                break;
                            }
                        case SkeletonType.KINECT1:
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
                                break;
                            }
                        case SkeletonType.KINECT2:
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

                                break;
                            }
                    }
                    }
                }

                writeableBmp.Unlock();
            }

        private Color getColor(int i, bool isheadcolor = false)
        {
            Color c = isheadcolor ? HeadColor : SignalColor;
            double pos = MainHandler.Time.CurrentPlayPosition;
            int index = (int)(pos * signal.rate);

            if (signal.data.Length >= (index * signal.dim) + i * jointValues + 3)

            {
                if ((signal.data[(index * signal.dim) + i * jointValues + 3]) < 0.1 || (signal.data[(index * signal.dim) + i * jointValues] == 0))
                {
                    c = Colors.White;
                   // c.A = 0;
                }

                else if ((signal.data[(index * signal.dim) + i * jointValues + 3]) < 0.5)
                {
 
                    c.A = 128;
                }
                else if ((signal.data[(index * signal.dim) + i * jointValues + 3]) < 1.0)
                {
 
                    c.A = 255;
                }
                //else
                //{
                //    c = SignalColor;
                //}
            }
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
            return signal.number / signal.rate;
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
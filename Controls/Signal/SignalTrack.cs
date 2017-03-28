using System;
using System.ComponentModel;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace ssi
{
    public delegate void SignalTrackChangeEventHandler(SignalTrack track, EventArgs e);

    public partial class SignalTrackStatic : Canvas
    {
        static public SignalTrack Selected = null;

        static public event SignalTrackChangeEventHandler OnTrackChange;

        static public void Select(SignalTrack t)
        {
            Unselect();
            Selected = t;

            if (Selected.Border != null)
            {
                Selected.Border.BorderBrush = Defaults.Brushes.Highlight;
            }

            OnTrackChange?.Invoke(Selected, null);
        }

        static public void Unselect()
        {
            if (Selected != null)
            {
                Selected.Border.BorderBrush = Defaults.Brushes.Conceal;
                Selected = null;
            }
        }
    }

    public class SignalTrack : SignalTrackStatic, INotifyPropertyChanged
    {
        private double signal_from_in_sec = 0;
        private double signal_to_in_sec = 0;
        private Signal resampled = null;
        private double render_width;
        private bool resample = false;
        private bool autoScaling = false;
        private int zoomLevel = 0;
        private int zoomOffset = 0;
        public int brushOffset = 0;
        private Pen pen;
       
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        public Border Border { get; set; }

        private Color signalColor;
        public Color SignalColor
        {
            get
            {
                return signalColor;
            }
            set
            {
                signalColor = value;
                pen = new Pen(new SolidColorBrush(signalColor), 1.0);
                pen.Freeze();
                RaisePropertyChanged("SignalColor");
                InvalidateVisual();
            }
        }

        private Color backgroundColor;
        public Color BackgroundColor
        {
            get
            {
                return backgroundColor;
            }
            set
            {
                backgroundColor = value;
                Background = new SolidColorBrush(backgroundColor);
                RaisePropertyChanged("BackgroundColor");
                InvalidateVisual();
            }
        }

        public bool AutoScaling
        {
            get { return autoScaling; }
            set
            {
                autoScaling = value;               
                RaisePropertyChanged("AutoScaling");
                InvalidateVisual();
            }
        }

        public SignalTrack()
        {            
            signalColor = Defaults.Colors.Foreground;
            backgroundColor = Defaults.Colors.Background;
            pen = new Pen(new SolidColorBrush(signalColor), 1.0);
        }

        public SignalTrack(Signal signal) : this()
        {           
            pen.Freeze();

            Signal = signal;
            MouseWheel += new MouseWheelEventHandler(OnSignalTrackMouseWheel);
        }

        public Signal Signal { get; set; }

        protected void Paint(DrawingContext dc, SignalTrack track, Signal signal, int dimension, double height, uint width, bool isAudio)
        {
            if (signal.number == 0 || signal.data == null || height == 0 || width == 0)
            {
                return;
            }

            if (isAudio)
            {
                for (int d = dimension; d <= dimension; d++)
                {
                    float zoomFactor = track.zoomLevel * ((signal.max[d] - signal.min[d]) / 10);
                    float zoomOffset = track.zoomOffset * ((signal.max[d] - signal.min[d]) / 10);
                    float minVal = signal.min[d] + zoomFactor;
                    float maxVal = signal.max[d] - zoomFactor;
                    float offset = minVal;
                    float scale = (float)height / (maxVal - offset);
                    offset += zoomOffset;

                    Point from = new Point();
                    Point to = new Point();

                    if (width == signal.number)
                    {
                        for (int i = 0; i < signal.number; i++)
                        {
                            from.X = to.X = i;
                            from.Y = height - (signal.data[i * signal.dim + d] - offset) * scale;
                            to.Y = height - (-signal.data[i * signal.dim + d] - offset) * scale;
                            if (to.Y > height) to.Y = height;
                            else if (to.Y < 0) to.Y = 0;
                            dc.DrawLine(pen, from, to);
                        }
                    }
                    else
                    {
                        float step = (float)width / (float)signal.number;
                        for (int i = 0; i < signal.number; i++)
                        {
                            from.X = to.X = (uint)i * step;
                            from.Y = height - (signal.data[i * signal.dim + d] - offset) * scale;
                            to.Y = height - (-signal.data[i * signal.dim + d] - offset) * scale;
                            if (to.Y > height) to.Y = height;
                            else if (to.Y < 0) to.Y = 0;
                            dc.DrawLine(pen, from, to);
                        }
                    }
                }
            }
            else
            {
                for (int d = dimension; d <= dimension; d++)
                {
                    float zoomFactor = track.zoomLevel * ((signal.max[d] - signal.min[d]) / 10);
                    float zoomOffset = track.zoomOffset * ((signal.max[d] - signal.min[d]) / 10);
                    float minVal = signal.min[d] + zoomFactor;
                    float maxVal = signal.max[d] - zoomFactor;
                    float offset = minVal;
                    float scale = (float)height / (maxVal - offset);
                    offset += zoomOffset;
                    Point from = new Point(0, height - (signal.data[d] - offset) * scale);
                    Point to = new Point();
                    if (width == signal.number)
                    {
                        for (int i = 0; i < signal.number; i++)
                        {
                            to.X = i;
                            to.Y = height - (signal.data[i * signal.dim + d] - offset) * scale;
                            if (to.Y > height) to.Y = height;
                            else if (to.Y < 0) to.Y = 0;
                            dc.DrawLine(pen, from, to);
                            from.X = to.X;
                            from.Y = to.Y;
                        }
                    }
                    else
                    {
                        float step = (float)width / (float)signal.number;
                        for (int i = 0; i < signal.number; i++)
                        {
                            to.X = (uint)i * step;
                            to.Y = height - (signal.data[i * signal.dim + d] - offset) * scale;
                            if (to.Y > height) to.Y = height;
                            else if (to.Y < 0) to.Y = 0;
                            dc.DrawLine(pen, from, to);
                            from.X = to.X;
                            from.Y = to.Y;
                        }
                    }
                }
            }
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Signal != null && Signal.loaded && render_width > 0)
            {
                double height = this.ActualHeight;
                uint len = (uint)(this.render_width + 0.5);
                if (resampled == null || resampled.number != len || resample)
                {
                    resampled = new Signal(this.Signal, len, signal_from_in_sec, signal_to_in_sec);
                    if (!autoScaling)
                    {
                        resampled.min = Signal.min;
                        resampled.max = Signal.max;
                    }
                    resample = false;
                }

                Paint(dc, this, resampled, Signal.ShowDim, ActualHeight, len, Signal.IsAudio);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // change track
            if (Selected != this)
            {
                SignalTrackStatic.Select(this);
            }
        }

        public void TimeRangeChanged(Timeline time)
        {
            double pixel = time.SelectionInPixel;
            this.signal_from_in_sec = time.SelectionStart;
            this.signal_to_in_sec = time.SelectionStop;
            this.render_width = pixel;
            this.Width = pixel;
            this.resample = true;
            this.InvalidateVisual();
        }


        private void OnSignalTrackMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
            }
            else
            {
            }
        }

        #region EXPORT

        public void ExportToXPS(Uri path, Canvas surface)
        {
            if (path == null) return;

            Transform transform = surface.LayoutTransform;
            surface.LayoutTransform = null;

            Size size = new Size(surface.ActualWidth, surface.ActualHeight);
            surface.Measure(size);
            surface.Arrange(new Rect(size));

            Package package = Package.Open(path.LocalPath, FileMode.Create);
            XpsDocument doc = new XpsDocument(package);
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
            writer.Write(surface);
            doc.Close();
            package.Close();
            surface.LayoutTransform = transform;
        }

        public void ExportToPng(Uri path, Canvas surface)
        {
            if (path == null) return;
            Transform transform = surface.LayoutTransform;
            surface.LayoutTransform = null;

            Size size = new Size(surface.ActualWidth, surface.ActualHeight);
            surface.Measure(size);
            surface.Arrange(new Rect(size));
            RenderTargetBitmap renderBitmap =
              new RenderTargetBitmap(
                (int)size.Width,
                (int)size.Height,
                96d,
                96d,
                PixelFormats.Pbgra32);
            renderBitmap.Render(surface);

            using (FileStream outStream = new FileStream(path.LocalPath, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderBitmap));
                encoder.Save(outStream);
            }

            surface.LayoutTransform = transform;
        }

        #endregion EXPORT
    }
}



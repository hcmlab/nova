using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Windows.Xps;
using System.Windows.Xps.Packaging;

namespace ssi
{
    public class AnnoTrackPlayEventArgs : EventArgs
    {
        public AnnoListItem item = null;
    }

    public class AnnoTrackMoveEventArgs : EventArgs
    {
        public double pos = 0;
    }

    public delegate void AnnoTrackChangeEventHandler(AnnoTrack track, EventArgs e);

    public delegate void AnnoTrackSegmentChangeEventHandler(AnnoTrackSegment segment, EventArgs e);

    public delegate void AnnoTrackResizeEventHandler(double pos);

    public partial class LabelColorPair
    {
        private string label;
        private string color;

        public String Label
        {
            get { return label; }
            set { label = value; }
        }

        public String Color
        {
            get { return color; }
            set { color = value; }
        }

        public LabelColorPair(string _label, string _color)
        {
            this.label = _label;
            this.color = _color;
        }
    }

    public partial class AnnoTrackStatic : Canvas
    {
        static protected AnnoTrack selected_track = null;
        static protected AnnoTrackSegment selected_segment = null;
        static protected int selected_zindex = 0;
        static protected int selected_zindex_max = 0;
        static public HashSet<LabelColorPair> used_labels = new HashSet<LabelColorPair>();
        static public int used_labels_last_index;
        static public double mouseDownPos;
        static public int closestindex = -1;
        static public int closestindexold = 0;
        public static bool continuousannomode = false;
        public static bool askforlabel = false;
        public static string Defaultlabel = "";
        public static string DefaultColor = "#000000";

        static public event AnnoTrackChangeEventHandler OnTrackChange;

        static public event AnnoTrackSegmentChangeEventHandler OnTrackSegmentChange;

        static public event AnnoTrackResizeEventHandler OnTrackResize;

        static public DispatcherTimer _timer = new DispatcherTimer();

        static public AnnoTrackSegment GetSelectedSegment()
        {
            return selected_segment;
        }

        static public AnnoTrack GetSelectedTrack()
        {
            return selected_track;
        }

        static public void SelectTrack(AnnoTrack t)
        {
            UnselectTrack();
            selected_track = t;
            t.select(true);

            if (OnTrackChange != null)
            {
                OnTrackChange(selected_track, null);
            }
        }

        static public void UnselectTrack()
        {
            if (selected_track != null)
            {
                selected_track.select(false);
                selected_track = null;
            }
        }

        static public void SelectSegment(AnnoTrackSegment s)
        {
            UnselectSegment();
            if (s != null)
            {
                s.select(true);
                selected_segment = s;
                selected_zindex = Panel.GetZIndex(selected_segment);
                Panel.SetZIndex(selected_segment, selected_zindex_max + 1);

                if (OnTrackSegmentChange != null)
                {
                    OnTrackSegmentChange(s, null);
                }
            }
        }

        static public void UnselectSegment()
        {
            if (selected_segment != null)
            {
                selected_segment.select(false);
                Panel.SetZIndex(selected_segment, selected_zindex);
                selected_segment = null;
            }
        }

        static public void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.Delete) || e.KeyboardDevice.IsKeyDown(Key.Back))
            {
                if (selected_segment != null && selected_track != null && GetSelectedTrack().isDiscrete)
                {
                    AnnoTrackSegment tmp = selected_segment;
                    UnselectSegment();
                    selected_track.remSegment(tmp);
                }
            }
        }

        static public void FireOnMove(double pos)
        {
            if (OnTrackResize != null)
            {
                OnTrackResize(pos);
            }
        }
    }

    public class AnnoTrack : AnnoTrackStatic, ITrack
    {
        private List<AnnoTrackSegment> segments = new List<AnnoTrackSegment>();
        private List<Line> lines = new List<Line>();
        private List<Line> markers = new List<Line>();
        private AnnoList anno_list = null;
        private Brush bgBrush;
        private Brush ctBrush;
        private Ellipse el = new Ellipse();
        private double currentPositionX = 0;
        public bool isDiscrete = true;
        public double samplerate = 1.0;
        public string TierId;
        private double borderlow = 0.0;
        private double borderhigh = 1.0;
        public HashSet<LabelColorPair> track_used_labels;

        private double lastX;
        private int direction;
        private bool annorightdirection = true;

        public AnnoList AnnoList
        {
            get { return anno_list; }
            set { anno_list = value; }
        }

        public Brush BackgroundColor
        {
            get { return bgBrush; }
            set { bgBrush = value; }
        }

        public Brush ContiniousBrush
        {
            get { return ctBrush; }
            set { ctBrush = value; }
        }

        private bool is_selected = false;

        public AnnoTrack(AnnoList list, int discrete, double sr = 1.0, string tierid = "default", double borderl = 0.0, double borderh = 1.0)
        {
            this.AllowDrop = true;
            this.anno_list = list;
            this.SizeChanged += new SizeChangedEventHandler(sizeChanged);
         
            if (discrete == 0 || discrete == 1) this.isDiscrete = true;
            else this.isDiscrete = false;
            this.samplerate = sr;
            this.TierId = tierid;
            this.borderlow = borderl;
            this.borderhigh = borderh;

            double median = (borderlow + borderhigh) / 2;
            double range = borderhigh - borderlow;

            AnnoTrack.SelectTrack(this);

            if (!isDiscrete)
            {
                Loaded += delegate
                {
                    initContinousValues(sr);
                    _timer.Interval = TimeSpan.FromMilliseconds(20);
                    _timer.Tick += new EventHandler(delegate (object s, EventArgs a)
                  {
                      if (continuousannomode && this.is_selected)
                      {
                          double closestposition = ViewHandler.Time.CurrentPlayPosition;
                          closestindex = getClosestContinousIndex(closestposition);
                          if (closestindex > -1)
                          {
                              if (this == Mouse.DirectlyOver || (Mouse.GetPosition(this).Y > 0 && Mouse.GetPosition(this).Y < this.ActualHeight && el == Mouse.DirectlyOver))
                              {
                                  double normal = 1.0 - (Mouse.GetPosition(this).Y / this.ActualHeight);
                                  double normalized = (normal * range) + borderlow;

                                  el.Height = this.ActualHeight / 10;
                                  el.Width = el.Height;
                                  el.SetValue(Canvas.TopProperty, (Mouse.GetPosition(this).Y - el.Height / 2));
                                  anno_list[closestindex].Label = (normalized).ToString();

                                  for (int i = closestindexold; i < closestindex; i++)
                                  {
                                      if (closestindexold > -1) anno_list[i].Label = (normalized).ToString();
                                  }
                                  closestindexold = closestindex;

                                  timeRangeChanged(ViewHandler.Time);
                              }
                          }
                      }
                      else el.Visibility = Visibility.Hidden;
                  });
                };
            }
            selected_track = this;
            foreach (AnnoListItem item in list)
            {
                //For now the TierId is overwritten based on the track id.
                item.Tier = TierId;

                LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                used_labels.Add(l);

                if (isDiscrete)
                {
                    addSegment(item);
                }
            }
        }

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

            // Restore previously saved layout
            surface.LayoutTransform = transform;
        }

        private void sizeChanged(object sender, SizeChangedEventArgs e)
        {
          
            if (AnnoList.AnnotationType == 0 || AnnoList.AnnotationType == 1)
            {
                this.Visibility = Visibility.Hidden;
                foreach (AnnoTrackSegment segment in segments)
                {
                    segment.Height = e.NewSize.Height;
                }
                this.Visibility = Visibility.Visible;
            }
            else
            {   //it has to be called twice, otherwise there are some weird effects.
                timeRangeChanged(ViewHandler.Time);
                timeRangeChanged(ViewHandler.Time);
            }
        }

        public void select(bool flag)
        {
            this.is_selected = flag;
            if (flag)
            {
                if (BackgroundColor != null)
                {
                    if (this.isDiscrete)
                    {
                        byte newAlpha = 100;
                        Color newColor = Color.FromArgb(newAlpha, ((SolidColorBrush)BackgroundColor).Color.R, ((SolidColorBrush)BackgroundColor).Color.G, ((SolidColorBrush)BackgroundColor).Color.B);
                        Brush brush = new SolidColorBrush(newColor);
                        this.Background = brush;
                    }
                    else if (ctBrush != null && !isDiscrete)
                    {
                        LinearGradientBrush myBrush = new LinearGradientBrush();
                        myBrush.StartPoint = new Point(0, 0);
                        myBrush.EndPoint = new Point(0, 1);
                        myBrush.GradientStops.Add(new GradientStop(((LinearGradientBrush)ctBrush).GradientStops[0].Color, 0));
                        myBrush.GradientStops.Add(new GradientStop(((LinearGradientBrush)ctBrush).GradientStops[1].Color, 1));
                        myBrush.Opacity = 0.6;
                        this.Background = myBrush;
                    }
                }
            }
            else
            {
                if (BackgroundColor != null && isDiscrete)
                {
                    this.Background = BackgroundColor;
                }
                else if (!isDiscrete)
                {
                    if (ctBrush == null)
                    {
                        LinearGradientBrush myBrush = new LinearGradientBrush();
                        myBrush.StartPoint = new Point(0, 0);
                        myBrush.EndPoint = new Point(0, 1);
                        myBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0));
                        myBrush.GradientStops.Add(new GradientStop(Colors.Red, 1));
                        myBrush.Opacity = 0.75;
                        ctBrush = myBrush;
                    }

                    this.Background = ctBrush;
                }
            }
        }

        public AnnoTrackSegment getSegment(AnnoListItem item)
        {
            foreach (AnnoTrackSegment segment in segments)
            {
                if (segment.Item == item)
                {
                    return segment;
                }
            }
            return null;
        }

        public AnnoTrackSegment addSegment(AnnoListItem item)
        {
            AnnoTrackSegment segment = new AnnoTrackSegment(item, this);
            segments.Add(segment);
            this.Children.Add(segment);
            selected_zindex_max = Math.Max(selected_zindex_max, Panel.GetZIndex(segment));

            return segment;
        }

        public void remSegment(AnnoTrackSegment s)
        {
            anno_list.Remove(s.Item);
            s.Track.Children.Remove(s);
            s.Track.segments.Remove(s);
        }

        public void initContinousValues(double sr)
        {
            //add markers
            for (int i = 0; i < 4; i++)
            {
                Line marker = new Line();
                marker.StrokeThickness = 1;
                marker.StrokeDashArray = new DoubleCollection() { 1.0, 7.0 };
                marker.Stroke = Brushes.DarkGray;
                marker.Y1 = (this.ActualHeight / 4) * i;
                marker.Y2 = marker.Y1;
                marker.X1 = 0;
                marker.X2 = this.ActualWidth;
                markers.Add(marker);
                this.Children.Add(markers[i]);
            }

            double median = (borderlow + borderhigh) / 2.0;

            //add lines

            int samples = (int)(ViewHandler.Time.TotalDuration * (1000 / (sr * 1000)) + 0.5);
            if (anno_list.Count < samples)
            {
                for (int i = anno_list.Count; i < samples; i++)
                {
                    if (i == 0)
                    {
                        AnnoListItem ali = new AnnoListItem(i * sr, sr, median.ToString("F4"), "Range: " + borderlow + "-" + borderhigh, TierId);
                        anno_list.Add(ali);
                    }
                    else
                    {
                        AnnoListItem ali = new AnnoListItem(i * sr, sr, median.ToString("F4"), "", TierId);
                        anno_list.Add(ali);
                    }
                }
            }

            int drawlinesnumber;
            if (this.ActualWidth == 0) drawlinesnumber = 1000;
            else if (anno_list.Count > this.ActualWidth) drawlinesnumber = (int)this.ActualWidth;
            else drawlinesnumber = anno_list.Count;

            for (int i = 0; i < drawlinesnumber; i++)
            {
                Line line = new Line();
                line.StrokeThickness = 1;
                line.Stroke = Brushes.Black;
                line.Y1 = median;
                line.Y2 = line.Y1;
                line.X1 = ViewHandler.Time.PixelFromTime(ViewHandler.Time.TotalDuration / 1000 * i);
                line.X2 = ViewHandler.Time.PixelFromTime(ViewHandler.Time.TotalDuration / 1000 * i + ViewHandler.Time.TotalDuration / 1000 * (i + 1));
                lines.Add(line);
                selected_zindex_max = Math.Max(selected_zindex_max, Panel.GetZIndex(line));
                this.Children.Add(line);
            }

            el.Width = this.ActualHeight / 10;
            el.Height = el.Width;
            el.Fill = Brushes.WhiteSmoke;
            el.Stroke = Brushes.Black;
            el.Visibility = Visibility.Hidden;
            el.SetValue(Canvas.LeftProperty, 0.0);
            this.Children.Add(el);

            if (isDiscrete) timeRangeChanged(ViewHandler.Time);
            if (!isDiscrete)
            {
                timeRangeChanged(ViewHandler.Time);
                timeRangeChanged(ViewHandler.Time);
            }
        }

        public void ContAnnoMode()
        {
            if (!continuousannomode)
            {
                _timer.Start();
                continuousannomode = true;

                closestindex = -1;
                closestindexold = closestindex;
            }
            else
            {
                _timer.Stop();
                continuousannomode = false;
            }
        }

        public void newAnnokey()
        {
            if (isDiscrete)
            {
                double start = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.CurrentSelectPosition);
                double stop = ViewHandler.Time.CurrentPlayPosition;

                if (stop < start)
                {
                    double temp = start;
                    start = stop;
                    stop = temp;
                }
                //  double stop = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X + AnnoTrackSegment.RESIZE_OFFSET);
                double len = stop - start;

                double closestposition = start;
                closestindex = getClosestContinousIndex(closestposition);
                closestindexold = closestindex;

                AnnoTrackStatic.used_labels.Clear();
                foreach (AnnoListItem item in AnnoTrack.GetSelectedTrack().AnnoList)
                {
                    if (item.Label != "")
                    {
                        LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                        bool detected = false;
                        foreach (LabelColorPair p in AnnoTrackStatic.used_labels)
                        {
                            if (p.Label == l.Label)
                            {
                                detected = true;
                            }
                        }

                        if (detected == false)
                        {
                            AnnoTrackStatic.used_labels.Add(l);
                        }
                    }
                }

                if (isDiscrete && stop < ViewHandler.Time.TotalDuration)
                {
                    AnnoListItem temp = new AnnoListItem(start, len, AnnoTrackStatic.Defaultlabel, AnnoTrackStatic.DefaultColor, TierId);
                    temp.Bg = AnnoTrackStatic.DefaultColor;
                    anno_list.Add(temp);
                    AnnoTrackSegment segment = new AnnoTrackSegment(temp, this);
                    annorightdirection = true;
                    segments.Add(segment);
                    this.Children.Add(segment);
                    SelectSegment(segment);
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            // change track
            if (selected_track != this)
            {
                AnnoTrack.SelectTrack(this);
            }

            UnselectSegment();
            if (isDiscrete)
            {
                // check for segment selection

                foreach (AnnoTrackSegment s in segments)
                {
                    if (s.IsMouseOver)
                    {
                        SelectSegment(s);
                        this.select(true);
                    }
                }
            }
        }

        protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);

            double start = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X);
            double stop = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X) + 0.5;
            //  double stop = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X + AnnoTrackSegment.RESIZE_OFFSET);
            double len = stop - start;
            double closestposition = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X);
            closestindex = getClosestContinousIndex(closestposition);
            closestindexold = closestindex;

            if (isDiscrete && stop < ViewHandler.Time.TotalDuration)
            {
                AnnoListItem temp = new AnnoListItem(start, len, AnnoTrackStatic.Defaultlabel, "", TierId, AnnoTrackStatic.DefaultColor);
                anno_list.Add(temp);
                AnnoTrackSegment segment = new AnnoTrackSegment(temp, this);
                segment.Width = 1;
                annorightdirection = true;
                segments.Add(segment);
                this.Children.Add(segment);
                SelectSegment(segment);
                this.select(true);
            }
        }

        public int getClosestContinousIndex(double nearestitem)
        {
            for (int i = 0; i < anno_list.Count; i++)
            {
                if (anno_list[i].Start - nearestitem < 1 / this.samplerate && anno_list[i].Start - nearestitem > 0)
                {
                    return i;
                }
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            double dx = e.GetPosition(Application.Current.MainWindow).X - lastX;

            direction = (dx > 0) ? 1 : 0;
            lastX = e.GetPosition(Application.Current.MainWindow).X;

            if (isDiscrete)
            {
                if (e.RightButton == MouseButtonState.Pressed /*&& this.is_selected*/)
                {
                    Point point = e.GetPosition(selected_segment);

                    if (selected_segment != null)
                    {
                        this.select(true);
                        double delta = point.X - selected_segment.ActualWidth;
                        if (annorightdirection)
                        {
                            //fight the rounding error
                            if (ViewHandler.Time.PixelFromTime(selected_segment.Item.Stop) > ViewHandler.Time.CurrentSelectPosition && ViewHandler.Time.PixelFromTime(selected_segment.Item.Start) > ViewHandler.Time.CurrentSelectPosition)
                            {
                                selected_segment.Item.Start = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.CurrentSelectPosition);
                            }
                            selected_segment.resize_right(delta);

                            FireOnMove(selected_segment.Item.Stop);
                            ViewHandler.Time.CurrentPlayPosition = selected_segment.Item.Stop;

                            Console.WriteLine(ViewHandler.Time.PixelFromTime(selected_segment.Item.Start) + "   " + ViewHandler.Time.PixelFromTime(selected_segment.Item.Stop) + "   " + ViewHandler.Time.CurrentSelectPosition + "    " + annorightdirection);

                            if (ViewHandler.Time.PixelFromTime(selected_segment.Item.Stop) >= ViewHandler.Time.CurrentSelectPosition - 1 && ViewHandler.Time.PixelFromTime(selected_segment.Item.Start) >= ViewHandler.Time.CurrentSelectPosition - 1 && point.X < 0) annorightdirection = false;
                            SelectSegment(selected_segment);
                            this.select(true);
                        }
                        else
                        {
                            delta = point.X;
                            //fight the rounding error
                            if (ViewHandler.Time.PixelFromTime(selected_segment.Item.Stop) < ViewHandler.Time.CurrentSelectPosition && ViewHandler.Time.PixelFromTime(selected_segment.Item.Start) < ViewHandler.Time.CurrentSelectPosition)
                            {
                                selected_segment.Item.Stop = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.CurrentSelectPosition);
                            }
                            selected_segment.resize_left(delta);
                            FireOnMove(selected_segment.Item.Start);
                            ViewHandler.Time.CurrentPlayPosition = selected_segment.Item.Start;
                            Console.WriteLine(ViewHandler.Time.PixelFromTime(selected_segment.Item.Start) + "   " + ViewHandler.Time.PixelFromTime(selected_segment.Item.Stop) + "   " + ViewHandler.Time.CurrentSelectPosition + "   " + annorightdirection);
                            if ((ViewHandler.Time.PixelFromTime(selected_segment.Item.Start) > ViewHandler.Time.CurrentSelectPosition - 1)) annorightdirection = true;
                            SelectSegment(selected_segment);
                            this.select(true);
                        }
                    }
                }

                if (selected_segment != null && this.is_selected)
                {
                    this.select(true);
                    Point point = e.GetPosition(selected_segment);

                    // check if use wants to resize/move

                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        // resize segment left
                        if (selected_segment.is_resizeable_left)
                        {
                            double delta = point.X;
                            if (selected_segment.ActualWidth - delta > AnnoTrackSegment.RESIZE_OFFSET)
                            {
                                selected_segment.resize_left(delta);
                                SelectSegment(selected_segment);
                                this.select(true);
                                FireOnMove(selected_segment.Item.Start);
                            }
                        }
                        // resize segment right
                        else if (selected_segment.is_resizeable_right)
                        {
                            double delta = point.X - selected_segment.ActualWidth;
                            if (point.X > AnnoTrackSegment.RESIZE_OFFSET)
                            {
                                if (point.X > ViewHandler.Time.PixelFromTime(ViewHandler.Time.SelectionStop)) delta = ViewHandler.Time.PixelFromTime(ViewHandler.Time.SelectionStop) - selected_segment.ActualWidth;
                                selected_segment.resize_right(delta);
                                SelectSegment(selected_segment);
                                this.select(true);
                                FireOnMove(selected_segment.Item.Stop);
                            }
                        }
                        // move segment
                        else if (selected_segment.is_moveable)
                        {
                            double pos = GetLeft(selected_segment);
                            double delta = point.X - selected_segment.ActualWidth / 2;
                            if (pos + delta >= 0 && pos + selected_segment.ActualWidth + delta <= this.Width)
                            {
                                selected_segment.move(delta);
                                SelectSegment(selected_segment);
                                this.select(true);
                                FireOnMove(selected_segment.Item.Start + (selected_segment.Item.Stop - selected_segment.Item.Start) * 0.5);
                            }
                        }
                    }
                    else
                    {
                        // check if use can resize/move
                        selected_segment.checkResizeable(point);
                    }
                }
            }
            else if (AnnoList.AnnotationType == 2)
            {
                if (continuousannomode) el.Visibility = Visibility.Visible;
                else el.Visibility = Visibility.Hidden;

                if (e.RightButton == MouseButtonState.Pressed && this.is_selected)
                {
                    double deltaDirection = currentPositionX - e.GetPosition(this).X;
                    currentPositionX = e.GetPosition(this).X;

                    if (deltaDirection < 0)
                    {
                        double closestposition = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X);
                        closestindex = getClosestContinousIndex(closestposition);
                        if (closestindex > -1)
                        {
                            double range = borderhigh - borderlow;
                            double normal = 1.0 - ((e.GetPosition(this).Y / this.ActualHeight));
                            double normalized = (normal * range) + borderlow;
                            anno_list[closestindex].Label = normalized.ToString();

                            for (int i = closestindexold; i < closestindex; i++)
                            {
                                anno_list[i].Label = normalized.ToString();
                            }
                            closestindexold = closestindex;
                            timeRangeChanged(ViewHandler.Time);
                            //  nicer drawing but slower
                            if (!isDiscrete)
                            {
                                timeRangeChanged(ViewHandler.Time);
                            }
                        }
                    }
                }
            }
        }

        public void timeRangeChanged(ViewTime time)
        {
            this.Width = time.SelectionInPixel;

            if (this.AnnoList.AnnotationType == 0 || this.AnnoList.AnnotationType == 1)
            {
                foreach (AnnoTrackSegment s in segments)
                {
                    s.Visibility = Visibility.Hidden;
                    if (s.Item.Start >= time.SelectionStart && s.Item.Start <= time.SelectionStop)
                    {
                        s.update();
                        s.Visibility = Visibility.Visible;
                    }
                    else if (s.Item.Stop >= time.SelectionStart && s.Item.Start <= time.SelectionStop)
                    {
                        s.update2();
                        s.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                if (this.ActualHeight > 0)
                {
                    //markers

                    if (markers.Count > 0)
                    {
                        for (int i = 0; i < markers.Count; i++)
                        {
                            if (i == markers.Count / 2)
                            {
                                markers[i].StrokeDashArray = new DoubleCollection() { 2.0, 5.0 };
                                markers[i].StrokeThickness = 1.5;
                            }
                            markers[i].Y1 = (this.ActualHeight / markers.Count) * i;
                            markers[i].Y2 = markers[i].Y1;
                            markers[i].X1 = 0;
                            markers[i].X2 = this.ActualWidth;
                        }
                    }

                    double range = time.SelectionStop - time.SelectionStart;

                    int linesinrangenum = 0;
                    foreach (AnnoListItem ali in anno_list)
                    {
                        if (ali.Start >= time.SelectionStart && ali.Stop <= time.SelectionStop)
                        {
                            linesinrangenum++;
                        }
                    }

                    if (linesinrangenum <= lines.Count)
                    {
                        foreach (Line l in lines)
                        {
                            l.Visibility = Visibility.Hidden;
                        }

                        int i = 0;
                        foreach (AnnoListItem ali in anno_list)
                        {
                            if (ali.Start >= time.SelectionStart && ali.Stop < time.SelectionStop)
                            {
                                lines[i % lines.Count].X1 = ViewHandler.Time.PixelFromTime(ali.Start);
                                if (i % lines.Count < lines.Count - 1 && ali.Stop < time.SelectionStop - ali.Duration) lines[i % lines.Count].X2 = lines[i % lines.Count + 1].X1;
                                else lines[i % lines.Count].X2 = ViewHandler.Time.PixelFromTime(ali.Stop);

                                //investigate when loaded from db
                                if (ali.Label == "") ali.Label = "0.5";

                                double value = double.Parse(ali.Label);
                                double rng = borderhigh - borderlow;
                                value = 1.0 - (value - borderlow) / rng;

                                lines[i % lines.Count].Y1 = (value) * this.ActualHeight;
                                if (i % lines.Count < lines.Count - 1 && ali.Stop < time.SelectionStop - ali.Duration) lines[i % lines.Count].Y2 = lines[i % lines.Count + 1].Y1;
                                else if (i > 0) lines[i % lines.Count].Y2 = lines[i % lines.Count - 1].Y1;
                                lines[i % lines.Count].Visibility = Visibility.Visible;
                                i++;
                            }
                        }
                    }
                    else
                    {
                        int i = 0;
                        int index = 0;

                        foreach (Line s in lines)
                        {
                            s.Visibility = Visibility.Hidden;

                            index = (int)((double)anno_list.Count / (double)lines.Count * (double)i + 0.5f);
                            if (index > anno_list.Count) index = anno_list.Count - 1;

                            if (anno_list[index].Start >= time.SelectionStart && anno_list[index].Stop <= time.SelectionStop)
                            {
                                int offset = (int)((double)anno_list.Count / (double)lines.Count + 0.5f);
                                s.X1 = ViewHandler.Time.PixelFromTime(anno_list[index].Start);
                                if (i < lines.Count - 1 && anno_list[index + offset].Stop <= time.SelectionStop) s.X2 = lines[i + 1].X1;
                                else s.X2 = ViewHandler.Time.PixelFromTime(anno_list[index].Start);

                                double median = 0;

                                double rng = borderhigh - borderlow;
                                if (index > 0)
                                {
                                    for (int k = index - offset; k < index + offset; k++)
                                    {
                                        double value = double.Parse(anno_list[k].Label);

                                        value = 1.0 - (value - borderlow) / rng;

                                        median = median + value;
                                    }
                                    median = median / (2 * offset);
                                    s.Y1 = (median) * this.ActualHeight;
                                }
                                else
                                {
                                    double value = double.Parse(anno_list[index].Label);
                                    value = 1.0 - (value - borderlow) / rng;
                                    s.Y1 = (value) * this.ActualHeight;
                                }
                                if (i < lines.Count - 1 && anno_list[index + offset].Stop <= time.SelectionStop) s.Y2 = lines[i + 1].Y1;
                                else s.Y2 = s.Y1;
                                s.Visibility = Visibility.Visible;
                            }
                            i++;
                        }
                    }
                }
            }
        }
    }
}
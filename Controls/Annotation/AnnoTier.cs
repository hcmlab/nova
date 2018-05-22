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
    public delegate void AnnoTierChangeEventHandler(AnnoTier track, EventArgs e);

    public delegate void AnnoTierSegmentChangeEventHandler(AnnoTierSegment segment, EventArgs e);

    public delegate void AnnoTierResizeEventHandler(double pos);

    public partial class AnnoTierStatic : Canvas
    {
        static protected AnnoTier selectedTier = null;
        static protected AnnoTierSegment selectedLabel = null;

        static protected int selectedZindex = 0;
        static protected int selectedZindexMax = 0;
        static public int closestIndex = -1;
        static public int closestIndexOld = 0;
        public static bool isLiveAnnoMode = false;
        public static bool askForLabel = false;
        public static AnnoTierSegment objectContainer = null;
        public static bool MouseActive = Properties.Settings.Default.LiveModeActivateMouse;

        static public event AnnoTierChangeEventHandler OnTierChange;

        static public event AnnoTierSegmentChangeEventHandler OnTierSegmentChange;

        static public event AnnoTierResizeEventHandler OnTrackResize;

        static public DispatcherTimer dispatcherTimer = new DispatcherTimer();

        static public AnnoTierSegment Label
        {
            get { return selectedLabel; }
        }

        static public AnnoTier Selected
        {
            get { return selectedTier; }
        }

        static public void Select(AnnoTier t)
        {
            Unselect();
            selectedTier = t;
            t.Select(true);

            OnTierChange?.Invoke(selectedTier, null);
        }

        static public void Unselect()
        {
            if (selectedTier != null)
            {
                selectedTier.Select(false);
                selectedTier = null;
            }
        }

        static public void SelectLabel(AnnoTierSegment s)
        {
            UnselectLabel();
            if (s != null)
            {
                s.select(true);

                if (objectContainer == null)
                {
                    objectContainer = s;
                }
                selectedLabel = s;
                selectedZindex = GetZIndex(selectedLabel);
                SetZIndex(selectedLabel, selectedZindexMax + 1);

                OnTierSegmentChange?.Invoke(s, null);
            }
        }

        static public void SelectPoint(AnnoTierSegment s)
        {
            UnselectLabel();
            if (s != null)
            {
                s.select(true);

                if (objectContainer == null)
                {
                    objectContainer = s;
                }
                selectedLabel = s;
                selectedZindex = GetZIndex(selectedLabel);
                SetZIndex(selectedLabel, selectedZindexMax + 1);

                OnTierSegmentChange?.Invoke(s, null);
            }
        }

        static public void UnselectLabel()
        {
            if (selectedLabel != null)
            {
                selectedLabel.select(false);
                SetZIndex(selectedLabel, selectedZindex);
                selectedLabel = null;
            }
        }

        static public void RemoveSegmentPressed(object sender, KeyEventArgs e)
        {
            if (selectedLabel != null && selectedTier != null/* && GetSelectedTrack().isDiscrete*/)
            {
                AnnoTierSegment tmp = selectedLabel;
                UnselectLabel();
                selectedTier.RemoveSegment(tmp);
            }
        }

        static public void SplitPressed(object sender, KeyEventArgs e)
        {
            AnnoTierSegment tmp = selectedLabel;
            UnselectLabel();
            selectedTier.SplitSegment(tmp);
        }

        static public void FireOnMove(double pos)
        {
            OnTrackResize?.Invoke(pos);
        }
    }

    public class AnnoTier : AnnoTierStatic, ITrack
    {
        #region Properties

        private AnnoTierUndoRedo _UnDoObject;

        public AnnoTierUndoRedo UnDoObject
        {
            get { return _UnDoObject; }
            set
            {
                _UnDoObject = value;
                //  UnDoObject.adornerevent += new EventHandler(UnDoObject_adornerevent);
            }
        }

        #endregion Properties

        private bool isSelected = false;
        public List<AnnoTierSegment> segments = new List<AnnoTierSegment>();

        private double currentPositionX = 0;

        public int lastLabelIndex;
        public string DefaultLabel;
        public double DefaultScore;
        public Color DefaultColor;
        private double dx = 0;
        private double lastX;
        private int direction;
        private bool annorightdirection = true;
        private bool isMouseAlreadydown = false;
        private double yPos = 0;
        private int level = 2;
        private List<Line> continuousTierLines = new List<Line>();
        private List<Line> continuousTierMarkers = new List<Line>();
        private Ellipse continuousTierEllipse = new Ellipse();

        public Border Border { get; set; }

        public AnnoList AnnoList { get; set; }

        public bool IsDiscreteOrFree
        {
            get { return AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE; }
        }

        public string CMLCompleteTrainOptions { get; set; }
        public string CMLCompletePredictOptions { get; set; }

        public bool IsGeometric
        {
            get
            {
                return (AnnoList.Scheme.Type == AnnoScheme.TYPE.POINT ||
                       AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON ||
                       AnnoList.Scheme.Type == AnnoScheme.TYPE.GRAPH ||
                       AnnoList.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION);
            }
        }

        public bool IsContinuous
        {
            get
            {
                return AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS;
            }
        }

        private Color minOrBackColor = Defaults.Colors.Background;

        public Color MinOrBackColor
        {
            get
            {
                return minOrBackColor;
            }
            set
            {
                minOrBackColor = value;
                AnnoList.Scheme.MinOrBackColor = minOrBackColor;
                updateBackground();
            }
        }

        private Color maxOrForeColor = Defaults.Colors.Foreground;

        public Color MaxOrForeColor
        {
            get
            {
                return maxOrForeColor;
            }
            set
            {
                maxOrForeColor = value;
                AnnoList.Scheme.MaxOrForeColor = maxOrForeColor;
                updateBackground();
            }
        }

        private void updateBackground()
        {
            if (AnnoList.Scheme.Type != AnnoScheme.TYPE.CONTINUOUS)
            {
                Background = new SolidColorBrush(AnnoList.Scheme.MinOrBackColor);
            }
            else
            {
                Background = new LinearGradientBrush(AnnoList.Scheme.MaxOrForeColor, AnnoList.Scheme.MinOrBackColor, 90.0);
            }
        }

        public AnnoTier(AnnoList anno)
        {
            AnnoList = anno;

            maxOrForeColor = anno.Scheme.MaxOrForeColor;
            minOrBackColor = anno.Scheme.MinOrBackColor;
            updateBackground();

            AllowDrop = true;
            SizeChanged += new SizeChangedEventHandler(sizeChanged);

            UnDoObject = new AnnoTierUndoRedo();
            UnDoObject.Container = this;

            double mean = (anno.Scheme.MinScore + anno.Scheme.MaxScore) / 2;

            DefaultColor = Defaults.Colors.Foreground;
            DefaultLabel = null;
            DefaultScore = double.NaN;

            switch (anno.Scheme.Type)
            {
                case AnnoScheme.TYPE.DISCRETE:

                    if (AnnoList.Scheme.Labels.Count > 0)
                    {
                        DefaultColor = AnnoList.Scheme.Labels[0].Color;
                        DefaultLabel = AnnoList.Scheme.Labels[0].Name;
                    }
                    break;

                case AnnoScheme.TYPE.FREE:

                    DefaultColor = AnnoList.Scheme.MaxOrForeColor;
                    if (AnnoList.Count > 0)
                    {
                        DefaultLabel = AnnoList[0].Label;
                    }
                    break;

                case AnnoScheme.TYPE.CONTINUOUS:

                    DefaultScore = mean;
                    break;
            }

            Select(this);

            if (!IsDiscreteOrFree)
            {
                Loaded += delegate
                {
                    if (anno.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                    {
                        InitContinousValues(anno.Scheme.SampleRate);
                        bool init = false;

                        dispatcherTimer.Interval = TimeSpan.FromMilliseconds(20);
                        dispatcherTimer.Tick += new EventHandler(delegate (object s, EventArgs a)
                        {
                            if (isLiveAnnoMode && isSelected)
                            {
                                double closestposition = MainHandler.Time.CurrentPlayPosition;
                                closestIndex = GetClosestContinuousIndex(closestposition);
                                if (closestIndex > -1)
                                {
                                    {
                                        continuousTierEllipse.Visibility = Visibility.Visible;

                                        double numberOfLevels = Properties.Settings.Default.ContinuousHotkeysNumber;
                                        double fac = 1 + (1 / (numberOfLevels - 1));
                                        double segmentHeight = (this.ActualHeight / numberOfLevels);

                                        if (!init)
                                        {
                                            yPos = yPos = (numberOfLevels - ((int)(numberOfLevels / 2) * fac)) * segmentHeight;
                                            init = true;
                                        }

                                        if ((this == Mouse.DirectlyOver || (Mouse.GetPosition(this).Y > 0 && Mouse.GetPosition(this).Y < this.ActualHeight && continuousTierEllipse == Mouse.DirectlyOver)) && MouseActive)
                                        {
                                            yPos = (Mouse.GetPosition(this).Y < 0.0 ? 0.0 : Mouse.GetPosition(this).Y);
                                        }
                                        UdpateContinuousPosition();
                                    }
                                }
                            }
                            else continuousTierEllipse.Visibility = Visibility.Hidden;
                        });
                    }
                    else if (anno.Scheme.Type == AnnoScheme.TYPE.POINT)
                    {
                        InitPointValues(anno);
                    }
                    else if (anno.Scheme.Type == AnnoScheme.TYPE.POLYGON)
                    { }
                    else if (anno.Scheme.Type == AnnoScheme.TYPE.GRAPH)
                    { }
                    else if (anno.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                    { }

                    anno.HasChanged = false;
                };
            }
            selectedTier = this;

            if (IsDiscreteOrFree)
            {
                foreach (AnnoListItem item in anno)
                {
                    AddSegment(item);
                }
            }
        }

        private void UdpateContinuousPosition()
        {
            double range = this.AnnoList.Scheme.MaxScore - this.AnnoList.Scheme.MinScore;

            double normal = 1.0 - (yPos / this.ActualHeight);
            double normalized = (normal * range) + this.AnnoList.Scheme.MinScore;

            continuousTierEllipse.Height = this.ActualHeight / 10;
            continuousTierEllipse.Width = continuousTierEllipse.Height;
            continuousTierEllipse.SetValue(Canvas.TopProperty, (yPos - continuousTierEllipse.Height / 2));
            continuousTierEllipse.SetValue(Canvas.LeftProperty, (MainHandler.Time.PixelFromTime(MainHandler.Time.CurrentPlayPosition) - continuousTierEllipse.Width / 2));
            AnnoList[closestIndex].Score = normalized;
            AnnoList[closestIndex].Confidence = 1.0;

            for (int i = closestIndexOld; i < closestIndex; i++)
            {
                if (closestIndexOld > -1)
                {
                    AnnoList[i].Score = normalized;
                    AnnoList[closestIndex].Confidence = 1.0;
                }
            }
            closestIndexOld = closestIndex;

            TimeRangeChanged(MainHandler.Time);
        }

        public void continuousSegmentUp()
        {
            double numberOfLevels = Properties.Settings.Default.ContinuousHotkeysNumber;
            double fac = 1 + (1 / (numberOfLevels - 1));
            double segmentHeight = (this.ActualHeight / numberOfLevels);

            double step = fac * segmentHeight;
            yPos = (yPos - step >= 0) ? yPos - step : yPos;
            if (yPos - step >= 0) level++;
        }

        public void continuousSegmentDown()
        {
            double numberOfLevels = Properties.Settings.Default.ContinuousHotkeysNumber;
            double fac = 1 + (1 / (numberOfLevels - 1));
            double segmentHeight = (this.ActualHeight / numberOfLevels);

            double step = fac * segmentHeight;
            yPos = (yPos + step <= this.ActualHeight) ? yPos + step : yPos;
            if (yPos + step <= this.ActualHeight) level--;
        }

        public void continuousSegmentToPosition(int position)
        {
            double numberOfLevels = Properties.Settings.Default.ContinuousHotkeysNumber;
            double fac = 1 + (1 / (numberOfLevels - 1));
            double segmentHeight = (this.ActualHeight / numberOfLevels);
            level = position;
            yPos = (numberOfLevels - (position * fac)) * segmentHeight;
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

        public void ExportToPNG(Uri path, Canvas surface)
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
            if (AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE || AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
            {
                this.Visibility = Visibility.Hidden;
                foreach (AnnoTierSegment segment in segments)
                {
                    segment.Height = e.NewSize.Height;
                }
                this.Visibility = Visibility.Visible;
            }
            else
            {   //it has to be called twice, otherwise there are some weird effects.
                continuousSegmentToPosition(level);
                TimeRangeChanged(MainHandler.Time);
                TimeRangeChanged(MainHandler.Time);
            }
        }

        public void Select(bool flag)
        {
            isSelected = flag;
            if (Border != null)
            {
                Border.BorderBrush = flag ? Defaults.Brushes.Highlight : Defaults.Brushes.Conceal;
            }
        }

        public AnnoTierSegment GetSegment(AnnoListItem item)
        {
            foreach (AnnoTierSegment segment in segments)
            {
                if (segment.Item == item)
                {
                    return segment;
                }
            }
            return null;
        }

        public AnnoTierSegment AddSegment(AnnoListItem item)
        {
            AnnoTierSegment segment = new AnnoTierSegment(item, this);
            segments.Add(segment);
            this.Children.Add(segment);
            selectedZindexMax = Math.Max(selectedZindexMax, GetZIndex(segment));

            return segment;
        }

        public void RemoveSegment(AnnoTierSegment s)
        {
            ChangeRepresentationObject RememberDelete = UnDoObject.MakeChangeRepresentationObjectForDelete((FrameworkElement)s);
            UnDoObject.InsertObjectforUndoRedo(RememberDelete);
            DeleteSegment(s);
        }

        public void DeleteSegment(AnnoTierSegment s)
        {
            AnnoList.Remove(s.Item);
            s.Tier.Children.Remove(s);
            s.Tier.segments.Remove(s);
        }

        public void SplitSegment(AnnoTierSegment s)
        {
            AnnoListItem second_segment = new AnnoListItem(MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition), s.Item.Stop - MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition), s.Item.Label, s.Item.Meta, s.Item.Color, s.Item.Confidence, s.Item.isGeometric, s.Item.Points);
            AnnoTierSegment second_s = AddSegment(second_segment);
            ChangeRepresentationObject RememberSplit = UnDoObject.MakeChangeRepresentationObjectForSplit(GetLeft(s), (FrameworkElement)s, (FrameworkElement)second_s);
            UnDoObject.InsertObjectforUndoRedo(RememberSplit);
            s.Item.Duration = MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition) - s.Item.Start;
            AnnoList.AddSorted(second_segment);
        }

        public void InitContinousValues(double sr)
        {
            double mean = (AnnoList.Scheme.MinScore + AnnoList.Scheme.MaxScore) / 2.0;
            //double initValue = mean;
            double initValue = double.NaN;



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
                continuousTierMarkers.Add(marker);
                this.Children.Add(continuousTierMarkers[i]);
            }

           

            //add lines

            int samples = (int)Math.Round(MainHandler.Time.TotalDuration * sr);
            double delta = 1.0 / sr;
            if (AnnoList.Count < samples)
            {
                for (int i = AnnoList.Count; i < samples; i++)
                {
                    AnnoListItem ali = new AnnoListItem(i * delta, delta, initValue, "", Colors.Black);
                    AnnoList.Add(ali);
                }
            }

            int drawlinesnumber = 1000;
            if (AnnoList.Count < 1000) drawlinesnumber = AnnoList.Count;

            for (int i = 0; i < drawlinesnumber; i++)
            {
                Line line = new Line();
                line.StrokeThickness = 1;
                line.Stroke = Brushes.Black;
                line.Y1 = mean;
                line.Y2 = line.Y1;
                line.X1 = MainHandler.Time.PixelFromTime(MainHandler.Time.TotalDuration / 1000 * i);
                line.X2 = MainHandler.Time.PixelFromTime(MainHandler.Time.TotalDuration / 1000 * i + MainHandler.Time.TotalDuration / 1000 * (i + 1));
                continuousTierLines.Add(line);
                line.Visibility = Visibility.Collapsed;
                selectedZindexMax = Math.Max(selectedZindexMax, GetZIndex(line));
                this.Children.Add(line);
            }

            continuousTierEllipse.Width = this.ActualHeight / 10;
            continuousTierEllipse.Height = continuousTierEllipse.Width;
            continuousTierEllipse.Fill = Brushes.Red;
            continuousTierEllipse.Stroke = Brushes.WhiteSmoke;
            continuousTierEllipse.Visibility = Visibility.Hidden;
            continuousTierEllipse.SetValue(Canvas.LeftProperty, continuousTierEllipse.Width / 2);
            continuousTierEllipse.SetValue(Canvas.TopProperty, this.ActualHeight / 2 - continuousTierEllipse.Height / 2);
            this.Children.Add(continuousTierEllipse);

            TimeRangeChanged(MainHandler.Time);
            if (!IsDiscreteOrFree)
            {
                TimeRangeChanged(MainHandler.Time);
            }
        }

        public void InitPointValues(AnnoList anno)
        {
            double sr = anno.Scheme.SampleRate;
            int numPoints = anno.Scheme.NumberOfPoints;
            int samples = (int)Math.Round(MainHandler.Time.TotalDuration * sr);

            double delta = 1.0 / sr;
            if (AnnoList.Count < samples)
            {
                Random rnd = new Random();
                for (int i = AnnoList.Count; i < samples; i++)
                {
                    PointList points = new PointList();
                    for (int j = 0; j < numPoints; ++j)
                    {
                        int x = -1;
                        int y = -1;
                        //x = rnd.Next(50, 200);
                        //y = rnd.Next(50, 200);
                        points.Add(new PointListItem(x, y, (j + 1).ToString(), 1.0));
                    }
                    AnnoListItem ali = new AnnoListItem(i * delta, delta, "Frame " + (i + 1).ToString(), "", anno.Scheme.MinOrBackColor, 1, true, points);
                    AnnoList.Add(ali);
                }
            }

            TimeRangeChanged(MainHandler.Time);
            //TimeRangeChanged(MainHandler.Time);
        }

        public void LiveAnnoMode(bool activated)
        {
            dispatcherTimer.Stop();
            if (!activated)
            {
                dispatcherTimer.Start();
                isLiveAnnoMode = true;

                closestIndex = -1;
                closestIndexOld = closestIndex;
            }
            else
            {
                dispatcherTimer.Stop();
                isLiveAnnoMode = false;
                continuousTierEllipse.Visibility = Visibility.Hidden;
            }
            TimeRangeChanged(MainHandler.Time);
        }





        public void NewAnnoKey()
        {
            if (IsDiscreteOrFree)
            {
                double start = MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition);
                double stop = MainHandler.Time.CurrentPlayPosition;

                if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                {
                    if (start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                    {
                        int round = (int)(start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate));
                        start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                    }

                    if (stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                    {
                        int round = (int)(stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate));
                        stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                    }
                }

                if (stop < start)
                {
                    double temp = start;
                    start = stop;
                    stop = temp;
                }

                //  double stop = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X + AnnoTierSegment.RESIZE_OFFSET);
                double len = stop - start;

                double closestposition = start;
                closestIndex = GetClosestContinuousIndex(closestposition);
                closestIndexOld = closestIndex;

                if (IsDiscreteOrFree && stop < MainHandler.Time.TotalDuration)
                {
                    AnnoListItem temp = new AnnoListItem(start, len, DefaultLabel, "", DefaultColor, 1.0);
                    temp.Color = DefaultColor;
                    AnnoList.AddSorted(temp);
                    AnnoTierSegment segment = new AnnoTierSegment(temp, this);

                    ChangeRepresentationObject ChangeRepresentationObjectforInsert = UnDoObject.MakeChangeRepresentationObjectForInsert(segment);
                    UnDoObject.InsertObjectforUndoRedo(ChangeRepresentationObjectforInsert);

                    annorightdirection = true;
                    segments.Add(segment);
                    Children.Add(segment);
                    SelectLabel(segment);
                }
            }
        }

        public void NewAnnoCopy(double start, double stop, string label, Color color, double confidence = 1.0)
        {
            if (stop < start)
            {
                double temp = start;
                start = stop;
                stop = temp;
            }
            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
            {
                if (start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                {
                    int round = (int)(start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate));
                    start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                }

                if (stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                {
                    int round = (int)(stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate));
                    stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                }
            }

            //  double stop = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X + AnnoTierSegment.RESIZE_OFFSET);
            double len = stop - start;

            double closestposition = start;
            closestIndex = GetClosestContinuousIndex(closestposition);
            closestIndexOld = closestIndex;

            if (AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
            {
                label = DefaultLabel;
                color = DefaultColor;
            }
            else if (AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                label = "";
                color = Colors.Black;
            }

            if (stop < MainHandler.Time.TotalDuration)
            {
                AnnoListItem temp = new AnnoListItem(start, len, label, "", color, confidence);
                temp.Color = this.DefaultColor;

                bool alreadyinlist = false;
                foreach (AnnoListItem ali in this.AnnoList)
                {
                    if (ali.Start == temp.Start && ali.Stop == temp.Stop)
                    {
                        alreadyinlist = true;
                        break;
                    }
                }

                if (!alreadyinlist)
                {
                    if (this.AnnoList.Scheme.Type != AnnoScheme.TYPE.CONTINUOUS) AnnoList.AddSorted(temp);
                    AnnoTierSegment segment = new AnnoTierSegment(temp, this);
                    annorightdirection = true;
                    ChangeRepresentationObject ChangeRepresentationObjectforInsert = UnDoObject.MakeChangeRepresentationObjectForInsert(segment);
                    UnDoObject.InsertObjectforUndoRedo(ChangeRepresentationObjectforInsert);
                    segments.Add(segment);
                    this.Children.Add(segment);
                    SelectLabel(segment);
                }
            }
        }

        public void SelectAnno(MouseButtonEventArgs e)
        {
            dx = 0;

            UnselectLabel();
            this.Select(true);

            //base.OnMouseRightButtonDown(e);

            double start = MainHandler.Time.TimeFromPixel(e.GetPosition(this).X);

            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
            {
                if (start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                {
                    int round = (int)(start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                    start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                }
            }

            double minsr = 0;
            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
            {
                int factor = (int)(Properties.Settings.Default.DefaultMinSegmentSize / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate));
                minsr = (factor + 1) * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
            }
            double stop = MainHandler.Time.TimeFromPixel(e.GetPosition(this).X) + Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr);
            //  double stop = ViewHandler.Time.TimeFromPixel(e.GetPosition(this).X + AnnoTierSegment.RESIZE_OFFSET);
            double len = stop - start;
            double closestposition = MainHandler.Time.TimeFromPixel(e.GetPosition(this).X);
            closestIndex = GetClosestContinuousIndex(closestposition);
            closestIndexOld = closestIndex;

            if (IsDiscreteOrFree && stop < MainHandler.Time.TotalDuration)
            {
                AnnoListItem temp = new AnnoListItem(start, len, this.DefaultLabel, "", this.DefaultColor);
                AnnoList.AddSorted(temp);
                AnnoTierSegment segment = new AnnoTierSegment(temp, this);

                segment.Width = 1;
                annorightdirection = true;
                ChangeRepresentationObject ChangeRepresentationObjectforInsert = UnDoObject.MakeChangeRepresentationObjectForInsert(segment);
                UnDoObject.InsertObjectforUndoRedo(ChangeRepresentationObjectforInsert);
                segments.Add(segment);

                this.Children.Add(segment);
                SelectLabel(segment);
                this.Select(true);

                selectedLabel.Item.Duration = Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr);
                selectedLabel.Item.Stop = selectedLabel.Item.Start + selectedLabel.Item.Duration;
            }
            else if (!IsDiscreteOrFree && Keyboard.IsKeyDown(Key.LeftShift) && stop < MainHandler.Time.TotalDuration)
            {
                AnnoListItem temp = new AnnoListItem(start, len, DefaultScore, "", Colors.Black);
                AnnoTierSegment segment = new AnnoTierSegment(temp, this);
                segment.Width = 1;
                annorightdirection = true;
                segments.Add(segment);
                this.Children.Add(segment);
                SelectLabel(segment);
                this.Select(true);
            }
        }

        public void LeftMouseButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (IsDiscreteOrFree && Keyboard.IsKeyDown(Key.LeftShift))
            {
                SelectAnno(e);
            }
            else
            {
                UnselectLabel();
                this.Select(true);

                // change track
                if (selectedTier != this)
                {
                    AnnoTier.Select(this);
                }
                else if (IsDiscreteOrFree || (!IsDiscreteOrFree && Keyboard.IsKeyDown(Key.LeftShift)))
                {
                    // check for segment selection

                    foreach (AnnoTierSegment s in segments)
                    {
                        if (s.IsMouseOver)
                        {
                            SelectLabel(s);
                            this.Select(true);
                            break;
                        }
                    }
                }
                else if (IsContinuous)
                {
                    double closestposition = MainHandler.Time.TimeFromPixel(e.GetPosition(this).X);
                    closestIndex = GetClosestContinuousIndex(closestposition);
                    if (closestIndex != -1)
                    {
                        AnnoListItem item = this.AnnoList[closestIndex];
                        AnnoTierSegment s = new AnnoTierSegment(item, this);
                        SelectLabel(s);
                    }
                }
            }
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!Keyboard.IsKeyDown(Key.LeftShift))
            {
                if (this.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    double closestposition = MainHandler.Time.TimeFromPixel(e.GetPosition(this).X);
                    closestIndex = GetClosestContinuousIndex(closestposition);
                    closestIndexOld = closestIndex;
                }
                LeftMouseButtonDown(e);
            }
        }

        public void RightMouseButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonDown(e);
            SelectAnno(e);
        }

        public int GetClosestContinuousIndex(double nearestitem)
        {
            for (int i = 0; i < AnnoList.Count; i++)
            {
                if (AnnoList[i].Start - nearestitem < 1 / AnnoList.Scheme.SampleRate && AnnoList[i].Start - nearestitem >= 0)
                {
                    return i;
                }
            }
            return -1;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            isMouseAlreadydown = false;
            base.OnMouseUp(e);

            double minsr = 0;

            if (Properties.Settings.Default.DefaultMinSegmentSize != 0 && Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
            {
                int factor = (int)(Properties.Settings.Default.DefaultMinSegmentSize / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                minsr = (factor) * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
            }

            if (selectedLabel != null && selectedLabel.Item.Duration < Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr))
            {
                if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0 && Properties.Settings.Default.DefaultMinSegmentSize != 0)
                {
                    int factor = (int)(Properties.Settings.Default.DefaultMinSegmentSize / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                    int round = (int)(selectedLabel.Item.Start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                    selectedLabel.Item.Start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                    selectedLabel.Item.Duration = Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr);
                    selectedLabel.Item.Stop = selectedLabel.Item.Start + selectedLabel.Item.Duration;
                }
                else
                {
                    selectedLabel.Item.Duration = Properties.Settings.Default.DefaultMinSegmentSize;
                    selectedLabel.Item.Stop = selectedLabel.Item.Start + selectedLabel.Item.Duration;
                }
            }
        }

        public new void MouseMove(MouseEventArgs e)
        {
            dx = e.GetPosition(Application.Current.MainWindow).X - lastX;

            direction = (dx > 0) ? 1 : 0;
            lastX = e.GetPosition(Application.Current.MainWindow).X;

            if (IsDiscreteOrFree || (!IsDiscreteOrFree && Keyboard.IsKeyDown(Key.LeftShift)))
            {
                if (e.RightButton == MouseButtonState.Pressed || (e.LeftButton == MouseButtonState.Pressed && Keyboard.IsKeyDown(Key.LeftShift))/*&& this.is_selected*/)
                {
                    Point point = e.GetPosition(selectedLabel);

                    if (selectedLabel != null)
                    {
                        this.Select(true);
                        double delta = point.X - selectedLabel.ActualWidth;
                        if (annorightdirection)
                        {
                            //fight the rounding error
                            if (MainHandler.Time.PixelFromTime(selectedLabel.Item.Stop) > MainHandler.Time.CurrentSelectPosition && MainHandler.Time.PixelFromTime(selectedLabel.Item.Start) > MainHandler.Time.CurrentSelectPosition)
                            {
                                selectedLabel.Item.Start = MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition);
                            }
                            selectedLabel.resize_right(delta);

                            FireOnMove(selectedLabel.Item.Stop);
                            MainHandler.Time.CurrentPlayPosition = selectedLabel.Item.Stop;

                            if (MainHandler.Time.PixelFromTime(selectedLabel.Item.Stop) >= MainHandler.Time.CurrentSelectPosition - 1 && MainHandler.Time.PixelFromTime(selectedLabel.Item.Start) >= MainHandler.Time.CurrentSelectPosition - 1 && point.X < 0) annorightdirection = false;
                            SelectLabel(selectedLabel); 
                            this.Select(true);
                        }
                        else
                        {
                            delta = point.X;
                            //fight the rounding error
                            if (MainHandler.Time.PixelFromTime(selectedLabel.Item.Stop) < MainHandler.Time.CurrentSelectPosition && MainHandler.Time.PixelFromTime(selectedLabel.Item.Start) < MainHandler.Time.CurrentSelectPosition)
                            {
                                selectedLabel.Item.Stop = MainHandler.Time.TimeFromPixel(MainHandler.Time.CurrentSelectPosition);
                            }
                            selectedLabel.resize_left(delta);
                            FireOnMove(selectedLabel.Item.Start);
                            MainHandler.Time.CurrentPlayPosition = selectedLabel.Item.Start;

                            if ((MainHandler.Time.PixelFromTime(selectedLabel.Item.Start) > MainHandler.Time.CurrentSelectPosition - 1)) annorightdirection = true;
                            SelectLabel(selectedLabel);
                            this.Select(true);
                        }

                        if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                        {
                            if (selectedLabel.Item.Start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                            {
                                int round = (int)(selectedLabel.Item.Start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                selectedLabel.Item.Start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            if (selectedLabel.Item.Stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                            {
                                int round = (int)(selectedLabel.Item.Stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                selectedLabel.Item.Stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }
                            SelectLabel(selectedLabel);
                        }
                    }
                }

                if (selectedLabel != null && this.isSelected)
                {
                    this.Select(true);
                    Point point = e.GetPosition(selectedLabel);

                    // check if use wants to resize/move

                    if (e.LeftButton == MouseButtonState.Pressed)
                    {
                        double segmentwidth = point.X * (selectedLabel.Item.Duration / selectedLabel.ActualWidth);

                        // resize segment right
                        if (selectedLabel.isResizeableRight)
                        {
                            double delta = point.X - selectedLabel.ActualWidth;
                            double pos = GetLeft(selectedLabel);

                            if (isMouseAlreadydown == false)
                            {
                                ChangeRepresentationObject ChangeRepresentationObjectOfResize = UnDoObject.MakeChangeRepresentationObjectForResize(pos, (FrameworkElement)selectedLabel);
                                UnDoObject.InsertObjectforUndoRedo(ChangeRepresentationObjectOfResize);
                                isMouseAlreadydown = true;
                                SelectLabel(selectedLabel);
                            }

                            double minsr = 0;

                            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                            {
                                int factor = (int)(Properties.Settings.Default.DefaultMinSegmentSize / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                minsr = (factor) * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            if (segmentwidth >= Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr))
                            {
                                if (point.X > MainHandler.Time.PixelFromTime(MainHandler.Time.SelectionStop)) delta = MainHandler.Time.PixelFromTime(MainHandler.Time.SelectionStop) - selectedLabel.ActualWidth;

                                selectedLabel.resize_right(delta);

                                SelectLabel(selectedLabel);
                                this.Select(true);
                                FireOnMove(selectedLabel.Item.Stop);
                            }
                            else
                            {
                                selectedLabel.Item.Duration = Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr);
                                selectedLabel.Item.Stop = selectedLabel.Item.Start + selectedLabel.Item.Duration;
                                SelectLabel(selectedLabel);
                                this.Select(true);
                            }

                            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                            {
                                if (selectedLabel.Item.Stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                                {
                                    int round = (int)(selectedLabel.Item.Stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                    selectedLabel.Item.Stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                                    SelectLabel(selectedLabel);
                                }
                            }
                        }
                        // resize segment left
                        else if (selectedLabel.isResizeableLeft)
                        {
                            double pos = GetLeft(selectedLabel);
                            if (isMouseAlreadydown == false)
                            {
                                ChangeRepresentationObject ChangeRepresentationObjectOfResize = UnDoObject.MakeChangeRepresentationObjectForResize(pos, (FrameworkElement)selectedLabel);
                                UnDoObject.InsertObjectforUndoRedo(ChangeRepresentationObjectOfResize);
                                isMouseAlreadydown = true;
                            }

                            double delta = point.X;

                            double minsr = 0;
                            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                            {
                                int factor = (int)(Properties.Settings.Default.DefaultMinSegmentSize / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate));
                                minsr = (factor + 1) * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            if (selectedLabel.Item.Duration - segmentwidth >= Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr))
                            {
                                selectedLabel.resize_left(delta);
                                SelectLabel(selectedLabel);
                                this.Select(true);
                                if (selectedLabel != null) FireOnMove(selectedLabel.Item.Start);
                            }
                            else
                            {
                                selectedLabel.Item.Start = selectedLabel.Item.Stop - Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr);
                                selectedLabel.Item.Duration = Math.Max(Properties.Settings.Default.DefaultMinSegmentSize, minsr);
                                SelectLabel(selectedLabel);
                            }

                            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                            {
                                if (selectedLabel.Item.Start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                                {
                                    int round = (int)(selectedLabel.Item.Start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                    selectedLabel.Item.Start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                                    SelectLabel(selectedLabel);
                                }
                            }
                        }

                        // move segment
                        else if (selectedLabel.isMoveable)
                        {
                            double pos = GetLeft(selectedLabel);
                            double delta = point.X - selectedLabel.ActualWidth / 2;
                            if (pos + delta >= 0 && pos + selectedLabel.ActualWidth + delta <= this.ActualWidth)
                            {
                                if (isMouseAlreadydown == false)
                                {
                                    ChangeRepresentationObject ChangeRepresentationObjectOfMove = UnDoObject.MakeChangeRepresentationObjectForMove(pos, (FrameworkElement)selectedLabel);
                                    UnDoObject.InsertObjectforUndoRedo(ChangeRepresentationObjectOfMove);
                                    isMouseAlreadydown = true;
                                }
                                selectedLabel.move(delta);
                                SelectLabel(selectedLabel);

                                FireOnMove(selectedLabel.Item.Start + (selectedLabel.Item.Stop - selectedLabel.Item.Start) * 0.5);
                            }

                            if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0)
                            {
                                if (selectedLabel.Item.Start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                                {
                                    int round = (int)(selectedLabel.Item.Start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                    selectedLabel.Item.Start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                                }

                                if (selectedLabel.Item.Stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                                {
                                    int round = (int)(selectedLabel.Item.Stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                    selectedLabel.Item.Stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                                }
                                SelectLabel(selectedLabel);
                            }
                        }
                    }
                    else
                    {
                        isMouseAlreadydown = false;

                        // check if use can resize/move
                        selectedLabel.checkResizeable(point);
                    }
                }
            }
            else if (AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS && !Keyboard.IsKeyDown(Key.LeftAlt))
            {
                if (isLiveAnnoMode) continuousTierEllipse.Visibility = Visibility.Visible;
                else continuousTierEllipse.Visibility = Visibility.Hidden;

                if (e.RightButton == MouseButtonState.Pressed && this.isSelected)
                {
                    double deltaDirection = currentPositionX - e.GetPosition(this).X;
                    currentPositionX = e.GetPosition(this).X;

                    if (deltaDirection < 0)
                    {
                        double closestposition = MainHandler.Time.TimeFromPixel(e.GetPosition(this).X);
                        closestIndex = GetClosestContinuousIndex(closestposition);
                        if (closestIndex > -1)
                        {
                            double range = AnnoList.Scheme.MaxScore - AnnoList.Scheme.MinScore;
                            double normal = 1.0 - ((e.GetPosition(this).Y / this.ActualHeight));
                            double normalized = (normal * range) + AnnoList.Scheme.MinScore;
                            AnnoList[closestIndex].Score = normalized;
                            AnnoList[closestIndex].Confidence = 1.0;

                            for (int i = closestIndexOld; i < closestIndex; i++)
                            {
                                AnnoList[i].Score = normalized;
                                AnnoList[i].Confidence = 1.0;
                            }
                            closestIndexOld = closestIndex;
                            TimeRangeChanged(MainHandler.Time);
                            ////  nicer drawing but slower
                            //if (!IsDiscreteOrFree)
                            //{
                            //    //TimeRangeChanged(MainHandler.Time);
                            //}
                        }
                    }
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            MouseMove(e);
        }


        Color InvertMeAColour(Color ColourToInvert)
        {
            return Color.FromRgb((byte)~ColourToInvert.R, (byte)~ColourToInvert.G, (byte)~ColourToInvert.B);
        }

        public void TimeRangeChanged(Timeline time)
        {
            this.Width = time.SelectionInPixel;



            

            //segments can happen in both, discrete and continuous annotations, so we check them in any case
            foreach (AnnoTierSegment s in segments)
            {
                s.Visibility = Visibility.Collapsed;
                if (s.Item.Start >= time.SelectionStart && s.Item.Start <= time.SelectionStop)
                {
                    s.update();
                    //if (s.Item.Confidence < Properties.Settings.Default.UncertaintyLevel /*&& CorrectMode == true*/) s.Visibility = Visibility.Visible;
                    s.Visibility = Visibility.Visible;
                }
                else if (s.Item.Stop >= time.SelectionStart && s.Item.Start <= time.SelectionStop)
                {
                    s.update2();
                    //if (s.Item.Confidence < Properties.Settings.Default.UncertaintyLevel /*&& CorrectMode == true*/) s.Visibility = Visibility.Visible;
                    s.Visibility = Visibility.Visible;
                }
            }

            if (this.AnnoList.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {

                //Color InvertedColor = InvertMeAColour(this.AnnoList.Scheme.MinOrBackColor);
                SolidColorBrush invertBrush = new SolidColorBrush(Colors.Magenta);

                if (this.ActualHeight > 0)
                {
                    //markers

                    if (continuousTierMarkers.Count > 0)
                    {
                        for (int i = 0; i < continuousTierMarkers.Count; i++)
                        {
                            if (i == continuousTierMarkers.Count / 2)
                            {
                                continuousTierMarkers[i].StrokeDashArray = new DoubleCollection() { 2.0, 5.0 };
                                continuousTierMarkers[i].StrokeThickness = 1.5;
                            }
                            continuousTierMarkers[i].Y1 = (this.ActualHeight / continuousTierMarkers.Count) * i;
                            continuousTierMarkers[i].Y2 = continuousTierMarkers[i].Y1;
                            continuousTierMarkers[i].X1 = 0;
                            continuousTierMarkers[i].X2 = this.ActualWidth;
                        }
                    }

                    double timeRange = time.SelectionStop - time.SelectionStart;

                    int linesinrangenum = 0;
                    foreach (AnnoListItem ali in AnnoList)
                    {
                        if (ali.Start >= time.SelectionStart && ali.Stop <= time.SelectionStop)
                        {
                            linesinrangenum++;
                        }
                    }

                    if (linesinrangenum <= continuousTierLines.Count)
                    {
                        foreach (Line l in continuousTierLines)
                        {
                            l.Visibility = Visibility.Hidden;
                        }

                        int i = 0;
                        foreach (AnnoListItem ali in AnnoList)
                        {
                            if (ali.Start >= time.SelectionStart && ali.Stop < time.SelectionStop)
                            {
                                continuousTierLines[i % continuousTierLines.Count].X1 = MainHandler.Time.PixelFromTime(ali.Start);
                                if (i % continuousTierLines.Count < continuousTierLines.Count - 1 && ali.Stop < time.SelectionStop - ali.Duration) continuousTierLines[i % continuousTierLines.Count].X2 = continuousTierLines[i % continuousTierLines.Count + 1].X1;
                                else continuousTierLines[i % continuousTierLines.Count].X2 = MainHandler.Time.PixelFromTime(ali.Stop);

                                if (!double.IsNaN(ali.Score))
                                {
                                    double range = AnnoList.Scheme.MaxScore - AnnoList.Scheme.MinScore;
                                    double value = 1.0 - (ali.Score - AnnoList.Scheme.MinScore) / range;

                                    continuousTierLines[i % continuousTierLines.Count].Y1 = (value) * this.ActualHeight;

                                    if (i % continuousTierLines.Count < continuousTierLines.Count - 1 && ali.Stop < time.SelectionStop - ali.Duration)
                                    {
                                        continuousTierLines[i % continuousTierLines.Count].Y2 = continuousTierLines[i % continuousTierLines.Count + 1].Y1;
                                    }
                                    else if (i > 0)
                                    {
                                        continuousTierLines[i % continuousTierLines.Count].Y2 = continuousTierLines[i % continuousTierLines.Count - 1].Y1;
                                    }

                                    continuousTierLines[i % continuousTierLines.Count].Visibility = Visibility.Visible;

                                    if(ali.Confidence < Properties.Settings.Default.UncertaintyLevel)
                                    {
                                        continuousTierLines[i % continuousTierLines.Count].Stroke = invertBrush;
                                    }
                                    else
                                    {
                                        continuousTierLines[i % continuousTierLines.Count].Stroke = Brushes.Black;
                                    }
                                }
                                else
                                {
                                    if (i > 0)
                                    {
                                        continuousTierLines[i % continuousTierLines.Count].Y1 = continuousTierLines[i % continuousTierLines.Count - 1].Y1;
                                        continuousTierLines[i % continuousTierLines.Count].Y2 = continuousTierLines[i % continuousTierLines.Count].Y1;
                                    }
                                }

                                i++;
                            }
                        }
                    }
                    else
                    {
                        int i = 0;
                        int index = 0;
                         double value = 0.5;
                        foreach (Line s in continuousTierLines)
                        {
                            s.Visibility = Visibility.Collapsed;

                            index = (int)((double)AnnoList.Count / (double)continuousTierLines.Count * (double)i + 0.5f);
                            if (index > AnnoList.Count) index = AnnoList.Count - 1;

                            if (AnnoList[index].Start >= time.SelectionStart && AnnoList[index].Stop <= time.SelectionStop)
                            {
                                int offset = (int)((double)AnnoList.Count / (double)continuousTierLines.Count + 0.5f);
                                s.X1 = MainHandler.Time.PixelFromTime(AnnoList[index].Start);
                                if (i < continuousTierLines.Count - 1 && AnnoList[index + offset].Stop <= time.SelectionStop) s.X2 = continuousTierLines[i + 1].X1;
                                else s.X2 = MainHandler.Time.PixelFromTime(AnnoList[index].Start);

                                double mean = 0;

                                double range = AnnoList.Scheme.MaxScore - AnnoList.Scheme.MinScore;
                                if (index >= 0)
                                {

                                    //for (int k = index - offset; k < index + offset; k++)
                                    //{
                                    //    if (!double.IsNaN(AnnoList[k].Score))
                                    //    {
                                    //        double value = 1.0 - (AnnoList[k].Score - AnnoList.Scheme.MinScore) / range;
                                    //        mean = mean + value;
                                    //        s.Visibility = Visibility.Visible;
                                    //    }
                                    //}
                                    //mean = mean / (2 * offset);

                                   

                                    if (!double.IsNaN(AnnoList[index].Score))
                                    {

                                        value = 1.0 - (AnnoList[index].Score - AnnoList.Scheme.MinScore) / range;
                                        s.Y1 = (value) * this.ActualHeight;
                                        s.Visibility = Visibility.Visible;

                                        if (AnnoList[index].Confidence < Properties.Settings.Default.UncertaintyLevel)
                                        {

                                            s.Stroke = invertBrush;
                                        }
                                        else
                                        {
                                            s.Stroke = Brushes.Black;
                                        }

                                    }

                                    else
                                    {
                                        
                                        s.Y1 = (value) * this.ActualHeight;
                                        s.Visibility = Visibility.Collapsed;

                                    }
                         
                                    
                                }
                                else
                                {
                                    if (!double.IsNaN(AnnoList[index].Score))
                                    {
                                        value = 1.0 - (AnnoList[index].Score - AnnoList.Scheme.MinScore) / range;
                                        s.Y1 = (value) * this.ActualHeight;
                                        s.Visibility = Visibility.Visible;
                                    }
                                    if (continuousTierLines.IndexOf(s) < continuousTierLines.Count - 1)
                                    {
                                        s.Y1 = continuousTierLines[continuousTierLines.IndexOf(s) + 1].Y1;
                                        s.Y2 = s.Y1;
                                        continuousTierLines[continuousTierLines.IndexOf(s)].Visibility = Visibility.Collapsed;
                                    }
                                }

                                if (i < continuousTierLines.Count - 1 && AnnoList[index + offset].Stop <= time.SelectionStop) s.Y2 = continuousTierLines[i + 1].Y1;
                                else s.Y2 = s.Y1;
                            }
                            i++;
                        }
                    }
                }
            }
        }
    }
}
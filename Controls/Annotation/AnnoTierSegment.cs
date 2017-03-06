using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    public class AnnoTierSegment : TextBlock
    {
        public const int RESIZE_OFFSET = 5;
        public const int MIN_WIDTH = 2;
        public const string CONFBRUSH = "HatchBrush";
        private PatternBrushes patternBrushes = new PatternBrushes();

        public bool isSelected;
        public bool isResizeableRight;
        public bool isResizeableLeft;
        public bool isMoveable;

        private AnnoListItem item = null;

        public AnnoListItem Item
        {
            get { return item; }
        }

        private AnnoTier tier = null;

        public AnnoTier Tier
        {
            get { return tier; }
        }

        public AnnoTierSegment(AnnoListItem item, AnnoTier tier)
        {
            this.tier = tier;
            this.item = item;

            this.isSelected = false;
            this.isResizeableLeft = false;
            this.isResizeableRight = false;
            this.isMoveable = false;

            this.Inlines.Add(item.Label);
            this.FontSize = 12;
            this.TextWrapping = TextWrapping.Wrap;
            if (item.Color != null)
            {
                Background = new SolidColorBrush(item.Color);
            }
            this.Foreground = Brushes.White;
            this.Opacity = 0.75;

            this.TextAlignment = TextAlignment.Center;
            this.TextTrimming = TextTrimming.WordEllipsis;

            ToolTip tt = new ToolTip();
            tt.Background = Brushes.Black;
            tt.Foreground = Brushes.White;
            if (item.Meta == null || item.Meta.ToString() == "")
            {
                tt.Content = item.Label;
            }
            else
            {
                tt.Content = item.Label + "\n\n" + item.Meta.Replace(";", "\n");
            }
            tt.StaysOpen = true;
            this.ToolTip = tt;

            item.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);

            update();
        }        

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            update();
        }

        public void rename(string label)
        {
            this.item.Label = label;
            this.Text = label;
            update();
        }

        public void move(double pixel)
        {
            double time = MainHandler.Time.TimeFromPixel(MainHandler.Time.PixelFromTime(this.item.Start) + pixel);
            this.item.Start = time;
            time = MainHandler.Time.TimeFromPixel(MainHandler.Time.PixelFromTime(this.item.Stop) + pixel);
            this.item.Stop = time;
            update();
        }

        public void resize_left(double pixel)
        {
            double time = MainHandler.Time.TimeFromPixel(MainHandler.Time.PixelFromTime(this.item.Start) + pixel);
            this.item.Start = time;
            update();

            //if (this != null && this.Item.Duration < Properties.Settings.Default.DefaultMinSegmentSize)
            //{
            //    this.Item.Duration = Properties.Settings.Default.DefaultMinSegmentSize;
            //    this.Item.Stop = AnnoTierStatic.Label.Item.Start + Properties.Settings.Default.DefaultMinSegmentSize;
            //}
        }

        public void resize_right(double pixel)
        {
            double time = MainHandler.Time.TimeFromPixel(MainHandler.Time.PixelFromTime(this.item.Stop) + pixel);
            this.item.Stop = time;
            update();

            //if (this != null && this.Item.Duration < Properties.Settings.Default.DefaultMinSegmentSize)
            //{
            //    this.Item.Duration = Properties.Settings.Default.DefaultMinSegmentSize;
            //    this.Item.Stop = AnnoTierStatic.Label.Item.Start + Properties.Settings.Default.DefaultMinSegmentSize;
            //}
        }

        public void update()
        {
            double start = MainHandler.Time.PixelFromTime(item.Start);
            double stop = MainHandler.Time.PixelFromTime(item.Start + item.Duration);
            double len = Math.Max(MIN_WIDTH, stop - start);

            this.Text = item.Label;

            if (len >= 0 && start >= 0)
            {
                this.Height = tier.ActualHeight;
                this.Width = len;
                this.Background = new SolidColorBrush(item.Color);
                if (item.Confidence < Properties.Settings.Default.UncertaintyLevel)
                {
                    VisualBrush vb = (System.Windows.Media.VisualBrush)patternBrushes.Resources[CONFBRUSH];
                    this.Background = vb;
                }

                Canvas.SetLeft(this, start);
            }
        }

        public void update2()
        {
            double start = MainHandler.Time.PixelFromTime(MainHandler.Time.SelectionStart);
            double stop = MainHandler.Time.PixelFromTime(item.Stop);
            double len = Math.Max(MIN_WIDTH, stop - start);

            this.Text = item.Label;

            if (len >= 0 && start >= 0)
            {
                this.Height = tier.ActualHeight;
                this.Width = len;
                this.Background = new SolidColorBrush(item.Color);
                if (item.Confidence < Properties.Settings.Default.UncertaintyLevel)
                {
                    var res = new PatternBrushes();
                    VisualBrush vb = (System.Windows.Media.VisualBrush)res.Resources[CONFBRUSH];
                    this.Background = vb;
                }
                Canvas.SetLeft(this, start);
            }
        }

        public void select(bool flag)
        {
            this.isSelected = flag;
            if (flag)
            {
                this.Background = SystemColors.HotTrackBrush;


            }
            else
            {
                this.Background = new SolidColorBrush(item.Color);
                if (item.Confidence < Properties.Settings.Default.UncertaintyLevel)
                {
                    var res = new PatternBrushes();
                    VisualBrush vb = (System.Windows.Media.VisualBrush)res.Resources[CONFBRUSH];
                    this.Background = vb;
                }
                //this.Opacity = 0.75;
            }
        }

        public bool isResizableOrMovable()
        {
            return this.isResizeableLeft || this.isResizeableRight || this.isMoveable;
        }

        public void resizeableLeft(bool flag)
        {
            this.isResizeableLeft = flag;
            if (flag)
            {
                this.Cursor = Cursors.SizeWE;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        public void resizeableRight(bool flag)
        {
            this.isResizeableRight = flag;
            if (flag)
            {
                this.Cursor = Cursors.SizeWE;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        public void movable(bool flag)
        {
            this.isMoveable = flag;
            if (flag)
            {
                this.Cursor = Cursors.SizeAll;
            }
            else
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        public void checkResizeable(Point point)
        {

            if (point.X > this.ActualWidth - RESIZE_OFFSET && point.X < this.ActualWidth)
            {
                if (this.isResizeableRight == false)
                {
                    this.resizeableRight(true);
                }
            }

            else if (point.X > 0 && point.X < RESIZE_OFFSET)
            {
                if (this.isResizeableLeft == false)
                {
                    this.resizeableLeft(true);
                }
            }
          
            else if (point.X > this.ActualWidth / 2 - RESIZE_OFFSET && point.X < this.ActualWidth / 2 + RESIZE_OFFSET)
            {
                if (this.isMoveable == false)
                {
                    this.movable(true);
                }
            }
            else
            {
                  if (this.isResizeableLeft)
                {
                    this.resizeableLeft(false);
                }
                 if (this.isResizeableRight)
                {
                    this.resizeableRight(false);
                }
                 if (this.isMoveable)
                {
                    this.movable(false);
                }
            }
           
        }
    }
}
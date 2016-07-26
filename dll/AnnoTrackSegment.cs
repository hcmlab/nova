using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    public class AnnoTrackSegment : TextBlock
    {
        public const int RESIZE_OFFSET = 5;
        public const int MIN_WIDTH = 2;

        public bool is_selected;
        public bool is_resizeable_right;
        public bool is_resizeable_left;
        public bool is_moveable;

        private AnnoListItem item = null;

        public AnnoListItem Item
        {
            get { return item; }
        }

        private AnnoTrack track = null;

        public AnnoTrack Track
        {
            get { return track; }
        }

        public AnnoTrackSegment(AnnoListItem item, AnnoTrack track)
        {
            this.track = track;
            this.item = item;

            this.is_selected = false;
            this.is_resizeable_left = false;
            this.is_resizeable_right = false;
            this.is_moveable = false;

            this.Inlines.Add(item.Label);
            this.FontSize = 12;
            this.TextWrapping = TextWrapping.Wrap;
            this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(item.Bg));
            this.Foreground = Brushes.White;
            this.Opacity = 0.75;
            this.TextAlignment = TextAlignment.Center;
            this.TextTrimming = TextTrimming.WordEllipsis;

            ToolTip tt = new ToolTip();
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

            MouseDown += new MouseButtonEventHandler(OnAnnoTrackSegmentMouseDown);

            update();
        }

        private void OnAnnoTrackSegmentMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            {
                AnnoTrackStatic.used_labels.Clear();

                foreach (AnnoListItem item in AnnoTrack.GetSelectedTrack().AnnoList)
                {
                    if (item.Label != "")
                    {
                        LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                        bool detected = false;
                        foreach (LabelColorPair p in AnnoTrackStatic.used_labels)
                        {
                            if (p.label == l.label)
                            {
                                detected = true;
                            }
                        }

                        if (detected == false) AnnoTrackStatic.used_labels.Add(l);
                    }
                }

                LabelInputBox inputBox = new LabelInputBox("Input", "Enter a new label name", this.Item.Label, AnnoTrackStatic.used_labels, 1, "", "", true);
                inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                inputBox.ShowDialog();
                inputBox.Close();
                if (inputBox.DialogResult == true)
                {
                    rename(inputBox.Result());
                    this.item.Bg = inputBox.Color();

                    AnnoTrackStatic.used_labels.Clear();
                    foreach (AnnoListItem a in track.AnnoList)
                    {
                        if (a.Label == this.item.Label) a.Bg = this.item.Bg;

                        if (a.Label != "")
                        {
                            LabelColorPair l = new LabelColorPair(a.Label, a.Bg);
                            bool detected = false;
                            foreach (LabelColorPair p in AnnoTrackStatic.used_labels)
                            {
                                if (p.label == l.label)
                                {
                                    detected = true;
                                }
                            }

                            if (detected == false) AnnoTrackStatic.used_labels.Add(l);
                        }
                    }

                    track.track_used_labels = AnnoTrackStatic.used_labels;
                }
            }
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
            double time = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.PixelFromTime(this.item.Start) + pixel);
            this.item.Start = time;
            time = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.PixelFromTime(this.item.Stop) + pixel);
            this.item.Stop = time;
            update();
        }

        public void resize_left(double pixel)
        {
            double time = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.PixelFromTime(this.item.Start) + pixel);
            this.item.Start = time;
            update();
        }

        public void resize_right(double pixel)
        {
            double time = ViewHandler.Time.TimeFromPixel(ViewHandler.Time.PixelFromTime(this.item.Stop) + pixel);
            this.item.Stop = time;
            update();
        }

        public void update()
        {
            double start = ViewHandler.Time.PixelFromTime(item.Start);
            double stop = ViewHandler.Time.PixelFromTime(item.Start + item.Duration);
            double len = Math.Max(MIN_WIDTH, stop - start);

            this.Text = item.Label;

            if (len >= 0 && start >= 0)
            {
                this.Height = track.ActualHeight;
                this.Width = len;
                this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(item.Bg));
                Canvas.SetLeft(this, start);
            }
        }

        public void select(bool flag)
        {
            this.is_selected = flag;
            if (flag)
            {
                this.Background = SystemColors.HotTrackBrush;
            }
            else
            {
                this.Background = (SolidColorBrush)(new BrushConverter().ConvertFrom(item.Bg));
                //this.Opacity = 0.75;
            }
        }

        public bool isResizableOrMovable()
        {
            return this.is_resizeable_left || this.is_resizeable_right || this.is_moveable;
        }

        public void resizeableLeft(bool flag)
        {
            this.is_resizeable_left = flag;
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
            this.is_resizeable_right = flag;
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
            this.is_moveable = flag;
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
            if (point.X > 0 && point.X < RESIZE_OFFSET)
            {
                if (this.is_resizeable_left == false)
                {
                    this.resizeableLeft(true);
                }
            }
            else if (point.X > this.ActualWidth - RESIZE_OFFSET && point.X < this.ActualWidth)
            {
                if (this.is_resizeable_right == false)
                {
                    this.resizeableRight(true);
                }
            }
            else if (point.X > this.ActualWidth / 2 - RESIZE_OFFSET && point.X < this.ActualWidth / 2 + RESIZE_OFFSET)
            {
                if (this.is_moveable == false)
                {
                    this.movable(true);
                }
            }
            else if (this.is_resizeable_left)
            {
                this.resizeableLeft(false);
            }
            else if (this.is_resizeable_right)
            {
                this.resizeableRight(false);
            }
            else if (this.is_moveable)
            {
                this.movable(false);
            }
        }
    }
}
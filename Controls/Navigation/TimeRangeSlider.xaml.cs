using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    public delegate void TimelineChanged(Timeline time);

    public partial class TimeRangeSlider : UserControl
    {
        private bool _followmedia = false;

        public bool followmedia
        {
            get { return _followmedia; }
            set { _followmedia = value; }
        }

        private double min = 1;

        public event TimelineChanged OnTimeRangeChanged;

        public TimeRangeSlider()
        {
            InitializeComponent();
                       
            slider.RangeStart = 0;
            slider.RangeStop = 100000;
            slider.RangeStartSelected = 0;
            slider.RangeStopSelected = 100000;
            slider.MinRange = 1;

            slider.MouseDoubleClick += new MouseButtonEventHandler(OnMouseDoubleClick);
            slider.PreviewMouseUp += (sender, args) => Update();
            slider.PreviewMouseMove += (sender, args) => MouseMove();
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            slider.SetSelectedRange(slider.RangeStart, slider.RangeStop);
        }

        public new void MouseMove()
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
            {
                Update();
            }
        }

        public void Update()
        {
              if (MainHandler.Time != null)
            {
               
                //seens to be a bug in avalon lib.try to fix it by adjusting the value

                if (slider.RangeStartSelected > slider.RangeStop) slider.RangeStartSelected = slider.RangeStop;
                if (slider.RangeStopSelected > slider.RangeStop) slider.RangeStopSelected = slider.RangeStop;
                if (slider.RangeStartSelected < slider.RangeStart) slider.RangeStartSelected = slider.RangeStart;
                if (slider.RangeStopSelected < slider.RangeStart) slider.RangeStopSelected = slider.RangeStart;
                if (slider.RangeStopSelected < slider.RangeStartSelected + 1)
                {
                    slider.RangeStartSelected = slider.RangeStopSelected - 1;
                }

                MainHandler.Time.SelectionStart = MainHandler.Time.TotalDuration * ((double)slider.RangeStartSelected / (double)slider.RangeStop);
                MainHandler.Time.SelectionStop = MainHandler.Time.TotalDuration * ((double)slider.RangeStopSelected / (double)slider.RangeStop);

                if (MainHandler.Time.SelectionStart < 0) MainHandler.Time.SelectionStart = 0;
                if (MainHandler.Time.SelectionStop - MainHandler.Time.SelectionStart < min)
                {
                    MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + min;
                }

                if (followmedia)
                {
                    if (MainHandler.Time.SelectionStop < MainHandler.Time.CurrentPlayPosition)
                    {
                        if (MainHandler.Time.TotalDuration > 0)
                        {
                            slider.RangeStartSelected = ((long)MainHandler.Time.SelectionStart * slider.RangeStop) / (long)MainHandler.Time.TotalDuration;
                            slider.RangeStopSelected = ((long)MainHandler.Time.SelectionStop * slider.RangeStop) / (long)MainHandler.Time.TotalDuration;
                        }
                    }
                }

                if (OnTimeRangeChanged != null)
                {
                    OnTimeRangeChanged(MainHandler.Time);
                    OnTimeRangeChanged(MainHandler.Time);
                }
            }
        }

        public void UpdateFixedRange(double duration)
        {
            if (MainHandler.Time != null)
            {
                MainHandler.Time.SelectionStart = 0;
                slider.RangeStartSelected = 0;
                //seems to be a bug in avalon lib.try to fix it by adjusting the value

                MainHandler.Time.SelectionStart = MainHandler.Time.TotalDuration * ((double)slider.RangeStartSelected / (double)slider.RangeStop);
                MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + duration;
                slider.RangeStopSelected = ((long)MainHandler.Time.SelectionStop * slider.RangeStop) / (long)MainHandler.Time.TotalDuration;

                if (MainHandler.Time.SelectionStart < 0) MainHandler.Time.SelectionStart = 0;
                if (MainHandler.Time.SelectionStop - MainHandler.Time.SelectionStart < min)
                {
                    MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + min;
                }

                if (followmedia)
                {
                    if (MainHandler.Time.SelectionStop < MainHandler.Time.CurrentPlayPosition)
                    {
                        if (MainHandler.Time.TotalDuration > 0)
                        {
                            slider.RangeStartSelected = ((long)MainHandler.Time.SelectionStart * slider.RangeStop) / (long)MainHandler.Time.TotalDuration;
                            slider.RangeStopSelected = ((long)MainHandler.Time.SelectionStop * slider.RangeStop) / (long)MainHandler.Time.TotalDuration;
                        }
                    }
                }

                if (OnTimeRangeChanged != null)
                {
                    OnTimeRangeChanged(MainHandler.Time);
                    OnTimeRangeChanged(MainHandler.Time);
                }
            }
        }

        public void MoveAndUpdate(bool moveRight, double moveWindowPercentage)
        {
            if (MainHandler.Time == null)
            {
                return;
            }

            var range = slider.RangeStopSelected - slider.RangeStartSelected;

            double step = range * moveWindowPercentage + 0.5;
            var moveStep = (long)Math.Max(1, step);

            if (moveRight)
            {
                var effectiveStep = Math.Min(moveStep, slider.RangeStop - slider.RangeStopSelected); // do not change selection range on border conditions

                slider.RangeStopSelected += effectiveStep;
                slider.RangeStartSelected += effectiveStep;
            }
            else
            {
                var effectiveStep = Math.Min(moveStep, slider.RangeStartSelected - slider.RangeStart); // do not change selection range on border conditions

                slider.RangeStartSelected -= effectiveStep;
                slider.RangeStopSelected -= effectiveStep;
            }

            Update();
        }
    }
}
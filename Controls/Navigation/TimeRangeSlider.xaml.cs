using System;
using System.Windows.Controls;
using System.Windows.Input;

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

            ui.RangeStart = 0;
            ui.RangeStop = 100000;
            ui.RangeStartSelected = 0;
            ui.RangeStopSelected = 100000;
            ui.MinRange = 1;

            ui.MouseDoubleClick += new System.Windows.Input.MouseButtonEventHandler(OnMouseDoubleClick);
            ui.PreviewMouseUp += (sender, args) => Update();
            ui.PreviewMouseMove += (sender, args) => MouseMove();
        }

        private void OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ui.SetSelectedRange(ui.RangeStart, ui.RangeStop);
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

                if (ui.RangeStartSelected > ui.RangeStop) ui.RangeStartSelected = ui.RangeStop;
                if (ui.RangeStopSelected > ui.RangeStop) ui.RangeStopSelected = ui.RangeStop;
                if (ui.RangeStartSelected < ui.RangeStart) ui.RangeStartSelected = ui.RangeStart;
                if (ui.RangeStopSelected < ui.RangeStart) ui.RangeStopSelected = ui.RangeStart;
                if (ui.RangeStopSelected < ui.RangeStartSelected + 1)
                {
                    ui.RangeStartSelected = ui.RangeStopSelected - 1;
                }

                MainHandler.Time.SelectionStart = MainHandler.Time.TotalDuration * ((double)ui.RangeStartSelected / (double)ui.RangeStop);
                MainHandler.Time.SelectionStop = MainHandler.Time.TotalDuration * ((double)ui.RangeStopSelected / (double)ui.RangeStop);

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
                            ui.RangeStartSelected = ((long)MainHandler.Time.SelectionStart * ui.RangeStop) / (long)MainHandler.Time.TotalDuration;
                            ui.RangeStopSelected = ((long)MainHandler.Time.SelectionStop * ui.RangeStop) / (long)MainHandler.Time.TotalDuration;
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
                ui.RangeStartSelected = 0;
                //seems to be a bug in avalon lib.try to fix it by adjusting the value

                MainHandler.Time.SelectionStart = MainHandler.Time.TotalDuration * ((double)ui.RangeStartSelected / (double)ui.RangeStop);
                MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + duration;
                ui.RangeStopSelected = ((long)MainHandler.Time.SelectionStop * ui.RangeStop) / (long)MainHandler.Time.TotalDuration;

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
                            ui.RangeStartSelected = ((long)MainHandler.Time.SelectionStart * ui.RangeStop) / (long)MainHandler.Time.TotalDuration;
                            ui.RangeStopSelected = ((long)MainHandler.Time.SelectionStop * ui.RangeStop) / (long)MainHandler.Time.TotalDuration;
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

            var range = ui.RangeStopSelected - ui.RangeStartSelected;

            double step = range * moveWindowPercentage + 0.5;
            var moveStep = (long)Math.Max(1, step);

            if (moveRight)
            {
                var effectiveStep = Math.Min(moveStep, ui.RangeStop - ui.RangeStopSelected); // do not change selection range on border conditions

                ui.RangeStopSelected += effectiveStep;
                ui.RangeStartSelected += effectiveStep;
            }
            else
            {
                var effectiveStep = Math.Min(moveStep, ui.RangeStartSelected - ui.RangeStart); // do not change selection range on border conditions

                ui.RangeStartSelected -= effectiveStep;
                ui.RangeStopSelected -= effectiveStep;
            }

            Update();
        }
    }
}
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    public delegate void TimelineChanged(Timeline time);

    public partial class TimeRangeSlider : UserControl
    {
        private Timeline _viewTime = null;
        private bool _followmedia = false;

        public Timeline Timeline
        {
            get { return _viewTime; }
            set { _viewTime = value; }
        }

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
            if (_viewTime != null)
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

                _viewTime.SelectionStart = _viewTime.TotalDuration * ((double)ui.RangeStartSelected / (double)ui.RangeStop);
                _viewTime.SelectionStop = _viewTime.TotalDuration * ((double)ui.RangeStopSelected / (double)ui.RangeStop);

                if (_viewTime.SelectionStart < 0) _viewTime.SelectionStart = 0;
                if (_viewTime.SelectionStop - _viewTime.SelectionStart < min)
                {
                    _viewTime.SelectionStop = _viewTime.SelectionStart + min;
                }

                if (followmedia)
                {
                    if (_viewTime.SelectionStop < Timeline.CurrentPlayPosition)
                    {
                        if (_viewTime.TotalDuration > 0)
                        {
                            ui.RangeStartSelected = ((long)_viewTime.SelectionStart * ui.RangeStop) / (long)_viewTime.TotalDuration;
                            ui.RangeStopSelected = ((long)_viewTime.SelectionStop * ui.RangeStop) / (long)_viewTime.TotalDuration;
                        }
                    }
                }

                if (OnTimeRangeChanged != null)
                {
                    OnTimeRangeChanged(_viewTime);
                    OnTimeRangeChanged(_viewTime);
                }
            }
        }

        public void UpdateFixedRange(double duration)
        {
            if (_viewTime != null)
            {
                _viewTime.SelectionStart = 0;
                ui.RangeStartSelected = 0;
                //seems to be a bug in avalon lib.try to fix it by adjusting the value

                _viewTime.SelectionStart = _viewTime.TotalDuration * ((double)ui.RangeStartSelected / (double)ui.RangeStop);
                _viewTime.SelectionStop = _viewTime.SelectionStart + duration;
                ui.RangeStopSelected = ((long)_viewTime.SelectionStop * ui.RangeStop) / (long)_viewTime.TotalDuration;

                if (_viewTime.SelectionStart < 0) _viewTime.SelectionStart = 0;
                if (_viewTime.SelectionStop - _viewTime.SelectionStart < min)
                {
                    _viewTime.SelectionStop = _viewTime.SelectionStart + min;
                }

                if (followmedia)
                {
                    if (_viewTime.SelectionStop < Timeline.CurrentPlayPosition)
                    {
                        if (_viewTime.TotalDuration > 0)
                        {
                            ui.RangeStartSelected = ((long)_viewTime.SelectionStart * ui.RangeStop) / (long)_viewTime.TotalDuration;
                            ui.RangeStopSelected = ((long)_viewTime.SelectionStop * ui.RangeStop) / (long)_viewTime.TotalDuration;
                        }
                    }
                }

                if (OnTimeRangeChanged != null)
                {
                    OnTimeRangeChanged(_viewTime);
                    OnTimeRangeChanged(_viewTime);
                }
            }
        }

        public void MoveAndUpdate(bool moveRight, double moveWindowPercentage)
        {
            if (_viewTime == null)
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
using System;
using System.Windows.Controls;

namespace ssi
{
    public delegate void TimeRangeChanged(ViewTime time);

    public partial class TimeRangeSlider : UserControl
    {
        private ViewTime _viewTime = null;

        public ViewTime ViewTime
        {
            get { return _viewTime; }
            set { _viewTime = value; }
        }

        public event TimeRangeChanged OnTimeRangeChanged;

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
            ui.PreviewMouseDown += (sender, args) => Update();
            ui.PreviewMouseMove += (sender, args) => Update();

            //OnTimeRangeChanged += time =>
            //{
            //if (_viewTime != null && ui != null) {
            // ui.RangeStop = (long)_viewTime.TotalDuration;
            //   ui.RangeStopSelected = (long)_viewTime.TotalDuration;
            //   ui.MinRange = Math.Max(2l, (long)_viewTime.TotalDuration / 1000l);
            // }
            // };
        }

        private void OnMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ui.SetSelectedRange(ui.RangeStart, ui.RangeStop);
        }

        public void Update()
        {
            if (_viewTime != null)
            {
                _viewTime.SelectionStart = _viewTime.TotalDuration * ((double)ui.RangeStartSelected / (double)ui.RangeStop);
                _viewTime.SelectionStop = _viewTime.TotalDuration * ((double)ui.RangeStopSelected / (double)ui.RangeStop);

                if (_viewTime.SelectionStop - _viewTime.SelectionStart < 1)
                {
                    _viewTime.SelectionStop = _viewTime.SelectionStart + 1;
                }

                if (OnTimeRangeChanged != null)
                {
                    OnTimeRangeChanged(_viewTime);
                    OnTimeRangeChanged(_viewTime);
                }
            }
        }

        public void MoveAndUpdate(bool moveRight, float moveWindowPercentage)
        {
            if (_viewTime == null)
            {
                return;
            }

            var range = ui.RangeStopSelected - ui.RangeStartSelected;
            var moveStep = (long)Math.Max(1, range * moveWindowPercentage + 0.5f);

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
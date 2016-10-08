using System;
using System.Windows.Controls;

namespace ssi
{
    public delegate void TimeRangeChanged(ViewTime time);

    public partial class TimeRangeSlider : UserControl
    {
        private ViewTime _viewTime = null;
        private bool _followmedia = false;

        public ViewTime ViewTime
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
                if (ui.RangeStartSelected > ui.RangeStop) ui.RangeStartSelected = ui.RangeStop;
                if (ui.RangeStopSelected > ui.RangeStop) ui.RangeStopSelected = ui.RangeStop;
                if (ui.RangeStartSelected < ui.RangeStart) ui.RangeStartSelected = ui.RangeStart;
                if (ui.RangeStopSelected < ui.RangeStart) ui.RangeStopSelected = ui.RangeStart;

                if (ui.RangeStopSelected < ui.RangeStartSelected)
                {
                    long temp;
                    temp = ui.RangeStopSelected;
                    ui.RangeStopSelected = ui.RangeStartSelected;
                    ui.RangeStartSelected = ui.RangeStopSelected;
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
                    if (_viewTime.SelectionStop < ViewTime.CurrentPlayPosition)
                    {
                        _viewTime.SelectionStop = ViewTime.CurrentPlayPosition;
                        _viewTime.SelectionStart = _viewTime.SelectionStop - min;

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

        public void MoveAndUpdate(bool moveRight, float moveWindowPercentage)
        {
            if (_viewTime == null)
            {
                return;
            }

            //if (ui.RangeStartSelected > ui.RangeStop) ui.RangeStartSelected = ui.RangeStop;
            //if (ui.RangeStopSelected > ui.RangeStop) ui.RangeStopSelected = ui.RangeStop;
            //if (ui.RangeStartSelected < ui.RangeStart) ui.RangeStartSelected = ui.RangeStart;
            //if (ui.RangeStopSelected < ui.RangeStart) ui.RangeStopSelected = ui.RangeStart;
            //if (ui.RangeStopSelected < ui.RangeStartSelected)
            //{
            //    long temp;
            //    temp = ui.RangeStopSelected;
            //    ui.RangeStopSelected = ui.RangeStartSelected;
            //    ui.RangeStartSelected = ui.RangeStopSelected;
            //}

            var range = ui.RangeStopSelected - ui.RangeStartSelected;

            float step = range * moveWindowPercentage + 0.5f;
            var moveStep = (long)Math.Max(1, step);
            Console.WriteLine(moveStep);

            if (moveRight)
            {
                var effectiveStep = Math.Min(moveStep, ui.RangeStop - ui.RangeStopSelected); // do not change selection range on border conditions
                Console.WriteLine(effectiveStep);

                //  if (effectiveStep > ui.RangeStop) effectiveStep = ui.RangeStop;

                ui.RangeStopSelected += effectiveStep;
                ui.RangeStartSelected += effectiveStep;
                Console.WriteLine(ui.RangeStopSelected + "  " + ui.RangeStartSelected);
            }
            else
            {
                var effectiveStep = Math.Min(moveStep, ui.RangeStartSelected - ui.RangeStart); // do not change selection range on border conditions
                                                                                               //   if (effectiveStep < 10) effectiveStep = 10;
                ui.RangeStartSelected -= effectiveStep;
                ui.RangeStopSelected -= effectiveStep;
            }

            Update();
        }
    }
}
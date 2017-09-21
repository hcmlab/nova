using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading;


namespace ssi
{
    enum MOUSEPOSTION
    {
        LEFT,
        RIGHT,
        CENTER
    }

    public delegate void TimelineChanged(Timeline time);

    public partial class TimeRangeSlider : UserControl
    {

        private static Mutex mut = new Mutex();

        public event TimelineChanged OnTimeRangeChanged;

        private bool _followmedia = false;


        public bool followmedia
        {
            get { return _followmedia; }
            set { _followmedia = value; }
        }

        private double minRangeInPixel = 1;
        private double min = 1;

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
                if (Keyboard.IsKeyDown(Key.LeftShift))
                {
                    UpdateProportional();
                }
                else
                {
                    Update();
                }
            }
        }

        public void Update()
        {

            if (MainHandler.Time != null)
            {

    

                MainHandler.Time.SelectionStart = MainHandler.Time.TotalDuration * ((double)slider.RangeStartSelected / (double)slider.RangeStop);
                MainHandler.Time.SelectionStop = MainHandler.Time.TotalDuration * ((double)slider.RangeStopSelected / (double)slider.RangeStop);



                //Fix slider < 0 and range < 1second
                if (MainHandler.Time.SelectionStart < 0) MainHandler.Time.SelectionStart = 0;
                if (MainHandler.Time.SelectionStop - MainHandler.Time.SelectionStart < min) MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + min;
         


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

        public void UpdateProportional()
        {

            if (MainHandler.Time != null)
            {

               

                //Sanity to check that slider ins in min / max range
                long newSliderStart = slider.RangeStartSelected < 0 ? 0 : slider.RangeStartSelected;
                long newSliderStop = slider.RangeStopSelected > slider.RangeStop ? slider.RangeStop : slider.RangeStopSelected;

                double originalStartTime = MainHandler.Time.SelectionStart;
                double originalStopTime = MainHandler.Time.SelectionStop;
                double newStartTime = MainHandler.Time.TotalDuration * ((double)newSliderStart / (double)slider.RangeStop);
                double newStopTime = MainHandler.Time.TotalDuration * ((double)newSliderStop / (double)slider.RangeStop);


                if (newStopTime > newStartTime)
                {
                    bool startTimeChanged = originalStartTime != newStartTime;
                    bool stopTimeChanged = originalStopTime != newStopTime;


                    Thumb sliderMid = ((Thumb)slider.Template.FindName("PART_MiddleThumb", slider));
                    MOUSEPOSTION mp;
                    if (Mouse.GetPosition(sliderMid).X <= sliderMid.ActualWidth * 0.10)
                    {
                        mp = MOUSEPOSTION.LEFT;
                    }
                    else if (Mouse.GetPosition(sliderMid).X > sliderMid.ActualWidth * 0.90)
                    {
                        mp = MOUSEPOSTION.RIGHT;
                    }
                    else
                    {
                        mp = MOUSEPOSTION.CENTER;
                    }

                    if (mp != MOUSEPOSTION.CENTER)
                    {

                        bool mediaPositionInBetween = MainHandler.Time.CurrentPlayPosition > originalStartTime && MainHandler.Time.CurrentPlayPosition < originalStopTime;
                        double proportion = mediaPositionInBetween ? Math.Abs((MainHandler.Time.CurrentPlayPosition - originalStartTime) / (originalStopTime - MainHandler.Time.CurrentPlayPosition)) : 1.0;

                        bool zoomOut = (originalStopTime - originalStartTime) <= (newStopTime - newStartTime);


                        //Forcing linear proportion until the mediacurser is 20% into the view
                        if (zoomOut && ( proportion < 0.2 || proportion > 2))
                        {
                            proportion = 1; // Math.Pow(proportion/100, 1.0/3.9); //
                        }


                        if (startTimeChanged && mp == MOUSEPOSTION.LEFT)
                        {
                            double distance = (originalStartTime - newStartTime) / proportion;
                            newStopTime = originalStopTime + distance;
                        }
                        else if (stopTimeChanged && mp == MOUSEPOSTION.RIGHT)
                        {
                            double distance = (originalStopTime - newStopTime) * proportion;
                            newStartTime = originalStartTime + distance;
                        }

                        //sanity checks
                        if (newStartTime < 0.0)
                            newStartTime = 0;
                        if (newStartTime > newStopTime)
                            newStopTime = newStartTime;
                        if (newStopTime > MainHandler.Time.TotalDuration)
                            newStopTime = MainHandler.Time.TotalDuration;
                    }

                    if (newStopTime - newStartTime > 1)
                    {

                        newSliderStop = (long)(((newStopTime) / MainHandler.Time.TotalDuration) * slider.RangeStop);
                        newSliderStart = (long)(((newStartTime) / MainHandler.Time.TotalDuration) * slider.RangeStop);

                        slider.RangeStartSelected = newSliderStart;
                        slider.RangeStopSelected = newSliderStop;


                        MainHandler.Time.SelectionStart = newStartTime;
                        MainHandler.Time.SelectionStop = newStopTime;

                        if (OnTimeRangeChanged != null)
                        {
                            OnTimeRangeChanged(MainHandler.Time);
                            OnTimeRangeChanged(MainHandler.Time);
                        }
                    }      
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

                MainHandler.Time.SelectionStart = MainHandler.Time.TotalDuration * (slider.RangeStartSelected / slider.RangeStop);
                MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + duration;

                double stop = MainHandler.Time.SelectionStop * slider.RangeStop;
                double dur = MainHandler.Time.TotalDuration;
                if (dur > 0)
                {
                    slider.RangeStopSelected = (uint)Math.Round(stop / dur);
                }

                if (MainHandler.Time.SelectionStart < 0) MainHandler.Time.SelectionStart = 0;
                if (MainHandler.Time.SelectionStop - MainHandler.Time.SelectionStart < 1)
                {
                    MainHandler.Time.SelectionStop = MainHandler.Time.SelectionStart + 1;
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
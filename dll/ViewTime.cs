using System;

namespace ssi
{
    public class ViewTime
    {
        private double totalDuration = 0;
        private double currentPlayPosition = 0;
        private double currentSelectPosition = 0;
        private double currentPlayPositionPrecise = 0;

        public double TotalDuration
        {
            get { return totalDuration; }
            set { totalDuration = value; }
        }

        public double CurrentPlayPosition
        {
            get { return currentPlayPosition; }
            set { currentPlayPosition = value; }
        }

        public double CurrentPlayPositionPrecise
        {
            get { return currentPlayPositionPrecise; }
            set { currentPlayPositionPrecise = value; }
        }

        public double CurrentSelectPosition
        {
            get { return currentSelectPosition; }
            set { currentSelectPosition = value; }
        }

        private double selectionStart = 0;

        public double SelectionStart
        {
            get { return selectionStart; }
            set { selectionStart = value; }
        }

        private double selectionStop = 0;

        public double SelectionStop
        {
            get { return selectionStop; }
            set { selectionStop = value; }
        }

        private double selectionInPixel = 0;

        public double SelectionInPixel
        {
            get { return selectionInPixel; }
            set { selectionInPixel = value; }
        }

        public double TimeFromPixel(double pixel)
        {
            if (pixel > SelectionInPixel)
            {
                return totalDuration;
            }
            else if (pixel > 0)
            {
                return selectionStart + (pixel / selectionInPixel) * (selectionStop - selectionStart);
            }
            else
            {
                return 0;
            }
        }

        public double PixelFromTime(double time)
        {
            if (SelectionStop < SelectionStart + 1)
            {
                SelectionStart = SelectionStop - 1;
            }

            if (time > selectionStop)
            {
                return SelectionInPixel;
            }
            if (time >= selectionStart)
            {
                return ((time - selectionStart) / (selectionStop - selectionStart)) * selectionInPixel;
            }
            else if (time < selectionStart)
            {
                double range = 1.0;
                if (selectionStop - selectionStart < 1.0) range = 1.0;
                else range = selectionStop - selectionStart;

                double range2 = 1.0;
                if (((selectionStart - time) < 1.0)) range2 = 1.0;
                else range2 = selectionStart - time;

                return (range2 / range) * selectionInPixel;
            }
            else
            {
                return 0;
            }
        }
    }
}
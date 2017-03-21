using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public class PointListItem : IObservableListItem
    {
        private double x;
        private double y;
        private string label;
        private double confidence;

        public double XCoord
        {
            get { return x; }
            set
            {
                x = value;
                OnPropertyChanged("X");
            }
        }

        public double YCoord
        {
            get { return y; }
            set
            {
                y = value;
                OnPropertyChanged("Y");
            }
        }

        public string Label
        {
            get { return label; }
            set
            {
                label = value;
                OnPropertyChanged("Label");
            }
        }

        public double Confidence
        {
            get { return confidence; }
            set
            {
                confidence = value;
                OnPropertyChanged("Confidence");
            }
        }

        public PointListItem(double x, double y, string label, double confidence)
        {
            this.x = x;
            this.y = y;
            this.label = label;
            this.confidence = confidence;
        }

        public class PointListItemComparer : IComparer<PointListItem>
        {
            int IComparer<PointListItem>.Compare(PointListItem a, PointListItem b)
            {


                if (a.x + a.y < b.x + b.y)
                {
                    return -1;
                }
                else if (a.x + a.y > b.x + b.y)
                {
                    return 1;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
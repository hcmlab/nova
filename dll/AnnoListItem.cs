using System;

namespace ssi
{
    public class AnnoListItem : MyListItem
    {
        private double start;
        private double duration;
        private String label;
        private String meta;
        private String tier;
        private String bg;

        public double Start
        {
            get { return start; }
            set
            {
                duration = Math.Max(0, duration + start - value);
                start = value;
                OnPropertyChanged("Start");
                OnPropertyChanged("Duration");
            }
        }

        public double Stop
        {
            get { return start + duration; }
            set
            {
                duration = Math.Max(0, value - start);
                OnPropertyChanged("Stop");
                OnPropertyChanged("Duration");
            }
        }

        public double Duration
        {
            get { return duration; }
            set
            {
                duration = Math.Max(0, value);
                OnPropertyChanged("Stop");
                OnPropertyChanged("Duration");
            }
        }

        public String Label
        {
            get { return label; }
            set
            {
                label = value;
                OnPropertyChanged("Label");
            }
        }

        public String Tier
        {
            get { return tier; }
            set
            {
                tier = value;
                OnPropertyChanged("Tier");
            }
        }

        public String Meta
        {
            get { return meta; }
            set
            {
                meta = value;
                OnPropertyChanged("Meta");
            }
        }

        public String Bg
        {
            get { return bg; }
            set
            {
                bg = value;
                OnPropertyChanged("Bg");
            }
        }

        public AnnoListItem(double _start, double _duration, String _label, String _meta = "", String _tier = "", String _bg = "#000000")
        {
            start = _start;
            duration = Math.Max(0, _duration);
            label = _label;
            meta = _meta;
            tier = _tier;
            bg = _bg;
        }
    }
}
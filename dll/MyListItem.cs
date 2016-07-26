using System;
using System.ComponentModel;

namespace ssi
{
    public class MyListItem : INotifyPropertyChanged
    {
        private Guid uid;
        private bool selected = false;

        public Guid Uid
        {
            get { return uid; }
            set
            {
                uid = value;
                OnPropertyChanged("Uid");
            }
        }

        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                OnPropertyChanged("Selected");
            }
        }

        public MyListItem()
        {
            uid = Guid.NewGuid();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
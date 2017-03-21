using ssi;
using System.ComponentModel;

namespace ssi
{
    public class IPointList : IObservableList<PointListItem>, INotifyPropertyChanged
    {
        public bool HasChanged { get; set; }

        public IPointList()
            : base(new PointListItem.PointListItemComparer())
        {
            foreach (PointListItem item in Items)
            {
                item.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            }

            HasChanged = false;
        }

        ~IPointList()
        {
            foreach (PointListItem item in Items)
            {
                item.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            }
        }

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnChildPropertyChanged(e.PropertyName);
            HasChanged = true;
        }

        override protected void ItemRemoved(PointListItem removedItem)
        {
            removedItem.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            HasChanged = true;
        }

        override protected void ItemAdded(PointListItem addedItem)
        {
            addedItem.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            HasChanged = true;
        }

        override protected void ItemReplaced(PointListItem replacedItem)
        {
            //overwrite and do anything
        }

        public event PropertyChangedEventHandler ChildPropertyChanged;

        protected void OnChildPropertyChanged(string propertyName)
        {
            if (this.ChildPropertyChanged != null)
                ChildPropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
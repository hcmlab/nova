using ssi;
using System.ComponentModel;

namespace ssi
{
    public class ISegmentationList : IObservableList<SegmentationListItem>, INotifyPropertyChanged
    {
        public bool HasChanged { get; set; }

        public ISegmentationList()
            : base(new SegmentationListItem.SegmentationListItemComparer())
        {
            foreach (SegmentationListItem item in Items)
            {
                item.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            }

            HasChanged = false;
        }

        ~ISegmentationList()
        {
            foreach (SegmentationListItem item in Items)
            {
                item.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            }
        }

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnChildPropertyChanged(e.PropertyName);
            HasChanged = true;
        }

        override protected void ItemRemoved(SegmentationListItem removedItem)
        {
            removedItem.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            HasChanged = true;
        }

        override protected void ItemAdded(SegmentationListItem addedItem)
        {
            addedItem.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            HasChanged = true;
        }

        override protected void ItemReplaced(SegmentationListItem replacedItem)
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
using System.ComponentModel;

namespace ssi
{
    public class IAnnoList : IObservableList<AnnoListItem>, INotifyPropertyChanged
    {
        public bool HasChanged { get; set; }

        public IAnnoList()
            : base(new AnnoListItem.AnnoListItemComparer())
        {
            foreach (AnnoListItem item in Items)
            {
                item.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            }

            HasChanged = false;
        }

        ~IAnnoList()
        {
            foreach (AnnoListItem item in Items)
            {
                item.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            }
        }

        private void item_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            OnChildPropertyChanged(e.PropertyName);
            HasChanged = true;
        }

        override protected void ItemRemoved(AnnoListItem removedItem)
        {
            removedItem.PropertyChanged -= new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            HasChanged = true;
        }

        override protected void ItemAdded(AnnoListItem addedItem)
        {
            addedItem.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(item_PropertyChanged);
            HasChanged = true;
        }

        override protected void ItemReplaced(AnnoListItem replacedItem)
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
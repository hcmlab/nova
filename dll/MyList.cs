using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ssi
{
    public class MyList<LISTITEM> : ObservableCollection<LISTITEM>
    {
        public MyList()
        {
            this.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SSIMUIList_CollectionChanged);
        }

        ~MyList()
        {
            this.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SSIMUIList_CollectionChanged);
        }

        private void SSIMUIList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action.Equals(System.Collections.Specialized.NotifyCollectionChangedAction.Remove))
            {
                foreach (LISTITEM a in e.OldItems)
                {
                    itemRemoved(a);
                }
            }

            if (e.Action.Equals(System.Collections.Specialized.NotifyCollectionChangedAction.Add))
            {
                foreach (LISTITEM a in e.NewItems)
                {
                    itemAdded(a);
                }
            }

            if (e.Action.Equals(System.Collections.Specialized.NotifyCollectionChangedAction.Replace))
            {
                foreach (LISTITEM a in e.NewItems)
                {
                    itemReplaced(a);
                }
            }
        }

        virtual protected void itemRemoved(LISTITEM removedItem)
        {
            //overwrite and do anything
        }

        virtual protected void itemAdded(LISTITEM addedItem)
        {
            //overwrite and do anything
        }

        virtual protected void itemReplaced(LISTITEM replacedItem)
        {
            //overwrite and do anything
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
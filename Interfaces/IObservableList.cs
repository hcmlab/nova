using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ssi
{   
    public class IObservableList<LISTITEM> : ObservableCollection<LISTITEM>
    {
        private IComparer<LISTITEM> comparer = null;

        public IObservableList()
        {
            this.CollectionChanged += new NotifyCollectionChangedEventHandler(NotifyCollectionChanged);            
        }

        public IObservableList(IComparer<LISTITEM> comparer)
            : this()
        {            
            this.comparer = comparer;
        }

        ~IObservableList()
        {
            this.CollectionChanged -= new NotifyCollectionChangedEventHandler(NotifyCollectionChanged);
        }

        public void AddSorted(LISTITEM item)
        {
            if (comparer == null)
            {
                Add(item);
            }
            else
            {
                int i = 0;
                while (i < Count && comparer.Compare(this[i], item) < 0)
                    i++;
                Insert(i, item);
            }            
        }

        private void NotifyCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action.Equals(NotifyCollectionChangedAction.Remove))
            {
                foreach (LISTITEM a in e.OldItems)
                {
                    ItemRemoved(a);
                }
            }

            if (e.Action.Equals(NotifyCollectionChangedAction.Add))
            {
                foreach (LISTITEM a in e.NewItems)
                {
                    ItemAdded(a);
                }
            }

            if (e.Action.Equals(NotifyCollectionChangedAction.Replace))
            {
                foreach (LISTITEM a in e.NewItems)
                {
                    ItemReplaced(a);
                }
            }
        }

        virtual protected void ItemRemoved(LISTITEM removedItem)
        {
            //overwrite and do anything
        }

        virtual protected void ItemAdded(LISTITEM addedItem)
        {
            //overwrite and do anything
        }

        virtual protected void ItemReplaced(LISTITEM replacedItem)
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
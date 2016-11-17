using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ssi
{   
    public class MyList<LISTITEM> : ObservableCollection<LISTITEM>
    {
        private IComparer<LISTITEM> comparer = null;

        public MyList()
        {
            this.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SSIMUIList_CollectionChanged);
        }

        public MyList(IComparer<LISTITEM> comparer)
            : this()
        {            
            this.comparer = comparer;
        }

        ~MyList()
        {
            this.CollectionChanged -= new System.Collections.Specialized.NotifyCollectionChangedEventHandler(SSIMUIList_CollectionChanged);
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
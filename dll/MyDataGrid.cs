using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Windows.Controls;
using System.Collections.ObjectModel;

namespace ssi
{
    public class MyDataGrid<MUIDATA> : DataGrid
    {
        protected ObservableCollection<MUIDATA> selectedRows;
        public ObservableCollection<MUIDATA> SelectedRows
        {
            get { return selectedRows; }
            set { selectedRows = value; 
                //todo test if is working
                foreach (MUIDATA a in value) {
                    this.SelectedItems.Add(a);
                }
            }
        }

        public MUIDATA SelectedRow
        {
            set
            {
                try
                {
                    if (value != null)
                    {
                        this.ScrollIntoView(value);
                    }
                }
                catch (Exception e)
                {
                    //Functions.log("Problems while scrolling to selected classifier!?!?!", e);
                    //ignore
                }
                finally { 
                    this.SelectedItem = value;
                }
            }
        }
        public MyDataGrid()
        {
            selectedRows = new ObservableCollection<MUIDATA>();
            this.SelectedCellsChanged += new SelectedCellsChangedEventHandler(MyDatagridControl_SelectedCellsChanged);
        }

        void MyDatagridControl_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            foreach (MUIDATA a in selectedRows)
            {
                try
                {
                    MyListItem b = (MyListItem)(Object)a;
                    b.Selected = false;
                }
                catch
                {
                    //ignore, can't cast to MuiListItem
                } 
            }
            selectedRows.Clear();
         
            foreach (DataGridCellInfo a in this.SelectedCells)
            {
                try
                {
                    if (!selectedRows.Contains((MUIDATA)a.Item))
                    {
                        selectedRows.Add((MUIDATA)a.Item);
                        try
                        {
                            MyListItem b = (MyListItem)(Object)a.Item;
                            b.Selected = true;
                        }
                        catch
                        {
                            //ignore, can't cast to MuiListItem
                        }
                    }
                }
                catch
                {
                    //ignore, selected new row insert
                }
            }

            OnSelectedRowsChanged(SelectedRows);
        }

        public event EventHandler<SelectedRowsChangedEventArgs> SelectedRowsChanged;
        protected void OnSelectedRowsChanged(ObservableCollection<MUIDATA> SelectedRowsName)
        {
            if (this.SelectedRowsChanged != null)
                SelectedRowsChanged(this, new SelectedRowsChangedEventArgs(SelectedRowsName));
        }

        public class SelectedRowsChangedEventArgs : EventArgs
        {
            private ObservableCollection<MUIDATA> selectedRowsName;

            public ObservableCollection<MUIDATA> SelectedRowsName
            {
                get { return selectedRowsName; }
                set { selectedRowsName = value; }
            }

            public SelectedRowsChangedEventArgs(ObservableCollection<MUIDATA> selectedRowsName) 
            {
                SelectedRowsName = selectedRowsName;
            }
        }

    }
}

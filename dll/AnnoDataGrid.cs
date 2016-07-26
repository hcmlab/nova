using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Windows.Controls;
using System.Collections.ObjectModel;

namespace ssi
{
    public class AnnoDataGrid : MyDataGrid<AnnoList>
    {
        public void editLabel(string label)
        {
            foreach (DataGridCellInfo a in this.SelectedCells)
            {                                   
                try
                {
                    AnnoListItem b = (AnnoListItem)(Object)a.Item;
                    b.Label = label;
                }
                catch
                {
                    //ignore, can't cast to MuiListItem
                }        
            }
        }
    }
    

}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ssi.Types.Polygon
{
    class DataGridChecker
    {

        private ListView polygonDataGrid;
        private ListView annoDataGrid;

        public DataGridChecker(MainControl control)
        {
            this.polygonDataGrid = control.polygonListControl.polygonDataGrid;
            this.annoDataGrid = control.annoListControl.annoDataGrid;
        }

        public bool polygonDGIsNotNull()
        {
            return polygonDataGrid.Items != null;
        }

        public bool polygonDGCountsZero()
        {
            return polygonDataGrid.SelectedItems.Count == 0;
        }

        public bool polygonDGCountsNotZero()
        {
            return polygonDataGrid.SelectedItems.Count != 0;
        }

        public bool polygonDGCountsOne()
        {
            return polygonDataGrid.SelectedItems.Count == 1;
        }

        public bool polygonDGCountsMoreThanOne()
        {
            return polygonDataGrid.SelectedItems.Count > 1;
        }

        public bool polygonDGContainsItems()
        {
            return polygonDataGrid.Items.Count > 0;
        }

        public bool annonDGIsNotNull()
        {
            return annoDataGrid.Items != null;
        }

        public bool annoDGCountsZero()
        {
            return annoDataGrid.SelectedItems.Count == 0;
        }

        public bool annoDGCountsNotZero()
        {
            return annoDataGrid.SelectedItems.Count != 0;
        }

        public bool annoDGCountsOne()
        {
            return annoDataGrid.SelectedItems.Count == 1;
        }

        public bool annoDGCountsMoreThanOne()
        {
            return annoDataGrid.SelectedItems.Count > 1;
        }

        public bool annoDGContainsItems()
        {
            return annoDataGrid.Items.Count > 0;
        }
        public bool annonDGIsNotNullAndCountsNotZero()
        {
            return annonDGIsNotNull() && annoDGCountsNotZero();
        }

        public bool annonDGIsNotNullAndCountsOne()
        {
            return annonDGIsNotNull() && annoDGCountsOne();
        }

        public bool annonDGIsNotNullAndCountsZero()
        {
            return annonDGIsNotNull() && annoDGCountsZero();
        }

        public bool polygonDGIsNotNullAndCountsNotZero()
        {
            return polygonDGIsNotNull() && polygonDGCountsNotZero();
        }

        public bool polygonDGIsNotNullAndCountsZero()
        {
            return polygonDGIsNotNull() && polygonDGCountsZero();
        }
        public bool polygonDGIsNotNullAndCountsOne()
        {
            return polygonDGIsNotNull() && polygonDGCountsOne();
        }

        public bool isSchemeTypePolygon()
        {
            return AnnoTierStatic.Selected != null && (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.POLYGON || 
                AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON);
        }
    }
}

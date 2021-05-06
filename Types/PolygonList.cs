using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public class PolygonList : IObservableListItem
    {
        private List<PolygonLabel> polygons;
        
        public PolygonList(List<PolygonLabel> polygons)
        {
            this.polygons = polygons;
        }

        public List<PolygonLabel> Polygons
        {
            get
            {
                List<PolygonLabel> polygonListClone = new List<PolygonLabel>();
                if (this.polygons is object)
                    foreach (PolygonLabel p in this.polygons)
                        polygonListClone.Add(new PolygonLabel(p.Polygon, p.Label, p.Color, p.ID));

                return polygonListClone;
            }

            set
            {
                this.polygons = value;
            }
        }

        public List<PolygonLabel> getRealList()
        {
            return this.polygons;
        }

        public void addPolygonLabel(PolygonLabel polygonLabel)
        {
            this.polygons.Add(polygonLabel);
        }

        public void removeLastPolygon()
        {
            this.polygons.RemoveAt(polygons.Count - 1);
        }

        public void removeAt(int pos)
        {
            this.polygons.RemoveAt(pos);
        }

        public void removeAllPolygons()
        {
            this.polygons.Clear();
        }

        public void removeExplicitPolygon(PolygonLabel pl)
        {
            for(int i = 0; i < polygons.Count; i++)
            {
                if (polygons[i].Equals(pl))
                {
                    polygons.RemoveAt(i);
                    break;
                }
            }
        }
    }
}
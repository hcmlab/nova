using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public class PolygonLabel
    {
        private List<PolygonPoint> polygon;
        private string label;

        public PolygonLabel(List<PolygonPoint> polygon, string label)
        {
            this.polygon = polygon;
            this.label = label;
            
        }        

        public List<PolygonPoint> Polygon
        {
            get 
            {
                List<PolygonPoint> polygonClone = new List<PolygonPoint>();

                foreach (PolygonPoint p in this.polygon)
                    polygonClone.Add(new PolygonPoint(p.X, p.Y));

                return polygonClone; 
            }

            set 
            { 
                this.polygon = value; 
            }
        }

        public void addPoint(PolygonPoint p)
        {
            this.polygon.Add(p);
        }

        public void removeLastPoint()
        {
            this.polygon.RemoveAt(polygon.Count - 1);
        }

        public void removeAt(int pos)
        {
            this.polygon.RemoveAt(pos);
        }

        public string Label { get => this.label; set => this.label = value; }
    }
}
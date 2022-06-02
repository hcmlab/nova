using ssi.Interfaces;
using ssi.Types.Polygon;
using System.Collections.Generic;
using System.Windows.Media;


namespace ssi
{
    public class PolygonLabel
    {
        private List<PolygonPoint> polygon;
        private string label;
        private Color color;
        private string confidence;
        private static int sID = 0;
        private readonly int id;
        private LabelInformations informations;

        public PolygonLabel(List<PolygonPoint> polygon, string label, Color color, string conf)
        {
            if (polygon is null)
                this.polygon = new List<PolygonPoint>();
            else
                this.polygon = polygon;
            
            this.label = label;
            this.color = color;
            this.Confidence = conf;

            id = sID;
            sID++;
        }

        public PolygonLabel(List<PolygonPoint> polygon, string label, Color color, string conf, LabelInformations infos)
        {
            if (polygon is null)
                this.polygon = new List<PolygonPoint>();
            else
                this.polygon = polygon;

            this.label = label;
            this.color = color;
            this.Confidence = conf;
            this.informations = infos;

            id = sID;
            sID++;
        }

        public PolygonLabel(List<PolygonPoint> polygon2, string label, Color color, string conf, int ID)
        {
            if (polygon2 is null)
                this.polygon = new List<PolygonPoint>();
            else
            {
                this.polygon = new List<PolygonPoint>();
                foreach (PolygonPoint point in polygon2)
                {
                    polygon.Add(point);
                }
            }

            this.label = label;
            this.color = color;
            this.Confidence = conf;

            id = ID;
        }

        public void addPointAfterSpecificID(double id, PolygonPoint newPoint)
        {
            List<PolygonPoint> polygonPoints = new List<PolygonPoint>();
            foreach (PolygonPoint point in this.Polygon)
            {
                polygonPoints.Add(new PolygonPoint(point.X, point.Y, point.PointID, false));
                if (point.PointID == id)
                    polygonPoints.Add(newPoint);
            }

            this.polygon = polygonPoints;
        }

        public void removePointAfterSpecificID(double id)
        {
            for(int i = 0; i < this.polygon.Count; i++)
            {
                if (this.polygon[i].PointID == id)
                {
                    this.removeAt(i + 1);
                }
            }
        }

        public List<PolygonPoint> Polygon
        {
            get 
            {
                return this.polygon;
            }
            set 
            { 
                this.polygon = value; 
            }
        }

        public List<PolygonPoint> getPolygonAsCopy()
        {
            List<PolygonPoint> copy = new List<PolygonPoint>();

            foreach(PolygonPoint point in this.polygon)
            {
                copy.Add(new PolygonPoint(point.X, point.Y));
            }

            return copy;
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

        public void removeAll()
        {
            this.polygon.Clear();
        }

        public string Label { get => this.label; set => this.label = value; }
        public Color Color { get => color; set => color = value; }

        public int ID => id;

        internal LabelInformations Informations { get => informations; set => informations = value; }
        public string Confidence { get => confidence; set => confidence = value; }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                return false;
            
            PolygonLabel pl = obj as PolygonLabel;

            if (pl is null)
                return false;

            if (pl.Label == this.label && pl.Color == this.color && pl.polygon.Count == this.polygon.Count && this.id == pl.ID)
            {
                for(int i = 0; i < this.Polygon.Count; i++)
                {
                    if (this.polygon[i].X != pl.polygon[i].X || this.polygon[i].Y != pl.polygon[i].Y)
                    {
                        return false;
                    }
                }
            }
            else
                return false;

            return true;
        }

        public override string ToString()
        {
            return label;
        }
    }
}
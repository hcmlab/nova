using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ssi
{ 
    public class AnnoScheme
    {
        public enum TYPE
        {
            DISCRETE,
            FREE,
            CONTINUOUS,
            POINT,
            POLYGON,
            DISCRETE_POLYGON,
            GRAPH,
            SEGMENTATION
        }



        public TYPE Type { get; set; }

        public class Label
        {
            public string Name { get; set; }
            public Color Color { get; set; }

            public Label(string name, Color color)
            {
                Name = name;
                Color = color;
            }
        }

        public class Attribute
        {
            public string Name { get; set; }
            public List<string> Values { get; set; }
            public AttributeTypes AttributeType { get; set; }
            public List<string> ExtraValues { get; set; }
            public AttributeTypes ExtraAttributeType { get; set; }

            public List<string> ExtraValues2 { get; set; }
            public AttributeTypes ExtraAttributeType2 { get; set; }


            public Attribute(string name, List<string> values, AttributeTypes type, List<string> xvalues = null, AttributeTypes xtype = AttributeTypes.BOOLEAN, List<string> xvalues2 = null, AttributeTypes xtype2 = AttributeTypes.BOOLEAN, string origin = null)
            {
                Name = name;
                Values = values;
                AttributeType = type;
                ExtraValues = xvalues;
                ExtraAttributeType = xtype;
                ExtraValues2 = xvalues2;
                ExtraAttributeType2 = xtype2;
                Origin = origin;
            }
            public string Origin { get; set; }
        }


        public AnnoScheme()
        {
            Type = TYPE.FREE;
            Name = "";
            Labels = new List<Label>();
            LabelAttributes = new List<Attribute>();
            MinScore = 0;
            MaxScore = 1;
            SampleRate = 1;
            MinOrBackColor = Defaults.Colors.Background;
            MaxOrForeColor = Defaults.Colors.Foreground;
            DefaultLabel = "";
            DefaultColor = Color.FromRgb(0, 0, 0);
            toSave = false;
        }

        public List<Attribute> LabelAttributes { get; set; }

        public string Name { get; set; }

        public bool toSave { get; set; }

        public List<Label> Labels { get; set; }

        public Color GetColorForLabel(string name)
        {
            foreach(Label label in Labels)
            {
                if (label.Name == name)
                {
                    return label.Color;
                }
            }

            return new Color();
        }

        public double MinScore { get; set; }

        public double MaxScore { get; set; }

        public double SampleRate { get; set; }

        public int NumberOfPoints { get; set; }

        public Color MinOrBackColor { get; set; }

        public Color MaxOrForeColor { get; set; }
        
        public string DefaultLabel { get; set; }

        public Color DefaultColor { get; set; }
        public enum AttributeTypes
        {
            [DisplayString(ResourceKey = "String")] STRING,
            [DisplayString(ResourceKey = "List")] LIST,
            [DisplayString(ResourceKey = "Boolean")] BOOLEAN,
            [DisplayString(ResourceKey = "None")] NONE,



        }
    }

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class DisplayStringAttribute : Attribute
    {
        private readonly string value;
        public string Value
        {
            get { return value; }
        }

        public string ResourceKey { get; set; }

        public DisplayStringAttribute(string v)
        {
            this.value = v;
        }

        public DisplayStringAttribute()
        {
        }
    }
}

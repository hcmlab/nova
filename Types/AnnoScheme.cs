using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        }

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

        public AnnoScheme()
        {
            Type = TYPE.FREE;
            Name = "";
            Labels = new List<Label>();
            MinScore = 0;
            MaxScore = 1;
            SampleRate = 1;
            MinOrBackColor = Defaults.Colors.Background;
            MaxOrForeColor = Defaults.Colors.Foreground;
        }

        public TYPE Type { get; set;  }

        public string Name { get; set; }

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

        public Color MinOrBackColor { get; set; }

        public Color MaxOrForeColor { get; set; }
    } 
}

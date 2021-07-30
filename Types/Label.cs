using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ssi.Types
{
    class Label
    {
        public Label(string name, Color color)
        {
            Name = name;
            Color = color;
        }

        public string Name { get; set; }
        public Color Color { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Label label &&
                   Name == label.Name &&
                   Color.Equals(label.Color);
        }

        public override int GetHashCode()
        {
            int hashCode = 97887982;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
            hashCode = hashCode * -1521134295 + Color.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}

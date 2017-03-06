using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ssi
{
    public class Defaults
    {
        public class Colors
        {
            public static Color Foreground = System.Windows.Media.Colors.Black;
            public static Color Background = System.Windows.Media.Colors.White;            
            public static Color Highlight = System.Windows.Media.Colors.LightGray;
            public static Color Conceal = System.Windows.Media.Colors.WhiteSmoke;
            public static Color GradientMin = System.Windows.Media.Colors.White;
            public static Color GradientMax = System.Windows.Media.Colors.LightBlue;
            public static Color Splitter = System.Windows.Media.Colors.WhiteSmoke;
        }

        public class Brushes
        {
            public static Brush Foreground = System.Windows.Media.Brushes.Black;
            public static Brush Background = System.Windows.Media.Brushes.White;
            public static Brush Highlight = System.Windows.Media.Brushes.LightGray;
            public static Brush Conceal = System.Windows.Media.Brushes.WhiteSmoke;
            public static Brush GradientMin = System.Windows.Media.Brushes.White;
            public static Brush GradientMax = System.Windows.Media.Brushes.LightBlue;
            public static Brush Splitter = System.Windows.Media.Brushes.WhiteSmoke;
        }

        public class Strings
        {
            public static string Unkown = "Unkown";
        }

        public static int SelectionBorderWidth = 7;
    }
}

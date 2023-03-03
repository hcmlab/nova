using System;
using System.Collections.Generic;
using System.IO;
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
            public static string Theme = "Default";
            public static string Darkmode = "Dark";

            public static Color Foreground = System.Windows.Media.Colors.Black;
            public static Color Background = System.Windows.Media.Colors.White;            
            public static Color Highlight = System.Windows.Media.Colors.LightGray;
            public static Color Conceal = System.Windows.Media.Colors.White;
            public static Color GradientMin = System.Windows.Media.Colors.White;
            public static Color GradientMax = System.Windows.Media.Colors.LightBlue;
            public static Color Splitter = System.Windows.Media.Colors.WhiteSmoke;
            public static Color AppBackground =  (Theme == Darkmode) ?  (Color)ColorConverter.ConvertFromString("#4e4e4e") : System.Windows.Media.Colors.White;
            public static Color ButtonColor = (Theme == Darkmode) ?  (Color)ColorConverter.ConvertFromString("#6AC0DB") : System.Windows.Media.Colors.LightGray;
            public static Color ButtonForeColor = (Theme == Darkmode) ? System.Windows.Media.Colors.White : System.Windows.Media.Colors.Black;

        }

        public class Brushes
        {
            public static Brush Foreground = new SolidColorBrush(Defaults.Colors.Foreground);
            public static Brush Background = new SolidColorBrush(Defaults.Colors.Background);
            public static Brush Highlight = new SolidColorBrush(Defaults.Colors.Highlight);
            public static Brush Conceal = new SolidColorBrush(Defaults.Colors.Conceal);
            public static Brush GradientMin = new SolidColorBrush(Defaults.Colors.GradientMin);
            public static Brush GradientMax = new SolidColorBrush(Defaults.Colors.GradientMax);
            public static Brush Splitter = new SolidColorBrush(Defaults.Colors.Splitter);
            public static Brush AppBackground = (Brush)new SolidColorBrush(Defaults.Colors.AppBackground);
            public static Brush ButtonColor = (Brush)new SolidColorBrush(Defaults.Colors.ButtonColor);
            public static Brush ButtonForeColor = (Brush)new SolidColorBrush(Defaults.Colors.ButtonForeColor);


            


        }

        public class Strings
        {
            public static string Unkown = "Unkown";         
        }


        public static List<string> LocalDataLocations()
        {
            string[] split = Properties.Settings.Default.DatabaseDirectory.Split(';');
            List<string> localLocations = new List<string>();
            foreach (string s in split)
            {
                localLocations.Add(s);
            }
            //localLocations.Add(Properties.Settings.Default.DatabaseDirectory);
            //localLocations.Add("W:\\nova\\data"); //TODO
            //localLocations.Add("K:\\nova\\data"); //TODO

            return localLocations;
        }

        public static string FileExistsinPath(string filename, string database, string session)
        {
            foreach (var path in Defaults.LocalDataLocations())
            {
                if (File.Exists(path + "\\" + database + "\\" + session + "\\" + filename))
                {
                    return path;
                }
            }

            return Defaults.LocalDataLocations().First();
        }

        public class CML
        {            
            public static string ChainFolderName = "chains";
            public static string ChainFileExtension = "chain";
            public static string ModelsFolderName = "models";
            public static string ModelsTrainerFolderName = "trainer";
            public static string ModelsTemplatesFolderName = "templates";
            public static string TrainerFileExtension = "trainer";
            public static string SchemeFolderName = "schemes";
            public static string GoldStandardFullName = "GOLD STANDARD";
            public static string GoldStandardName = "gold";
            public static string StreamTypeNameFeature = "feature";
            public static string FusionFolderName = "fusion";
            public static string FusionBayesianNetworkFolderName = "bayesnet";
            public static string BayesianNetworkextension = "xdsl";
            public static string SessionSetExtension = "set";
        }

        public class Lightning
        {
            public static string LNEndPoint = "https://ln.novaannotation.com";
            public static string LNAddressUrl = "https://novaannotation.com/";
            public static string LNBitsEndPoint = "https://lnbits.novaannotation.com";
            public static string LnAddressDomain = "novaannotation.com";

            //  MainHandler.Decode("aHR0cDovL2hjYWkuYml0Y29pbmZpeGVzdGhpcy5vcmc6NTAwMA==");
            public static string LNTippingInvoiceID = "7cdd5e6ca21b47889d9ae42740bb9031";


        }



        public static int SelectionBorderWidth = 7;

        public static double DefaultSampleRate = 25.0;

        public static Color DefaultColors(int index)
        {
            if (index % 8 == 0) return System.Windows.Media.Colors.Khaki;
            else if (index % 8 == 1) return System.Windows.Media.Colors.SkyBlue;
            else if (index % 8 == 2) return System.Windows.Media.Colors.YellowGreen;
            else if (index % 8 == 3) return System.Windows.Media.Colors.Tomato;
            else if (index % 8 == 4) return System.Windows.Media.Colors.RosyBrown;
            else if (index % 8 == 5) return System.Windows.Media.Colors.Goldenrod;
            else if (index % 8 == 6) return System.Windows.Media.Colors.LightSeaGreen;
            else if (index % 8 == 7) return System.Windows.Media.Colors.LightGray;
            else return System.Windows.Media.Colors.White;
        }


      


    }
}

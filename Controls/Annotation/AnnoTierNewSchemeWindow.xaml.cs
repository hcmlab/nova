using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für NewTierWindow.xaml
    /// </summary>
    public partial class AnnoTierNewSchemeWindow : Window
    {
        public class CMLScheme
        {
            public string Path { get; set; }
            public string Name { get; set; }
            public AnnoScheme.TYPE Type { get; set; }      
            public override string ToString()
            {
                return Name + " (" + Type.ToString() + ")";
            }      
        }

        public AnnoScheme Scheme { get; set; }

        public bool LoadedFromFile { get; set; }

        public AnnoTierNewSchemeWindow(double defaultSr)
        {
            InitializeComponent();

            Scheme = new AnnoScheme() { SampleRate = defaultSr };
            LoadedFromFile = false;

            string schemesDir = Properties.Settings.Default.CMLDirectory + "\\" +
                                Defaults.CML.SchemeFolderName + "\\";

            if (Directory.Exists(schemesDir))
            {
                foreach (string schemeDir in Directory.GetDirectories(schemesDir))
                {
                    foreach (string schemeFile in Directory.GetFiles(schemeDir, "*.annotation"))
                    {
                        AnnoScheme.TYPE type = (AnnoScheme.TYPE)Enum.Parse(typeof(AnnoScheme.TYPE), System.IO.Path.GetFileName(schemeDir).ToUpper());
                        CMLScheme cmlScheme = new CMLScheme() { Path = schemeFile, Name = System.IO.Path.GetFileNameWithoutExtension(schemeFile), Type = type };
                        combobox_cml.Items.Add(cmlScheme);
                    }
                }
            }

            if (combobox_cml.Items.Count > 0)
            {
                combobox_cml.SelectedIndex = 0;
            }
            else
            {
                combobox_cml.Visibility = Visibility.Collapsed;
                button_cml.Visibility = Visibility.Collapsed;
                textblock_cml.Visibility = Visibility.Collapsed;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            string name = (sender as Button).Name.ToString();

            if (name == "button_cml")
            {
                CMLScheme cmlScheme = (CMLScheme)combobox_cml.SelectedItem;
                Scheme = AnnoList.LoadfromFile(cmlScheme.Path).Scheme;
                LoadedFromFile = true;
            }
            else
            {
                AnnoScheme.TYPE annoType = AnnoScheme.TYPE.FREE;

                switch (name)
                {
                    case "button_discrete":
                        annoType = AnnoScheme.TYPE.DISCRETE;
                        break;
                    case "button_free":
                        annoType = AnnoScheme.TYPE.FREE;
                        break;
                    case "button_continuous":
                        annoType = AnnoScheme.TYPE.CONTINUOUS;
                        break;
                    case "button_point":
                        annoType = AnnoScheme.TYPE.POINT;
                        break;
                    case "button_polygon":
                        annoType = AnnoScheme.TYPE.POLYGON;
                        break;
                    case "button_graph":
                        annoType = AnnoScheme.TYPE.GRAPH;
                        break;
                    case "button_segmentation":
                        annoType = AnnoScheme.TYPE.SEGMENTATION;
                        break;
                }

                Scheme.Type = annoType;

                if (Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    Scheme.MinScore = 0.0;
                    Scheme.MaxScore = 1.0;
                    Scheme.MinOrBackColor = Defaults.Colors.GradientMin;
                    Scheme.MaxOrForeColor = Defaults.Colors.GradientMax;
                }
                else if (Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    Scheme.NumberOfPoints = 1;
                    Scheme.MaxOrForeColor = Colors.Green;
                }
            }
        }


    }
}
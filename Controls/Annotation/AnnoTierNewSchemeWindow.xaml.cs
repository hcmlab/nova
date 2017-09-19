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
                        cmlCombobox.Items.Add(cmlScheme);
                    }
                }
            }

            if (cmlCombobox.Items.Count > 0)
            {
                cmlCombobox.SelectedIndex = 0;
            }
            else
            {
                cmlCombobox.Visibility = Visibility.Collapsed;
                cmlSeparator.Visibility = Visibility.Collapsed;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            if (cmlRadioButton.IsChecked == true)
            {
                CMLScheme cmlScheme = (CMLScheme)cmlCombobox.SelectedItem;
                Scheme = AnnoList.LoadfromFile(cmlScheme.Path).Scheme;
                LoadedFromFile = true;
            }
            else
            {
                AnnoScheme.TYPE annoType = AnnoScheme.TYPE.FREE;

                if (freeRadioButton.IsChecked == true)
                {
                    annoType = AnnoScheme.TYPE.FREE;
                }
                else if (discreteRadioButton.IsChecked == true)
                {
                    annoType = AnnoScheme.TYPE.DISCRETE;
                }
                else if (continuousRadioButton.IsChecked == true)
                {
                    annoType = AnnoScheme.TYPE.CONTINUOUS;
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;

            Close();
        }
    }
}
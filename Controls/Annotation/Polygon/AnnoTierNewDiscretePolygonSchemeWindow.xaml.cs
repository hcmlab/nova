using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using Label = ssi.Types.Label;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoTierNewPolygonSchemeWindow.xaml
    /// </summary>
    public partial class AnnoTierNewDiscretePolygonSchemeWindow : Window
    {
        private AnnoScheme scheme;
        private ObservableCollection<Label> items;

        public AnnoTierNewDiscretePolygonSchemeWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();
            this.scheme = scheme;

            if(scheme.Name != "")
                nameTextBox.Text = scheme.Name;
            else
                nameTextBox.Text = "Unknown";

            items = new ObservableCollection<Label>();
            foreach (AnnoScheme.Label label in scheme.Labels)
            {
                items.Add(new Label(label.Name, label.Color));
            }
            LabelsListBox.ItemsSource = items;

            srTextBox.Text = scheme.SampleRate.ToString();
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            Color color = Colors.Black;
            bool contains = true;
            while (contains)
            {
                Random rnd = new Random();
                Byte[] b = new Byte[3];
                rnd.NextBytes(b);
                color = Color.FromRgb(b[0], b[1], b[2]);

                List<Color> usedColors = new List<Color>();

                contains = false;
                foreach (Label label in LabelsListBox.Items)
                {
                    if (usedColors.Contains(label.Color))
                    {
                        contains = true;
                        break;
                    }

                    usedColors.Add(label.Color);
                }
            }
            
            
            Label item = new Label("", color);
            items.Add(item);
            LabelsListBox.SelectedItem = item;
            LabelsListBox.ScrollIntoView(item);
        }
    
        private void DeleteLabel_Click(object sender, RoutedEventArgs e)
        {
            IEditableCollectionView items = LabelsListBox.Items; 

            if (items.CanRemove)
            {
                items.Remove(LabelsListBox.SelectedItem);
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            scheme.Name = nameTextBox.Text == "" ? Defaults.Strings.Unknown : nameTextBox.Text;

            if (double.TryParse(srTextBox.Text, out double value))
            {
                scheme.SampleRate = value;
            }

            List<string> usedLabels = new List<string>();
            List<Color> usedColors = new List<Color>();

            scheme.Labels.Clear();

            foreach (Label a in LabelsListBox.Items)
            {
                if (a.Name != "" && !usedLabels.Contains(a.Name))
                {
                    if(usedColors.Contains(a.Color))
                    {
                        MessageBox.Show("It is not possible to allocate one color to two or more labels!", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    scheme.Labels.Add(new AnnoScheme.Label(a.Name, a.Color));
                    usedColors.Add(a.Color);
                    usedLabels.Add(a.Name);
                }
            }

            if(usedLabels.Count == 0)
            {
                MessageBox.Show("You can not create a discrete polygon scheme with no labels!", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

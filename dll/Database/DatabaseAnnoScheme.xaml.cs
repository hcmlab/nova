using System;
using System.Collections.Generic;
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

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAnnoScheme.xaml
    /// </summary>
    public partial class DatabaseAnnoScheme : Window
    {
        private List<LabelColorPair> items;
        private List<LabelColorPair> labelcolors;
        private HashSet<LabelColorPair> usedlabels;

        public DatabaseAnnoScheme(string name = null, HashSet<LabelColorPair> _usedlabels = null, bool isDiscrete = true, Brush mincolor = null, Brush maxcolor = null, string samplerate = null, string min = null, string max = null)
        {
            InitializeComponent();
            scheme_colorpickermin.SelectedColor = Colors.Blue;
            scheme_colorpickermax.SelectedColor = Colors.Red;

            if (isDiscrete) scheme_colorpickermin.SelectedColor = Colors.LightYellow;
            scheme_max.Text = (1.0).ToString();
            scheme_min.Text = (0.0).ToString();
            scheme_fps.Text = (25).ToString();
            scheme_name.Text = "NewAnnotationType";

            this.usedlabels = _usedlabels;

            labelcolors = new List<LabelColorPair>();

            if (name != null) scheme_name.Text = name;

            if (isDiscrete) scheme_type.SelectedIndex = 0;
            else scheme_type.SelectedIndex = 1;

            if (mincolor != null) scheme_colorpickermin.SelectedColor = (mincolor as SolidColorBrush).Color;
            if (maxcolor != null) scheme_colorpickermax.SelectedColor = (maxcolor as SolidColorBrush).Color;
            if (samplerate != null) scheme_fps.Text = samplerate;
            if (min != null) scheme_min.Text = min;
            if (max != null) scheme_max.Text = max;

            if (usedlabels != null)
            {
                items = new List<LabelColorPair>();
                foreach (LabelColorPair lp in usedlabels)
                {
                    items.Add(new LabelColorPair(lp.Label, lp.Color) { Label = lp.Label, Color = lp.Color });
                    labelcolors.Add(new LabelColorPair(lp.Label, lp.Color));
                }

                AnnotationResultBox.ItemsSource = items;
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox l = new LabelInputBox("Add new Label", "Add new label", "", null, 1, "", "", true);
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

            if (l.DialogResult == true)
            {
                items = new List<LabelColorPair>();
                string label = l.Result();
                string color = l.Color();
                LabelColorPair lcp = new LabelColorPair(label, color);

                labelcolors.Add(lcp);

                foreach (LabelColorPair lp in labelcolors)
                {
                    items.Add(new LabelColorPair(label, color) { Label = lp.Label, Color = lp.Color });
                }

                AnnotationResultBox.ItemsSource = items;
            }
        }

        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            IEditableCollectionView items = AnnotationResultBox.Items; //Cast to interface
            if (items.CanRemove)
            {
                foreach (LabelColorPair lp in labelcolors)
                {
                    string selection = ((LabelColorPair)AnnotationResultBox.SelectedItem).Label;
                    if (lp.Label == selection)
                    {
                        labelcolors.Remove(lp);
                        break;
                    }
                }
                items.Remove(AnnotationResultBox.SelectedItem);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            this.Close();
        }

        public string GetName()
        {
            return scheme_name.Text;
        }

        public string GetMin()
        {
            return scheme_min.Text;
        }

        public string GetMax()
        {
            return scheme_max.Text;
        }

        public string GetFps()
        {
            return scheme_fps.Text;
        }

        public string GetType()
        {
            return scheme_type.SelectionBoxItem.ToString();
        }

        public List<LabelColorPair> GetLabelColorPairs()
        {
            return labelcolors;
        }

        public string GetColorMin()
        {
            return scheme_colorpickermin.SelectedColor.ToString();
        }

        public string GetColorMax()
        {
            return scheme_colorpickermax.SelectedColor.ToString();
        }

        private void scheme_type_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (scheme_type.SelectedIndex == 0)
            {
                Colorlabel.Content = "Color";
                MaxColorLabel.Visibility = Visibility.Hidden;
                scheme_colorpickermax.Visibility = Visibility.Collapsed;
                scheme_min.Visibility = Visibility.Hidden;
                scheme_max.Visibility = Visibility.Hidden;
                scheme_fps.Visibility = Visibility.Hidden;
                MaxValLabel.Visibility = Visibility.Hidden;
                MinValLabel.Visibility = Visibility.Hidden;
                FPSLabel.Visibility = Visibility.Hidden;

                labelslabel.Visibility = Visibility.Visible;
                AnnotationResultBox.Visibility = Visibility.Visible;
                AddAnnotation.Visibility = Visibility.Visible;
                DeleteAnnotation.Visibility = Visibility.Visible;
            }
            else
            {
                Colorlabel.Content = "Min Color";
                scheme_colorpickermin.IsEnabled = true;
                MaxColorLabel.Visibility = Visibility.Visible;
                MaxValLabel.Visibility = Visibility.Visible;
                MinValLabel.Visibility = Visibility.Visible;
                FPSLabel.Visibility = Visibility.Visible;
                scheme_colorpickermax.Visibility = Visibility.Visible;
                scheme_min.Visibility = Visibility.Visible;
                scheme_max.Visibility = Visibility.Visible;
                scheme_fps.Visibility = Visibility.Visible;

                labelslabel.Visibility = Visibility.Hidden;
                AnnotationResultBox.Visibility = Visibility.Hidden;
                AddAnnotation.Visibility = Visibility.Hidden;
                DeleteAnnotation.Visibility = Visibility.Hidden;
            }
        }
    }
}
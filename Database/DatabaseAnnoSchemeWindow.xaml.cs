using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAnnoScheme.xaml
    /// </summary>
    public partial class DatabaseAnnoSchemeWindow : Window
    {
        private List<AnnoScheme.Label> items;
        private List<AnnoScheme.Label> labelcolors;
        private HashSet<AnnoScheme.Label> usedlabels;

        public DatabaseAnnoSchemeWindow(string name = null, HashSet<AnnoScheme.Label> _usedlabels = null, AnnoScheme.TYPE isDiscrete = AnnoScheme.TYPE.DISCRETE, Brush mincolor = null, Brush maxcolor = null, string samplerate = null, string min = null, string max = null)
        {
            InitializeComponent();
            scheme_colorpickermin.SelectedColor = Colors.Blue;
            scheme_colorpickermax.SelectedColor = Colors.Red;

            if (isDiscrete == AnnoScheme.TYPE.DISCRETE || isDiscrete == AnnoScheme.TYPE.FREE) scheme_colorpickermin.SelectedColor = Colors.LightYellow;
            scheme_max.Text = (1.0).ToString();
            scheme_min.Text = (0.0).ToString();
            scheme_fps.Text = (25).ToString();
            scheme_name.Text = "NewAnnotationType";

            this.usedlabels = _usedlabels;

            labelcolors = new List<AnnoScheme.Label>();

            if (name != null) scheme_name.Text = name;

            if (isDiscrete == AnnoScheme.TYPE.DISCRETE) scheme_type.SelectedIndex = 0;
            else if (isDiscrete == AnnoScheme.TYPE.FREE) scheme_type.SelectedIndex = 1;
            else if (isDiscrete == AnnoScheme.TYPE.CONTINUOUS) scheme_type.SelectedIndex = 2;

            if (mincolor != null) scheme_colorpickermin.SelectedColor = (mincolor as SolidColorBrush).Color;
            if (maxcolor != null) scheme_colorpickermax.SelectedColor = (maxcolor as SolidColorBrush).Color;
            if (samplerate != null) scheme_fps.Text = samplerate;
            if (min != null) scheme_min.Text = min;
            if (max != null) scheme_max.Text = max;

            if (usedlabels != null)
            {
                items = new List<AnnoScheme.Label>();
                foreach (AnnoScheme.Label lp in usedlabels)
                {
                    items.Add(new AnnoScheme.Label(lp.Name, lp.Color) { Name = lp.Name, Color = lp.Color });
                    labelcolors.Add(new AnnoScheme.Label(lp.Name, lp.Color));
                }

                AnnotationResultBox.ItemsSource = items;
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
            input["label"] = new UserInputWindow.Input() { Label = "Class label", DefaultValue = "" };
            input["color"] = new UserInputWindow.Input() { Label = "Color code (#RGB)", DefaultValue = "" };            
            UserInputWindow dialog = new UserInputWindow("Add a new class label", input);
            dialog.ShowDialog();
                                    
            if (dialog.DialogResult == true)
            {
                items = new List<AnnoScheme.Label>();
                string name = dialog.Result("label");
                Color color = (Color)ColorConverter.ConvertFromString(dialog.Result("color"));
                AnnoScheme.Label lcp = new AnnoScheme.Label(name, color);


                labelcolors.Add(lcp);

                foreach (AnnoScheme.Label lp in labelcolors)
                {
                    items.Add(new AnnoScheme.Label(name, color) { Name = lp.Name, Color = lp.Color });
                }

                AnnotationResultBox.ItemsSource = items;
            }
        }

        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            IEditableCollectionView items = AnnotationResultBox.Items; //Cast to interface
            if (items.CanRemove)
            {
                foreach (AnnoScheme.Label lp in labelcolors)
                {
                    string selection = ((AnnoScheme.Label)AnnotationResultBox.SelectedItem).Name;
                    if (lp.Name == selection)
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

        public List<AnnoScheme.Label> GetLabelColorPairs()
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
            else if (scheme_type.SelectedIndex == 1)
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

                labelslabel.Visibility = Visibility.Hidden;
                AnnotationResultBox.Visibility = Visibility.Hidden;
                AddAnnotation.Visibility = Visibility.Hidden;
                DeleteAnnotation.Visibility = Visibility.Hidden;
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
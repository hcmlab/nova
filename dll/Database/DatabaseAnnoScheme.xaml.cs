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

        List<LabelColorPair> items;
        List<LabelColorPair> labelcolors;
        public DatabaseAnnoScheme()
        {
            InitializeComponent();
            scheme_type.SelectedIndex = 0;
            scheme_colorpickermin.SelectedColor = Colors.Red;
            scheme_colorpickermax.SelectedColor = Colors.Blue;
          
            labelcolors = new List<LabelColorPair>();
        }



        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {

            LabelInputBox l = new LabelInputBox("Add new Label", "Add new label", "", null, 1, "", "", true);
            l.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            l.ShowDialog();

          
            
            if (l.DialogResult ==  true)
            {
                items = new List<LabelColorPair>();
                string label = l.Result();
                string color = l.Color();
                LabelColorPair lcp = new LabelColorPair(label,color);

                labelcolors.Add(lcp);

                foreach(LabelColorPair lp in labelcolors)
                {
                    items.Add(new LabelColorPair(label, color) { Label = lp.label, Color = lp.color });

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
                   string selection = ((LabelColorPair) AnnotationResultBox.SelectedItem).label;
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
                scheme_min.IsEnabled = false;
                scheme_max.IsEnabled = false;
                scheme_fps.IsEnabled = false;

                AnnotationResultBox.IsEnabled = true;
                AddAnnotation.IsEnabled = true;
                DeleteAnnotation.IsEnabled = true;


            }

            else
            {
                Colorlabel.Content = "Min Color";
                scheme_colorpickermin.IsEnabled = true;
                MaxColorLabel.Visibility = Visibility.Visible;
                scheme_colorpickermax.Visibility = Visibility.Visible;
                scheme_min.IsEnabled = true;
                scheme_max.IsEnabled = true;
                scheme_fps.IsEnabled = true;

                AnnotationResultBox.IsEnabled = false;
                AddAnnotation.IsEnabled = false;
                DeleteAnnotation.IsEnabled = false;

            }
        }
    }
}

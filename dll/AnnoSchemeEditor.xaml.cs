using System;
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
    public partial class AnnoSchemeEditor : Window
    {
        private List<AnnotationSchemeSegment> items;
        private List<AnnotationSchemeSegment> schemes;
        private HashSet<LabelColorPair> usedlabels;
        private AnnoList list;

        public AnnoSchemeEditor(string name = null, HashSet<LabelColorPair> _usedlabels = null, AnnoType isDiscrete = AnnoType.DISCRETE, Brush mincolor = null, Brush maxcolor = null, string samplerate = null, string min = null, string max = null)
        {
            InitializeComponent();
            scheme_colorpickermin.SelectedColor = Colors.Blue;

            items = new List<AnnotationSchemeSegment>();
            schemes = new List<AnnotationSchemeSegment>();
            list = new AnnoList();
            list.AnnotationScheme = new AnnotationScheme();
            list.AnnotationScheme.LabelsAndColors = new List<LabelColorPair>();
            list.AnnotationType = AnnoType.DISCRETE;

            if (isDiscrete == AnnoType.DISCRETE || isDiscrete == AnnoType.FREE) scheme_colorpickermin.SelectedColor = Colors.LightYellow;

            scheme_name.Text = "New Scheme";

            this.usedlabels = _usedlabels;

            if (name != null) scheme_name.Text = name;

            if (usedlabels != null)
            {
                foreach (LabelColorPair lp in usedlabels)
                {
                    items.Add(new AnnotationSchemeSegment() { Label = lp.Label, BindingColor = (Color)ColorConverter.ConvertFromString(lp.Color) });
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
            items = new List<AnnotationSchemeSegment>();

            if (l.DialogResult == true)
            {
                schemes.Add(new AnnotationSchemeSegment() { Label = l.Result(), BindingColor = (Color)ColorConverter.ConvertFromString(l.Color()) });
            }

            foreach (var item in schemes)
            {
                items.Add(item);
            }
            AnnotationResultBox.ItemsSource = items;
        }

        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            IEditableCollectionView items = AnnotationResultBox.Items; //Cast to interface
            if (items.CanRemove)
            {
                items.Remove(AnnotationResultBox.SelectedItem);
            }

            schemes.Clear();

            foreach (var item in AnnotationResultBox.Items)
            {
                AnnotationSchemeSegment s = new AnnotationSchemeSegment();
                s.Label = ((AnnotationSchemeSegment)item).Label;
                s.BindingColor = ((AnnotationSchemeSegment)item).BindingColor;
                schemes.Add(s);
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

        public AnnoList GetAnnoList()
        {
            foreach (AnnotationSchemeSegment a in AnnotationResultBox.Items)
            {
                LabelColorPair lcp = new LabelColorPair(a.Label, "#" + a.BindingColor.R.ToString("X2") + a.BindingColor.G.ToString("X2") + a.BindingColor.B.ToString("X2"));
                list.AnnotationScheme.LabelsAndColors.Add(lcp);
            }

            LabelColorPair garbage = new LabelColorPair("GARBAGE", "#FF000000");
            list.AnnotationScheme.LabelsAndColors.Add(garbage);
            list.Clear();

            list.AnnotationScheme.mincolor = scheme_colorpickermin.SelectedColor.ToString();
            list.Name = this.scheme_name.Text;
         
            return list;
        }

        private void Label_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null && filenames[0].EndsWith(".annotation"))
                {
                    try
                    {
                        list = AnnoList.LoadfromFileNew(filenames[0]);

                        if (list.AnnotationType == AnnoType.DISCRETE)
                        {
                            foreach (var item in list.AnnotationScheme.LabelsAndColors)
                            {
                                AnnotationSchemeSegment ass = new AnnotationSchemeSegment();
                                ass.Label = item.Label;
                                ass.BindingColor = (Color)ColorConverter.ConvertFromString(item.Color);
                                if (!schemes.Contains(ass)) schemes.Add(ass);
                            }
                        }
                        else if (list.AnnotationType == AnnoType.FREE)
                        {
                            HashSet<LabelColorPair> usedlabels = new HashSet<LabelColorPair>();
                            foreach (AnnoListItem item in list)
                            {
                                LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                                bool detected = false;
                                foreach (LabelColorPair p in usedlabels)
                                {
                                    if (p.Label == l.Label)
                                    {
                                        detected = true;
                                        break;
                                    }
                                }

                                if (detected == false)
                                {
                                    usedlabels.Add(l);
                                    AnnotationSchemeSegment ass = new AnnotationSchemeSegment();
                                    ass.Label = item.Label;
                                    ass.BindingColor = (Color)ColorConverter.ConvertFromString(item.Bg);
                                    if (!schemes.Contains(ass)) schemes.Add(ass);
                                }
                                 
                            }
                        }
                        else return;

                        foreach (var item in schemes)
                        {
                            items.Add(item);
                        }
                        AnnotationResultBox.ItemsSource = items;

                        this.scheme_name.Text = list.Name;
                        this.scheme_colorpickermin.SelectedColor = (Color)ColorConverter.ConvertFromString(list.AnnotationScheme.mincolor);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("This is not a valid annotation file");
                    }
                }
            }
        }

        private void TextBoxEx_TextChanged(object sender, TextChangedEventArgs e)
        {
            Console.WriteLine(e.Source.ToString());
        }

        private void TextBoxEx_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            Console.WriteLine(e.Source.ToString());
        }
    }

    public class AnnotationSchemeSegment
    {
        public string Label { get; set; }

        public Color BindingColor { get; set; }
    }
}
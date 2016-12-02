using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;

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
                    items.Add(new AnnotationSchemeSegment() { Label = lp.Label, BindingColor = (Color)ColorConverter.ConvertFromString(lp.Color)});
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


            foreach(var item in schemes)
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

            foreach(var item in AnnotationResultBox.Items)
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
            foreach(AnnotationSchemeSegment a in AnnotationResultBox.Items)
            {
                LabelColorPair lcp = new LabelColorPair(a.Label, "#" + a.BindingColor.R.ToString("X2") + a.BindingColor.G.ToString("X2") + a.BindingColor.B.ToString("X2"));
                list.AnnotationScheme.LabelsAndColors.Add(lcp);
            }

            LabelColorPair garbage = new LabelColorPair("GARBAGE", "#FF000000");
            list.AnnotationScheme.LabelsAndColors.Add(garbage);

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
                    list = new AnnoList(filenames[0]);
                    list.Lowborder = 0.0;
                    list.Highborder = 1.0;
                    list.Filepath = filenames[0];
                    list.AnnotationScheme = new AnnotationScheme();
                    list.AnnotationScheme.LabelsAndColors = new List<LabelColorPair>();


                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.Load(filenames[0]);

                        XmlNode annotation = doc.SelectSingleNode("annotation");

                        XmlNode info = annotation.SelectSingleNode("info");
                        list.Ftype = info.Attributes["ftype"].Value;
                        int size = Int32.Parse(info.Attributes["size"].Value);

                        XmlNode meta = annotation.SelectSingleNode("meta");
                        if (meta != null)
                        {
                            if (meta.Attributes["role"] != null) list.Role = meta.Attributes["role"].Value;
                            if (meta.Attributes["annotator"] != null) list.Annotator = meta.Attributes["annotator"].Value;
                        }

                        XmlNode scheme = annotation.SelectSingleNode("scheme");
                        if (scheme.Attributes["name"] != null) list.Name = scheme.Attributes["name"].Value;
                        string type = "FREE";
                        if (scheme.Attributes["type"] != null) type = scheme.Attributes["type"].Value;
                        if (scheme.Attributes["color"] != null) list.AnnotationScheme.mincolor = scheme.Attributes["color"].Value;
                        else list.AnnotationScheme.mincolor = Colors.LightYellow.ToString();

                        items = new List<AnnotationSchemeSegment>();

                        if (type == "DISCRETE") list.AnnotationType = AnnoType.DISCRETE;
                        else return;
                        Dictionary<string, string> LabelIds = new Dictionary<string, string>();

                        if (list.AnnotationType == AnnoType.DISCRETE)
                        {
                            list.usesAnnoScheme = true;
                          
                            foreach (XmlNode item in scheme)
                            {
                                LabelIds.Add(item.Attributes["id"].Value, item.Attributes["name"].Value);

                                string color = "#000000";
                                if (item.Attributes["color"] != null) color = item.Attributes["color"].Value;

                                AnnotationSchemeSegment s = new AnnotationSchemeSegment();
                                s.Label = item.Attributes["name"].Value;
                                s.BindingColor = (Color)ColorConverter.ConvertFromString(color);

                            if (!schemes.Contains(s))  schemes.Add(s);
                            }


                            foreach (var item in schemes)
                            {
                                items.Add(item);
                            }
                            AnnotationResultBox.ItemsSource = items;

                            this.scheme_name.Text = list.Name;
                            this.scheme_colorpickermin.SelectedColor = (Color)ColorConverter.ConvertFromString(list.AnnotationScheme.mincolor); 



                        }
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
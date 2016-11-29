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
        private List<LabelColorPair> items;
        private List<LabelColorPair> labelcolors;
        private HashSet<LabelColorPair> usedlabels;
        private AnnoList list;

        public AnnoSchemeEditor(string name = null, HashSet<LabelColorPair> _usedlabels = null, AnnoType isDiscrete = AnnoType.DISCRETE, Brush mincolor = null, Brush maxcolor = null, string samplerate = null, string min = null, string max = null)
        {
            InitializeComponent();
            scheme_colorpickermin.SelectedColor = Colors.Blue;

            list = new AnnoList();
            list.AnnotationScheme = new AnnotationScheme();
            list.AnnotationScheme.LabelsAndColors = new List<LabelColorPair>();
            list.AnnotationType = AnnoType.DISCRETE;

            if (isDiscrete == AnnoType.DISCRETE || isDiscrete == AnnoType.FREE) scheme_colorpickermin.SelectedColor = Colors.LightYellow;

            scheme_name.Text = "New Scheme";

            this.usedlabels = _usedlabels;

            labelcolors = new List<LabelColorPair>();

            if (name != null) scheme_name.Text = name;

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

        public AnnoList GetAnnoList()
        {
            return list;
        }

        public List<LabelColorPair> GetLabelColorPairs()
        {
            return labelcolors;
        }

        public string GetColorMin()
        {
            return scheme_colorpickermin.SelectedColor.ToString();
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
                        else if (scheme.Attributes["mincolor"] != null) list.AnnotationScheme.mincolor = scheme.Attributes["mincolor"].Value;
                        if (scheme.Attributes["maxcolor"] != null) list.AnnotationScheme.maxcolor = scheme.Attributes["maxcolor"].Value;

                        if (type == "DISCRETE") list.AnnotationType = AnnoType.DISCRETE;
                        else return;
                        Dictionary<string, string> LabelIds = new Dictionary<string, string>();

                        if (list.AnnotationType == AnnoType.DISCRETE)
                        {
                            list.usesAnnoScheme = true;
                            items = new List<LabelColorPair>();
                            foreach (XmlNode item in scheme)
                            {
                                LabelIds.Add(item.Attributes["id"].Value, item.Attributes["name"].Value);

                                string color = "#000000";
                                if (item.Attributes["color"] != null) color = item.Attributes["color"].Value;

                                string label = item.Attributes["name"].Value;
                                LabelColorPair lcp = new LabelColorPair(label, color);
                                list.AnnotationScheme.LabelsAndColors.Add(lcp);
                                items.Add(new LabelColorPair(label, color) { Label = item.Attributes["name"].Value, Color = color });
                            }

                            AnnotationResultBox.ItemsSource = items;
                            this.scheme_name.Text = list.Name;
                            this.scheme_colorpickermin.SelectedColor = Colors.LightYellow;

                            LabelColorPair garbage = new LabelColorPair("GARBAGE", "#FF000000");
                            list.AnnotationScheme.LabelsAndColors.Add(garbage);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("This is not a valid annotation file");
                    }
                }
            }
        }
    }
}
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
        private HashSet<AnnoScheme.Label> usedlabels;
        private AnnoList list;

        public AnnoSchemeEditor(string name = null, HashSet<AnnoScheme.Label> _usedlabels = null, AnnoScheme.TYPE isDiscrete = AnnoScheme.TYPE.DISCRETE, Brush mincolor = null, Brush maxcolor = null, string samplerate = null, string min = null, string max = null)
        {
            InitializeComponent();
            scheme_colorpickermin.SelectedColor = Colors.Blue;

            items = new List<AnnotationSchemeSegment>();
            schemes = new List<AnnotationSchemeSegment>();
            list = new AnnoList();
            list.Scheme = new AnnoScheme() { Type = AnnoScheme.TYPE.DISCRETE };
            list.Scheme.Labels = new List<AnnoScheme.Label>();
            list.Scheme.Type = AnnoScheme.TYPE.DISCRETE;

            if (isDiscrete == AnnoScheme.TYPE.DISCRETE || isDiscrete == AnnoScheme.TYPE.FREE) scheme_colorpickermin.SelectedColor = Colors.LightYellow;

            scheme_name.Text = "New Scheme";

            this.usedlabels = _usedlabels;

            if (name != null) scheme_name.Text = name;

            if (usedlabels != null)
            {
                foreach (AnnoScheme.Label lp in usedlabels)
                {
                    items.Add(new AnnotationSchemeSegment() { Label = lp.Name, Color = lp.Color });
                }

                AnnotationResultBox.ItemsSource = items;
            }
        }

        private void AnnotationResultBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            /*
            AnnoTierNewLabelWindow l = new AnnoTierNewLabelWindow("Add new Label", "Add new label", "", null, 1, "", "", true);
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
            */
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
                s.Color = ((AnnotationSchemeSegment)item).Color;
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
            list.Scheme.Labels.Clear();

            foreach (AnnotationSchemeSegment a in AnnotationResultBox.Items)
            {
                AnnoScheme.Label lcp = new AnnoScheme.Label(a.Label, a.Color);
                list.Scheme.Labels.Add(lcp);
            }

            AnnoScheme.Label garbage = new AnnoScheme.Label("GARBAGE", Colors.Black);
            list.Scheme.Labels.Add(garbage);
            list.Clear();

            list.Scheme.MinOrBackColor = scheme_colorpickermin.SelectedColor.Value;
            list.Scheme.Name = this.scheme_name.Text;
         
            return list;
        }

        private void Label_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                IEditableCollectionView itemstoremove = AnnotationResultBox.Items;
                for(int i=0; i<AnnotationResultBox.Items.Count;i++)
                {
                    if (itemstoremove.CanRemove) itemstoremove.Remove(AnnotationResultBox.Items[i]);
                }

                AnnotationResultBox.ItemsSource = null;
                items = new List<AnnotationSchemeSegment>();


                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null && filenames[0].EndsWith(".annotation"))
                {
                    try
                    {
                        list = AnnoList.LoadfromFile(filenames[0]);

                        if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                        {
                            foreach (var item in list.Scheme.Labels)
                            {
                                AnnotationSchemeSegment ass = new AnnotationSchemeSegment();
                                ass.Label = item.Name;
                                ass.Color = item.Color;
                                if (!schemes.Contains(ass) && ass.Label != "GARBAGE") schemes.Add(ass);
                            }
                        }
                        else if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                        {
                            HashSet<AnnoScheme.Label> usedlabels = new HashSet<AnnoScheme.Label>();
                            foreach (AnnoListItem item in list)
                            {
                                AnnoScheme.Label l = new AnnoScheme.Label(item.Label, item.Color);
                                bool detected = false;
                                foreach (AnnoScheme.Label p in usedlabels)
                                {
                                    if (p.Name == l.Name)
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
                                    ass.Color = item.Color;
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

                        this.scheme_name.Text = list.Scheme.Name;
                        if(list.Scheme.MinOrBackColor!= null) this.scheme_colorpickermin.SelectedColor = list.Scheme.MinOrBackColor;
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

        public Color Color { get; set; }
    }
}
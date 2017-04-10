using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAnnoScheme.xaml
    /// </summary>
    public partial class AnnoTierNewDiscreteSchemeWindow : Window
    {
        private ObservableCollection<AnnotationSchemeSegment> items;        

        public AnnoTierNewDiscreteSchemeWindow()
        {
            InitializeComponent();

            DataContext = this;

            items = new ObservableCollection<AnnotationSchemeSegment>();
            LabelsListBox.ItemsSource = items;

            backroundColorPicket.SelectedColor = Defaults.Colors.Background;
            schemeNameTextField.Text = Defaults.Strings.Unkown;            
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            AnnotationSchemeSegment item = new AnnotationSchemeSegment() { Label = "", Color = Colors.Black };
            items.Add(item);
            LabelsListBox.SelectedItem = item;
            LabelsListBox.ScrollIntoView(item);            
        }

        private void DeleteLabel_Click(object sender, RoutedEventArgs e)
        {
            IEditableCollectionView items = LabelsListBox.Items; //Cast to interface
            if (items.CanRemove)
            {
                items.Remove(LabelsListBox.SelectedItem);
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
            List<string> usedLabels = new List<string>();

            AnnoList list = new AnnoList();
            list.Scheme.Type = AnnoScheme.TYPE.DISCRETE;
            list.Scheme.MinOrBackColor = backroundColorPicket.SelectedColor.Value;            
            list.Scheme.Name = schemeNameTextField.Text == "" ? Defaults.Strings.Unkown : schemeNameTextField.Text;

            foreach (AnnotationSchemeSegment a in LabelsListBox.Items)
            {
                if (a.Label != "" && !usedLabels.Contains(a.Label))
                {
                    AnnoScheme.Label lcp = new AnnoScheme.Label(a.Label, a.Color);
                    list.Scheme.Labels.Add(lcp);
                    usedLabels.Add(a.Label);
                }
            }

            AnnoScheme.Label garbage = new AnnoScheme.Label("GARBAGE", Colors.Black);
            list.Scheme.Labels.Add(garbage);
         
            return list;
        }

        private void AnnoList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null && filenames[0].EndsWith(".annotation"))
                {
                    try
                    {
                        AnnoList list = AnnoList.LoadfromFile(filenames[0]);

                        if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                        {
                            foreach (var item in list.Scheme.Labels)
                            {
                                AnnotationSchemeSegment ass = new AnnotationSchemeSegment();
                                ass.Label = item.Name;
                                ass.Color = item.Color;
                                if (!items.Contains(ass) && ass.Label != "GARBAGE" && ass.Label != "") items.Add(ass);
                            }
                        }
                        else if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                        {
                            HashSet<string> labelNames = new HashSet<string>();
                            foreach (AnnoListItem item in list)
                            {
                                labelNames.Add(item.Label);
                            }
                            foreach(string name in labelNames)
                            {                                 
                                AnnotationSchemeSegment ass = new AnnotationSchemeSegment();
                                ass.Label = name;
                                ass.Color = list.Scheme.MaxOrForeColor;
                                if (!items.Contains(ass) && ass.Label != "GARBAGE" && ass.Label != "") items.Add(ass);
                            }
                        }
                        else return;

                        schemeNameTextField.Text = list.Scheme.Name;
                        backroundColorPicket.SelectedColor = list.Scheme.MinOrBackColor;
                    }
                    catch
                    {
                        MessageTools.Warning("This is not a valid annotation file");
                    }
                }
            }
        }
    }

    public class AnnotationSchemeSegment
    {
        public string Label { get; set; }

        public Color Color { get; set; }
    }
}
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
    public partial class DatabaseAdminStreamWindow : Window
    {
        private ObservableCollection<DimLabel> items;
        private DatabaseStream stream;

        public DatabaseAdminStreamWindow(ref DatabaseStream stream)
        {
            InitializeComponent();

            DataContext = this;
            this.stream = stream;

            items = new ObservableCollection<DimLabel>();
            foreach (var label in stream.DimLabels)
            {
                items.Add(new DimLabel() { Dim = label.Key, Name = label.Value });
            }
            LabelsListBox.ItemsSource = items;

            
            name.Text = stream.Name;
            fileExt.Text = stream.FileExt;
            sr.Text = stream.SampleRate.ToString();
            type.Text = stream.Type;

            //if (stream.DimLabels.Count == 0)
            //{
            //    DeleteLabel.Visibility = Visibility.Hidden;
            //}
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            DimLabel item = new DimLabel() { Name = "", Dim = 0 };
            items.Add(item);
            LabelsListBox.SelectedItem = item;
            LabelsListBox.ScrollIntoView(item);
        }

        private void DeleteLabel_Click(object sender, RoutedEventArgs e)
        {
            if(LabelsListBox.Items.Count > 0)
            {
                IEditableCollectionView items = LabelsListBox.Items; //Cast to interface
                if (items.CanRemove)
                {
                    items.Remove(LabelsListBox.SelectedItem);
                }
            }
          
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {

            stream.FileExt = fileExt.Text;
            stream.Name = name.Text;
            stream.Type = type.Text;
            stream.SampleRate = int.Parse(sr.Text);
            List<int> usedLabels = new List<int>();


            //scheme.Name = schemeNameTextField.Text == "" ? Defaults.Strings.Unkown : schemeNameTextField.Text;
            //scheme.Labels.Clear();

            stream.DimLabels.Clear();
            foreach (DimLabel a in LabelsListBox.Items)
            {
                if (!usedLabels.Contains(a.Dim))
                {
                    stream.DimLabels.Add(key: a.Dim, value: a.Name);
                    usedLabels.Add(a.Dim);
                }
            }

          

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }


        private void AnnoList_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null && filenames[0].EndsWith(".streamdict"))
                {
                    //try
                    //{
                    //    AnnoList list = AnnoList.LoadfromFile(filenames[0]);

                    //    if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                    //    {
                    //        foreach (var item in list.Scheme.Labels)
                    //        {
                    //            Label label = new Label() { Name = item.Name, Color = item.Color };
                    //            if (!items.Contains(label) && label.Name != "GARBAGE" && label.Name != "") items.Add(label);
                    //        }
                    //    }
                    //    else if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                    //    {
                    //        HashSet<string> labelNames = new HashSet<string>();
                    //        foreach (AnnoListItem item in list)
                    //        {
                    //            labelNames.Add(item.Label);
                    //        }
                    //        foreach (string name in labelNames)
                    //        {
                    //            Label label = new Label() { Name = name, Color = list.Scheme.MaxOrForeColor };
                    //            if (!items.Contains(label) && label.Name != "GARBAGE" && label.Name != "") items.Add(label);
                    //        }
                    //    }
                    //    else return;

                    //    schemeNameTextField.Text = list.Scheme.Name;

                    //}
                    //catch
                    //{
                    //    MessageTools.Warning("This is not a valid Stream Dimension Dictionary file");
                    //}
                }
            }
        }

        public class DimLabel
        {
            public int Dim { get; set; }
            public string Name { get; set; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
            DimLabel item = new DimLabel() { Name = "", Dim = LabelsListBox.Items.Count };
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
                    try
                    {

                       var input = File.ReadAllLines(filenames[0]);

                        if (!input[0].Contains("="))
                        {
                            for (int i = 0; i < input.Length; i++)
                            {
                                if (input[i] != "" && input[i] != ",")
                                {
                                    if (input[i].EndsWith(","))
                                    {
                                        input[i] = input[i].Remove(input[i].Length - 1, 1);
                                    }

                                    DimLabel item = new DimLabel() { Name = input[i], Dim = i };
                                    items.Add(item);

                                }
                            }
                        }

                        else
                        {
                            for (int i = 0; i < input.Length; i++)
                            {
                                if (input[i] != "" && input[i] != ",")
                                {
                                    if (input[i].EndsWith(","))
                                    {
                                        input[i] = input[i].Remove(input[i].Length - 1, 1);
                                    }


                                    string[] split = input[i].Split('=');

                                    int value;
                                    DimLabel item;
                                    if (int.TryParse(split[0], out value))
                                    {
                                        item = new DimLabel() { Name = split[1], Dim = value };

                                    }
                                    else  if (int.TryParse(split[1], out value))
                                    {
                                        item = new DimLabel() { Name = split[0], Dim = value };
                                    }



                                    else return ;

                                
                                 
                                    items.Add(item);

                                }
                            }
                        }

                       

  

                    }
                    catch
                    {
                        MessageTools.Warning("This is not a valid Stream Dimension Dictionary (streamdict) file");
                    }
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
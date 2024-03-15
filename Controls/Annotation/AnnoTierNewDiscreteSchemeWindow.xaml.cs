using ssi.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Serialization;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAnnoScheme.xaml
    /// </summary>
    public partial class AnnoTierNewDiscreteSchemeWindow : Window
    {
        private ObservableCollection<Label> items;
        private AnnoScheme scheme;

        public AnnoTierNewDiscreteSchemeWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            DataContext = this;
            this.scheme = scheme;

            items = new ObservableCollection<Label>();
            foreach (AnnoScheme.Label label in scheme.Labels)
            {
                items.Add(new Label() { Name = label.Name, Color = label.Color, ID = label.ID });
            }
            LabelsListBox.ItemsSource = items;

            backroundColorPicket.SelectedColor = scheme.MinOrBackColor;
            schemeNameTextField.Text = scheme.Name;     

            if (scheme.Labels.Count > 0)
            {
                DeleteLabel.Visibility = Visibility.Hidden;
            }
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            Label item = new Label() { Name = "", Color = Colors.Black };
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
            List<string> usedLabels = new List<string>();

            scheme.MinOrBackColor = backroundColorPicket.SelectedColor.Value;
            scheme.Name = schemeNameTextField.Text == "" ? Defaults.Strings.Unknown : schemeNameTextField.Text;
            scheme.Labels.Clear();

            foreach (Label a in LabelsListBox.Items)
            {                
                if (a.Name != "" && !usedLabels.Contains(a.Name))
                {
                    scheme.Labels.Add(new AnnoScheme.Label(a.Name, a.Color));
                    usedLabels.Add(a.Name);
                }
            }

            AnnoScheme.Label garbage = new AnnoScheme.Label("GARBAGE", Colors.Black);
            scheme.Labels.Add(garbage);

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
                if (filenames != null && filenames[0].EndsWith(".annotation"))
                {
                    try
                    {
                        AnnoList list = AnnoList.LoadfromFile(filenames[0]);

                        scheme.Examples = list.Scheme.Examples;
                        scheme.LabelAttributes = list.Scheme.LabelAttributes;
                        scheme.Description = list.Scheme.Description;   

                        if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                        {
                            foreach (var item in list.Scheme.Labels)
                            {
                                Label label = new Label() { Name = item.Name, Color = item.Color, ID = item.ID};
                                if (!items.Contains(label) && label.Name != "GARBAGE" && label.Name != "") items.Add(label);
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
                                Label label = new Label() { Name = name, Color = list.Scheme.MaxOrForeColor };
                                if (!items.Contains(label) && label.Name != "GARBAGE" && label.Name != "") items.Add(label);
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

        public class Label
        {
            public string Name { get; set; }
            public Color Color { get; set; }

            public int ID { get; set; }
        }

        private void Attributes_Click(object sender, RoutedEventArgs e)
        {
            AnnoTierAttributesWindow aaw = new AnnoTierAttributesWindow(ref scheme);
            aaw.ShowDialog();
            if (aaw.DialogResult != true)
            {
                return;
            }

        }

        private void Description_Click(object sender, RoutedEventArgs e)
        {
            DescriptionWindow aaw = new DescriptionWindow(ref scheme);
            aaw.ShowDialog();
            if (aaw.DialogResult != true)
            {
                return;
            }
        }

 

        private void Examples_Click(object sender, RoutedEventArgs e)
        {
            AnnoTierExamplesWindow aaw = new AnnoTierExamplesWindow(ref scheme);
            aaw.ShowDialog();
            if (aaw.DialogResult != true)
            {
                return;
            }
        }
    }
}
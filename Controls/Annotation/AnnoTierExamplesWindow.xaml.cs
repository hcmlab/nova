using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static ssi.AnnoTierExamplesWindow;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAnnoScheme.xaml
    /// </summary>
    public partial class AnnoTierExamplesWindow : Window
    {
        private ObservableCollection<LabelAttributeDisplay> items;
        private AnnoScheme scheme;

        public AnnoTierExamplesWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            DataContext = this;
            this.scheme = scheme;
            LabelsListBox.ItemsSource = null;
            items = new ObservableCollection<LabelAttributeDisplay>();


            foreach (AnnoScheme.Example example in scheme.Examples)
            {
                items.Add(new LabelAttributeDisplay() { Name = example.Value, Label = example.Annotation });
            }
            LabelsListBox.ItemsSource = items;
  

    
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            LabelAttributeDisplay item = new LabelAttributeDisplay() { Name = "", Label = ""};
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
            scheme.Examples.Clear();

            foreach (LabelAttributeDisplay a in LabelsListBox.Items)
            {                
                if (a.Name != "" && !usedLabels.Contains(a.Name))
                {
                    LabelAttribute la = new LabelAttribute();
                    List<string> values = new List<string>();
                    AnnoScheme.AttributeTypes type = AnnoScheme.AttributeTypes.LIST;
                    la.Name = Name;



                    scheme.Examples.Add(new AnnoScheme.Example(a.Name, a.Label));
                    usedLabels.Add(a.Name);
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


        public class LabelAttributeDisplay
        {
            public string Name { get; set; }
            public string Label { get; set; }
           
        }


        public class LabelAttribute
        {
            public string Name { get; set; }
            public List<string> Values { get; set; }
            public AnnoScheme.AttributeTypes AttributeType { get; set; }
        }

        private void Attributes_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
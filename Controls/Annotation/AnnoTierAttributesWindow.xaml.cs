using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static ssi.AnnoTierAttributesWindow;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAnnoScheme.xaml
    /// </summary>
    public partial class AnnoTierAttributesWindow : Window
    {
        private ObservableCollection<LabelAttributeDisplay> items;
        private AnnoScheme scheme;

        public AnnoTierAttributesWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();

            DataContext = this;
            this.scheme = scheme;
            LabelsListBox.ItemsSource = null;
            items = new ObservableCollection<LabelAttributeDisplay>();


            foreach (AnnoScheme.Attribute attribute in scheme.LabelAttributes)
            {
                string valuesstring = "";
                foreach(string value in attribute.Values) {
                    valuesstring += value + ";";
                }
                valuesstring = valuesstring.Remove(valuesstring.Length - 1, 1);
                items.Add(new LabelAttributeDisplay() { Name = attribute.Name, Values = valuesstring });
            }
            LabelsListBox.ItemsSource = items;
  

    
        }

        private void AddLabel_Click(object sender, RoutedEventArgs e)
        {
            LabelAttributeDisplay item = new LabelAttributeDisplay() { Name = "", Values = ""};
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
            scheme.LabelAttributes.Clear();

            foreach (LabelAttributeDisplay a in LabelsListBox.Items)
            {                
                if (a.Name != "" && !usedLabels.Contains(a.Name))
                {
                    LabelAttribute la = new LabelAttribute();
                    List<string> values = new List<string>();
                    AnnoScheme.AttributeTypes type = AnnoScheme.AttributeTypes.LIST;
                    la.Name = Name;
                    string[] splitvalues = a.Values.Split(';');
                    foreach(string entry in splitvalues)
                    {
                        values.Add(entry);
                    }

                    if (values[0].ToLower().Contains("true") || values[0].ToLower().Contains("false"))
                    {
                        type = AnnoScheme.AttributeTypes.BOOLEAN;
                    }
                        
                    if (values.Count == 1)
                    {
                        type = AnnoScheme.AttributeTypes.STRING;
                    }


                    scheme.LabelAttributes.Add(new AnnoScheme.Attribute(a.Name, values, type));
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
            public string Values { get; set; }
           
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
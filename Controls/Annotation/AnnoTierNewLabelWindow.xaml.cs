using SharpDX;
using Svg;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using static ssi.AnnoScheme;

namespace ssi
{
    public partial class AnnoTierNewLabelWindow : Window
    {
        private AnnoScheme scheme;

        Dictionary<string, UIElement> result;
        public AnnoListItem Result { get; set; }

        public class Input
        {
            public Input()
            {
                Label = "";
                DefaultValue = "";
                Attributes = new List<string>();
                AttributeType = AnnoScheme.AttributeTypes.STRING;
            }
            public string Label { get; set; }
            public List<string> Attributes { get; set; }
            public string DefaultValue { get; set; }
            public AnnoScheme.AttributeTypes AttributeType { get; set; }
        }
        public string Label { get; set; }
        public string DefaultValue { get; set; }

        public AnnoTierNewLabelWindow(AnnoScheme scheme, AnnoListItem item)
        {
            InitializeComponent();

            discreteSchemeGrid.Visibility = Visibility.Collapsed;
            continuousSchemeGrid.Visibility = Visibility.Collapsed;
            freeSchemeGrid.Visibility = Visibility.Collapsed;

           
      

            Result = item;
            this.scheme = scheme;
            confidenceSlider.Value = item.Confidence;
            colorPicker.SelectedColor = item.Color;
            //descriptiontextBox.Text = item.Meta;
            switch (scheme.Type)
            {
                case TYPE.CONTINUOUS:
                    if (!double.IsNaN(item.Score)) scoreTextBox.Text = item.Score.ToString();
                    else scoreTextBox.Text = "";
                    infoLabel.Text = "Edit continuous label";                    
                    continuousSchemeGrid.Visibility = Visibility.Visible;                    
                    break;

                case TYPE.DISCRETE:
                    infoLabel.Text = "Edit discrete label";
                    discreteSchemeGrid.Visibility = Visibility.Visible;
                    foreach(AnnoScheme.Label label in scheme.Labels)
                    {
                        labelComboBox.Items.Add(label.Name);
                    }                    
                    labelComboBox.SelectedItem = item.Label;
                    AddAttributeUIElements(ref item);



                    break;

                case AnnoScheme.TYPE.FREE:
                    infoLabel.Text = "Edit free label";
                    freeSchemeGrid.Visibility = Visibility.Visible;
                    labelTextBox.Text = item.Label;
                    AddAttributeUIElements(ref item);
                    break;
            }
        }



    private string ParseAttributes(string name, string meta)
        {
            string value = "";
            if (!meta.StartsWith("attributes:"))
            {
                return "";
            }
            else
            {
                meta = meta.Replace("attributes:{", "");
                meta = meta.Replace("}}", "}");
                string[] split = meta.Split(',');
                foreach(string s in split)
                {
                    if (s.Contains(name))
                    {

                        value = s.Split(':')[1];
                        value = value.Replace("{", "");
                        value = value.Replace("}", "");

                        break;
                    }
                }

            }

            return value;
        }
    private void AddAttributeUIElements(ref AnnoListItem item)
    {
        if (scheme.LabelAttributes.Count > 0)
        {

            Dictionary<string, Input> input = new Dictionary<string, Input>();
            
            foreach (var attribute in scheme.LabelAttributes)
            {
                string currentValue = ParseAttributes(attribute.Name, item.Meta);
                string defaultvalue = currentValue == "" ? attribute.Values[0] : currentValue;
                input[attribute.Name] = new Input() { Label = attribute.Name, DefaultValue = defaultvalue, Attributes = attribute.Values, AttributeType = attribute.AttributeType };
            }
            result = new Dictionary<string, UIElement>();
            TextBox firstTextBox = null;
            foreach (KeyValuePair<string, Input> element in input)
            {
                System.Windows.Controls.Label label = new System.Windows.Controls.Label() { Content = element.Value.Label };

                Thickness tk = label.Margin; tk.Left = 5; tk.Right = 0; tk.Bottom = 0; label.Margin = tk;

                inputGrid.Children.Add(label);

                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                inputGrid.RowDefinitions.Add(rowDefinition);

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, inputGrid.RowDefinitions.Count - 1);


                if (element.Value.AttributeType == AnnoScheme.AttributeTypes.STRING)
                {
                    TextBox textBox = new TextBox() { Text = element.Value.DefaultValue };
                    //textBox.GotFocus += TextBox_GotFocus;
                    if (firstTextBox == null)
                    {
                        firstTextBox = textBox;
                    }
                    Thickness margin = textBox.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; textBox.Margin = margin;
                    result.Add(element.Key, textBox);
                    inputGrid.Children.Add(textBox);
                    if (firstTextBox != null)
                    {
                        firstTextBox.Focus();
                    }

                    Grid.SetColumn(textBox, 1);
                    Grid.SetRow(textBox, inputGrid.RowDefinitions.Count - 1);
                }
                else if (element.Value.AttributeType == AnnoScheme.AttributeTypes.BOOLEAN)
                {
                    CheckBox cb = new CheckBox()
                    {
                        IsChecked = (element.Value.DefaultValue.ToLower() == "false") ? false : true
                    };

                    Thickness margin = cb.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; cb.Margin = margin;
                    result.Add(element.Key, cb);
                    inputGrid.Children.Add(cb);


                    Grid.SetColumn(cb, 1);
                    Grid.SetRow(cb, inputGrid.RowDefinitions.Count - 1);
                }
                else if (element.Value.AttributeType == AnnoScheme.AttributeTypes.LIST)
                {
                    ComboBox cb = new ComboBox()
                    {
                        ItemsSource = element.Value.Attributes
                    };
                    cb.SelectedItem = element.Value.DefaultValue;
                    Thickness margin = cb.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; cb.Margin = margin;
                    result.Add(element.Key, cb);
                    inputGrid.Children.Add(cb);

                    Grid.SetColumn(cb, 1);
                    Grid.SetRow(cb, inputGrid.RowDefinitions.Count - 1);
                }
            }
            }
        }


        public string AttributesResult()
        {
            string attributejson = "attributes:{";
            foreach(var element in result)
            {
              //  if(element.Value.GetType() GetType().ToString() == "System.Windows.Controls.TextBox")
                {
                    if(element.Value.GetType().Name == "CheckBox")
                    {
                        attributejson = attributejson + element.Key + ":{"+ ((CheckBox)element.Value).IsChecked + "},";
                    }
                    else if (element.Value.GetType().Name == "ComboBox")
                    {
                        attributejson = attributejson + element.Key + ":{" + ((ComboBox)element.Value).SelectedItem + "},";
                    }
                    else if (element.Value.GetType().Name == "TextBox")
                    {
                        attributejson = attributejson + element.Key + ":{" + ((TextBox)element.Value).Text + "},";
                    }
                    //var test = element.Value.ToString() ;
                }


            }

            attributejson = attributejson.Remove(attributejson.Length - 1, 1);
     
            attributejson = attributejson + "}";
            return attributejson;
        }


        private void okButton()
        {
            DialogResult = true;

            Result.Confidence = confidenceSlider.Value;
            Result.Color = colorPicker.SelectedColor.Value;

            if(scheme.LabelAttributes.Count > 0)
            {
                Result.Meta = AttributesResult();
            }


            //Result.Meta = descriptiontextBox.Text;

            switch (scheme.Type)
            {
                case AnnoScheme.TYPE.CONTINUOUS:

                    if (scoreTextBox.Text == "")
                    {
                        Result.Score = double.NaN;
                    }
                    else
                    {
                        double score = 0;
                        double.TryParse(scoreTextBox.Text, out score);
                        Result.Score = score;

                    }
                    break;

                case AnnoScheme.TYPE.DISCRETE:
                    Result.Label = (string)labelComboBox.SelectedItem;
                    Result.Color = scheme.Labels.Find(x => x.Name == Result.Label).Color;
                    break;

                case AnnoScheme.TYPE.FREE:
                    Result.Label = labelTextBox.Text;
                    break;
            }

            Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            okButton();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
        
        private void confidenceSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            confidenceLabelValue.Content = Math.Round(this.confidenceSlider.Value, 2).ToString();
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

    }
}
using Svg;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    public partial class AnnoTierNewLabelWindow : Window
    {
        private AnnoScheme scheme;

        public AnnoListItem Result { get; set; }

        public AnnoTierNewLabelWindow(AnnoScheme scheme, AnnoListItem item)
        {
            InitializeComponent();

            discreteSchemeGrid.Visibility = Visibility.Collapsed;
            continuousSchemeGrid.Visibility = Visibility.Collapsed;
            freeSchemeGrid.Visibility = Visibility.Collapsed;

            if(scheme.LabelAttributes.Count > 0)
            {
                foreach(var attribute in scheme.LabelAttributes) {
                    if (attribute.AttributeType == AnnoScheme.AttributeTypes.BOOLEAN)
                    {
                        CheckBox cb = new CheckBox();
                        cb.IsChecked = (attribute.Values[0].ToLower() == "false") ? false : true;
                        Label label = new Label();
                        label.Content = attribute.Name;
                        //TODO ADD ELEMENTS TO UI
                    }
                }

            }
      

            Result = item;
            this.scheme = scheme;
            confidenceSlider.Value = item.Confidence;
            colorPicker.SelectedColor = item.Color;
            descriptiontextBox.Text = item.Meta;
            switch (scheme.Type)
            {
                case AnnoScheme.TYPE.CONTINUOUS:
                    if (!double.IsNaN(item.Score)) scoreTextBox.Text = item.Score.ToString();
                    else scoreTextBox.Text = "";
                    infoLabel.Text = "Edit continuous label";                    
                    continuousSchemeGrid.Visibility = Visibility.Visible;                    
                    break;

                case AnnoScheme.TYPE.DISCRETE:
                    infoLabel.Text = "Edit discrete label";
                    discreteSchemeGrid.Visibility = Visibility.Visible;
                    foreach(AnnoScheme.Label label in scheme.Labels)
                    {
                        labelComboBox.Items.Add(label.Name);
                    }                    
                    labelComboBox.SelectedItem = item.Label;
                    break;

                case AnnoScheme.TYPE.FREE:
                    infoLabel.Text = "Edit free label";
                    freeSchemeGrid.Visibility = Visibility.Visible;
                    labelTextBox.Text = item.Label;
                    break;
            }
        }

        private void okButton()
        {
            DialogResult = true;

            Result.Confidence = confidenceSlider.Value;
            Result.Color = colorPicker.SelectedColor.Value;

            if(scheme.LabelAttributes.Count != 0)
            {


            }


            Result.Meta = descriptiontextBox.Text;

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

      
    }
}
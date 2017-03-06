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

            Result = item;
            this.scheme = scheme;
            confidenceSlider.Value = item.Confidence;
            colorPicker.SelectedColor = item.Color;

            switch (scheme.Type)
            {
                case AnnoScheme.TYPE.CONTINUOUS:
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
                    labelTextBox.Focus();
                    labelTextBox.SelectAll();
                    break;
            }
        }
        
        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            Result.Confidence = confidenceSlider.Value;
            Result.Color = colorPicker.SelectedColor.Value;

            switch (scheme.Type)
            {
                case AnnoScheme.TYPE.CONTINUOUS:
                    Result.Label = "0";
                    double score = 0;
                    if (double.TryParse(scoreTextBox.Text, out score))
                    {
                        Result.Label = scoreTextBox.Text;
                    }
                    break;

                case AnnoScheme.TYPE.DISCRETE:
                    Result.Label = (string) labelComboBox.SelectedItem;
                    Result.Color = scheme.Labels.Find(x => x.Name == Result.Label).Color;
                    break;

                case AnnoScheme.TYPE.FREE:
                    Result.Label = labelTextBox.Text;                    
                    break;
            }

            Close();
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
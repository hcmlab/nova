using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for LabelInputBox.xaml
    /// </summary>
    public partial class LabelInputBox : Window, INotifyPropertyChanged
    {
        private Color color1;
        private bool hascolorpicker = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color sColor
        {
            get
            {
                return this.color1;
            }

            set
            {
                this.color1 = value;

                this.RaisePropertyChanged("sColor");
            }
        }

        public SolidColorBrush Brush { get; set; }

        protected void RaisePropertyChanged(string property)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

        private HashSet<LabelColorPair> suggestions;

        public LabelInputBox(String header, String info, String text, HashSet<LabelColorPair> _suggestions = null, int fields = 1, String text2 = "", String text3 = "", bool _hascolorpicker = false, bool usesannotationscheme = false)
        {
            this.Brush = Brushes.Black;
            this.sColor = Brush.Color;
            this.DataContext = this;
            InitializeComponent();
            this.Title = header;
            this.ib_label.Content = info;
            this.ib_labelText.Text = text;

       
            this.suggestions = _suggestions;
            this.hascolorpicker = _hascolorpicker;

            if (fields == 2)
            {
                this.ib_labelText2.Visibility = Visibility.Visible;
                this.ib_labelText2.Text = text2;
            }
            if (fields == 3)
            {
                this.ib_labelText2.Visibility = Visibility.Visible;
                this.ib_labelText2.Text = text2;
                this.ib_labelText3.Visibility = Visibility.Visible;
                this.ib_labelText3.Text = text3;
            }

            if (_suggestions == null)
            {
                this.ib_suggestions_comboBox.Visibility = Visibility.Hidden;
            }

            if (hascolorpicker == true)
            {
                ib_color.Visibility = Visibility.Visible;

                if (suggestions != null)
                {
                    if (suggestions != null && suggestions.Count < 1)
                    {
                        Brush = Brushes.Black;
                        color1 = Brush.Color;
                        this.sColor = Brush.Color;
                    }
                    else
                    {
                        int i = 0;
                        foreach (LabelColorPair s in suggestions)
                        {
                            if (i == this.ib_suggestions_comboBox.SelectedIndex + 1)
                            {
                                Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(s.Color));
                            }
                            i++;
                        }
                        color1 = Brush.Color;
                        this.sColor = Brush.Color;
                    }

                    foreach (LabelColorPair s in suggestions)
                    {
                        this.ib_suggestions_comboBox.Items.Add(s.Label);
                    }
                    this.ib_suggestions_comboBox.SelectionChanged += ib_suggestions_comboBox_SelectionChanged;
                    if (this.ib_suggestions_comboBox.Items.Count > 0)
                    {
                        if (text != null && text != "") this.ib_suggestions_comboBox.SelectedIndex = this.ib_suggestions_comboBox.Items.IndexOf(text);
                        else this.ib_suggestions_comboBox.SelectedIndex = AnnoTrack.GetSelectedTrack().track_used_labels_last_index;
                    }
                }
                else //todo: only other case now that uses colorpicker and not suggestion list. probably change this in the future to some more advanced logic
                {
                    color1 = Colors.Black; // ((SolidColorBrush)(AnnoTrack.GetSelectedTrack().BackgroundColor)).Color;
                    this.sColor = color1;
                }
            }
            else
            {
                if (_suggestions != null)
                {
                    foreach (LabelColorPair s in suggestions)
                    {
                        this.ib_suggestions_comboBox.Items.Add(s.Label);
                    }

                    if (this.ib_suggestions_comboBox.Items.Count > 0)
                    {
                        this.ib_suggestions_comboBox.SelectedIndex = 0;
                    }
                }
            }

            if (usesannotationscheme == true)
            {
                this.ib_labelText.IsEnabled = false;
                this.ib_color.IsEnabled = false;
            }
            else this.ib_labelText.SelectAll();
        }

        public void setPWfield(string text)
        {
            this.ib_labelpass.Visibility = Visibility.Visible;
            this.ib_labelpass.Password = text;
        }

        public void showSlider(bool showslider, double Confidence)
        {
            if (showslider)
            {
                this.confidencelabel.Visibility = Visibility.Visible;
                this.confidencelabelvalue.Visibility = Visibility.Visible;
                this.Slider.Visibility = Visibility.Visible;
                this.Slider.Value = Confidence;
            }
        }

        public double ResultSlider()
        {
            return Math.Round(this.Slider.Value, 2);
        }

        public string ResultPw()
        {
            return this.ib_labelpass.Password;
        }

        private void ib_suggestions_comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (hascolorpicker)
            {
                if (e.AddedItems.Count > 0)
                {
                    this.ib_labelText.Text = (string)e.AddedItems[0];
                    AnnoTrack.GetSelectedTrack().track_used_labels_last_index = this.ib_suggestions_comboBox.SelectedIndex;
                    if (suggestions != null)
                    {
                        int i = 0;
                        foreach (LabelColorPair s in suggestions)
                        {
                            if (i == this.ib_suggestions_comboBox.SelectedIndex)
                            {
                                Brush = (SolidColorBrush)(new BrushConverter().ConvertFrom(s.Color));
                            }
                            i++;
                        }
                        color1 = Brush.Color;
                        this.sColor = Brush.Color;
                    }
                }
            }
        }

        private void ib_ok_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        private void ib_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        public string Result()
        {
            if (this.ib_suggestions_comboBox.SelectedItem != null)
            {
                if (this.ib_labelText.Text != this.ib_suggestions_comboBox.SelectedItem.ToString())
                {
                    AnnoTrack.GetSelectedTrack().track_used_labels_last_index = this.ib_suggestions_comboBox.Items.Count;
                }
            }

            return this.ib_labelText.Text;
        }

        public string Result2()
        {
            return this.ib_labelText2.Text;
        }

        public string Result3()
        {
            return this.ib_labelText3.Text;
        }

        public string Color()
        {
            return color1.ToString();
        }

        public string SelectedItem()
        {
            return this.ib_suggestions_comboBox.SelectedItem.ToString();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            confidencelabelvalue.Content = Math.Round(this.Slider.Value, 2).ToString();
        }
    }
}
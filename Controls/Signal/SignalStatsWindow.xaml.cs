using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Input;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für SignalStatsWindow.xaml
    /// </summary>
    public partial class SignalStatsWindow : Window
    {
        private Signal signal;
        private int selected_dim;
        private List<Brush> brushes = new List<Brush>();
        private ResourceDictionaryCollection pieSeriesPalette;
        private int numclasses = 5;
        private bool isActive = false;

        public Timeline vt = MainHandler.Time;

        public SignalStatsWindow(Signal _signal, int selected_dimension)
        {
            InitializeComponent();

            this.signal = _signal;
            this.selected_dim = selected_dimension;
            this.Title = signal.FileName + " Dimension: " + selected_dimension;

            this.MinBox.Text = signal.min[selected_dimension].ToString();
            this.MaxBox.Text = signal.max[selected_dimension].ToString();
            this.isActive = true;
            calculateValues();
        }

        private void calculateValues()
        {
            ((PieSeries)mcChart.Series[0]).ItemsSource = null;
            selectHeatColors();
            List<KeyValuePair<string, int>> keyvalues = new List<KeyValuePair<string, int>>();
            int[] count = new int[numclasses];
            for (int i = 0; i < numclasses; i++)
            {
                count[i] = 0;
            }

            float minVal = float.Parse(MinBox.Text);
            float maxVal = float.Parse(MaxBox.Text);
            float factor = (maxVal - minVal) / numclasses;

            double average = 0;
            double averagesamples = 0;

            for (int d = selected_dim; d <= selected_dim; d++)
            {
                
                for (int i = 0; i < signal.number; i++)
                {
                    if (MainHandler.Time.SelectionStart < i / signal.rate && MainHandler.Time.SelectionStop > i / signal.rate)
                    {
                        average += signal.data[i * signal.dim + d];
                        averagesamples++;
                        for (int j = 1; j <= numclasses; j++)
                        {
                            float val = signal.data[i * signal.dim + d];
                            
                            if (val <= ( minVal + j * factor) && val >= (minVal + (j - 1) * factor))
                            {
                                count[j - 1]++;
                                break;
                            }
                        }
                    }
                }
            }

            average = average / averagesamples;
            AvgBox.Text = average.ToString();

            keyvalues.Clear();

            for(int i = 0; i< numclasses; i++)
            {
                keyvalues.Add(new KeyValuePair<string, int>((minVal + i * factor) + "-" + (minVal + (i+1) * factor), count[i]));
            }

            ((PieSeries)mcChart.Series[0]).ItemsSource = keyvalues;
        }

        public void timeRangeChanged(Timeline time)
        {
           if(this.isActive) calculateValues();
        }

      
        private void setPalette()
        {
            this.mcChart.Palette.Clear();

            pieSeriesPalette = new System.Windows.Controls.DataVisualization.ResourceDictionaryCollection();
            foreach (Brush brush in brushes)
            {
                System.Windows.ResourceDictionary pieDataPointStyles = new ResourceDictionary();
                Style stylePie = new Style(typeof(PieDataPoint));
                stylePie.Setters.Add(new Setter(PieDataPoint.TemplateProperty, (ControlTemplate)FindResource("MyPieDataPointTemplate")));
                stylePie.Setters.Add(new Setter(PieDataPoint.BackgroundProperty, brush));
                pieDataPointStyles.Add("DataPointStyle", stylePie);
                pieSeriesPalette.Add(pieDataPointStyles);
            }

            this.mcChart.Palette = pieSeriesPalette;
        }

        private void selectHeatColors()
        {
            brushes.Clear();
            brushes.Add(new SolidColorBrush(Color.FromArgb(0xFF, 0xF8, 0x26, 0x00)));
            brushes.Add(new SolidColorBrush(Color.FromArgb(0xFF, 0xFC, 0x77, 0x00)));
            brushes.Add(new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xAB, 0x1F)));
            brushes.Add(new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xC3, 0x60)));
            brushes.Add(new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xDD, 0xA4)));

            setPalette();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
           Regex regex = new Regex("^[.][0-9]+$|^[\\-]?[0-9]*[.]{0,1}[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }

        private void BoxKeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Return || e.Key == Key.Enter)
            {
                calculateValues();
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.isActive = false;
        }
    }
}
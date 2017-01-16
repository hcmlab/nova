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
        private uint selected_dim;
        private List<Brush> brushes = new List<Brush>();
        private ResourceDictionaryCollection pieSeriesPalette;
        private int numclasses = 5;
        private bool isActive = false;

        public Timeline vt = MainHandler.Time;

        public SignalStatsWindow(Signal _signal, uint selected_dimension)
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


            //TODO:
            //Some smarter logic to scale, maybe even make the borders adjustable by the user
            for (uint d = selected_dim; d <= selected_dim; d++)
            {
                float minVal = float.Parse(MinBox.Text);
                float maxVal = float.Parse(MaxBox.Text);
                float factor = (maxVal - minVal) / numclasses;
                for (int i = 0; i < signal.number; i++)
                {
                    if (MainHandler.Time.SelectionStart < i / signal.rate && MainHandler.Time.SelectionStop > i / signal.rate)
                    {
                        for (int j = 1; j <= numclasses; j++)
                        {
                            if ((signal.data[i * signal.dim + d]) <( minVal + j * factor) && (signal.data[i * signal.dim + d]) >= (minVal + (j - 1) * factor))
                            {
                                count[j - 1]++;
                            }
                        }
                    }
                }
            }


            keyvalues.Clear();
            keyvalues.Add(new KeyValuePair<string, int>("Very Low", count[0]));
            keyvalues.Add(new KeyValuePair<string, int>("Low", count[1]));
            keyvalues.Add(new KeyValuePair<string, int>("Medium", count[2]));
            keyvalues.Add(new KeyValuePair<string, int>("High", count[3]));
            keyvalues.Add(new KeyValuePair<string, int>("Very High", count[4]));
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
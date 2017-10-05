using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ssi
{
    /// <summary>
    /// Interaction logic for ConfmatWindow.xaml
    /// </summary>
    public partial class ConfmatWindow : Window
    {
        string[] lines = null;
        string content = "";
        Dictionary<string, int> meta = null;
        double ua = 0, wa = 0;
        int nc = 0;
        string[] classes = null;
        int[,] counts = null;        
        int[] total = null;
        double[] acc = null;

        GradientStopCollection colormap = null;

        private static Color getColorByOffset(GradientStopCollection collection, double offset)
        {
            GradientStop[] stops = collection.OrderBy(x => x.Offset).ToArray();
            if (offset <= 0) return stops[0].Color;
            if (offset >= 1) return stops[stops.Length - 1].Color;
            GradientStop left = stops[0], right = null;
            foreach (GradientStop stop in stops)
            {
                if (stop.Offset >= offset)
                {
                    right = stop;
                    break;
                }
                left = stop;
            }            
            offset = Math.Round((offset - left.Offset) / (right.Offset - left.Offset), 2);
            byte a = (byte)((right.Color.A - left.Color.A) * offset + left.Color.A);
            byte r = (byte)((right.Color.R - left.Color.R) * offset + left.Color.R);
            byte g = (byte)((right.Color.G - left.Color.G) * offset + left.Color.G);
            byte b = (byte)((right.Color.B - left.Color.B) * offset + left.Color.B);
            return Color.FromArgb(a, r, g, b);
        }

        public ConfmatWindow(string path)
        {
            InitializeComponent();

            colormap = new GradientStopCollection();
            colormap.Add(new GradientStop(Colors.Red, 0.0));
            colormap.Add(new GradientStop(Colors.Blue, 1.0));

            parse(path);
            showMeta();
            showCM();

            metaGrid.Visibility = Visibility.Collapsed;
        }

        void showCM()
        {
            if (classes != null && counts != null)
            {
                {
                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                    cmGrid.RowDefinitions.Add(rowDefinition);
                }
                for (int i = 0; i < nc; i++)
                {
                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Star);
                    cmGrid.RowDefinitions.Add(rowDefinition);
                }            
                {
                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                    cmGrid.RowDefinitions.Add(rowDefinition);
                }
                {
                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                    cmGrid.RowDefinitions.Add(rowDefinition);
                }

                {
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    columnDefinition.Width = new GridLength(1, GridUnitType.Auto);
                    cmGrid.ColumnDefinitions.Add(columnDefinition);
                }            
                for (int i = 0; i < nc; i++)
                {
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    columnDefinition.Width = new GridLength(1, GridUnitType.Star);
                    cmGrid.ColumnDefinitions.Add(columnDefinition);
                }
                {
                    ColumnDefinition columnDefinition = new ColumnDefinition();
                    columnDefinition.Width = new GridLength(1, GridUnitType.Auto);
                    cmGrid.ColumnDefinitions.Add(columnDefinition);
                }

                for (int i = 0; i < nc; i++)
                {
                    Label label = new Label() { Content = classes[i], HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
                    cmGrid.Children.Add(label);                 
                    Grid.SetColumn(label, i + 1);
                    Grid.SetRow(label, 0);
                }
                for (int i = 0; i < nc; i++)
                {
                    Label label = new Label() { Content = classes[i], HorizontalContentAlignment = HorizontalAlignment.Right, VerticalContentAlignment = VerticalAlignment.Center };
                    cmGrid.Children.Add(label);
                    Grid.SetColumn(label, 0);
                    Grid.SetRow(label, i + 1);
                }

                for (int i = 0; i < nc; i++)
                {
                    for (int j = 0; j < nc; j++)
                    {
                        double value = counts[i, j] / ((double)total[i]);                        
                        Label label = new Label() { Content = Math.Round(100*value,1), HorizontalContentAlignment = HorizontalAlignment.Center, VerticalContentAlignment = VerticalAlignment.Center };
                        label.Background = new SolidColorBrush(getColorByOffset(colormap, value));
                        label.Foreground = Brushes.White;
                        cmGrid.Children.Add(label);
                        Grid.SetColumn(label, i + 1);
                        Grid.SetRow(label, j + 1);
                    }
                }

                for (int i = 0; i < nc; i++)
                {
                    double value = Math.Round(acc[i], 1);                    
                    Label label = new Label() { Content = value + " %", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center };
                    cmGrid.Children.Add(label);
                    Grid.SetColumn(label, nc + 1);
                    Grid.SetRow(label, i + 1);
                }
                {
                    string value = Math.Round(ua,1) + " % (" + Math.Round(wa,1) + " %)";
                    Label label = new Label() { Content = value, HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center };
                    label.FontWeight = FontWeights.Bold;
                    cmGrid.Children.Add(label);
                    Grid.SetColumn(label, nc + 1);
                    Grid.SetRow(label, nc + 1);
                }
                {
                    Label startLabel = new Label() { Content = "0 %", HorizontalContentAlignment = HorizontalAlignment.Right, VerticalContentAlignment = VerticalAlignment.Center };
                    Label stopLabel = new Label() { Content = "100 %", HorizontalContentAlignment = HorizontalAlignment.Left, VerticalContentAlignment = VerticalAlignment.Center };
                    Canvas canvas = new Canvas() { Background = new LinearGradientBrush(colormap) };
                    cmGrid.Children.Add(startLabel);
                    Grid.SetColumn(startLabel, 0);
                    Grid.SetRow(startLabel, nc + 2);
                    cmGrid.Children.Add(canvas);
                    Grid.SetColumn(canvas, 1);
                    Grid.SetRow(canvas, nc + 2);                    
                    Grid.SetColumnSpan(canvas, nc);
                    cmGrid.Children.Add(stopLabel);
                    Grid.SetColumn(stopLabel, nc + 1);
                    Grid.SetRow(stopLabel, nc + 2);                    
                }
            }
        }
    

        void showMeta()
        {
            if (meta != null)
            {
                foreach (KeyValuePair<string, int> item in meta)
                {
                    Label labelKey = new Label() { Content = item.Key };
                    metaGrid.Children.Add(labelKey);

                    Label labelValue = new Label() { Content = item.Value.ToString() };                                        
                    metaGrid.Children.Add(labelValue);

                    RowDefinition rowDefinition = new RowDefinition();
                    rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                    metaGrid.RowDefinitions.Add(rowDefinition);

                    Grid.SetColumn(labelKey, 0);
                    Grid.SetRow(labelKey, metaGrid.RowDefinitions.Count - 1);
                    Grid.SetColumn(labelValue, 1);
                    Grid.SetRow(labelValue, metaGrid.RowDefinitions.Count - 1);
                }
            }
        }

        void parse(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            content = File.ReadAllText(path);
            lines = File.ReadAllLines(path);

            if (lines == null)
            {
                return;
            }

            meta = new Dictionary<string, int>();

            int line = 0;

            for (int i = 0; i < 5 && lines.Length > line; i++, line++)
            {
                string[] tokens = lines[line].Split(';');
                if (tokens.Length == 2)
                {
                    string key = tokens[0];
                    int value = 0;
                    int.TryParse(tokens[1], out value);
                    meta.Add(key, value);
                }                
            }

            if (meta.ContainsKey("#classes"))
            {
                nc = meta["#classes"];
            }
            
            if (nc <= 0)
            {
                return;
            }

            line++;
            classes = new string[nc];            
            counts = new int[nc,nc];
            acc = new double[nc];
            total = new int[nc];
            for (int i = 0; i < nc && lines.Length > line; i++, line++)
            {
                string[] tokens = lines[line].Split(';');
                if (tokens.Length == nc + 2)
                {
                    classes[i] = tokens[0];
                    acc[i] = 0;
                    total[i] = 0;
                    for (int j = 0; j < nc; j++)
                    {
                        counts[i, j] = 0;
                        int.TryParse(tokens[j + 1], out counts[i, j]);
                        total[i] += counts[i, j];
                    }
                    double.TryParse(tokens[nc + 1], out acc[i]);
                }
            }

            if (lines.Length > line)
            {
                string[] tokens = lines[line++].Split(';');
                double.TryParse(tokens[nc + 1], out ua);
                double.TryParse(tokens[nc + 2], out wa);
            }
        }            

        private void CopyClick(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(content);
        }

        private void SaveClick(object sender, RoutedEventArgs e)
        {
            string path = FileTools.SaveFileDialog("eval.csv", "csv", "CSV files|*.csv|All files (*.*)|*.*", "");
            if (path != null)
            {
                File.WriteAllText(path, content);
            }
        }

        private void MetaChecked(object sender, RoutedEventArgs e)
        {
            metaGrid.Visibility = Visibility.Visible;
        }

        private void MetaUnchecked(object sender, RoutedEventArgs e)
        {
            metaGrid.Visibility = Visibility.Collapsed;
        }

        private void CloseClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

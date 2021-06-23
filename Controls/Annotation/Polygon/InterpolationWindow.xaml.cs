using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ssi
{
    public partial class InterpolationWindow : Window
    {
        AnnoList list;
        MainHandler mainHandler;
        public InterpolationWindow(AnnoList list, MainHandler mainHandler)
        {
            InitializeComponent();
            this.list = list;
            this.mainHandler = mainHandler;

            foreach (AnnoListItem item in this.list)
            {
                SourceFrames.Items.Add(new ComboboxItem(item.Label, item));
                TargetFrames.Items.Add(new ComboboxItem(item.Label, item));
            }
            SourceFrames.SelectedItem = SourceFrames.Items[0];
            TargetFrames.SelectedItem = TargetFrames.Items[0];
        }

        private void Interpolate_Click(object sender, RoutedEventArgs e)
        {
            if(SourceFrames.SelectedIndex < TargetFrames.SelectedIndex && SourceLabelsListBox.SelectedValue != null && TargetLabelsListBox.SelectedValue != null)
            {

                PolygonLabel selectedSourcePolygon = (PolygonLabel)SourceLabelsListBox.SelectedValue;
                PolygonLabel selectedTargetPolygon = (PolygonLabel)TargetLabelsListBox.SelectedValue;
                double selectedSourcePolygonArea = calculatePolygonArea(selectedSourcePolygon.Polygon);
                double selectedTargetPolygonArea = calculatePolygonArea(selectedTargetPolygon.Polygon);
                
                Point pSource = calculatePolygonMidPoint(selectedSourcePolygon.Polygon, selectedSourcePolygonArea);
                Point pTarget = calculatePolygonMidPoint(selectedTargetPolygon.Polygon, selectedTargetPolygonArea);

                double xDif = pTarget.X - pSource.X;
                double yDif = pTarget.Y - pSource.Y;

                int framesBetween = TargetFrames.SelectedIndex - SourceFrames.SelectedIndex;

                double xStepPerFrame = xDif / framesBetween;
                double yStepPerFrame = yDif / framesBetween;

                mainHandler.handleInterpolation((ComboboxItem)SourceFrames.SelectedItem, selectedSourcePolygon, selectedTargetPolygon, xStepPerFrame, yStepPerFrame, framesBetween);
                DialogResult = true;
                Close();
            }
            else
            {
                MessageBoxResult mb = MessageBoxResult.OK;
                mb = MessageBox.Show("Not able to interpolate with the selected values.", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private double calculatePolygonArea(List<PolygonPoint> vertices)
        {
            double sum = 0;
            for (int i = 0; i < vertices.Count; i++)
            {
                if (i == vertices.Count - 1)
                {
                    sum += vertices[i].X * vertices[0].Y - vertices[0].X * vertices[i].Y;
                }
                else
                {
                    sum += vertices[i].X * vertices[i + 1].Y - vertices[i + 1].X * vertices[i].Y;
                }
            }

            double area = 0.5 * Math.Abs(sum);
            return area;
        }

        private Point calculatePolygonMidPoint(List<PolygonPoint> vertices, double area)
        {
            double Cx = 0.0f;
            double Cy = 0.0f;
            double tmp = 0.0f;
            int k;

            for (int i = 0; i <= vertices.Count - 1; i++)
            {
                k = (i + 1) % (vertices.Count);
                tmp = vertices[i].X * vertices[k].Y - vertices[k].X * vertices[i].Y;
                Cx += (vertices[i].X + vertices[k].X) * tmp;
                Cy += (vertices[i].Y + vertices[k].Y) * tmp;
            }
            Cx *= 1.0 / (6.0 * area);
            Cy *= 1.0 / (6.0 * area);

            return new Point(Cx, Cy);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void sourceFrameChanged(object sender, RoutedEventArgs e)
        {
            ObservableCollection<PolygonLabel> items = new ObservableCollection<PolygonLabel>();
            PolygonList polygonList = ((ComboboxItem)SourceFrames.SelectedValue).Value.PolygonList;
            foreach (PolygonLabel label in polygonList.Polygons)
            {
                items.Add(label);
            }
            SourceLabelsListBox.ItemsSource = items;
            //2. Bild anpassen
        }

        private void sourceLabelChanged(object sender, RoutedEventArgs e)
        {
            //1. Bild anpassen (selektiertes Label Fett zeichnen)
        }

        private void targetFrameChanged(object sender, RoutedEventArgs e)
        {
            ObservableCollection<PolygonLabel> items = new ObservableCollection<PolygonLabel>();
            PolygonList polygonList = ((ComboboxItem)TargetFrames.SelectedValue).Value.PolygonList;
            foreach (PolygonLabel label in polygonList.Polygons)
            {
                items.Add(label);
            }
            TargetLabelsListBox.ItemsSource = items;
            //2. Bild anpassen
        }

        private void targetLabelChanged(object sender, RoutedEventArgs e)
        {
            //1. Bild anpassen (selektiertes Label Fett zeichnen)
        }
    }

    public class ComboboxItem
    {
        public string Text { get; set; }
        public AnnoListItem Value { get; set; }

        public ComboboxItem(string text, AnnoListItem value)
        {
            Text = text;
            Value = value;
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
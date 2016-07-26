using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace ssi
{
    internal class ContinuousColorTheme
    {
        private static LinearGradientBrush myBrush = new LinearGradientBrush();
        private static List<String> list = new List<String>();

        public ContinuousColorTheme()
        {
        }

        public static List<String> List
        {
            get
            {
                list.Clear();
                list.Add("BlueRed");
                list.Add("RedBlue");
                list.Add("Heatmap");
                return list;
            }
        }

        public static LinearGradientBrush RedBlue
        {
            get
            {
                myBrush = new LinearGradientBrush();
                myBrush.StartPoint = new Point(0, 0);
                myBrush.EndPoint = new Point(0, 1);
                myBrush.GradientStops.Add(new GradientStop(Colors.Blue, 0));
                myBrush.GradientStops.Add(new GradientStop(Colors.Red, 1));
                return myBrush;
            }
        }

        public static LinearGradientBrush BlueRed
        {
            get
            {
                myBrush = new LinearGradientBrush();
                myBrush.StartPoint = new Point(0, 0);
                myBrush.EndPoint = new Point(0, 1);
                myBrush.GradientStops.Add(new GradientStop(Colors.Red, 0));
                myBrush.GradientStops.Add(new GradientStop(Colors.Blue, 1));
                return myBrush;
            }
        }

        public static LinearGradientBrush Heatmap
        {
            get
            {
                myBrush = new LinearGradientBrush();
                myBrush.StartPoint = new Point(0, 0);
                myBrush.EndPoint = new Point(0, 1);

                myBrush.GradientStops.Add(new GradientStop(Color.FromArgb(255, 255, 0, 0), 0));
                myBrush.GradientStops.Add(new GradientStop(Color.FromArgb(216, 255, 255, 0), 0.33));
                myBrush.GradientStops.Add(new GradientStop(Color.FromArgb(140, 0, 255, 0), 0.66));
                myBrush.GradientStops.Add(new GradientStop(Color.FromArgb(164, 0, 0, 255), 1));

                return myBrush;
            }
        }

        //Add more here if needed, check them in viewhandler resultbrush.

        public static LinearGradientBrush RedYellowGreen
        {
            get
            {
                myBrush = new LinearGradientBrush();
                myBrush.StartPoint = new Point(0, 0);
                myBrush.EndPoint = new Point(0, 1);
                myBrush.GradientStops.Add(new GradientStop(Colors.Red, 0));
                myBrush.GradientStops.Add(new GradientStop(Colors.Yellow, 0.5));
                myBrush.GradientStops.Add(new GradientStop(Colors.Green, 1));
                return myBrush;
            }
        }
    }
}
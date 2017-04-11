using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for AnnoTierControl.xaml
    /// </summary>
    public partial class AnnoTierControl : UserControl
    {
        public double currentTime = 0;

        public AnnoTierControl()
        {
            InitializeComponent();
        }

        public void Add(AnnoTier tier)
        {
            for (int i = 0; i < grid.RowDefinitions.Count; i += 2)
            {
                grid.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
            }

            if (grid.RowDefinitions.Count > 0)
            {
                // add splitter
                RowDefinition split = new RowDefinition();
                split.Height = new GridLength(1, GridUnitType.Auto);
                grid.RowDefinitions.Add(split);
                GridSplitter splitter = new GridSplitter();
                splitter.Background = Defaults.Brushes.Splitter;
                splitter.ResizeDirection = GridResizeDirection.Rows;
                splitter.Height = 5;
                splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                splitter.VerticalAlignment = VerticalAlignment.Stretch;
                splitter.ShowsPreview = true;
                Grid.SetColumnSpan(splitter, 1);
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, grid.RowDefinitions.Count - 1);
                grid.Children.Add(splitter);
            }

            StackPanel sp = new StackPanel();
            sp.Orientation = Orientation.Horizontal;

            // add anno tier
            RowDefinition row = new RowDefinition();
            row.Height = new GridLength(1, GridUnitType.Star);
            grid.RowDefinitions.Add(row);

            Grid.SetColumn(tier, 0);
            Grid.SetRow(tier, grid.RowDefinitions.Count - 1);
            grid.Children.Add(tier);

            Label label = new Label();
            label.Content = " " + tier.AnnoList.Scheme.Name;
            label.HorizontalAlignment = HorizontalAlignment.Left;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.Foreground = Brushes.Black;
            Color color = Defaults.Colors.Highlight;
            color.A = 128;
            label.Background = new SolidColorBrush(color);
            label.IsHitTestVisible = false;

            sp.Children.Add(label);           

            if (tier.IsGeometric)
            {
                string fp = "../../../Resources/visible.png";
                if (File.Exists(fp))
                {
                    Image img = new Image();
                    img.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(fp, UriKind.Relative));
                    img.HorizontalAlignment = HorizontalAlignment.Left;
                    img.VerticalAlignment = VerticalAlignment.Center;
                    double size = 32.0;
                    img.Width = size;
                    img.Height = size;

                    Button butt = new Button();
                    butt.Width = size;
                    butt.Height = size;
                    butt.Content = img;
                    butt.Background = Brushes.Transparent;
                    butt.BorderBrush = Brushes.Transparent;
                    butt.BorderThickness = new Thickness(0);
                    butt.Click += visibilityClick;
                    int seed = Environment.TickCount;
                    Random rnd = new Random(seed);
                    string name = "b" + rnd.Next(1, int.MaxValue).ToString();
                    butt.Name = name;
                    sp.Children.Add(butt);
                    
                }
            }

            Grid.SetColumn(sp, 0);
            Grid.SetRow(sp, grid.RowDefinitions.Count - 1);
            grid.Children.Add(sp);

            Border border = new Border();
            border.BorderThickness = new Thickness(Defaults.SelectionBorderWidth, 0, 0, 0);
            border.BorderBrush = Defaults.Brushes.Highlight;
            border.IsHitTestVisible = false;
            Grid.SetColumn(border, 0);
            Grid.SetRow(border, grid.RowDefinitions.Count - 1);
            grid.Children.Add(border);

            tier.Border = border;
        }

        public void Remove(AnnoTier tier)
        {
            int rowIndex = Grid.GetRow(tier.Border);
            int childIndex = 0;

            bool isLast = rowIndex == grid.RowDefinitions.Count - 1;

            // remove children:

            // splitter            
            childIndex = grid.Children.IndexOf(tier.Border);
            if (!isLast) grid.Children.RemoveAt(childIndex + 1);
            // track
            childIndex = grid.Children.IndexOf(tier.Border);
            grid.Children.RemoveAt(childIndex - 2);
            // label
            childIndex = grid.Children.IndexOf(tier.Border);
            grid.Children.RemoveAt(childIndex - 1);
            // border
            childIndex = grid.Children.IndexOf(tier.Border);
            grid.Children.RemoveAt(childIndex);

            // update row indices of remaining children:

            int row = 0;
            for (int i = 0; i < grid.Children.Count; i++)
            {
                if ((i + 1) % 4 == 0)
                {
                    row++;
                }
                Grid.SetRow(grid.Children[i], row);
                if ((i + 1) % 4 == 0)
                {
                    row++;
                }
            }

            // remove rows:

            grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);
            if (!isLast) grid.RowDefinitions.RemoveAt(grid.RowDefinitions.Count - 1);

            // resize 

            for (int i = 0; i < grid.RowDefinitions.Count; i += 2)
            {
                grid.RowDefinitions[i].Height = new GridLength(1, GridUnitType.Star);
            }
        }


        public void visibilityClick(object sender, RoutedEventArgs e)
        {
            Button butt = (Button)sender;

            Image img = (Image)butt.Content;

            string location = img.Source.ToString();
            string[] split = (location.Split(new char[] { '/' }));

            string fp = "../../../Resources/";

            bool visible = false;
            if (split[split.Length - 1] == "visible.png")
            {
                fp += "notvisible.png";
            }
            else if (split[split.Length - 1] == "notvisible.png")
            {
                visible = true;
                fp += "visible.png";
            }

            int annoTierIndex = 0;
            if (grid.Children.Count > 3)
            {
                bool stop = false;
                foreach (var child1 in grid.Children)
                {
                    StackPanel sp = null;
                    try
                    {
                        sp = (StackPanel)child1;
                    }
                    catch (Exception ex)
                    {
                        ++annoTierIndex;
                        continue;
                    }

                    if (sp != null)
                    {
                        foreach (var child2 in sp.Children)
                        {
                            Button butt2 = null;
                            try
                            {
                                butt2 = (Button)child2;
                            }
                            catch (Exception ex)
                            {

                                continue;
                            }

                            if (butt2 != null)
                            {
                                if (butt.Name == butt2.Name)
                                {
                                    stop = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (stop)
                    {
                        --annoTierIndex;
                        break;
                    }
                    ++annoTierIndex;
                }
            }
            AnnoTier at = (AnnoTier)grid.Children[annoTierIndex];

            at.AnnoList.Show = visible;

            img.Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(fp, UriKind.Relative));
            butt.Content = img;
            //call geometricOverlayUpdate(pos)
        }


    }
}
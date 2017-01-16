using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

//using Xceed.Wpf.Toolkit;

namespace ssi
{
    /// <summary>
    /// Interaction logic for SignalTrack.xaml
    /// </summary>
    ///

    public partial class SignalTrackEx : UserControl, INotifyPropertyChanged
    {
        private Color fg_color;
        private Color bg_color;
        bool isAudio = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color SignalColor
        {
            get
            {
                return this.fg_color;
            }

            set
            {
                this.fg_color = value;
                if (track != null)
                {
                    track.changeColor(new SolidColorBrush(fg_color));
                    track.SignalColor = new SolidColorBrush(fg_color);
                }
                this.RaisePropertyChanged("SignalColor");
            }
        }

        public Color BackColor
        {
            get
            {
                return this.bg_color;
            }

            set
            {
                this.bg_color = value;
                if (track != null)
                {
                    track.BackgroundColor = new SolidColorBrush(bg_color);
                    track.select(false);
                }
                this.RaisePropertyChanged("BackColor");
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

        static public event SignalTrackChangeEventHandler OnChange;

        private SignalTrack track = null;

        public SignalTrackEx()
        {
            this.BackColor = Color.FromArgb(255, 255, 255, 255);
            this.SignalColor = Color.FromArgb(255, 0, 0, 0);
            this.Brush = Brushes.Blue;
            this.DataContext = this;
        
            InitializeComponent();
        }

        public SignalTrack signaltrack
        {
            get { return track; }
            set { track = value; }
        }

        public void AddTrack(SignalTrack track)
        {
            Grid.SetColumn(track, 0);
            Grid.SetRow(track, 1);

            for (int i = 0; i < track.getSignal().dim; i++)
            {
                DimComboBox.Items.Add(i);
            }
            DimComboBox.SelectedItem = 0;

            this.grid.Children.Add(track);
            this.track = track;
            this.isAudio = track.getSignal().IsAudio;
            if (this.isAudio) StatsButton.Visibility = Visibility.Collapsed;
        }

        public void RemoveTrack(SignalTrack track)
        {
            this.grid.Children.Remove(track);
        }

        private void ZoomInButton_Click(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    track.zoom(3, 0);
                }
                else
                {
                    track.zoom(1, 0);
                }
            }
        }

        private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    track.zoom(-3, 0);
                }
                else
                {
                    track.zoom(-1, 0);
                }
            }
        }

        private void MoveUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    track.zoom(0, -6);
                }
                else
                {
                    track.zoom(0, -1);
                }
            }
        }

        private void MoveDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    track.zoom(0, 6);
                }
                else
                {
                    track.zoom(0, 1);
                }
            }
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                track.zoomReset();
            }
        }

        private void DimComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (track != null)
            {
                uint index = (uint)DimComboBox.SelectedIndex;
                track.getSignal().ShowDim = index;
                track.InvalidateVisual();

                if (OnChange != null)
                {
                    OnChange(track, null);
                }
            }
        }

        private void AutoScaleCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                track.AutoScaling = true;
            }
        }

        private void AutoScaleCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (track != null)
            {
                track.AutoScaling = false;
            }
        }

        private void StatsButton_Click(object sender, RoutedEventArgs e)
        {
            SignalStatsWindow ssw = new SignalStatsWindow(track.getSignal(), track.getSignal().ShowDim);
            MainHandler.Time.OnTimelineChanged += ssw.timeRangeChanged;
            ssw.Topmost = true;
            ssw.WindowStartupLocation = WindowStartupLocation.Manual;
            ssw.Show();

            if(ssw.DialogResult == false)
            {
                MainHandler.Time.OnTimelineChanged -= ssw.timeRangeChanged;
            }
            

        }
    }
}
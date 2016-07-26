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
        private Color color1;
        private Color color2;

        public event PropertyChangedEventHandler PropertyChanged;

        public Color SignalColor
        {
            get
            {
                return this.color1;
            }

            set
            {
                this.color1 = value;
                if (track != null)
                {
                    track.changeColour(new SolidColorBrush(color1));
                    track.SignalColor = new SolidColorBrush(color1);
                }
                this.RaisePropertyChanged("SignalColor");
            }
        }

        public Color BackColor
        {
            get
            {
                return this.color2;
            }

            set
            {
                this.color2 = value;
                if (track != null)
                {
                    track.BackgroundColor = new SolidColorBrush(color2);
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
                DimComboBox.Items.Add(i + 1);
            }
            DimComboBox.SelectedItem = 1;

            this.grid.Children.Add(track);
            this.track = track;
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
    }
}
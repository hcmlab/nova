using System.Windows.Controls;
using System.Windows.Media;

namespace ssi
{
    /// <summary>
    /// Interaction logic for NavigatorControl.xaml
    /// </summary>
    public partial class NavigatorControl : UserControl
    {
        public NavigatorControl()
        {
            InitializeComponent();




            newAnnoButton.Background = Defaults.Brushes.ButtonColor;
            newAnnoButton.Foreground = Defaults.Brushes.ButtonForeColor;
            if (!newAnnoButton.IsEnabled)
            {
                newAnnoButton.Foreground = Brushes.Black;
            }

            playButton.Background = Defaults.Brushes.ButtonColor;
            playButton.Foreground = Defaults.Brushes.ButtonForeColor;
            if(!playButton.IsEnabled)
            {
                playButton.Foreground = Brushes.Black;
            }

 
            jumpFrontButton.Background = Defaults.Brushes.ButtonColor;
            jumpFrontButton.Foreground = Defaults.Brushes.ButtonForeColor;
            if (!jumpFrontButton.IsEnabled)
            {
                jumpFrontButton.Foreground = Brushes.Black;
            }

            jumpEndButton.Background = Defaults.Brushes.ButtonColor;
            jumpEndButton.Foreground = Defaults.Brushes.ButtonForeColor;
            if (!jumpEndButton.IsEnabled)
            {
                jumpEndButton.Foreground = Brushes.Black;
            }


            clearButton.Background = Defaults.Brushes.ButtonColor;
            clearButton.Foreground = Defaults.Brushes.ButtonForeColor;
            if (!clearButton.IsEnabled)
            {
                clearButton.Foreground = Brushes.Black;
            }

        }
    }
}
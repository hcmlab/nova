using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace ssi.Controls


{
    /// <summary>
    /// Interaktionslogik für DescriptionWindow.xaml
    /// </summary>
    public partial class DescriptionWindow : Window
    {
        private AnnoScheme scheme;

        public DescriptionWindow(ref AnnoScheme scheme)
        {
            InitializeComponent();
            this.scheme = scheme;
            ReplyBox.Text = scheme.Description;
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {

            this.scheme.Description = ReplyBox.Text;
            this.Close();

        }

        private void ReplyBox_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Scrollviewer.ScrollToEnd();

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            //if (!AppGeneral.IsClosing)
            {
                this.Hide();
                e.Cancel = true;
            }
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }
    }
}

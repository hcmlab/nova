using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    public delegate void HandlerLoaded(MainHandler handler);

    public partial class MainControl : UserControl
    {
        private MainHandler handler = null;
        public event HandlerLoaded OnHandlerLoaded;

        public MainControl()
        {
            InitializeComponent();
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
        }

        private void ControlLoaded(object sender, RoutedEventArgs e)
        {
            if (this.ActualWidth > 0 && this.handler == null)
            {
                this.handler = new MainHandler(this);
                if (OnHandlerLoaded != null)
                {
                    OnHandlerLoaded(this.handler);
                }
            }
        }
    }
}
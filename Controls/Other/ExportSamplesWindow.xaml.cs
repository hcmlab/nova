using System.Windows;

namespace ssi
{
    /// <summary>
    /// Interaction logic for ExportSamplesWindow.xaml
    /// </summary>
    public partial class ExportSamplesWindow : Window
    {
        public ExportSamplesWindow()
        {
            InitializeComponent();

            control.closeEvent += control_closeEvent;
        }

        private void control_closeEvent()
        {
            Close();
        }
    }
}
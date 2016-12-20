using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            Certainty.Text = Properties.Settings.Default.UncertaintyLevel.ToString();
            Annotator.Text = Properties.Settings.Default.Annotator;
            DefaultZoom.Text = Properties.Settings.Default.DefaultZoominSeconds.ToString();
            Segmentmindur.Text = Properties.Settings.Default.DefaultMinSegmentSize.ToString();
            Samplerate.Text = Properties.Settings.Default.DefaultDiscreteSampleRate.ToString();
            DBServer.Text = Properties.Settings.Default.MongoDBIP;
            DBUser.Text = Properties.Settings.Default.MongoDBUser;
            DBPW.Password = Properties.Settings.Default.MongoDBPass;
            UpdatesCheckbox.IsChecked = Properties.Settings.Default.CheckUpdateonStartup;
            Overwritedbcheckbox.IsChecked = Properties.Settings.Default.DBAskbeforeoverwrite;
        }

        public double Uncertainty()
        {
            return double.Parse(Certainty.Text);
        }

        public string AnnotatorName()
        {
            return Annotator.Text;
        }

        public string ZoomInseconds()
        {
            return DefaultZoom.Text;
        }

        public string SegmentMinDur()
        {
            return Segmentmindur.Text;
        }

        public string MongoServer()
        {
            return DBServer.Text;
        }

        public string MongoUser()
        {
            return DBUser.Text;
        }

        public string MongoPass()
        {
            return DBPW.Password;
        }

        public string SampleRate()
        {
            return Samplerate.Text;
        }

        public bool CheckforUpdatesonStartup()
        {
            return (UpdatesCheckbox.IsChecked == true);
        }

        public bool DBaskforOverwrite()
        {
            return (Overwritedbcheckbox.IsChecked == true);
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[.][0-9]+$|^[0-9]*[.]{0,1}[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }

        private void IntNumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[0-9]+$|^[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
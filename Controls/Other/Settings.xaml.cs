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
            DefaultZoom.Text = Properties.Settings.Default.DefaultZoomInSeconds.ToString();
            Segmentmindur.Text = Properties.Settings.Default.DefaultMinSegmentSize.ToString();
            Samplerate.Text = Properties.Settings.Default.DefaultDiscreteSampleRate.ToString();
            string[] tokens = Properties.Settings.Default.DatabaseAddress.Split(':');
            if (tokens.Length == 2)
            {
                DBHost.Text = tokens[0];
                DBPort.Text = tokens[1];
            }            
            DBUser.Text = Properties.Settings.Default.MongoDBUser;
            DBPassword.Password = Properties.Settings.Default.MongoDBPass;
            DBConnnect.IsChecked = Properties.Settings.Default.DatabaseAutoLogin;
            UpdatesCheckbox.IsChecked = Properties.Settings.Default.CheckUpdateOnStart;
            OverwriteAnnotation.IsChecked = Properties.Settings.Default.DatabaseAskBeforeOverwrite;
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

        public string DatabaseAddress()
        {
            return DBHost.Text + ":" + DBPort.Text;
        }

        public string MongoUser()
        {
            return DBUser.Text;
        }

        public string MongoPass()
        {
            return DBPassword.Password;
        }

        public string SampleRate()
        {
            return Samplerate.Text;
        }

        public bool CheckforUpdatesonStartup()
        {
            return (UpdatesCheckbox.IsChecked == true);
        }

        public bool DBAutoConnect()
        {
            return (DBConnnect.IsChecked == true);
        }

        public bool DBAskforOverwrite()
        {
            return (OverwriteAnnotation.IsChecked == true);
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
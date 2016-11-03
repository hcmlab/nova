using System;
using System.Collections.Generic;
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
using System.Text.RegularExpressions;

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
            DBServer.Text = Properties.Settings.Default.MongoDBIP;
            DBUser.Text = Properties.Settings.Default.MongoDBUser;
            DBPW.Password = Properties.Settings.Default.MongoDBPass;
        }


        public double Uncertainty()
        {
            return double.Parse(Certainty.Text);
        }

        public string AnnotatorName()
        {
            return Annotator.Text;
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

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("^[.][0-9]+$|^[0-9]*[.]{0,1}[0-9]*$");
            e.Handled = !regex.IsMatch((sender as TextBox).Text.Insert((sender as TextBox).SelectionStart, e.Text));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }


 
}

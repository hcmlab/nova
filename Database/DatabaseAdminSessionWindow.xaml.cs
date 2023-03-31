using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Windows;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DataBaseSessionWindow.xaml
    /// </summary>
    public partial class DatabaseAdminSessionWindow : Window
    {
        private DatabaseSession session;

        public DatabaseAdminSessionWindow(ref DatabaseSession session, bool showname = true)
        {
            InitializeComponent();

            this.session = session;

            if(showname == false)
            {
                NameField.Visibility = Visibility.Collapsed;
                NameLabel.Visibility = Visibility.Collapsed;
            }

            NameField.Text = session.Name;
            LanguageField.Text = session.Language;
            LocationField.Text = session.Location;
            DurationField.Text = session.Duration.ToString();
            DatePicker.SelectedDate = session.Date.Year == 1 ? DateTime.Today : session.Date;
        }       

        public DateTime SessionDate()
        {
            if (DatePicker.SelectedDate != null)
                return DatePicker.SelectedDate.Value;
            else return new DateTime();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            session.Name = NameField.Text == "" ? Defaults.Strings.Unknown : NameField.Text;
            session.Language = LanguageField.Text;
            session.Location = LocationField.Text;
            session.Date = DatePicker.SelectedDate.Value;
            double duration = 0;
            double.TryParse(DurationField.Text, out duration);
            session.Duration = duration;

            DialogResult = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
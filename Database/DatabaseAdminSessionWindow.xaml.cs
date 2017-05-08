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

        public DatabaseAdminSessionWindow(ref DatabaseSession session)
        {
            InitializeComponent();

            this.session = session;

            NameField.Text = session.Name;
            LanguageField.Text = session.Language;
            LocationField.Text = session.Location;
            DatePicker.SelectedDate = session.Date;
        }       

        public DateTime SessionDate()
        {
            if (DatePicker.SelectedDate != null)
                return DatePicker.SelectedDate.Value;
            else return new DateTime();
        }

        private void OkClick(object sender, RoutedEventArgs e)
        {
            session.Name = NameField.Text == "" ? Defaults.Strings.Unkown : NameField.Text;
            session.Language = LanguageField.Text;
            session.Location = LocationField.Text;
            session.Date = DatePicker.SelectedDate != null ? DatePicker.SelectedDate.Value : new DateTime();

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
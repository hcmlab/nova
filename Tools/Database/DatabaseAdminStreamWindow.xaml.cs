using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseMediaWindow.xaml
    /// </summary>
    public partial class DatabaseAdminStreamWindow : Window
    {
        private DatabaseStream stream;

        public DatabaseAdminStreamWindow(ref DatabaseStream stream)
        {
            InitializeComponent();

            this.stream = stream;

            GetRoles();
            GetSubjects();
            GetStreamTypes();

            NameField.Text = stream.Name;
            CustomUrlField.Text = stream.URL;
            AuthentificationCheckBox.IsChecked = stream.ServerAuth;

            RolesResultBox.SelectedItem = stream.Role;
            SubjectsResultBox.SelectedItem = stream.Subject;
            StreamTypesResultsBox.SelectedItem = stream.StreamType;
        }
        
        private void OkClick(object sender, RoutedEventArgs e)
        {
            stream.Name = NameField.Text == "" ? Defaults.Strings.Unkown : NameField.Text;
            stream.Role = RolesResultBox.SelectedItem != null ? RolesResultBox.SelectedItem.ToString() : "";
            stream.StreamType = StreamTypesResultsBox.SelectedItem != null ? StreamTypesResultsBox.SelectedItem.ToString() : "";
            stream.Subject = SubjectsResultBox.SelectedItem != null ? SubjectsResultBox.SelectedItem.ToString() : "";
            stream.URL = CustomUrlField.Text;
            stream.ServerAuth = AuthentificationCheckBox.IsChecked.Value;

            DialogResult = true;
            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void GetRoles(string selecteditem = null)
        {
            RolesResultBox.Items.Clear();
            List<string> roles = DatabaseHandler.GetRoles();
            roles.Add("");
            foreach (string role in roles)
            {
                RolesResultBox.Items.Add(role);
            }
            RolesResultBox.SelectedItem = selecteditem;
        }

        public void GetSubjects(string selecteditem = null)

        {
            SubjectsResultBox.Items.Clear();
            List<string> subjects = DatabaseHandler.GetSubjects();
            subjects.Add("");      
            foreach (string subject in subjects)
            {
                SubjectsResultBox.Items.Add(subject);
            }
            SubjectsResultBox.SelectedItem = selecteditem;
        }

        public void GetStreamTypes(string selecteditem = null)
        {
            StreamTypesResultsBox.Items.Clear();
            List<string> streamTypes = DatabaseHandler.GetStreamTypes();
            streamTypes.Add("");         
            foreach (string streamType in streamTypes)
            {
                StreamTypesResultsBox.Items.Add(streamType);
            }
            StreamTypesResultsBox.SelectedItem = selecteditem;
        }

        


    }
}
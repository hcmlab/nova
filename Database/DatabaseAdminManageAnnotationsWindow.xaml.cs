using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ssi
{
    /// <summary>
    /// Interaktionslogik für DatabaseAdminWindow.xaml
    /// </summary>
    public partial class DatabaseAdminManageAnnotationsWindow : Window
    {
        public DatabaseAdminManageAnnotationsWindow()
        {
            InitializeComponent();

            GetDatabases(DatabaseHandler.DatabaseName);
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Select(ListBox list, string select)
        {
            if (select != null)
            {
                foreach (string item in list.Items)
                {
                    if (item == select)
                    {
                        list.SelectedItem = item;
                    }
                }
            }
        }

        public void GetDatabases(string selectedItem = null)
        {
            DatabaseBox.Items.Clear();

            List<string> databases = DatabaseHandler.GetDatabases();

            foreach (string db in databases)
            {
                DatabaseBox.Items.Add(db);
            }

            Select(DatabaseBox, selectedItem);
        }

        private void DataBaseResultsBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatabaseBox.SelectedItem != null)
            {
                string name = DatabaseBox.SelectedItem.ToString();
                DatabaseHandler.ChangeDatabase(name);
                GetAnnotations();                
            }
        }

        public void addAnnoToList(ref List<DatabaseAnnotation> annotations, BsonDocument annotation)
        {
            ObjectId id = annotation["_id"].AsObjectId;

            string sessionName = "";
            DatabaseHandler.GetObjectName(ref sessionName, DatabaseDefinitionCollections.Sessions, annotation["session_id"].AsObjectId);
            string roleName = "";
            DatabaseHandler.GetObjectName(ref roleName, DatabaseDefinitionCollections.Roles, annotation["role_id"].AsObjectId);
            string schemeName = "";
            DatabaseHandler.GetObjectName(ref schemeName, DatabaseDefinitionCollections.Schemes, annotation["scheme_id"].AsObjectId);
            string annotatorName = "";
            DatabaseHandler.GetObjectName(ref annotatorName, DatabaseDefinitionCollections.Annotators, annotation["annotator_id"].AsObjectId);
            string annotatorFullName = "";
            DatabaseHandler.GetObjectField(ref annotatorFullName, DatabaseDefinitionCollections.Annotators, annotation["annotator_id"].AsObjectId, "fullname");

            bool isFinished = false;
            try
            {
                isFinished = annotation["isFinished"].AsBoolean;

            }
            catch (Exception ex) { }

            bool islocked = false;
            try
            {
                islocked = annotation["isLocked"].AsBoolean;

            }
            catch (Exception ex) { }

            DateTime date = DateTime.Today;
            try
            {
                date = annotation["date"].ToUniversalTime();
            }
            catch (Exception ex) { }

            annotations.Add(new DatabaseAnnotation() { Id = id, Role = roleName, Scheme = schemeName, Annotator = annotatorName, AnnotatorFullName = annotatorFullName, Session = sessionName, IsFinished = isFinished, IsLocked = islocked, Date = date });
        }

        public void GetAnnotations(string selectedItem = null)
        {
            List<BsonDocument> annotations = DatabaseHandler.GetCollection(DatabaseDefinitionCollections.Annotations, false);            

            if (AnnotationsBox.HasItems)
            {
                AnnotationsBox.ItemsSource = null;
            }
           
            List<DatabaseAnnotation> items = new List<DatabaseAnnotation>();
            foreach (var annotation in annotations)
            {
                addAnnoToList(ref items, annotation);
            }
            AnnotationsBox.ItemsSource = items;           
        }                

        private void DeleteAnnotations_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationsBox.SelectedItem != null)
            {
                var annotations = AnnotationsBox.SelectedItems;
                foreach (DatabaseAnnotation annotation in annotations)
                {
                    DatabaseHandler.DeleteAnnotation(annotation.Id);
                }
                GetAnnotations();
            }            
        }

        private void ChangeFinishedState(ObjectId id, bool state)
        {
            var annos = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isFinished", state);
            annos.UpdateOne(filter, update);
        }

        private void ChangeLockedState(ObjectId id, bool state)
        {
            var annos = DatabaseHandler.Database.GetCollection<BsonDocument>(DatabaseDefinitionCollections.Annotations);
            var builder = Builders<BsonDocument>.Filter;

            var filter = builder.Eq("_id", id);
            var update = Builders<BsonDocument>.Update.Set("isLocked", state);
            annos.UpdateOne(filter, update);
        }


        private void IsFinishedCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, true);
        }

        private void IsFinishedCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeFinishedState(anno.Id, false);
        }

        private void IsLockedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeLockedState(anno.Id, false);
        }

        private void IsLockedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            DatabaseAnnotation anno = (DatabaseAnnotation)((CheckBox)sender).DataContext;
            ChangeLockedState(anno.Id, true);
        }
    }
}
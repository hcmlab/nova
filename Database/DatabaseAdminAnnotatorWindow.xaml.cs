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
using MongoDB.Bson;
using MongoDB.Driver;

namespace ssi
{
    /// <summary>
    /// Interaction logic for DatabaseAnnotatorWindow.xaml
    /// </summary>
    /// 

    public partial class DatabaseAdminAnnotatorWindow : Window
    {
        DatabaseAnnotator annotator;

        public DatabaseAdminAnnotatorWindow(ref DatabaseAnnotator annotator, List<string> names = null)
        {
            InitializeComponent();

            this.annotator = annotator;

            if (names == null)
            {
                NameBox.Items.Add(annotator.Name);
                NameBox.SelectedIndex = 0;
                NameBox.IsEnabled = false;
               
                foreach (var item in RoleBox.Items )
                {
                    var comboBoxItem = item as ComboBoxItem;
                    if (comboBoxItem.Content.Equals(annotator.Role))
                    {
                        RoleBox.SelectedItem = item;
                    }
                }
            }
            else
            {
                NameBox.ItemsSource = names;
            }
        } 

        private void OkClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            annotator.Name = (string)NameBox.SelectedItem;
            annotator.Role = RoleBox.SelectionBoxItem.ToString();
           

            Close();
        }

        private void CancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

    }
}


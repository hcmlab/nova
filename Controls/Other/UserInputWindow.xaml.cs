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

namespace ssi
{
    /// <summary>
    /// Interaction logic for UserInputWindow.xaml
    /// </summary>
    public partial class UserInputWindow : Window
    {        
        Dictionary<string, TextBox> result;

        public class Input
        {
            public Input()
            {
                Label = "";
                DefaultValue = "";
            }
            public string Label { get; set; }
            public string DefaultValue { get; set; }
        }

        public UserInputWindow(string info, IDictionary<string,Input> dictionary)
        {
            InitializeComponent();

            result = new Dictionary<string, TextBox>();

            infoTextBlock.Text = info;

            TextBox firstTextBox = null;
            foreach (KeyValuePair<string,Input> item in dictionary)
            {
                Label label = new Label() { Content = item.Value.Label };
                inputGrid.Children.Add(label);

                TextBox textBox = new TextBox() { Text = item.Value.DefaultValue };
                textBox.GotFocus += TextBox_GotFocus;
                if (firstTextBox == null)
                {
                    firstTextBox = textBox;                    
                }
                Thickness margin = textBox.Margin; margin.Top = 5; margin.Right = 5; margin.Bottom = 5; textBox.Margin = margin;                
                result.Add(item.Key, textBox);
                inputGrid.Children.Add(textBox);
                
                RowDefinition rowDefinition = new RowDefinition();
                rowDefinition.Height = new GridLength(1, GridUnitType.Auto);
                inputGrid.RowDefinitions.Add(rowDefinition);

                Grid.SetColumn(label, 0);
                Grid.SetRow(label, inputGrid.RowDefinitions.Count - 1);
                Grid.SetColumn(textBox, 1);
                Grid.SetRow(textBox, inputGrid.RowDefinitions.Count - 1);
            }

            if (firstTextBox != null)
            {
                firstTextBox.Focus();
            }
        }

        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void okButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public string Result(string name)
        {
            return result[name].Text;
        }

        public bool ResultAsInt(string name, out int value)
        {
            return int.TryParse(result[name].Text, out value);            
        }

        public bool ResultAsDouble(string name, out double value)
        {
            return double.TryParse(result[name].Text, out value);            
        }
    }
}

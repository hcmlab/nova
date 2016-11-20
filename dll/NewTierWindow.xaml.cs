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
    /// Interaktionslogik für NewTierWindow.xaml
    /// </summary>
    public partial class NewTierWindow : Window
    {
        AnnoType at = AnnoType.FREE;
        public NewTierWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            at = AnnoType.FREE;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            at = AnnoType.DISCRETE;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            at = AnnoType.CONTINUOUS;
        }


        public AnnoType Result()
        {
            return at;
        }

    }
}

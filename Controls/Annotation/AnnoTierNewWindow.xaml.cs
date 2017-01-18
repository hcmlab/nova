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
    public partial class AnnoTierNewWindow : Window
    {
        AnnoScheme.TYPE annotype = AnnoScheme.TYPE.FREE;
        public AnnoTierNewWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            annotype = AnnoScheme.TYPE.FREE;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            annotype = AnnoScheme.TYPE.DISCRETE;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            annotype = AnnoScheme.TYPE.CONTINUOUS;
        }


        public AnnoScheme.TYPE Result()
        {
            return annotype;
        }

    }
}

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
    /// Interaction logic for AnnoTierNewContinuousSchemeWindow.xaml
    /// </summary>
    public partial class QuestionWindow : Window
    {
        int result;
        public int Result { get { return result; } }

        public class Input
        {
            public String Question { get;  set; }
            public String YesButton { get; set; }
            public String NoButton { get; set; }
            public String CancelButton { get; set; }
        }

        public QuestionWindow(Input defaultInput)
        {
            InitializeComponent();
            // 1 yes, 0 no, 2 cancel
            result = 2;

            question_text.Text = defaultInput.Question;
            button_yes_text.Text = defaultInput.YesButton;
            button_cancel_text.Text = defaultInput.CancelButton;
            if (defaultInput.NoButton == "")
            {
                button_no.Visibility = Visibility.Hidden;
                NoColumn.Width = new GridLength(0, GridUnitType.Star);
            }
            else
            {
                button_no_text.Text = defaultInput.NoButton;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button b = sender as Button;

            switch (b.Name.ToString())
            {
                case "button_yes":
                    result = 1;
                    break;
                case "button_no":
                    result = 0;
                    break;
                case "button_cancel":
                    result = 2;
                    break;
            }
            DialogResult = true;
            Close();
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace NOVA_Light
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string currentsource = "";
        public string sourcepath = "";
        public string targetpath = "";
        public string userid = "";
        public string random = "";
        public string move = "";
        int index = 0;


        public List<string> classes = new List<string>();
        List<Button> Buttons = new List<Button>();
        public MainWindow()
        {
            InitializeComponent();
            Info.Content = " ";
            try
            {
                loadConfig();
            }
            catch
            {
                MessageBox.Show("Couln't load config.");
            }

            if (random == "true" || random == "True") currentsource = getRandomImage();
            else currentsource = getNextImage();


            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(currentsource);
            bitmap.EndInit();
            testimage.Source = bitmap;


        }

        public void loadConfig()
        {

            XmlDocument doc = new XmlDocument();
            XmlTextReader reader = new XmlTextReader("config.xml");
            reader.WhitespaceHandling = WhitespaceHandling.None;
            reader.MoveToContent();
            reader.Read();

            doc.Load(reader);

            sourcepath = System.IO.Path.GetFullPath(doc.FirstChild.ChildNodes[0].InnerText.ToString());
            targetpath = System.IO.Path.GetFullPath(doc.FirstChild.ChildNodes[1].InnerText.ToString());
            userid = doc.FirstChild.ChildNodes[2].InnerText.ToString();
            string classesstring = doc.FirstChild.ChildNodes[3].InnerText.ToString();
            string[] classessplit = classesstring.Split(';');

            Buttons.Clear();

            foreach (string s in classessplit)
            {
                classes.Add(s);
                Button b = new Button();
                b.Content = s;
                b.Click += Button_Clicked;
                b.MinHeight = 30;
                b.Margin = new Thickness(10);
                Buttons.Add(b);
                Stackpanel.Children.Add(b);
            }

            if (!Directory.Exists(targetpath + "\\" + userid))
            {
                Directory.CreateDirectory(targetpath + "\\" + userid);
            }

            random = doc.FirstChild.ChildNodes[4].InnerText.ToString();
            move = doc.FirstChild.ChildNodes[5].InnerText.ToString();

        }

        public string getRandomImage()
        {
            var rand = new Random();
            var files = Directory.GetFiles(sourcepath, "*.jpg");
            return files[rand.Next(files.Length)];
        }

        public string getNextImage()
        {
            var files = Directory.GetFiles(sourcepath, "*.jpg");
            index = index + 1;
            return files[index - 1];
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            string button = (sender as Button).Content.ToString();
            Button_Clicked_funct(button);
        }


        private void Button_Clicked_funct(string button)
        {
            if (!Directory.Exists(targetpath + "\\" + userid + "\\" + button + "\\"))
            {
                Directory.CreateDirectory(targetpath + "\\" + userid + "\\" + button + "\\");
            }

            if(move == "True" || move == "true")
            {
                System.IO.File.Move(currentsource, targetpath + "\\" + userid + "\\" + button + "\\" + System.IO.Path.GetFileName(currentsource));
            }
            else
            {
                System.IO.File.Copy(currentsource, targetpath + "\\" + userid + "\\" + button + "\\" + System.IO.Path.GetFileName(currentsource), true);
            }

            if (random == "true" || random == "True") currentsource = getRandomImage();
            else currentsource = getNextImage();
           
            BitmapImage bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(currentsource);
            bitmap.EndInit();
            testimage.Source = bitmap;
        }


        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key >= Key.D0 && e.Key <= Key.D9) || (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            {
               if(e.Key - Key.D1 > -1 && e.Key - Key.D1 < Buttons.Count && Buttons.ElementAt(e.Key - Key.D1) != null)
                {
                    string button = Buttons.ElementAt(e.Key - Key.D1).Content.ToString();
                    Button_Clicked_funct(button);
                    string originalText = Info.Content.ToString();

                    var backgroundWorker = new BackgroundWorker();
                    backgroundWorker.DoWork += (s, ea) => Thread.Sleep(TimeSpan.FromMilliseconds(200));
                    backgroundWorker.RunWorkerCompleted += (s, ea) =>
                    {
                        Info.Content =  originalText;
                      
                    };

                    Info.Content = "Last Selection was: " + button;
                    backgroundWorker.RunWorkerAsync();
                }



            }
        }



        
    }
}
